# Project Overview
- Game Title: LeghMobile
- High-Level Concept: 2D platformer with wave-based combat and NPC interactions.
- Target Platform: WebGL
- Render Pipeline: URP2D

# Game Mechanics
## Core Gameplay Loop
Players start in a peaceful zone (Grid0), interact with NPCs via a dialogue system, and upon finishing the conversation, transition to a combat zone (Grid1) where they face multiple waves of enemies.

## Controls and Input Methods
- Movement and Interaction: New Input System.
- Dialogue: Triggered by proximity and interaction key, progressed by interaction key.

# Key Asset & Context
- **Scripts**:
  - `Fase7LevelManager.cs`: Controls the wave system and grid management for Fase 7.
  - `DialogueData.cs`: ScriptableObject holding dialogue content and flags.
  - `DialogueSystem.cs`: Manages the display and flow of dialogues.
- **Scene Assets**:
  - `Assets/Scenes/Fase7.unity`: The scene to be modified.
  - `Grid0Group`: Initial safe area.
  - `Grid1`: First combat area.
  - `SpawnGroup`: Container for enemy spawn points.
  - `D7 - Encontro Ermelinda.asset`: The dialogue asset that should trigger the transition.

# Implementation Steps

## 1. Modify `DialogueData.cs`
- **Description**: Add a new boolean flag `triggerLevelStart` to the `DialogueData` ScriptableObject. This will allow specific dialogues to trigger level-specific logic upon completion.
- **Assigned role**: developer
- **Dependencies**: None
- **Parallelizable**: Yes

## 2. Modify `DialogueSystem.cs`
- **Description**: In the `FinishDialogue` method, check if the current `dialogueData` has `triggerLevelStart` set to true. If so, call `Fase7LevelManager.Instance.OnDialogueStartFinished()`.
- **Assigned role**: developer
- **Dependencies**: Step 1
- **Parallelizable**: No

## 3. Modify `Fase7LevelManager.cs`
- **Description**: 
  - Add public fields: `public GameObject grid0Group;` and `public GameObject spawnGroup;`.
  - Update `Start()`: Remove the call to `StartLevel(1)`. Ensure `grid0Group` is active, while `grid1`, `grid2`, `grid3`, `grid4`, and `spawnGroup` are inactive. Clear the `levelUI` and `missingEnemies` text.
  - Implement `public void OnDialogueStartFinished()`: This method will disable `grid0Group`, enable `spawnGroup`, enable `grid1` (via `StartLevel`), and call `StartLevel(1)` to begin the waves and teleport the player.
- **Assigned role**: developer
- **Dependencies**: None
- **Parallelizable**: Yes

## 4. Configure Assets and Scene
- **Description**:
  - Open `Assets/Scenes/Fase7.unity`.
  - Locate the `LEVELMANAGER` object and assign `Grid0Group` and `SpawnGroup` to the new fields in `Fase7LevelManager`.
  - Ensure `Grid1` and `SpawnGroup` are inactive by default in the scene (optional but recommended).
  - Locate `Assets/Scriptable/Dialogue/Fase 7/D7 - Encontro Ermelinda.asset` and check the `triggerLevelStart` boolean.
- **Assigned role**: developer
- **Dependencies**: Step 1, Step 3
- **Parallelizable**: No

# Verification & Testing
- **Manual Test**: Start the game in Fase 7. Verify the player starts in Grid0 and no waves begin.
- **Manual Test**: Interact with the NPC (Ermelinda) and finish the dialogue.
- **Manual Test**: Verify that immediately after the dialogue ends:
  - The player is teleported to the Wave 1 start position.
  - Grid0 disappears.
  - Grid1 and enemy spawners appear.
  - The wave system UI updates and enemies start spawning.
- **Code Check**: Ensure `Fase7LevelManager.Instance` is correctly handled to avoid NullReferenceExceptions if the dialogue ends in a scene without this manager.
