using System.Reflection;

namespace CoffinTech.Extensions;

public static class PropertyInfoExtensions
{
    public static bool TryGetValue(
        this PropertyInfo property,
        object instance,
        out object value)
    {
        value = null;

        if (property == null)
            return false;

        try
        {
            value = property.GetValue(instance);
            return true;
        }
        catch
        {
            return false;
        }
    }
}