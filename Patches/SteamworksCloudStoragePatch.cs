using System.Text;
using System.Runtime.CompilerServices;
using CoffinTech.Logger;
using CoffinTech.SaveData;
using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppVampireSurvivors.Data;
using Il2CppVampireSurvivors.Framework.Platforms.Saves;
using Il2CppVampireSurvivors.Framework.Platforms.Standalone;
using Il2CppVampireSurvivors.Framework.Platforms.SteamworksIntegration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace CoffinTech.Patches;

//Overtake Steam Storage Solution in favor of a Standalone-esque one
[HarmonyPatch(typeof(SteamworksCloudStorage))]
public static class SteamworksCloudStoragePatch
{
    // Constants
    private const string ModdedBlobName = "Modded.json";
    private const string TempFileExtension = ".tmp";
    private const string BackupFileExtension = ".bak.json";
    private const int MaxBlobNameLength = 256;
    
    // Instance fields
    private static string _storagePath = "";
    private static readonly Dictionary<string, StandaloneStorage.Blob> Blobs = new();
    private static bool _isReady = false;
    
    // Mod data access
    internal static JObject ObjectToRead { get; private set; } = new();

    #region Harmony Patches

    [HarmonyPatch(nameof(SteamworksCloudStorage.InitAsync))]
    [HarmonyPrefix]
    public static bool InitAsync(SteamworksCloudStorage __instance, ref string containerName,
        ref string containerDisplayName, ref StorageOperationComplete onComplete)
    {
        try
        {
            DebugLogger.Msg("StandaloneStorage InitAsync: Starting initialization");
            
            // Initialize storage path with validation
            _storagePath = ValidateAndConstructStoragePath();
            
            // Initialize blobs dictionary
            Blobs.Clear();
            
            // Mark as ready
            _isReady = true;
            __instance.m_IsReady = true;
            
            DebugLogger.Msg($"StandaloneStorage InitAsync: Successfully initialized at {_storagePath}");
            onComplete.Invoke(StorageResult.Successful);
        }
        catch (Exception ex)
        {
            DebugLogger.Msg($"StandaloneStorage Error: InitAsync - {ex.Message}");
            onComplete.Invoke(StorageResult.Failed);
        }
        
        return false;
    }

    [HarmonyPatch(nameof(SteamworksCloudStorage.GetBlobsAsync))]
    [HarmonyPrefix]
    public static bool GetBlobsAsync(ref string blobName, ref StorageOperationCompleteWithData onComplete,
        ref bool skipCache)
    {
        try
        {
            DebugLogger.Msg($"StandaloneStorage GetBlobsAsync: Requesting blob '{blobName}'");
            
            // Force modded blob name
            blobName = ModdedBlobName;
            
            // Validate operation
            StorageResult validateResult = ValidateOperation(blobName, "GetBlobsAsync");
            if (validateResult != StorageResult.Successful)
            {
                onComplete.Invoke(validateResult, null);
                return false;
            }
            
            // Check cache first
            if (!skipCache && Blobs.TryGetValue(blobName, out var cachedBlob))
            {
                DebugLogger.Msg($"StandaloneStorage GetBlobsAsync: Found blob '{blobName}' in cache");
                
                if (cachedBlob.IsEmpty)
                {
                    onComplete.Invoke(StorageResult.NotFound, null);
                }
                else
                {
                    onComplete.Invoke(StorageResult.Successful, cachedBlob.Data.ToArray());
                }
                return false;
            }
            
            // Load from disk
            StandaloneStorage.Blob? blob = LoadBlobFromDisk(blobName);
            if (blob != null)
            {
                Blobs[blobName] = blob;
                DebugLogger.Msg($"StandaloneStorage GetBlobsAsync: Successfully loaded blob '{blobName}' from disk");
                onComplete.Invoke(StorageResult.Successful, blob.Data.ToArray());
            }
            else
            {
                DebugLogger.Msg($"StandaloneStorage GetBlobsAsync: Blob '{blobName}' not found on disk");
                ObjectToRead = new JObject();
                onComplete.Invoke(StorageResult.NotFound, null);
            }
        }
        catch (Exception ex)
        {
            DebugLogger.Msg($"StandaloneStorage Error: GetBlobsAsync - {ex.Message}");
            onComplete.Invoke(StorageResult.Failed, null);
        }
        
        return false;
    }

