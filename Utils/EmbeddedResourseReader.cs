using System.Reflection;

namespace CoffinTech.Utils;

public static class EmbeddedResourseReader
{
    public static void ReadEmbeddedText(Assembly assembly, string resourceName, out string? resource)
    {
        using Stream? stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null) resource = null;
        using StreamReader reader = new StreamReader(stream);
        resource = reader.ReadToEnd();
    }
}