# Project Overview
- **Game Title**: LeghMobile
- **High-Level Concept**: A highly responsive 2D side-scrolling platformer for WebGL and mobile devices where the player unlocks distinct movement, combat, and passive skills to progress through challenging levels.
- **Players**: Single-player
- **Inspiration / Reference Games**: Celeste, Hollow Knight, Mega Man
- **Tone / Art Direction**: Retro 2D platformer with clean, responsive movement, colorful mechanics, and polished impact feedback.
- **Target Platform**: Mobile (iOS & Android) + WebGL
- **Screen Orientation / Resolution**: Landscape (1920x1080)
- **Render Pipeline**: Built-in Render Pipeline

---

# Game Mechanics

## Core Gameplay Loop
The player navigates through levels (fases) with platforming challenges, avoiding hazards and defeating enemies. Progression is driven by finding unlock triggers that grant new mechanics. These mechanics allow access to new areas and alter how the player tackles obstacles and fights enemies.

## Controls and Input Methods
- **On-Screen Buttons & Touch / Keyboard Inputs**: 
  - Movement (A/D or Left/Right Arrows or joystick)
  - Jump (Space or South button)
  - Dash / Spear Dash (Q key or East button)
  - Attack (Left Mouse or West button)
- **Mobile Polish Focus**: Smooth physics transitions, visual indicators for cooldowns, camera shake on impact, and hit-stop visual feedback to enhance gamplay responsiveness on touch screens.

---

# UI
- **Active Mechanic Indicators**: A visual indicator or button state update (e.g., `buttonDash` color modifications already in `PlayerMovements.cs`) to show mechanic availability.
- **Unlock Feedback**: Brief HUD text overlay or dialogue popup when a new mechanic trigger is activated, keeping the player informed of their new abilities.

---

# Key Asset & Context

### 1. `SkillType.cs` (New Enum)
Defines the skills that the player can unlock and use.
```csharp
public enum SkillType
{
    Sword,
    Dash,
    FireBall,
    Invocation,
    Spear
}
```

### 2. `PlayerSkillsManager.cs` (New Component)
Maintains the unlocked states persistently in `PlayerPrefs` and exposes properties for active setups.
```csharp
using UnityEngine;

public class PlayerSkillsManager : MonoBehaviour
{
    public static PlayerSkillsManager Instance { get; private set; }

    [Header("Persistence Prefix")]
    [SerializeField] private string prefsPrefix = "LeghSkill_";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void UnlockSkill(SkillType skill)
    {
        PlayerPrefs.SetInt(prefsPrefix + skill.ToString(), 1);
        PlayerPrefs.Save();
    }

    public bool IsSkillUnlocked(SkillType skill)
    {
        // For existing mechanics, we can default them to unlocked if appropriate,
        // or require them to be unlocked via triggers.
        // For standard Dash, we default to unlocked or check PlayerPrefs.
        return PlayerPrefs.GetInt(prefsPrefix + skill.ToString(), 0) == 1;
    }

    public void ResetAllSkills()
    {
        foreach (SkillType skill in System.Enum.GetValues(typeof(SkillType)))
        {
            PlayerPrefs.DeleteKey(prefsPrefix + skill.ToString());
        }
        PlayerPrefs.Save();
    }
}
```

### 3. `MechanicUnlockTrigger.cs` (New Component)
Placed on any scene trigger to unlock a specific mechanic.
```csharp
using UnityEngine;

public class MechanicUnlockTrigger : MonoBehaviour
{
    [SerializeField] private SkillType skillToUnlock;
    [SerializeField] private GameObject unlockVfxPrefab;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerSkillsManager.Instance.UnlockSkill(skillToUnlock);
            
            if (unlockVfxPrefab != null)
            {
                Instantiate(unlockVfxPrefab, transform.position, Quaternion.identity);
            }
            
            // Optionally play a sound, dialogue, or disable trigger
            gameObject.SetActive(false);
        }
    }
}
```

### 4. `EnemyStats.cs` (Modified Component)
Introduce configurable HP and damage-receiving logic so the Spear can deal configurable damage, falling back to `Death()` if HP reaches 0.
```csharp
using UnityEngine;

public class EnemyStats : MonoBehaviour
{
    [SerializeField] private int maxHealth = 1;
    private int currentHealth;

    private void Awake()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        if (currentHealth <= 0)
        {
            Death();
        }
    }

    public void Death()
    {
        Destroy(this.gameObject);
    }
}
```