    [HarmonyPatch(nameof(SteamworksCloudStorage.SetBlob))]
    [HarmonyPrefix]
    public static bool SetBlob(SteamworksCloudStorage __instance, ref string blobName,
        ref Il2CppStructArray<byte> data, ref StorageResult __result)
    {
        try
        {
            int dataLength = data?.Length ?? 0;
            DebugLogger.Msg($"StandaloneStorage SetBlob: Setting blob '{blobName}' with {dataLength} bytes");
            
            // Force modded blob name
            blobName = ModdedBlobName;
            
            // Validate operation
            StorageResult validateResult = ValidateOperation(blobName, "SetBlob");
            if (validateResult != StorageResult.Successful)
            {
                __result = validateResult;
                return false;
            }
            
            // Process and inject mod data
            byte[] processedData = ProcessDataForModInjection(data);
            
            // Set blob in memory
            SetBlobInternal(blobName, processedData, makeDirty: true);
            
            DebugLogger.Msg($"StandaloneStorage SetBlob: Successfully set blob '{blobName}'");
            __result = StorageResult.Successful;
        }
        catch (Exception ex)
        {
            DebugLogger.Msg($"StandaloneStorage Error: SetBlob - {ex.Message}");
            __result = StorageResult.Failed;
        }
        
        return false;
    }

    [HarmonyPatch(nameof(SteamworksCloudStorage.CommitAsync))]
    [HarmonyPrefix]
    public static bool CommitAsync(ref StorageOperationComplete onComplete, ref CommitOptions options,
        ref bool createBackup)
    {
        try
        {
            DebugLogger.Msg($"StandaloneStorage CommitAsync: Committing with backup={createBackup}");
            
            // Validate storage is ready
            if (!_isReady)
            {
                DebugLogger.Msg("StandaloneStorage CommitAsync: Storage not initialized");
                onComplete.Invoke(StorageResult.StorageNotInitialized);
                return false;
            }
            
            int committedCount = 0;
            int failedCount = 0;
            bool hasDirty = false;
            
            foreach (var kvp in Blobs)
            {
                string blobName = kvp.Key;
                StandaloneStorage.Blob blob = kvp.Value;
                
                if (!blob.IsDirty)
                {
                    DebugLogger.Msg($"StandaloneStorage CommitAsync: Skipping clean blob '{blobName}'");
                    continue;
                }
                
                hasDirty = true;
                
                try
                {
                    CommitBlobInternal(blobName, blob, createBackup);
                    blob.ClearDirty();
                    committedCount++;
                    DebugLogger.Msg($"StandaloneStorage CommitAsync: Successfully committed blob '{blobName}'");
                }
                catch (Exception ex)
                {
                    failedCount++;
                    DebugLogger.Msg($"StandaloneStorage CommitAsync: Failed to commit blob '{blobName}': {ex.Message}");
                }
            }
            
            if (!hasDirty)
            {
                DebugLogger.Msg("StandaloneStorage CommitAsync: Nothing to commit");
                onComplete.Invoke(StorageResult.NothingToCommit);
                return false;
            }
            
            if (failedCount > 0)
            {
                DebugLogger.Msg($"StandaloneStorage CommitAsync: Completed with {committedCount} successful, {failedCount} failed");
                onComplete.Invoke(StorageResult.Failed);
            }
            else
            {
                DebugLogger.Msg($"StandaloneStorage CommitAsync: All {committedCount} blobs committed successfully");
                onComplete.Invoke(StorageResult.Successful);
            }
        }
        catch (Exception ex)
        {
            DebugLogger.Msg($"StandaloneStorage Error: CommitAsync - {ex.Message}");
        
            onComplete.Invoke(StorageResult.Failed);
        }
        
        return false;
    }

