namespace Vheos.Mods.Outward;
public class VariousDelayed : AMod, IDelayedInit
{
    #region Constants
    private static readonly Item[] ARROWS = new[]
{
        "Arrow".ToItem(),
        "Flaming Arrow".ToItem(),
        "Poison Arrow".ToItem(),
        "Venom Arrow".ToItem(),
        "Palladium Arrow".ToItem(),
        "Explosive Arrow".ToItem(),
        "Forged Arrow".ToItem(),
        "Holy Rage Arrow".ToItem(),
        "Soul Rupture Arrow".ToItem(),
        "Mana Arrow".ToItem(),
    };
    #endregion

    // Settings
    private static ModSetting<int> _arrowStackSize;
    private static ModSetting<int> _bulletStackSize;
    protected override void Initialize()
    {
        _arrowStackSize = CreateSetting(nameof(_arrowStackSize), 15, IntRange(0, 100));
        _bulletStackSize = CreateSetting(nameof(_bulletStackSize), 12, IntRange(0, 100));

        // Events
        _arrowStackSize.AddEvent(UpdateArrowsStackSize);
        _bulletStackSize.AddEvent(() => "Bullet".ToItem().m_stackable.m_maxStackAmount = _bulletStackSize);
    }
    protected override void SetFormatting()
    {
        _arrowStackSize.Format("Arrows stack size");
        _bulletStackSize.Format("Bullets stack size");
    }
    protected override string Description
    => "• Mods that need to run after the game is initialized";
    protected override string SectionOverride
    => "";
    protected override string ModName
    => "Various (delayed)";
    protected override void LoadPreset(string presetName)
    {
        switch (presetName)
        {
            case nameof(Preset.Vheos_CoopSurvival):
                ForceApply();
                _arrowStackSize.Value = 20;
                _bulletStackSize.Value = 20;
                break;
        }
    }

    // Utility
    private void UpdateArrowsStackSize()
    {
        foreach (var arrow in ARROWS)
            arrow.m_stackable.m_maxStackAmount = _arrowStackSize;
    }

    // Hooks


}