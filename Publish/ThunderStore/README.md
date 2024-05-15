# SkilledCarryWeight
Increases max carry weight based on skill level. The skills that increase max carry weight and the amount they increase it by are completely configurable. Also adds a quick attach/detach feature for carts and can configure increasing your max carry weight to make carts to be easier to pull. Uses Jotunn to sync configuration if installed on server.

**Server-Side Info**: This mod does work as a client-side only mod and only needs to be installed on the server if you wish to enforce configuration settings.

## Details
Vanilla skills are automatically detected so if new skills get added in a future update this mod will let you configure how they affect your max carry weight. 

Max carry weight is increased based on skill level for each skill enabled in the configuration using the following equation: 
```
Increase = Coefficient * ((Skill Level) ^ Power)
```

Effective cart mass if calculated using the following equation if `CarryWeightAffectsCart` is enabled.
```
ModifiedMass = Max(
	Mass * (1 - MaxMassReduction), 
	Mass * (MinCarryWeight/MaxCarryWeight) ^ Power
)
```

## Configuration
Changes made to the configuration settings will be reflected in-game immediately (no restart required) and they will also sync to clients if the mod is on the server. The mod also has a built in file watcher so you can edit settings via an in-game configuration manager (changes applied upon closing the in-game configuration manager) or by changing values in the file via a text editor or mod manager.


<div class="header">
	<h3>Global Section</h3>
    These settings control how verbose the output to the log is.
</div>
<table>
	<tbody>
		<tr>
            <th align="center">Setting</th>
            <th align="center">Server Sync</th>
			<th align="center">Description</th>
		</tr>
            <td align="center"><b>Verbosity</b></td>
            <td align="center">No</td>
			<td align="left">
                Low will log basic information about the mod. Medium will log information that is useful for troubleshooting. High will log a lot of information, do not set it to this without good reason as it will slow down your game.
				<ul>
					<li>Acceptable values: Low, Medium, High</li>
					<li>Default value: Low</li>
				</ul>
			</td>
		</tr>
  </tbody>
</table>

<div class="header">
	<h3>Cart Mass Section</h3>
    These settings control how your maximum carry weight affects the mass of carts and how easy they are to pull.
</div>
<table>
	<tbody>
		<tr>
            <th align="center">Setting</th>
            <th align="center">Server Sync</th>
			<th align="center">Description</th>
		</tr>
		<tr>
            <td align="center"><b>CarryWeightAffectsCart</b></td>
            <td align="center">Yes</td>
			<td align="left">
                Set to true/enabled to allow your max carry weight affect how easy carts are to pull by reducing the mass of carts you pull.
				<ul>
					<li>Acceptable values: False, True</li>
					<li>Default value: true</li>
				</ul>
			</td>
		</tr>
        <tr>
            <td align="center"><b>MaxMassReduction</b></td>
            <td align="center">Yes</td>
			<td align="left">
                Maximum reduction in cart mass due to increased max carry weight. Limits effective cart mass to always be equal to or greater than: <br>Mass * (1 - MaxMassReduction).
				<ul>
					<li>Acceptable values: (0, 1)</li>
					<li>Default value: 0.7</li>
				</ul>
			</td>
		</tr>
		<tr>
            <td align="center"><b>MinCarryWeight</b></td>
            <td align="center">Yes</td>
			<td align="left">
                Minimum value your maximum carry weight must be before it starts making carts easier to pull.
				<ul>
					<li>Acceptable values: (300, 1000)</li>
					<li>Default value: 300</li>
				</ul>
			</td>
		</tr>
		<tr>
            <td align="center"><b>Power</b></td>
            <td align="center">Yes</td>
			<td align="left">
                Affects how much your maximum carry weight making pulling carts easier. Higher powers make your maximum carry weight reduce the mass of carts more.
				<ul>
					<li>Acceptable values: (0, 3)</li>
					<li>Default value: 1</li>
				</ul>
			</td>
		</tr>
  </tbody>
</table>

<div class="header">
	<h3>Quick Cart Section</h3>
    These settings control how attaching and detaching to carts work.
