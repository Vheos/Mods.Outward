# How to:
- Install the latest [BepInEx](https://github.com/BepInEx/BepInEx/releases/latest/) and **ConfigurationManager** ([official release](https://github.com/BepInEx/BepInEx.ConfigurationManager/releases/latest) **OR** [Outward fork](https://github.com/Mefino/BepInEx.ConfigurationManager/releases/latest) + [SideLoader](https://github.com/sinai-dev/Outward-SideLoader/releases/latest))
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
    - it should, but I haven't tested ANY of these mods online, so I can't guarantee :P
- **Will mod ___ break my save file?**
    - it shouldn't, but it's a good habit to backup your save files before trying out new stuff :)
- **I change some settings but nothing happens! Why?**
    - some settings update the game instantly, others have towait for a loading screen, and some even require a full game restart. There's no detailed information yet about each setting, sorry!
- **I found a bug! How to report?**
    - choose one of the contact options below, then describe what's wrong and post your output log (`C:\Users\[YOUR_USERNAME]\AppData\LocalLow\Nine Dots Studio\Outward\output_log.txt`) via [Pastebin](https://pastebin.com/)
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
- **1.11.0**
    - moved some settings from `Various`, `Needs` and `Gamepad` into new mods - `Inns`, `Tools` and `Quickslots`
    - `Tools`: added `More gathering tools`, `Gathering tools durability cost` and `Chance to break Flint and Steel` settings
    - `Quickslots`: added `Contextual skills`, `Replace quickslots on equip` and `Assign by using free quickslot` settings
    - `Various`: added  `Randomize title screen`
    - *bugfix: `Various -> Inn rent duration` was initialized too early to take effect*
- **1.10.0**
    - `Various`: added `Inn stashes` setting and `Enable cheats` hotkey
    - `GUI`: added `Text scale` setting
    - `Interactions`: added `Highlights` settings
    - *bugfix: `Crafting -> Preserve durability ratios` produced broken results when all ingredients were indestructible*
    - *bugfix: `Descriptions -> Freshness bar` settings didn't work for foods in shops*
- **1.9.0**
    - `Various`: added `Inn rent duration`, `Base stamina regen` and `Temperature` settings
    - added `Tree Randomizer` mod (`Skills` section)
    - added a global advanced setting to `Reset to defaults` and `Load preset` (for now, only my personal presets)
    - split `Prices` mod into `Merchants` and `Prices` (`Skills` section)
    - renamed `Skill Editor` to `Editor`
    - renamed `Skill Limits` to `Limits`
    - *bugfix: `Prices -> Learn mutually exclusive skills` was multiplying the price every time the skill was highlighted*
- **1.8.1**
    - `Interactions -> Disallowed in combat`: added `PullLevers`, removed `Talk` and `Revive`
    - renamed `Skills` mod to `Skill Editor`
    - created a new section called `Skills` and moved relevant mods
    - added debug logging to help track down user-submitted bugs
    - *bugfix: `Targeting -> Auto-target actions` didn't work at all*
- **1.8.0**
    - Thunderstore release \o/