using System.Reflection;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppVampireSurvivors.Graphics;
using UnityEngine;
using UnityEngine.Bindings;

namespace CoffinTech.Utils;

public static class SpriteImporter
{
    /// <summary>
    /// Allows the user to load a png texture straight into a Vampire Survivors Sprite. This is done in a hacky way for Unity 6 right now
    /// </summary>
    /// <param name="callingAssembly">Assembly containing the embedded resources</param>>
    /// <param name="nameSpacePath">Path to the file, typically this is 'namespace'.'folder' if you don't store it in the root </param>
    /// <param name="filename">Name of the file</param>
    /// <returns>Returns a Texture2D of the png you supply</returns>
    public static Texture2D LoadTextureFromAssembly(Assembly callingAssembly, string nameSpacePath, string filename)
    {
        ArgumentNullException.ThrowIfNull(callingAssembly);
        if (string.IsNullOrEmpty(nameSpacePath)) 
            throw new ArgumentException($"{nameof(nameSpacePath)} cannot be null or empty");
        if (string.IsNullOrEmpty(filename)) 
            throw new ArgumentException($"{nameof(filename)} cannot be null or empty");
        
        var resourceName = $"{nameSpacePath}.{filename}";


        using var stream = callingAssembly.GetManifestResourceStream(resourceName);
        if (stream == null) 
            throw new ArgumentException($"Resource {resourceName} not found");
            
        var bytes = new byte[stream.Length];
        stream.Read(bytes, 0, bytes.Length);
        var texture = new Texture2D(2, 2);
        unsafe
        {
            var ptr = UnityEngine.Object.MarshalledUnityObject.MarshalNotNull(texture);
            fixed (byte* bytesPtr = bytes)
            {
                var managedSpanWrapper = new ManagedSpanWrapper(bytesPtr, bytes.Length);
                ImageConversion.LoadImage_Injected(ptr, ref managedSpanWrapper, false);
            }
        }
            
        texture.name = filename.Split('.').First();
        texture.filterMode = FilterMode.Point;
        return texture;
    }

    public static AssetBundle LoadAssetBundleFromAssembly(Assembly callingAssembly, string nameSpacePath,
        string filename)
    {
        ArgumentNullException.ThrowIfNull(callingAssembly);
        if (string.IsNullOrEmpty(nameSpacePath)) 
            throw new ArgumentException($"{nameof(nameSpacePath)} cannot be null or empty");
        if (string.IsNullOrEmpty(filename)) 
            throw new ArgumentException($"{nameof(filename)} cannot be null or empty");
        
        var resourceName = $"{nameSpacePath}.{filename}";


        using var stream = callingAssembly.GetManifestResourceStream(resourceName);
        if (stream == null) 
            throw new ArgumentException($"Resource {resourceName} not found");
            
        var bytes = new byte[stream.Length];
        stream.Read(bytes, 0, bytes.Length);
        var assetBundle = new AssetBundle();
        unsafe
        {
            var ptr = UnityEngine.Object.MarshalledUnityObject.MarshalNotNull(assetBundle);
            fixed (byte* bytesPtr = bytes)
            {
                var managedSpanWrapper = new ManagedSpanWrapper(bytesPtr, bytes.Length);
                assetBundle = Unmarshal.UnmarshalUnityObject<AssetBundle>(AssetBundle.LoadFromMemory_Internal_Injected(ref managedSpanWrapper, 0U));
            }
        }
        return assetBundle;
    }
    
    public static unsafe UnityEngine.Object LoadAsset_Internal(AssetBundle assetBundle, string name, Il2CppSystem.Type type)
    {
        ArgumentNullException.ThrowIfNull(assetBundle);

        var unitySelf = UnityEngine.Object.MarshalledUnityObject.MarshalNotNull(assetBundle);
        if (unitySelf == IntPtr.Zero)
            ThrowHelper.ThrowNullReferenceException(assetBundle);

        var managedSpanWrapper = default(ManagedSpanWrapper);
        if (StringMarshaller.TryMarshalEmptyOrNullString(name, ref managedSpanWrapper))
        {
            var gcHandlePtr = AssetBundle.LoadAsset_Internal_Injected(unitySelf, ref managedSpanWrapper, type);
            return Unmarshal.UnmarshalUnityObject<UnityEngine.Object>(gcHandlePtr);
        }

        fixed (char* namePtr = name)
        {
            managedSpanWrapper = new ManagedSpanWrapper(namePtr, name.Length);
            var gcHandlePtr = AssetBundle.LoadAsset_Internal_Injected(unitySelf, ref managedSpanWrapper, type);
            return Unmarshal.UnmarshalUnityObject<UnityEngine.Object>(gcHandlePtr);
        }
    }
    
    public static void SpriteStrip(Texture2D texture, string name, int spriteCount)
    {
        int baseWidth = texture.width / spriteCount;

        for (int i = 0; i < spriteCount; i++)
        {
            string nameAppend = name + "_0" + (i+1);
            Sprite sprite = LoadSprite(texture, new Rect(baseWidth * i, 0, baseWidth, texture.height), nameAppend);
            SpriteManager.RegisterSprite(sprite);
        }
    }
    
    public static void SpriteSingle(Texture2D texture, string name, int width, int height, int x, int y = 0)
    {
        string nameAppend = name;
        Sprite sprite = LoadSprite(texture, new Rect(x, y, width, height), nameAppend);
        SpriteManager.RegisterSprite(sprite);
    }
    internal static Sprite LoadSprite(Texture2D texture, Rect rect, string name)
    {
        Sprite sprite = Sprite.Create(texture, new Rect((int)rect.x, (int)rect.y, (int)rect.width, (int)rect.height), new Vector2(0.5f, 0.5f));
        sprite.name = name;
        return sprite;
    }
}
