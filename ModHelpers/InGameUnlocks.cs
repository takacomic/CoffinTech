using Il2CppVampireSurvivors.Data;
using Il2CppVampireSurvivors.Framework;

namespace CoffinTech.Utils;

public class InGameUnlocks
{
    public static void InGameSecretCharacterUnlock(CharacterType characterType)
    {
        GM.Core.PlayerOptions.UnlockCharacter(characterType);
        if (!GM.Core.PlayerOptions.Config.UnlockedCharacters.Contains(characterType)) GM.Core.PlayerOptions.Config.UnlockedCharacters.Add(characterType);
        GM.Core.PlayerOptions.BuyCharacter(characterType);
        if (!GM.Core.PlayerOptions.Config.BoughtCharacters.Contains(characterType)) GM.Core.PlayerOptions.Config.BoughtCharacters.Add(characterType);
        GM.Core.PlayerOptions.RevealCharacter(characterType);
        GM.Core.PlayerOptions.Save();
        SoundManager.PlaySound(SfxType.ThingFound, new SoundManager.SoundConfig
        {
            Volume = new Il2CppSystem.Nullable<float>(1),
            Detune = -1000f,
            Rate = 0.5f
        });
    }
    
    public static void InGameSecretCharacterSkinUnlock(CharacterType characterType, SkinType skinType)
    {
        GM.Core.PlayerOptions.UnlockSkin(characterType, skinType);
        GM.Core.PlayerOptions.Save();
        SoundManager.PlaySound(SfxType.ThingFound, new SoundManager.SoundConfig
        {
            Volume = new Il2CppSystem.Nullable<float>(1),
            Detune = -1000f,
            Rate = 0.5f
        });
    }
}