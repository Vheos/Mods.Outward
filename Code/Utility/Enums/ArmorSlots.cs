namespace Vheos.Mods.Outward;

[Flags]
public enum ArmorSlots
{
    None = 0,
    Head = 1 << 1,
    Chest = 1 << 2,
    Feet = 1 << 3,
    All = Head | Chest | Feet,
}