# Desert Endless Runner (Unity 3D)

A 3D desert endless runner for **Android** and **Windows PC**, written in C#. Everything (character, desert world, obstacles,
coins, power-ups, UI, particles and audio) is generated procedurally, so no paid or external assets are needed.

> **Status: written without access to Unity.** The code was statically checked but has **not been compiled or play-tested in Unity**.
> See "Known limitations / troubleshooting" below.

## Requirements
- **Unity 2022.3 LTS** (project is set to 2022.3.62f1; any 2022.3.x works). Built-in render pipeline, legacy Input Manager, uGUI.
- Unity Hub with the **Android Build Support** module (for Android) and/or **Windows Build Support** (Windows Mono/IL2CPP).

## Quick start
1. Extract the ZIP.
2. Unity Hub -> **Add project from disk** -> select the extracted `Desert_Endless_Runner` folder.
3. Open it (if asked, pick Unity 2022.3 LTS). Wait for compilation.
4. On first open the setup tool runs automatically (menu **Desert Runner > 1. Run Full Project Setup** repeats it). It:
   creates folders, registers scenes in Build Settings, applies Android/Windows player settings, adds required shaders to
   *Always Included*, and exports prefabs/materials/textures/meshes/WAV audio into `Assets/`. It then opens `Gameplay.unity`.
5. Open `Assets/Scenes/Gameplay.unity` (or `MainMenu.unity`) and press **Play**.

No manual GameObject creation or reference wiring is needed: `GameBootstrap` builds the camera, lighting, player, track, managers
and UI when either scene starts.

## Controls
| Action | Windows | Android |
|---|---|---|
| Move left / right | Left/Right arrow, A / D | Swipe left / right |
| Jump | Up arrow, W, Space | Swipe up |
| Slide | Down arrow, S | Swipe down |
| Pause | Esc / P, or the pause button | Pause button (also auto-pauses on app switch) |

(In the editor you can also swipe with the mouse.)

## Gameplay summary
- Auto-run, 3 lanes, jump, slide (swipe down in mid-air = fast fall then slide). Speed rises from 13 to 30 m/s.
- Obstacles: crates, rocks, fallen palms, cacti, stone barriers, temple pillars, abandoned vehicle, wooden barriers, plus overhead
  gates you must slide under. Rows are generated so every row has a solvable route (jump, slide or lane change).
- Power-ups: **Magnet** (10 s), **Shield** (absorbs one hit, 20 s timeout), **Speed Boost** (6 s, faster and smashes obstacles),
  **Score x2** (12 s), **Coin Bonus** (+25 coins).
- Score = distance (x multiplier) + coins x 10. High score, best distance, total coins and sound setting persist in PlayerPrefs.
- Revive on game over costs 50 / 100 / 150 coins (max 3 per run) and clears obstacles ahead.

## Project layout
```
Assets/
  Scenes/      MainMenu.unity, Gameplay.unity
  Scripts/     Core (GameManager, GameBootstrap, SaveData, PoolManager)
               Player (PlayerController, PlayerVisual, PlayerEffects, InputHandler)
               Environment (TrackGenerator, PropFactory, MaterialFactory, MeshFactory, DesertAtmosphere)
               Gameplay (Obstacle, Collectible, PowerUpManager)
               UI (UIFactory, MainMenuUI, GameplayUI)   Camera (FollowCamera)   Audio (AudioManager)
  Editor/      ProjectSetupTool.cs  (setup + build menu items)
  Prefabs/ Materials/ Textures/ Models/ Audio/ Particles/ Resources/   (filled by the setup tool)
Packages/manifest.json     ProjectSettings/ (ProjectVersion + EditorBuildSettings; Unity generates the rest)
```

## Build for Windows
1. File > Build Settings > select **Windows, Mac, Linux** > Target Platform **Windows**, Architecture x86_64 > Switch Platform.
2. Scenes In Build must list `MainMenu` (0) and `Gameplay` (1) - the setup tool does this.
3. **Build** (or menu **Desert Runner > Build > Windows 64-bit**, output in `Builds/Windows/`).

## Build for Android
1. Install the Android Build Support module (with OpenJDK and Android SDK & NDK) via Unity Hub.
2. File > Build Settings > **Android** > Switch Platform.
3. Player Settings (already applied by the setup tool): landscape auto-rotation, package `com.desertrunner.endlessrunner`,
   min API 24, IL2CPP, ARM64 + ARMv7, Gamma color space. Change the package name before publishing.
4. Connect a device with USB debugging, **Build And Run**, or use **Desert Runner > Build > Android APK**
   (output in `Builds/Android/`). For Play Store use *Build App Bundle* and ARM64 only.

## Known limitations / troubleshooting
- **Not compiled or run in Unity by the author.** If a compile error appears, it is most likely a small API mismatch; the first
  error in the Console shows the file/line.
- ProjectSettings only ships `ProjectVersion.txt` and `EditorBuildSettings.asset`; Unity generates the remaining files
  on first open and the setup tool applies the settings.
- If a scene shows errors or won't open: **Desert Runner > Rebuild Scenes** recreates both scenes and Build Settings.
- Prefabs/materials/audio in `Assets/` are exported by the setup tool on first open (they are reference exports; the running game
  builds objects from code and does not depend on them). Run **Desert Runner > Export Prefabs, Materials, Textures and Audio** to redo.
- Materials use the Standard shader (built-in pipeline). Do not convert the project to URP/HDRP without porting `MaterialFactory`.
- Unity 6 / new Input System: set *Active Input Handling* to **Both** or **Input Manager (Old)** in Player Settings.
- Performance: if an older phone is slow, lower `TrackGenerator.SegmentCount`, `QualitySettings.shadowDistance`
  (in `DesertAtmosphere`) or the sandstorm rate.
