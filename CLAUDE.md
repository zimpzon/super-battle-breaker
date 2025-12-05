# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview
TilemapLighting is a Unity 2D project focused on tilemap-based lighting systems. The project uses Unity 6000.0.45f1 and Universal Render Pipeline (URP) for 2D rendering.

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
├── Script/          # C# scripts
│   └── Balls/       # Ball-related game logic
├── Scenes/          # Unity scene files
│   └── SampleScene.unity
├── Settings/        # URP and rendering settings
├── Tilemap/         # Tilemap assets and palettes
└── Material/        # Physics materials and shaders
```

## Key Architecture Components

**Rendering:**
- Uses Universal Render Pipeline (URP) with 2D renderer configuration
- URP settings located in `Assets/Settings/URP_2DRenderer.asset`
- Configured for 2D lighting and tilemap rendering

**Scripts:**
- `Assets/Script/Balls/BallScript.cs` - Basic MonoBehaviour template (currently empty implementation)
- Scripts follow Unity C# conventions with MonoBehaviour inheritance

**Physics:**
- Physics materials in `Assets/Material/Physics/`
- 2D physics setup with custom materials for ball interactions

**Tilemap System:**
- Tilemap palettes in `Assets/Tilemap/Palettes/`
- BlocksPalette.prefab for tile management

## Unity-Specific Notes
- Project uses Unity 6 (6000.0.45f1)
- 2D project setup with URP
- Input system configured via `InputSystem_Actions.inputactions`
- No package.json - dependencies managed through Unity Package Manager
- Build outputs go to platform-specific folders (not tracked in git)