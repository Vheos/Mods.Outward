namespace Vheos.Mods.Outward;
using System.Collections;

public class Initialization : AMod
{
    #region enum
    private enum TitleScreenCharacterVisibility
    {
        Enable = 1,
        Disable = 2,
        Randomize = 3,
    }

    #endregion

    // Settings
    private static ModSetting<bool> _skipStartupVideos;
    private static ModSetting<TitleScreenCharacterVisibility> _titleScreenHideCharacters;
    private static Dictionary<TemperatureSteps, ModSetting<Vector2>> _temperatureDataByEnum;
    protected override void Initialize()
    {
        _skipStartupVideos = CreateSetting(nameof(_skipStartupVideos), false);
        _titleScreenHideCharacters = CreateSetting(nameof(_titleScreenHideCharacters), TitleScreenCharacterVisibility.Enable);
    }
    protected override void SetFormatting()
    {
        _skipStartupVideos.Format("Skip startup videos");
        _skipStartupVideos.Description =
            "Saves ~3 seconds each time you launch the game";

        _titleScreenHideCharacters.Format("Title screen characters");
        _titleScreenHideCharacters.Description =
            "If you think they are ruining the view :)\n" +
            "(requires game restart)";
    }
    protected override string Description
    => "• Mods that need to run before anything else";
    protected override string SectionOverride
    => "";
    protected override void LoadPreset(string presetName)
    {
        switch (presetName)
        {
            case nameof(Preset.Vheos_CoopSurvival):
                ForceApply();
                _skipStartupVideos.Value = true;
                _titleScreenHideCharacters.Value = TitleScreenCharacterVisibility.Randomize;
                break;
        }
    }

    // Hooks
    // Title screen
    [HarmonyPatch(typeof(TitleScreenLoader), nameof(TitleScreenLoader.LoadTitleScreenCoroutine)), HarmonyPostfix]
    private static IEnumerator TitleScreenLoader_LoadTitleScreenCoroutine_Post(IEnumerator original, TitleScreenLoader __instance)
    {
        while (original.MoveNext())
            yield return original.Current;

        #region quit
        if (_titleScreenHideCharacters.Value == TitleScreenCharacterVisibility.Enable)
            yield break;
        #endregion

        bool state = true;
        switch (_titleScreenHideCharacters.Value)
        {
            case TitleScreenCharacterVisibility.Disable: state = false; break;
            case TitleScreenCharacterVisibility.Randomize: state = System.DateTime.Now.Ticks % 2 == 0; break;
        }

        foreach (var characterVisuals in __instance.transform.GetAllComponentsInHierarchy<CharacterVisuals>())
            characterVisuals.GOSetActive(state);
    }

    // Skip startup video
    [HarmonyPatch(typeof(StartupVideo), nameof(StartupVideo.Awake)), HarmonyPrefix]
    private static bool StartupVideo_Awake_Pre()
    {
        StartupVideo.HasPlayedOnce = _skipStartupVideos.Value;
        return true;
    }
}