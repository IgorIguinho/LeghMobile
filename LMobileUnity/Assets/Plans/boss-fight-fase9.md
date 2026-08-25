# Project Overview
- Game Title: Legh Mobile
- High-Level Concept: 2D Mobile Action Platformer featuring stage-based levels, enemy encounters, boss fights, and mobile-optimized physics and combat mechanics.
- Players: Single player
- Inspiration / Reference Games: Classic 2D action platformers (e.g., Megaman, Castlevania, Hollow Knight).
- Tone / Art Direction: 2D Pixel / Stylized action adventure.
- Target Platform: WebGL / Mobile (Android & iOS)
- Screen Orientation / Resolution: Landscape (1920x1080 / 16:9)
- Render Pipeline: Built-in Render Pipeline (with optional URP Light2D compatibility)

# Game Mechanics
## Core Gameplay Loop
In Stage 9 (Fase 9), the player enters a dedicated boss arena. The boss operates on a 4-state Finite State Machine (FSM):
1. **Attack Sequence State (Sequência de Ataque)**:
   - The boss flies/floats smoothly directly between arena waypoints (`waypoints`).
   - Boss aims and fires a sequence of projectiles targeted at the player (`timeBetweenShots`, `shotsPerSequence` determined by `BossPhaseData`).
   - Boss is **totally immune to damage** during this state (ignores `TakeDamage`).
2. **Vulnerable State (Vulnerável)**:
   - The boss stops at its current waypoint and enters an opening window for a fixed duration (`vulnerabilityDuration`).
   - Visual cues trigger: SpriteRenderer color shift / flash (`vulnerableColor`, e.g., yellow) and optional light activation (`bossLight`).
   - In this state, `BossController` accepts damage via `IDamageable.TakeDamage(int damage)`.
   - If the player does not attack within `vulnerabilityDuration`, the boss returns to `AttackSequence`.
3. **Hit Reaction State (Reação a Dano)**:
   - Triggered immediately upon receiving damage in `Vulnerable` state.
   - The boss evaluates current health against the phase thresholds (`phaseSettings`) to increase intensity (shots per sequence, speed).
   - The boss executes a high-speed flight/retreat towards the waypoint furthest away from the player (`runSpeed` / `fleeSpeed`).
   - During this retreat, the boss **does not inflict contact damage** to the player and remains immune.
   - Upon arriving at the destination waypoint, the boss resets its visual tint and transitions back to `AttackSequence`.
4. **Dead State (Derrotado)**:
   - Triggered when health reaches 0.
   - Stops all movement and shooting routines.
   - Invokes completion sequence: opens concluding dialogue if configured (`PlayerInteract.CanOpenDialogue(true, deathDialogue)`) and activates the level completion trigger object (`finishLevelObject.SetActive(true)`).

## Controls and Input Methods
- Mobile touch / On-screen buttons or Keyboard inputs via `InputReader`.
- Player attacks with sword/melee.
- **Projectile Parry / Destruction Mechanic**: In `PlayerAttack.cs`, attacks detect enemy projectiles on the configured `projectileLayer` (using zero-allocation `ContactFilter2D` and `Physics2D.OverlapBox`) and cleanly destroy/deactivate them.

# UI
- **Boss Health Slider**: Optional `Slider` reference in `BossController` (matching `Fase7Boss` pattern), initialized with `maxHealth` and updated on damage.
- **Visual Feedback**: Real-time color tinting on `SpriteRenderer` and optional `bossLight` component for vulnerability cues.

# Key Asset & Context
- **`Assets/Scripts/Enemys/BossController.cs`**:
  - Main script handling FSM (`BossState`: `AttackSequence`, `Vulnerable`, `HitReaction`, `Dead`), health, fly movement, shooting, phase transitions, and zero-allocation mobile optimization.
  - Implements `IDamageable`.
  - Serialized struct `BossPhaseData`: `int healthThreshold`, `int shotsPerSequence`, `float moveSpeed`, `float timeBetweenShots`.
  - Inspector configuration:
    - `int maxHealth = 10`
    - `Transform[] waypoints`
    - `BossPhaseData[] phaseSettings`
    - `float defaultFlySpeed = 4f`
    - `float fleeSpeed = 8f`
    - `float vulnerabilityDuration = 2.5f`
    - `Color normalColor = Color.white`, `Color vulnerableColor = Color.yellow`
    - `Behaviour bossLight` (null-safe check)
    - `GameObject projectilePrefab`
    - `Transform firePoint`
    - `LayerMask playerLayer`
    - `float playerDetectRadius = 20f`
    - `DialogueData deathDialogue`
    - `GameObject finishLevelObject`
    - `Slider bossHealthSlider`
- **`Assets/Scripts/Player/PlayerAttack.cs`**:
  - Adds `[SerializeField] private LayerMask projectileLayer;`
  - Pre-allocated `Collider2D[] projectileHitBuffer = new Collider2D[8];` and `ContactFilter2D projectileFilter;` initialized in `Awake()`.
  - Destroys or releases to `Fase7PoolManager` any hit projectile during the attack execution window.
- **`Assets/Scripts/Enemys/IDamageable.cs`**: Existing interface (`void TakeDamage(int damage)`).
- **`Assets/Scripts/Enemys/ProjectileEnemyComplex.cs`**: Reference projectile script.

# Implementation Steps

1. **Implement `BossController.cs`**
   - **Description**: Create `Assets/Scripts/Enemys/BossController.cs` implementing the 4-state FSM (`AttackSequence`, `Vulnerable`, `HitReaction`, `Dead`), smooth flight movement between waypoints, targeted projectile shooting, phase updates, immunity logic, dialogue/finish triggers on death, and zero-allocation mobile physics checks.
   - **Assigned role**: developer
   - **Dependencies**: None
   - **Parallelizable**: No

2. **Update `PlayerAttack.cs` for Projectile Parry / Destruction**
   - **Description**: Add `projectileLayer`, pre-allocated buffer/`ContactFilter2D`, and projectile destruction logic in `PlayerAttack.cs` without altering existing enemy or box attack logic.
   - **Assigned role**: developer
   - **Dependencies**: None
   - **Parallelizable**: Yes

3. **Validate Compilation and Architecture**
   - **Description**: Verify clean compilation of both scripts in the Unity project without compiler errors, missing namespace issues, or runtime exceptions.
   - **Assigned role**: developer
   - **Dependencies**: Step 1, Step 2
   - **Parallelizable**: No

# Verification & Testing
- **Unit & Logic Checks**:
  1. Verify zero compilation errors across `BossController.cs` and `PlayerAttack.cs`.
  2. Verify that `BossController` implements `IDamageable` and rejects damage when not in `Vulnerable` state.
  3. Verify that `BossController` calculates the furthest waypoint from the player during `HitReaction` and travels there at `fleeSpeed`.
  4. Verify that `PlayerAttack.cs` detects objects on `projectileLayer` without GC allocation (`Physics2D.OverlapBox` buffer) and safely destroys/deactivates them.
  5. Verify `bossLight` and `deathDialogue` null safety when unassigned in the inspector.
  6. Verify proper phase progression (transitioning `shotsPerSequence` and speed when health drops below thresholds).
