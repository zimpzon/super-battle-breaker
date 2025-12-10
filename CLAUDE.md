# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview
This is a Unity 2D hybrid game combining match-3 mechanics with physics-based ball launching gameplay. The project uses Unity 6000.0.45f1 and Universal Render Pipeline (URP) with 2D lighting for visual effects. Players match bricks on a board to spawn colored balls that interact with blocks and other game elements.

## Development Commands
Unity projects are typically developed through the Unity Editor IDE. There are no traditional build scripts like npm or gradle in this repository.

**Building:**
- Open the project in Unity Editor (File > Open Project)
- Build via: File > Build Settings > Build
- The project uses Assembly-CSharp and Assembly-CSharp-Editor assemblies

**Testing:**
- Unity Test Framework can be accessed via Window > General > Test Runner
- No custom test scripts or commands found in the project

## Project Structure
```
Assets/
├── Script/Balls/           # Ball physics and behavior
├── Modules/
│   ├── Game/              # Core game management (GameScript)
│   └── Connect3/          # Match-3 game logic
├── PlayFabSDK/            # PlayFab integration for analytics/leaderboards
├── Scenes/                # Unity scene files
├── Settings/              # URP and rendering settings
├── Tilemap/               # Tilemap assets and palettes
└── Material/              # Physics materials and shaders
```

## Core Game Architecture

**Game Management:**
- `GameScript` - Central singleton managing game state, scoring, audio, and ball spawning
- Handles start/restart logic and integrates match-3 with ball physics
- Manages PlayFab integration for analytics and leaderboards

**Match-3 System:**
- `BoardScript` - Grid-based match-3 board logic (referenced but not in visible files)
- `BrickScript` - Individual brick behavior for matching
- Integration with ball spawning when matches occur

**Physics System:**
- `BallScriptVerlet` - Advanced Verlet integration physics for realistic ball movement
- `BallScript` - Standard Unity physics ball behavior
- `BlockBoardScript` - Spawns scrolling block rows that interact with balls
- Custom physics materials for different collision behaviors

**Rendering & Effects:**
- Universal Render Pipeline (URP) with 2D renderer
- 2D lighting system with colored lights on balls matching brick colors
- Trail renderers for visual ball effects
- Custom shaders and materials

## Key Gameplay Flow
1. Player matches 3+ bricks on the board (match-3 mechanics)
2. Successful matches spawn colored balls from `BallSpawnPoint`
3. Balls inherit color and type from matched bricks
4. Balls interact with scrolling blocks and other physics objects
5. Score increases based on matches and ball interactions

## Unity-Specific Notes
- Project uses Unity 6 (6000.0.45f1)
- 2D project setup with URP and lighting
- Input system configured via `InputSystem_Actions.inputactions`
- PlayFab SDK integrated for player statistics and leaderboards
- Dependencies managed through Unity Package Manager (no package.json)
- Build outputs go to platform-specific folders (not tracked in git)
- **Important:** Do not do defensive coding. If a component like SpriteRenderer is missing, the code should crash rather than silently ignoring the error