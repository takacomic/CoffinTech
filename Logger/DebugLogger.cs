using CoffinTech;
using MelonLoader;

namespace CoffinTech.Logger;

internal static class DebugLogger
{
    public static void Msg(string txt)
    {
        if (!ModSettings.DebugLoggingEnabled)
        {
            return;
        }

        MelonLogger.Msg(txt);
    }
}