</div>
<table>
	<tbody>
		<tr>
            <th align="center">Setting</th>
            <th align="center">Server Sync</th>
			<th align="center">Description</th>
		</tr>
		<tr>
            <td align="center"><b>QuickCartKey</b></td>
            <td align="center">No</td>
			<td align="left">
                The hotkey used to attach to or detach from a nearby cart.
				<ul>
					<li>Acceptable values: KeyCode</li>
					<li>Default value: G</li>
				</ul>
			</td>
		</tr>
        <tr>
            <td align="center"><b>AttachDistance</b></td>
            <td align="center">Yes</td>
			<td align="left">
                Maximum distance to attach a cart from.
				<ul>
					<li>Acceptable values: (2, 8)</li>
					<li>Default value: 5</li>
				</ul>
			</td>
		</tr>
		<tr>
            <td align="center"><b>AttachOutOfPlace</b></td>
            <td align="center">Yes</td>
			<td align="left">
                Allow attaching the cart even when out of place.
				<ul>
					<li>Acceptable values: False, True</li>
					<li>Default value: true</li>
				</ul>
			</td>
		</tr>
  </tbody>
</table>

<div class="header">
	<h3>Individual Skill Sections</h3>
    Each skill gets it's own section with the following configuration options.
</div>
<table>
	<tbody>
		<tr>
            <th align="center">Setting</th>
            <th align="center">Server Sync</th>
			<th align="center">Description</th>
		</tr>
		<tr>
            <td align="center"><b>Enabled</b></td>
            <td align="center">Yes</td>
			<td align="left">
                Set to true/enabled to allow this skill to increase your max carry weight.
				<ul>
					<li>Acceptable values: False, True</li>
					<li>Default value: <i>Depends on the skill</i></li>
				</ul>
			</td>
		</tr>
        <tr>
            <td align="center"><b>Coefficient</b></td>
            <td align="center">Yes</td>
			<td align="left">
                Value to multiply the skill level by to determine how much extra carry weight it grants.
				<ul>
					<li>Acceptable values: (0, 10)</li>
					<li>Default value: 0.25</li>
				</ul>
			</td>
		</tr>
		<tr>
            <td align="center"><b>Power</b></td>
            <td align="center">Yes</td>
			<td align="left">
                Power the skill level is raised to before multiplying by Coefficient to determine extra carry weight.
				<ul>
					<li>Acceptable values: (0, 10)</li>
					<li>Default value: 1</li>
				</ul>
			</td>
		</tr>
  </tbody>
</table>

## Known Issues
None so far, tell me if you find any.

## Donations/Tips
My mods will always be free to use but if you feel like saying thanks you can tip/donate.

| My Ko-fi: | [![ko-fi](https://ko-fi.com/img/githubbutton_sm.svg)](https://ko-fi.com/searica) |
|-----------|---------------|

## Source Code
Source code is available on Github.

| Github Repository: | <img height="18" src="https://github.githubassets.com/favicons/favicon-dark.svg"></img><a href="https://github.com/searica/SkilledCarryWeight"> SkilledCarryWeight</a> |
|-----------|---------------|

### Contributions
If you would like to provide suggestions, make feature requests, or reports bugs and compatibility issues you can either open an issue on the Github repository or tag me (@searica) with a message on my discord [Searica's Mods](https://discord.gg/sFmGTBYN6n).

I'm a grad student and have a lot of personal responsibilities on top of that so I can't promise I will respond quickly, but I do intend to maintain and improve the mod in my free time.

## Shameless Self Plug (Other Mods By Me)
If you like this mod you might like some of my other ones.

#### Building Mods
- [More Vanilla Build Prefabs](https://thunderstore.io/c/valheim/p/Searica/More_Vanilla_Build_Prefabs/)
- [Extra Snap Points Made Easy](https://thunderstore.io/c/valheim/p/Searica/Extra_Snap_Points_Made_Easy/)
- [AdvancedTerrainModifiers](https://thunderstore.io/c/valheim/p/Searica/AdvancedTerrainModifiers/)
- [BuildRestrictionTweaksSync](https://thunderstore.io/c/valheim/p/Searica/BuildRestrictionTweaksSync/)
- [ToolTweaks](https://thunderstore.io/c/valheim/p/Searica/ToolTweaks/)

#### Gameplay Mods
- [CameraTweaks](https://thunderstore.io/c/valheim/p/Searica/CameraTweaks/)
- [DodgeShortcut](https://thunderstore.io/c/valheim/p/Searica/DodgeShortcut/)
- [FortifySkillsRedux](https://thunderstore.io/c/valheim/p/Searica/FortifySkillsRedux/)
- [ProjectileTweaks](https://thunderstore.io/c/valheim/p/Searica/ProjectileTweaks/)
- [SafetyStatus](https://thunderstore.io/c/valheim/p/Searica/SafetyStatus/)