### 5. `FollowCam.cs` (Modified Component)
Support camera screen shake.
```csharp
using System.Collections;
using UnityEngine;

// Add these to FollowCam:
private float shakeDuration = 0f;
private float shakeMagnitude = 0.1f;
private Vector3 shakeOffset = Vector3.zero;

public void TriggerShake(float duration, float magnitude)
{
    shakeDuration = duration;
    shakeMagnitude = magnitude;
}

// Update transform logic to apply shake offset in LateUpdate or FixedUpdate:
// Vector3 targetPos = ... + shakeOffset;
```

---

# Implementation Steps

## Step 1: Create unified Skills System (Enum, Manager & Unlock Trigger)
- **Description**: Implement `SkillType` enum, `PlayerSkillsManager` to persist unlocked abilities in `PlayerPrefs`, and `MechanicUnlockTrigger` to unlock abilities in level. Add `PlayerSkillsManager` to the Player prefab.
- **Assigned role**: developer
- **Dependencies**: None
- **Parallelizable**: No

## Step 2: Implement Camera Shake in `FollowCam`
- **Description**: Add screen shake support to `FollowCam.cs` with a public `TriggerShake(float duration, float magnitude)` function that applies a small randomized offset during rendering.
- **Assigned role**: developer
- **Dependencies**: None
- **Parallelizable**: Yes

## Step 3: Upgrade `EnemyStats` with damage support
- **Description**: Upgrade `EnemyStats.cs` with a `TakeDamage(int damage)` method and a configurable `maxHealth` field. Keep the existing `Death()` functionality intact.
- **Assigned role**: developer
- **Dependencies**: None
- **Parallelizable**: Yes

## Step 4: Implement Spear Dash and Mobile Polish in `PlayerMovements`
- **Description**: 
  - Define custom configurable fields for Spear Dash in the inspector: `spearVisual` GameObject, `spearArea` / `spearLength` Vector2 collision offsets, `spearDamage` int, `enemyKnockbackForce` float, `playerKnockbackForce` float, `hitStopDuration` float, and `shakeMagnitude` float.
  - Modify the `Dash()` coroutine or implement a specialized `SpearDash()` routine.
  - Check if `SkillType.Spear` is unlocked in `PlayerSkillsManager`. If unlocked, active Spear Visual, perform Physics2D OverlapBox detection during the dash on `enemyLayer`.
  - On collision with an "Enemy":
    - Call `TakeDamage` on the enemy.
    - Check for `Rigidbody2D` on the enemy and apply horizontal knockback force in the dash direction.
    - Cancel dash, re-enable gravity.
    - Apply a backwards knockback impulse to the Player's `Rigidbody2D` (opposite to movement direction).
    - Trigger Screen Shake on `FollowCam` and Hit-Stop (brief pause in time scale).
  - Cleanly toggle `spearVisual` off on dash exit/cancel.
- **Assigned role**: developer
- **Dependencies**: Step 1, Step 2, Step 3
- **Parallelizable**: No

## Step 5: Configure the Player Prefab in Inspector
- **Description**: Open Player prefab, add the `PlayerSkillsManager` script, create a child GameObject representing the `SpearVisual` (with a placeholder spear sprite/visual), and assign the fields in `PlayerMovements` (Spear Visual, Area, Spear Damage, Knockbacks, etc.).
- **Assigned role**: developer
- **Dependencies**: Step 4
- **Parallelizable**: No

---

# Verification & Testing

### 1. Skill Unlock Test
- Place a `MechanicUnlockTrigger` in a test level configured to unlock the `Dash` and another for `Spear`.
- Verify entering the triggers unlocks the corresponding abilities. Check if the unlocked states persist across loading another level or restarting the play session.

### 2. Standard Dash Test
- Verify that before unlocking `Dash`, the Player cannot dash.
- Verify that after unlocking `Dash` (but not `Spear`), the standard dash works perfectly without any errors, and trails / buttons behave exactly as before.

### 3. Spear Dash Collision & Damage Test
- Unlock both `Dash` and `Spear`.
- Perform a dash into an enemy. Verify that:
  - The Spear Visual is enabled during the dash and disabled immediately upon collision or when the dash ends.
  - The enemy takes damage/dies.
  - The enemy is knocked back (if Rigidbody2D is present).
  - The dash is cancelled immediately.
  - The player receives a small backwards impulse.
  - The camera shakes on impact.
  - Hit-stop briefly pauses time scale, creating a highly polished mobile action feel.

### 4. Regression Test
- Verify that standard movement, jump, gravity inversion zones, fast-speed zones, and platform crushes continue to work flawlessly without compilation or runtime errors.
