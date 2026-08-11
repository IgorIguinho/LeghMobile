# Project Overview
- Game Title: Legh Mobile (Fase 7 Boss Fight)
- High-Level Concept: Boss fight in a multi-platform environment where the boss reacts to the player's vertical position.
- Players: Single player
- Target Platform: WebGL / Mobile
- Render Pipeline: Built-in

# Game Mechanics
## Core Gameplay Loop
The player fights a boss (Fase7Boss) that has different attack routines. The boss now detects which of the 4 platforms the player is on and adjusts its behavior: walking and shooting if the player is on a different level, jumping to the player's level for melee attacks, or using a specific charge attack if the player is on the highest platform (Platform 4).

## Controls and Input Methods
- New behavior is automated based on player position relative to 4 reference GameObjects defined in the Inspector.

# UI
- No changes to existing UI (Boss health bar already exists).

# Key Asset & Context
- **Fase7Boss.cs**: Main script to be modified.
- **Fase7BossConfig.cs**: ScriptableObject to store new configuration parameters (cooldowns, forces, section indices).
- **Platform Reference Objects**: 4 GameObjects in the scene representing the floor/platform levels.

# Mudanças no Comportamento do Boss
Esta seção descreve como as novas regras afetarão o fluxo de combate do Boss na Fase 7:

1.  **Consciência Espacial**: O Boss agora "sabe" em qual das 4 plataformas o jogador está. Ele não agirá mais de forma puramente aleatória se o jogador estiver em uma altura diferente.
2.  **Perseguição Estática (Walk & Shoot)**: Se o jogador subir ou descer para uma plataforma diferente, o Boss não tentará simplesmente andar em sua direção no mesmo nível. Em vez disso, ele patrulhará sua plataforma atual por 2 segundos (ajustável) e disparará um projétil, forçando o jogador a se movimentar.
3.  **Ataque Melee com Salto**: O Boss agora pode "caçar" o jogador verticalmente. Se ele decidir realizar um ataque corpo a corpo e o jogador estiver em outra plataforma (exceto a 4), o Boss dará um pulo para a plataforma do jogador antes de desferir o golpe.
4.  **Zona de Perigo (Plataforma 4)**: Se o jogador tentar se refugiar na Plataforma 4 (a mais alta/específica), o Boss interromperá seus ataques normais e usará imediatamente o ataque de Carga (Beam System) na seção configurada. O Boss é programado para nunca saltar para esta plataforma, mantendo-a como uma zona de mecânica específica.
5.  **Transições de Estado**: O sistema de escolha de ações continua funcionando, mas é filtrado pela posição do jogador, tornando a luta mais dinâmica e menos previsível apenas pela distância horizontal.

# Implementation Steps

## Step 1: Update Configuration Script
**Description**: Add new parameters to `Fase7BossConfig` to allow easy tuning of the new behaviors.
**Assigned role**: developer
**Dependencies**: None
**Parallelizable**: Yes
- Add `float walkDurationDifferentPlatform` (default 2s).
- Add `int platform4ChargeSection` (to specify which beam section to trigger).
- Add `float jumpForceY` and `float jumpForceX` for the platform jump.

## Step 2: Define Platforms in Fase7Boss
**Description**: Modify `Fase7Boss` to include references to the 4 platforms and internal state for tracking current platform.
**Assigned role**: developer
**Dependencies**: Step 1
**Parallelizable**: No
- Add `public Transform[] platformReferences = new Transform[4]` (Index 0-2 for normal platforms, Index 3 for Platform 4).
- Add a helper method `int GetPlatformIndex(Transform target)` that returns the index of the platform closest to the target's Y position.
- Add `BossState.WalkingToShoot` to the `BossState` enum.

## Step 3: Implement Platform-Aware Decision Logic
**Description**: Modify `DecideNextAction` and `FixedUpdate` to handle the new rules.
**Assigned role**: developer
**Dependencies**: Step 2
**Parallelizable**: No
- In `FixedUpdate`, when `state == Idle`, check if `playerPlatform != bossPlatform`.
- **Rule 1 (Platform 4)**: If `playerPlatform == 3`, trigger `ChargeAttackRoutine(config.platform4ChargeSection)` immediately.
- **Rule 2 (Different Platform Walk/Shoot)**: If `playerPlatform != bossPlatform` (and not 3), enter `WalkingToShoot` state.
- **Rule 3 (Melee Jump)**: Modify the Melee action logic: if the player is on a different platform (and not 3), perform a jump routine before the melee attack.

## Step 4: Implement Walk and Shoot Routine
**Description**: Create a coroutine that handles walking back and forth for a specific duration and then shooting.
**Assigned role**: developer
**Dependencies**: Step 3
**Parallelizable**: No
- Implement `WalkAndShootRoutine`:
    - Walk for `config.walkDurationDifferentPlatform` (2 seconds).
    - Use `ShootProjectile()`.
    - Return to `Idle`.

## Step 5: Implement Jump to Platform Logic
**Description**: Implement the physics/movement for the boss to jump between platforms.
**Assigned role**: developer
**Dependencies**: Step 3
**Parallelizable**: No
- Implement `JumpToPlatform(int targetIndex)`:
    - Apply `jumpForceY` and a horizontal force towards the player.
    - Prevent jumping if the target is Platform 4.
    - Transition to `AttackingMelee` upon landing or reaching height.

# Verification & Testing
- **Manual Test**: Place the player on each of the 4 platforms and observe boss reaction.
- **Edge Case**: Verify the boss *never* jumps to Platform 4 even if the player stays there.
- **Physics Test**: Ensure the boss jump force is sufficient to reach the platforms without overshooting significantly (tunable via Config).
- **State Test**: Ensure the boss returns to `Idle` and resumes normal decision making after the special routines.
