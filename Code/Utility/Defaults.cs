namespace Vheos.Mods.Outward;

public static class Defaults
{
    public const float FixedTimeDelta = 0.022f;
    public const float EnemyHealthResetTime = 24f;
    public const int ArmorTrainingPenaltyReduction = 50;
    public const float BaseStaminaRegen = 2.4f;
    public const int InnRentTime = 12;
    public const string InnQuestsFamilyName = "Inns";

    public static readonly AreaManager.AreaEnum[] Regions = new[]
{
        AreaManager.AreaEnum.CierzoOutside,
        AreaManager.AreaEnum.Emercar,
        AreaManager.AreaEnum.HallowedMarsh,
        AreaManager.AreaEnum.Abrassar,
        AreaManager.AreaEnum.AntiqueField,
        AreaManager.AreaEnum.Caldera,
    };
    public static readonly AreaManager.AreaEnum[] Cities = new[]
    {
        AreaManager.AreaEnum.CierzoVillage,
        AreaManager.AreaEnum.Berg,
        AreaManager.AreaEnum.Monsoon,
        AreaManager.AreaEnum.Levant,
        AreaManager.AreaEnum.Harmattan,
        AreaManager.AreaEnum.NewSirocco,
    };
    public static readonly Dictionary<TemperatureSteps, Vector2> TemperateDataByStep = new()
    {
        [TemperatureSteps.Hottest] = new Vector2(40, 101),
        [TemperatureSteps.VeryHot] = new Vector2(28, 92),
        [TemperatureSteps.Hot] = new Vector2(20, 80),
        [TemperatureSteps.Warm] = new Vector2(14, 62),
        [TemperatureSteps.Neutral] = new Vector2(0, 50),
        [TemperatureSteps.Fresh] = new Vector2(-14, 38),
        [TemperatureSteps.Cold] = new Vector2(-20, 26),
        [TemperatureSteps.VeryCold] = new Vector2(-30, 14),
        [TemperatureSteps.Coldest] = new Vector2(-45, -1),
    };
    public static readonly Dictionary<AreaManager.AreaEnum, int> SquadCountsByRegion = new()
    {
        [AreaManager.AreaEnum.CierzoOutside] = 41,
        [AreaManager.AreaEnum.Emercar] = 34,
        [AreaManager.AreaEnum.HallowedMarsh] = 36,
        [AreaManager.AreaEnum.Abrassar] = 30,
        [AreaManager.AreaEnum.AntiqueField] = 36,
        [AreaManager.AreaEnum.Caldera] = 65,
    };
    public static readonly Dictionary<SkillSlotLevel, int> PricesBySkillSlotLevel = new()
    {
        [SkillSlotLevel.Basic] = 50,
        [SkillSlotLevel.Breakthrough] = 500,
        [SkillSlotLevel.Advanced] = 600,
    };
}
