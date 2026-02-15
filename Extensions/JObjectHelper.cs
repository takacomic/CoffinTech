using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json.Linq;

namespace CoffinTech.Extensions;

public static class JObjectHelper
{
    /// <summary>
    /// Attempts to parse a JSON string into a JObject.
    /// </summary>
    public static bool TryParse(
        string json,
        [MaybeNullWhen(false)] out JObject result)
    {
        result = null;
        if (string.IsNullOrWhiteSpace(json))
            return false;

        try
        {
            result = JObject.Parse(json);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Attempts to read a JSON file and parse it into a JObject.
    /// </summary>
    public static bool TryParseFile(
        string filePath,
        [MaybeNullWhen(false)] out JObject result)
    {
        result = null;
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            return false;

        try
        {
            string json = File.ReadAllText(filePath);
            return TryParse(json, out result);
        }
        catch
        {
            return false;
        }
    }
}