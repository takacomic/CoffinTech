using System.Reflection;
using UnityEngine;
using UnityEngine.Bindings;

namespace CoffinTech.Tools;

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
        
        string resourceName = $"{nameSpacePath}.{filename}";

        
        using (Stream stream = callingAssembly.GetManifestResourceStream(resourceName))
        {
            if (stream == null) 
                throw new ArgumentException($"Resource {resourceName} not found");
            
            byte[] bytes = new byte[stream.Length];
            stream.Read(bytes, 0, bytes.Length);
            Texture2D texture = new Texture2D(2, 2);
            unsafe
            {
                IntPtr ptr = UnityEngine.Object.MarshalledUnityObject.MarshalNotNull(texture);
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
    }
}