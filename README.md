# How to:
- Download and install the latest [BepInEx](https://github.com/BepInEx/BepInEx/releases/latest/) and [ConfigurationManager](https://github.com/BepInEx/BepInEx.ConfigurationManager/releases/latest)
- Download this mod from [GitHub](https://github.com/Vheos777/OutwardMods/releases), [Thunderstore](https://outward.thunderstore.io/package/Vheos/VheosModPack/) or [Nexus](https://www.nexusmods.com/outward/mods/203?tab=files)
- Move the unzipped `Vheos` folder to `Outward\BepInEx\plugins\`
- Press `F1` in game to open the `Configuration Manager` window
- Enjoy <3

# FAQ:
- **How to change the default `Configuration Manager` hotkey?**
    - check out `Outward\BepInEx\config\com.bepis.bepinex.configurationmanager.cfg` :)
- **How to unhide a mod?**
    - tick the `Advanced Settings` checkbox at the top of the `Configuration Manager` window and you will see all hidden mods :)
- **Will mod ___ work online?**
    - It should, but I haven't tested ANY of these mods online, so I can't guarantee :P
- **Will mod ___ break my save file?**
    - it shouldn't, but it's a good habit to backup your save files before trying out new stuff :)
- **I change some settings but nothing happens. Why?**
    - some settings update the game instantly, others have towait for a loading screen, and some even require a full game restart. There's no detailed information yet about each setting, sorry!
- **Can I see the source code?**
    - yep, all my mods are open source and available at [GitHub](https://github.com/Vheos777/OutwardMods)! Feel free to study, clone and/or edit the code as you please :)

# Contact:
Write a comment on the [Nexus mod page](https://www.nexusmods.com/outward/mods/203?tab=posts),  
write a message in [Outward Modding Community](https://discord.gg/zKyfGmy7TR) -> `#vheos-mod-pack`
tag me in [Outward](https://discord.com/invite/outward) -> `#outward-modding`
or send me a DM on Discord - `Vheos#5865` :)

# Credits:
`Sinai`, `raphendyr`, `ehaugw`, `SpicerXD`, `IggyTheMad`, `Tau37`  
and other passionate people in the [Outward Modding Community](https://discord.gg/zKyfGmy7TR) Discord server!  
Love you all <3  

# Overview:
**Coming soon!**  
In the meantime, you can check out the [Nexus mod page](https://www.nexusmods.com/outward/mods/203) :)

# Changelog:
- **1.10.0**
    - in `Various`: added `Inn stashes` setting and `Enable cheats` hotkey
    - in `GUI`: added `Text scale` setting
    - in `Interactions`: added `Highlights` settings
    - bugfix: `Craftign -> Preserve durability ratios` produced broken results when all ingredients were indestructible
    - bugfix: `Descriptions -> Freshness bar` settings didn't work for foods in shops
- **1.9.0**
    - in `Various`: added `Inn rent duration`, `Base stamina regen` and `Temperature` settings
    - added `Tree Randomizer` mod (`Skills` section)
    - added a global advanced setting to `Reset to defaults` and `Load preset` (for now, only my personal presets)
    - split `Prices` mod into `Merchants` and `Prices` (`Skills` section)
    - renamed `Skill Editor` to `Editor`
    - renamed `Skill Limits` to `Limits`
    - bugfix: `Prices -> Learn mutually exclusive skills` was multiplying the price every time the skill was highlighted
- **1.8.1**
    - in `Interactions -> Disallowed in combat`: added `PullLevers`, removed `Talk` and `Revive`
    - renamed `Skills` mod to `Skill Editor`
    - created a new section called `Skills` and moved relevant mods
    - added debug logging to help track down user-submitted bugs
    - bugfix: `Targeting -> Auto-target actions` didn't work at all
- **1.8.0**
    - Thunderstore release \o/