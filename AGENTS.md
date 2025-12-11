# Repository Guidelines

## Project Structure & Module Organization
- Unity project root; gameplay assets live under `Assets/`. Core modules sit in `Assets/Modules` (`Blocks`, `Connect3`, `Game`) while shared ball logic is in `Assets/Script/Balls`.
- Scenes: `Assets/Scenes/MenuScene.unity` and `Assets/Scenes/SampleScene.unity` are the main entry points for menu and gameplay testing. Prefabs and materials sit in `Assets/Prefab` and `Assets/Material`.
- Platform and service integrations: `Assets/Plugins` and `Assets/PlayFabSDK` hold third-party binaries and PlayFab client code. Shared data (sprites, scriptable objects) is under `Assets/Resources` and `Assets/Texture`.

## Build, Test, and Development Commands
- Open the project in Unity (keep to the current Editor version used by the team). Typical headless build example: `Unity -projectPath . -quit -batchmode -buildWindows64Player Build/TilemapLighting.exe` (create `Build/` if missing).
- For scripting, you can open `TilemapLighting.sln` in Rider/VS; assemblies are generated from `.csproj` files in the root.
- Play mode checks: open `SampleScene` in the Editor, press Play, and validate block spawning and movement without console errors.

## Coding Style & Naming Conventions
- C# scripts use 4-space indentation, PascalCase for classes/methods, and camelCase for private serialized fields (e.g., `blockSpacing`). Keep MonoBehaviour field refs serialized instead of `FindObjectOfType` when possible.
- Keep behaviour scripts small and composable; prefer early returns to reduce nesting, as seen in `BlockBoardScript`.
- If you add assets, ensure `.meta` files are kept in sync and referenced prefabs are not broken.

## Testing Guidelines
- No automated tests are currently present. Cover changes with targeted play mode checks: verify block advancement timing, collisions, and UI flows in both `MenuScene` and `SampleScene`.
- When fixing bugs, note reproduction steps and expected outcomes; add editor gizmos or debug logs temporarily when diagnosing physics/tilemap issues.

## Commit & Pull Request Guidelines
- Use short, imperative commit messages (e.g., `add block spawn cooldown`, `fix stuck bricks`).
- In PRs, include: summary of changes, affected scene/prefab paths, manual test notes (Play mode scenarios run), and screenshots/GIFs for UI changes. Link related issues or tasks and call out any data/schema updates.
