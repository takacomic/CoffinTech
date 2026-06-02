using CoffinTech;
using MelonLoader;

namespace CoffinTech.Logger;

internal static class DebugLogger
{
    public static void Msg(string txt)
    {
        if (!CoffinTechMod.DebugLoggingEnabled)
        {
            return;
        }

        MelonLogger.Msg(txt);
    }
}
