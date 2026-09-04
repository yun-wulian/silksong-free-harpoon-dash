# Free Harpoon Dash

A BepInEx 5.4.23.4 plugin for the Unity Mono build of Hollow Knight: Silksong.

This targets the needle movement ability implemented by the game's `Harpoon Dash` PlayMaker FSM:

- `Harpoon Dash / Can Do?` always follows its success transition instead of requiring `PlayerData.silk > 0`.
- The constant `TakeSilk(1)` action in `Harpoon Dash / Take Control` is skipped.
- The current movement input is snapped to eight directions when the throw starts. WASD combinations therefore aim horizontally, vertically, or diagonally; no input keeps the current facing direction.
- On the ground, the eight-direction override applies only to up and the two up-diagonal octants. Horizontal and all three downward octants are left entirely to the game: pure down can enter native Silk Soar, while down-diagonal Harpoon input keeps the native horizontal throw. All eight directions use the override while airborne.
- Harpoon raycasts, the needle position, dash velocity, arrival distance, damage direction, and dash visuals all use the same snapped direction.
- Eight-direction travel continues to use the native `SetVelocityAsAngle` action with the snapped `Travel Angle`; the hero physics/render root remains unrotated. Needle art, Harpoon Breakers, Dash Damager, and Dash Effect carry the directional transforms instead.
- When a non-horizontal throw selects terrain, a cast of the hero's real, unrotated collider shortens the final physics step before penetration. It exits through native `END` and normal catch instead of `CATCH → To Needle`, avoiding the original horizontal-only X snap into a vertical wall. Surface normals are checked against travel direction, so floors, ceilings, slopes, and vertical walls are protected while native horizontal Harpoon collision remains untouched. Enemy, ring, and open-air interactions keep their existing paths.

## Debug unlock

`Debug.UnlockHarpoonDashBeforeStory` defaults to `false`. Turn it on through BepInEx Configuration Manager or `BepInEx/config/modcraft.silksong.free-harpoon-dash.cfg` to test Harpoon Dash before its story unlock.

The option only overrides the runtime availability check. It never writes `PlayerData.hasHarpoonDash`, so turning it off immediately restores the save's real progression state. Once the story has unlocked Harpoon Dash normally, turning Debug off leaves the ability available.

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