    [HarmonyPatch(nameof(SteamworksCloudStorage.EraseAllAsync))]
    [HarmonyPrefix]
    public static bool EraseAllAsync(ref StorageOperationComplete onComplete)
    {
        try
        {
            DebugLogger.Msg("StandaloneStorage EraseAllAsync: Erasing all storage files");
            
            // Validate storage is ready
            if (!_isReady)
            {
                throw new InvalidOperationException("Storage not initialized");
            }
            
            // Validate storage path exists
            if (!Directory.Exists(_storagePath))
            {
                DebugLogger.Msg("StandaloneStorage EraseAllAsync: Storage path does not exist, nothing to erase");
                onComplete.Invoke(StorageResult.Successful);
                return false;
            }
            
            int erasedCount = 0;
            string blobPath = GetBlobFilePath(ModdedBlobName);
            string backupPath = Path.ChangeExtension(blobPath, BackupFileExtension);
            string tempPath = blobPath + TempFileExtension;
            string[] pathsToDelete = { blobPath, backupPath, tempPath };
            
            foreach (string filePath in pathsToDelete)
            {
                try
                {
                    if (File.Exists(filePath))
                    {
                        File.Delete(filePath);
                        erasedCount++;
                    }
                }
                catch (Exception ex)
                {
                    DebugLogger.Msg($"StandaloneStorage EraseAllAsync: Failed to delete file '{filePath}': {ex.Message}");
                    // Continue with other files
                }
            }
            
            // Clear in-memory blobs
            Blobs.Clear();
            ObjectToRead = new JObject();
            
            DebugLogger.Msg($"StandaloneStorage EraseAllAsync: Erased {erasedCount} files successfully");
            onComplete.Invoke(StorageResult.Successful);
        }
        catch (Exception ex)
        {
            DebugLogger.Msg($"StandaloneStorage Error: InitAsync - {ex.Message}");
            onComplete.Invoke(StorageResult.Failed);
        }
        
        return false;
    }

    #endregion

    #region Core Storage Operations

    private static StandaloneStorage.Blob LoadBlobFromDisk(string blobName)
    {
        string filePath = GetBlobFilePath(blobName);
        
        if (!File.Exists(filePath))
        {
            return null;
        }
        
        try
        {
            byte[] fileData = File.ReadAllBytes(filePath);
            byte[] processedData = ProcessDataForModExtraction(fileData);
            return new StandaloneStorage.Blob(processedData, dirtyFlag: false);
        }
        catch (Exception ex)
        {
            throw new IOException($"Failed to load blob '{blobName}' from disk: {ex.Message}", ex);
        }
    }

