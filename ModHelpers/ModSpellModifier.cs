using Il2CppVampireSurvivors.Data;
using Il2CppVampireSurvivors.Objects;
using Il2CppVampireSurvivors.Spells;
using Il2CppZenject;

namespace CoffinTech.Utils;

public class ModSpellModifier
{
    private string _spell;
    internal SecretType _secretType;
    
    public virtual void Start(PlayerOptions player, SignalBus signalBus, SpellsManager spellsManager,
        DataManager dataManager)
    {
    }

    public virtual void Activate(PlayerOptions player, SignalBus signalBus, SpellsManager spellsManager,
        DataManager dataManager)
    {
        
    }
}