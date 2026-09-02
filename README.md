# Free Harpoon Dash

A BepInEx 5.4.23.4 plugin for the Unity Mono build of Hollow Knight: Silksong.

This targets the horizontal needle movement ability implemented by the game's `Harpoon Dash` PlayMaker FSM:

- `Harpoon Dash / Can Do?` always follows its success transition instead of requiring `PlayerData.silk > 0`.
- The constant `TakeSilk(1)` action in `Harpoon Dash / Take Control` is skipped.

Silkspear and all silk skills keep their original costs.

## Build and install

Set `SILKSONG_GAME_ROOT` and build from the repository root:

```powershell
$env:SILKSONG_GAME_ROOT = 'C:\Program Files (x86)\Steam\steamapps\common\Hollow Knight Silksong'
dotnet build .\FreeHarpoonDash.csproj -c Release
```

The successful build copies `FreeHarpoonDash.dll` to `BepInEx\plugins`. To build without installing it:

```powershell
dotnet build .\FreeHarpoonDash.csproj -c Release -p:DeployMod=false
```

To uninstall, remove `BepInEx\plugins\FreeHarpoonDash.dll`.