    private static void CommitBlobInternal(string blobName, StandaloneStorage.Blob blob, bool createBackup)
    {
        string filePath = GetBlobFilePath(blobName);
        string backupPath = Path.ChangeExtension(filePath, BackupFileExtension);
        bool backupCreated = false;
        
        try
        {
            // Create backup if requested and file exists
            if (createBackup && File.Exists(filePath))
            {
                WriteFileAtomically(backupPath, File.ReadAllBytes(filePath));
                backupCreated = true;
            }
            
            // Write blob data atomically
            if (!blob.IsEmpty)
            {
                WriteFileAtomically(filePath, blob.Data);
            }
            else if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
        catch (Exception)
        {
            // Restore backup if we created one and operation failed
            if (backupCreated && File.Exists(backupPath))
            {
                try
                {
                    File.Copy(backupPath, filePath, true);
                }
                catch (Exception restoreEx)
                {
                    DebugLogger.Msg($"StandaloneStorage: Failed to restore backup for blob '{blobName}': {restoreEx.Message}");
                }
            }
            
            throw;
        }
    }

    #endregion

    #region Data Processing

    private static byte[] ProcessDataForModInjection(Il2CppStructArray<byte> data)
    {
        // Convert IL2CPP array to managed array
        byte[] managedArray = ConvertIl2CppArrayToManaged(data);
        
        try
        {
            JObject obj;
            if (managedArray.Length == 0)
            {
                obj = new JObject();
            }
            else
            {
                string json = Encoding.UTF8.GetString(managedArray);
                obj = JsonConvert.DeserializeObject(json) as JObject ?? new JObject();
            }
            
            // Inject mod data
            obj["ModData"] = ModOptionsData.ObjectToWrite;
            
            string stringedObj = obj.ToString();
            return Encoding.UTF8.GetBytes(stringedObj);
        }
        catch (Exception ex)
        {
            DebugLogger.Msg($"StandaloneStorage: Failed to process data for mod injection: {ex.Message}");
            JObject fallback = new JObject
            {
                ["ModData"] = ModOptionsData.ObjectToWrite
            };
            return Encoding.UTF8.GetBytes(fallback.ToString());
        }
    }

    private static byte[] ProcessDataForModExtraction(byte[] data)
    {
        try
        {
            if (data == null || data.Length == 0)
            {
                ObjectToRead = new JObject();
                return data ?? Array.Empty<byte>();
            }
            
            string json = Encoding.UTF8.GetString(data);
            JObject obj = JObject.Parse(json);
            
            // Extract mod data for reading
            ObjectToRead = obj["ModData"] as JObject ?? new JObject();
            
            // Remove mod data from main object
            obj.Remove("ModData");
            
            string serialized = JsonConvert.SerializeObject(obj);
            return Encoding.UTF8.GetBytes(serialized);
        }
        catch (Exception ex)
        {
            DebugLogger.Msg($"StandaloneStorage: Failed to process data for mod extraction: {ex.Message}");
            ObjectToRead = new JObject();
            return data ?? Array.Empty<byte>();
        }
    }

    #endregion

    #region Blob Management

    private static void SetBlobInternal(string blobName, byte[] data, bool makeDirty = true)
    {
        if (!Blobs.TryGetValue(blobName, out var blob))
        {
            Blobs[blobName] = new StandaloneStorage.Blob(data, dirtyFlag: makeDirty);
        }
        else
        {
            blob.SetData(data);
        }
    }

    #endregion

    #region File Operations

    private static string ValidateAndConstructStoragePath()
    {
        string basePath = Directory.GetCurrentDirectory();
        basePath = Path.GetFullPath(basePath);
        string storagePath = Path.Combine(basePath, "UserData", "Saves");
        
        // Normalize and validate path
        storagePath = Path.GetFullPath(storagePath);
        
        if (!storagePath.StartsWith(basePath, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Invalid storage path: potential path traversal detected");
        }
        
        Directory.CreateDirectory(storagePath);
        return storagePath;
    }

    private static string GetBlobFilePath(string blobName)
    {
        ValidateBlobName(blobName);
        return Path.Combine(_storagePath, blobName);
    }

    private static void WriteFileAtomically(string filePath, byte[] data)
    {
        string tempPath = filePath + TempFileExtension;
        
        try
        {
            // Write to temp file first
            File.WriteAllBytes(tempPath, data);
            
            // Verify file was written correctly
            var fileInfo = new FileInfo(tempPath);
            if (fileInfo.Length != data.Length)
            {
                throw new IOException($"File write incomplete: expected {data.Length}, wrote {fileInfo.Length}");
            }
            
            // Atomic move to final location
            File.Move(tempPath, filePath, true);
            
            DebugLogger.Msg($"StandaloneStorage: Successfully wrote {data.Length} bytes to {filePath}");
        }
        finally
        {
            // Cleanup temp file if it exists
            if (File.Exists(tempPath))
            {
                try
                {
                    File.Delete(tempPath);
                }
                catch (Exception cleanupEx)
                {
                    DebugLogger.Msg($"StandaloneStorage: Failed to cleanup temp file '{tempPath}': {cleanupEx.Message}");
                }
            }
        }
    }

    #endregion

    #region Validation

    private static StorageResult ValidateOperation(string blobName, [CallerMemberName] string operation = "")
    {
        if (!_isReady)
        {
            DebugLogger.Msg($"StandaloneStorage {operation}: Storage not initialized");
            return StorageResult.StorageNotInitialized;
        }
        
        try
        {
            ValidateBlobName(blobName);
            return StorageResult.Successful;
        }
        catch (Exception ex)
        {
            DebugLogger.Msg($"StandaloneStorage {operation}: {ex.Message}");
            return StorageResult.Failed;
        }
    }

    private static void ValidateBlobName(string blobName)
    {
        if (string.IsNullOrWhiteSpace(blobName))
        {
            throw new ArgumentException("Blob name cannot be null or empty");
        }
        
        if (blobName.Contains("..") || blobName.Contains("/") || blobName.Contains("\\"))
        {
            throw new ArgumentException("Invalid blob name: path traversal characters detected");
        }
        
        if (blobName.Length > MaxBlobNameLength)
        {
            throw new ArgumentException($"Blob name too long: maximum {MaxBlobNameLength} characters");
        }
    }

    #endregion

    #region IL2CPP Utilities

    private static byte[] ConvertIl2CppArrayToManaged(Il2CppStructArray<byte> il2Array)
    {
        if (il2Array == null)
            return Array.Empty<byte>();
        
        int length = il2Array.Length;
        byte[] managedArray = new byte[length];
        
        for (int i = 0; i < length; i++)
        {
            managedArray[i] = il2Array[i];
        }
        
        return managedArray;
    }

    #endregion
}
