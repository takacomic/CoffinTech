using Il2CppDarkTonic.MasterAudio;
using Il2CppVampireSurvivors.Data;

namespace CoffinTech.Defaults;

public class SoundManager
{
    public static PlaySoundResult PlaySound(SfxType sfxType, Il2CppVampireSurvivors.Framework.SoundManager.SoundConfig soundConfig = null, float durationMillis = 0f, int maxInstances = 10, float time = 0f)
    {
        return Il2CppVampireSurvivors.Framework.SoundManager.PlaySound(sfxType, soundConfig, durationMillis, maxInstances, time);
    }

}