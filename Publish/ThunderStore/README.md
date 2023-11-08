# SkilledCarryWeight
Increases max carry weight based on skill level. The skills that increase max carry weight and the amount they increase it by are completely configurable. Uses Jotunn to sync configuration if installed on server.

## Details
Vanilla skills are automatically detected so if new skills get added in a future update this mod will let you configure how they affect your max carry weight. 

Max carry weight is increased based on skill level for each skill enabled in the configuration using the following equation: 
```
Increase = Coefficient * ((Skill Level) ^ Power)
```

**Server-Side Info**: This mod does work as a client-side only mod and only needs to be installed on the server if you wish to enforce configuration settings.

## Configuration
Changes made to the configuration settings will be reflected in-game immediately (no restart required) and they will also sync to clients if the mod is on the server. The mod also has a built in file watcher so you can edit settings via an in-game configuration manager (changes applied upon closing the in-game configuration manager) or by changing values in the file via a text editor or mod manager.

### Global Section:

**Verbosity**
- Low will log basic information about the mod. Medium will log information that is useful for troubleshooting. High will log a lot of information, do not set it to this without good reason as it will slow down your game.
  - Acceptable values: Low, Medium, High
  - Default value: Low.

### Individual Skills Section
Each skill gets it's own section with the following configuration options.

**Enabled** [Synced with Server]
- Set to true/enabled to allow this skill to increase your max carry weight.
  - Acceptable values: False, True
  - Default value: depends on the skill

**Coefficient** [Synced with Server]
- Value to multiply the skill level by to determine how much extra carry weight it grants.
  - Acceptable values: (0, 10)
  - Default value: 0.5

**Power** [Synced with Server]
- Power the skill level is raised to before multiplying by Coefficient to determine extra carry weight.
  - Acceptable values: (0, 10)
  - Default value: 1

## Known Issues
None so far, tell me if you find any.

## Donations/Tips
 My mods will always be free to use but if you feel like saying thanks you can tip/donate.

  [![ko-fi](https://ko-fi.com/img/githubbutton_sm.svg)](https://ko-fi.com/searica)

## Source Code
Github: https://github.com/searica/SkilledCarryWeight

### Contributions
If you would like to provide suggestions, make feature requests, or reports bugs and compatibility issues you can either open an issue on the Github repository or tag me (@searica) with a message on my discord [Searica's Mods](https://discord.gg/sFmGTBYN6n).
<!--the [Jotunn discord](https://discord.gg/DdUt6g7gyA), or the [Odin Plus discord](https://discord.gg/mbkPcvu9ax)-->

I'm a grad student and have a lot of personal responsibilities on top of that so I can't promise I will respond quickly, but I do intend to maintain and improve the mod in my free time.

## Shameless Self Plug (Other Mods By Me)
If you like this mod you might like some of my other ones.

#### Building Mods
- [More Vanilla Build Prefabs](https://valheim.thunderstore.io/package/Searica/More_Vanilla_Build_Prefabs/)
- [Extra Snap Points Made Easy](https://valheim.thunderstore.io/package/Searica/Extra_Snap_Points_Made_Easy/)
- [BuildRestrictionTweaksSync](https://valheim.thunderstore.io/package/Searica/BuildRestrictionTweaksSync/)

#### Gameplay Mods
- [FortifySkillsRedux](https://valheim.thunderstore.io/package/Searica/FortifySkillsRedux/)
- [DodgeShortcut](https://valheim.thunderstore.io/package/Searica/DodgeShortcut/)
- [ProjectileTweaks](https://github.com/searica/ProjectileTweaks)
