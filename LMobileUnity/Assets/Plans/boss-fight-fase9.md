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
In Stage 9 (Fase 9), the player enters a dedicated boss arena. The boss operates on a 3-state Finite State Machine (FSM):
1. **Attack Sequence State (Sequência de Ataque)**: The boss randomly repositions between waypoints around the arena, firing a sequence of projectiles targeted at the player.
2. **Vulnerable State (Vulnerável)**: The boss stops at a waypoint and becomes vulnerable for a short duration (`vulnerabilityDuration`). A visual cue (light/sprite flash) indicates this opening. The player must strike the boss using sword or fire attacks.
3. **Hit Reaction State (Reação a Dano)**: Upon taking damage via the `IDamageable` interface, the boss triggers a fast run across the arena, updates its phase settings based on current health from the progression table (`phaseSettings`), and resumes the attack sequence with increased intensity (more shots per sequence).

## Controls and Input Methods
- Mobile touch / On-screen buttons or Keyboard inputs via `InputReader`.
- Player attacks with sword/fire. Player sword attacks hit objects on the `enemyLayer` (`IDamageable`) and can deflect/destroy enemy projectiles on `EnemyProjectile` layer (Parry mechanic).

# UI
- Boss Health display / UI integration (optional slider connection similar to `Fase7Boss` health slider).
- Visual feedback on boss vulnerability (light flash / color tint).

# Key Asset & Context
- **`Assets/Scripts/Enemys/BossController.cs`**: New primary monolithic script handling FSM, health (`IDamageable`), movement between waypoints, shooting, lighting triggers, and mobile optimization.
- **`Assets/Scripts/Enemys/IDamageable.cs`**: Existing interface (`void TakeDamage(int damage)`). `BossController` implements `IDamageable`.
- **`Assets/Scripts/Enemys/ProjectileEnemyComplex.cs`**: Existing projectile script used for boss shots.
- **`Assets/Scripts/Enemys/AtiradorEnemy.cs`**: Reference code for shooting logic and targeting.
- **`Assets/Scripts/Player/PlayerAttack.cs`**: Updated to support parrying/destroying boss projectiles filtered by layer mask (`projectileLayer`).

# Implementation Steps

1. **Create `BossController.cs` script**
   - **Description**: Implement the `BossController` class in `Assets/Scripts/Enemys/BossController.cs`.
     - Implement `IDamageable` interface (`TakeDamage(int damage)`).
     - Define `BossPhaseData` struct with `healthThreshold` and `shotsPerSequence`.
     - Expose inspector parameters: `waypoints` (`Transform[]`), `phaseSettings` (`BossPhaseData[]`), `timeBetweenShots`, `vulnerabilityDuration`, `runSpeed`, `bossLight` (`Component` or `Behaviour` with `if (bossLight != null)` safety checks for URP/Built-in compatibility), `projectilePrefab`, `spawnPoint`, `playerLayer`, `playerDetectRadius`.
     - Implement 3-state FSM (`AttackSequence`, `Vulnerable`, `HitReaction`, `Dead`).
     - Implement zero-allocation mobile optimizations: pre-cached `ContactFilter2D`, pre-allocated buffer for `Physics2D.OverlapCircleNonAlloc`, cached `WaitForSeconds` timers, cached component references (`Transform`, `SpriteRenderer`, `Rigidbody2D`, `Animator`).
   - **Assigned role**: developer
   - **Dependencies**: None
   - **Parallelizable**: No

2. **Update `PlayerAttack.cs` for Projectile Parry / Layer Filtering**
   - **Description**: Add a `projectileLayer` `LayerMask` field and non-alloc overlap check (`Physics2D.OverlapBox` with `ContactFilter2D`) inside `PlayerAttack.cs` to detect and destroy/deactivate enemy projectiles on attack.
   - **Assigned role**: developer
   - **Dependencies**: Step 1
   - **Parallelizable**: No

3. **Validate Compilation and API Usage**
   - **Description**: Verify `BossController` and `PlayerAttack` compile cleanly without syntax, assembly, or null reference issues.
   - **Assigned role**: developer
   - **Dependencies**: Step 1, Step 2
   - **Parallelizable**: No

# Verification & Testing
- **Unit / Manual Checks**:
  1. Verify `BossController` compiles without errors and implements `IDamageable`.
  2. Verify null-check logic on `bossLight` when no `Light2D` component is assigned.
  3. Test FSM state transitions: Attack Sequence -> Vulnerable -> Hit Reaction -> Next Phase / Death.
  4. Test player sword attack interaction with `BossController` during Vulnerable state.
  5. Test projectile destruction/parry when player attacks projectiles on `EnemyProjectile` layer.
  6. Verify zero garbage allocation in `Update()` during state transitions and target acquisition.
