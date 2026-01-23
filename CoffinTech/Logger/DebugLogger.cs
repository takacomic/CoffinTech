using MelonLoader;

namespace CoffinTech.Logger;

internal static class DebugLogger
{
    public static void Msg(string txt)
    {
        #if DEBUG
        MelonLogger.Msg(txt);
        #endif
    }
}