---
name: 3D-Shooter-architecture
description: >
  Use this skill when modifying, debugging, refactoring, or extending
  the 3D-Shooter-master project. This skill provides architectural context,
  component responsibilities, dependencies, state ownership, configuration
  dependencies, architectural guardrails, change-impact guidance,
  modification workflow, validation strategy, failure diagnosis,
  and maintenance rules.
---

# 3D-Shooter — Agent Architecture Guide

## 1. Purpose
This skill provides architectural boundaries, context, and workflows for AI agents working on the `3D-Shooter-master` project. It ensures modifications respect the current (primitive) structure while providing guardrails against the fragile patterns (e.g., lack of null-checking, unpooled instantiations, and hardcoded tags) inherent to this prototype.

## 2. Scope
This architecture covers the Unity C# codebase (`Assets/Scripts/`), scene configurations (`Assets/Scenes/`), global Unity configurations (`ProjectSettings/`), and persistence (`PlayerPrefs`).

## 3. Source of Truth
This document provides architectural context and agent guidance. It is not the ultimate source of truth for implementation details. 

If this skill conflicts with the actual project, apply this priority:
1. Current executable/source implementation (`.cs` files)
2. Unity Scene/Prefab serialized configurations (`.unity`, `.prefab`)
3. Unity Project Settings (e.g., `InputManager.asset`, TagManager)
4. Generated architecture skill (this document)

**Rule:** Prefer the actual project.

## 4. Architecture Overview
**Architectural Style:** Unity Component-Based Architecture (Prototype/Monolithic) + Static Event Bus.

The project relies heavily on Unity's standard `MonoBehaviour` lifecycle. Systems are highly coupled. Cross-component communication is primarily handled via a central static delegate bus (`GameEventManager.cs`). Physical interactions and combat rely heavily on Unity 3D Physics (Raycasts, Colliders) and hardcoded Game Object Tags. There is currently no Object Pooling; lifecycle is managed entirely via raw `Instantiate()` and `Destroy()` calls.

## 5. Architecture Model
```text
[ InputManager.asset ] 
        ↓
[ Player Components ] (Player, Thruster, Rotate, Shield, Laser)
        ↓ (Raycasts / Triggers)
[ Game Entities ] (EnemyMovement, Asteroid, Pickup)
        ↓ (Event Triggers)
[ GameEventManager.cs ] (Static Delegate Bus)
        ↓ (UI & Spawners listen)
[ Systems & UI ] (GameUI, GameScore, GameTimer, EnemySpawner, AsteroidManager)
```

## 6. Component Responsibilities

*   **`Player.cs` / `Thruster.cs` / `Rotate.cs`**: Owns player input polling and 3D transform manipulation.
*   **`Laser.cs`**: Owns raycast-based combat logic for both Player and Enemies.
*   **`Shield.cs`**: Owns health state, regeneration logic, and triggering player death.
*   **`EnemyMovement.cs`**: Owns primitive rule-based steering AI (raycast obstacle avoidance).
*   **`EnemyAttack.cs`**: Owns enemy targeting logic and triggering `Laser.cs`.
*   **`Explosion.cs`**: Owns visual hit feedback, physics knockback (`AddForce`), and entity destruction.
*   **`AsteroidManager.cs` / `EnemySpawner.cs`**: Own global entity instantiation (Spawners).
*   **`Pickup.cs`**: Owns collectible logic and triggering score increments.
*   **`GameEventManager.cs`**: Central static bus decoupling core game state events from UI.
*   **`GameUI.cs` / `GameScore.cs` / `GameTimer.cs` / `ShieldUI.cs`**: Presentation layer reacting to `GameEventManager`.

## 7. State Ownership

*   **Player Health**: Owned strictly by `Shield.cs`.
*   **Game Score**: Owned strictly by `GameScore.cs`.
*   **High Score (Persistent)**: Owned by `GameScore.cs` (read/written to `PlayerPrefs`).
*   **Match Time**: Owned by `GameTimer.cs`.
*   **Target Reference**: Owned locally by `FollowCam.cs`, `EnemyMovement.cs`, and `EnemyAttack.cs` (Duplicated state: each script caches the player transform independently).

**Warning:** The duplicated target caching (`TargetPlayer()` method) across multiple scripts is a known risk. If the player is destroyed, Unity overloads `== null` to `true`, avoiding crashes, but leaving stale references.

## 8. Lifecycle Ownership

*   **Player Lifecycle**: Created by `GameUI.cs` (on `StartGame`). Destroyed by `Shield.cs` / `Explosion.cs` when health < 1.
*   **Enemy Lifecycle**: Created by `EnemySpawner.cs` (via `InvokeRepeating`). Destroyed by `Explosion.cs` or self-destructs via `GameEventManager` when game restarts.
*   **Asteroid Lifecycle**: Mass-created by `AsteroidManager.cs` (1000 at a time). Mass-destroyed upon player death.
*   **Explosions / Lasers**: Ephemeral lifecycle. Created on-demand via `Instantiate()`, destroyed via `Destroy(gameObject, time)`.

## 9. Control Flow & Data Flow

**Primary Core Loop Flow:**
```text
MainMenu 'Play' Button
→ GameEventManager.StartGame()
→ GameUI instantiates Player & hides Menu
→ AsteroidManager spawns grid & Pickups
→ EnemySpawner begins InvokeRepeating
```

**Combat Flow:**
```text
Input (Fire1) 
→ Player.cs 
→ Laser.cs (Physics.Raycast)
→ Hit Object Tag Check ("Enemy" / "Pickup")
→ EnemyMovement.BlowUp() OR Pickup.Collect()
→ GameEventManager.IncrementScore()
→ GameScore.cs updates UI
```

## 10. Configuration & Environment Dependencies

This project relies critically on Unity Editor configurations. Inspect these before modifying logic:

*   **Hardcoded Tags**: `Player`, `Enemy`, `Pickup`, `MainCamera`. Raycasts and caching functions will silently fail if these tags are removed or misspelled in the Editor.
*   **Input Axes (`ProjectSettings/InputManager.asset`)**: Requires standard axes (`Horizontal`, `Vertical`) and custom mappings (`Fire1` for shooting, `Fire3` for thrusting, `Roll` for rotation).
*   **PlayerPrefs**: Uses the string key `"highScore"`.
*   **Inspector Serialized Fields**: Component relationships (like `_lasers` array in `Player.cs` or `_enemyPrefab` in `EnemySpawner.cs`) are wired in the Unity Inspector.

## 11. Architectural Invariants

*   UI components must never reference Game Entities directly; they must listen to `GameEventManager`.
*   Score and Health modifications must pass through `GameEventManager` delegates to ensure UI synchronization.
*   Entities subject to raycast combat must possess colliders and appropriately assigned Unity Tags.
*   Spawners must listen to `OnStartGame` and `OnPlayerDestroyed` to manage their lifecycles.

## 12. Architectural Guardrails

**MUST:**
*   Always use `CompareTag("TagName")` instead of `tag == "TagName"`.
*   Always perform a null check when using `GetComponent<T>()` before invoking methods on the component.
*   Check for `GameEventManager` subscribers (`if (EventName != null)`) before invoking events.

**MUST NOT:**
*   Introduce direct coupling between UI components (Canvas elements) and Gameplay elements (Player/Enemies).
*   Call `GameObject.FindGameObjectWithTag()` inside `Update()` without caching the result.

**SHOULD:**
*   Migrate away from heavy `Instantiate`/`Destroy` patterns toward Object Pooling to resolve severe GC spikes.
*   Consolidate the duplicated `TargetPlayer()` logic into a central Manager.

**EXCEPTION:**
*   You may modify architecture to introduce an `ObjectPoolManager` or `GameManager` if requested to fix performance or coupling, as the current architecture is fragile.

## 13. Decision Rules

*   **IF a feature changes UI text or bars** → Modify the specific UI script (e.g., `ShieldUI.cs`) and hook into `GameEventManager`.
*   **IF adding a new enemy or interactive object** → Ensure it has an `Explosion` component, correct Collider, and the proper Unity Tag for `Laser.cs` to detect it.
*   **IF adding new combat mechanics** → Inspect `Laser.cs` and `Explosion.cs` to understand the raycast-to-component invocation chain.
*   **IF fixing performance issues** → Target `AsteroidManager.cs` (spawns 1000 objects on start) and replace `Destroy`/`Instantiate` in `Explosion.cs` with an Object Pool.

## 14. Change Impact Matrix

*"Inspect" means the component may be affected and must be evaluated, but does not necessarily require modification.*

| Feature | Core Logic (`Player`/`Enemy`) | Spawners (`Asteroid`/`Enemy`) | UI Scripts | Config (Tags/Input) |
| :--- | :--- | :--- | :--- | :--- |
| **New Weapon Type** | Modify | — | — | Configure |
| **New Enemy Type** | Modify | Inspect | — | Configure |
| **New Resource (e.g., Mana)**| Inspect | — | Modify | — |
| **Performance Opt. (Pooling)**| Modify | Modify | — | — |
| **Input System Overhaul** | Modify | — | — | Configure |

## 15. Platform / Framework Specific Dependencies

**Unity Engine specifics:**
*   Uses `Physics.Raycast` (3D Physics engine).
*   Relies on Unity's built-in `Invoke` and `InvokeRepeating` for timing (e.g., `EnemySpawner.cs`, `Laser.cs`).
*   Uses `Vector3.SmoothDamp` and `Quaternion.Slerp` for smooth interpolation.
*   Heavy reliance on the Inspector to assign Prefab references.

## 16. Modification Workflow

1. Understand the request.
2. Identify affected behavior and corresponding tags/inputs.
3. Identify likely components using architecture rules (e.g., Is this UI or Combat?).
4. Inspect actual `.cs` source files.
5. Search for `GetComponent` assumptions that might break.
6. Establish current behavior and baseline.
7. Determine change impact (Check Matrix).
8. Implement the smallest architecture-consistent change.
9. *Crucial:* Add null-checks if interacting with raw components via Raycast hits.
10. Verify there are no syntax or Unity-specific compilation errors.
11. Re-evaluate architectural invariants.
12. Report modified files and any missing Inspector assignments the user will need to configure manually in the Unity Editor.

## 17. Validation Workflow

*   **Static Validation:** Type-check C# code. Ensure Unity namespaces (`UnityEngine`, `UnityEngine.UI`) are included.
*   **Configuration Validation:** Ensure any new Tags or Layers required by the code are communicated to the user. Ensure `[SerializeField]` variables are noted so the user knows to assign them in the Editor.
*   **Architectural Validation:** Ensure UI updates still flow through `GameEventManager` and not via direct object references.

## 18. Common Failure Modes

### NullReferenceException on Combat Hit
*   **Symptoms:** Console throws NRE when firing lasers; lasers stop working.
*   **Likely Causes:** `Laser.cs` calls `hit.transform.GetComponent<EnemyMovement>().BlowUp();` but the object tagged "Enemy" lacks the `EnemyMovement` script.
*   **Diagnostic Order:** Check object Tags -> Check components attached to the Prefab -> Add null check in `Laser.cs`.

### Massive Lag Spike / Freeze
*   **Symptoms:** Game completely freezes for a second upon Player Death or Game Start.
*   **Likely Causes:** `AsteroidManager.cs` instantiating 1,000 objects in a triple `for` loop on start, or destroying/instantiating 1,000 explosions on death.
*   **Diagnostic Order:** Inspect `PlaceAsteroids()` and `DestroyAsteroids()` loops.
*   **Validation:** Suggest migration to Object Pooling or reduced `_asteroidsPerAxis` count.

### Enemies / Camera Stop Tracking
*   **Symptoms:** Enemies fly straight; camera stops following.
*   **Likely Causes:** Tag `"Player"` is missing from the player prefab, causing `TargetPlayer()` to silently fail.
*   **Diagnostic Order:** Check `GameObject.FindGameObjectWithTag("Player")` calls.

## 19. Definition of Done

*   Required `.cs` source files inspected.
*   All new `GetComponent<T>()` calls include null safety checks.
*   No direct coupling introduced between Game Logic and Canvas UI.
*   Hardcoded tags/inputs used match existing project conventions.
*   Build/compile succeeds (C# syntax valid).
*   User has been instructed on any Inspector/Editor variable assignments required by the code changes.
*   Architecture documentation updated if architectural responsibilities changed (e.g., introduction of a GameManager).

## 20. Architecture Evolution Rules

DEFAULT: Preserve the existing EventManager and MonoBehaviour structure.

EXCEPTION: Architecture SHOULD evolve to fix the critical performance and stability issues of this prototype.
*   **Pooling:** Introducing an `ObjectPool` to replace `Instantiate`/`Destroy` is highly encouraged.
*   **Managers:** Consolidating `FindGameObjectWithTag("Player")` into a single `PlayerManager` or `GameManager` singleton/service locator is encouraged to stop redundant polling.

Before changing architecture:
1. Explain why the current design (e.g., 1000 Instantiates) is insufficient.
2. Identify affected components (e.g., `AsteroidManager`, `Explosion`, `EnemySpawner`).
3. Prefer incremental changes (e.g., pool lasers first, then asteroids).

## 21. Maintenance Rules

Update this skill when:
*   A new Core Manager (e.g., `AudioManager`, `PoolManager`) is introduced.
*   The project migrates from the Legacy Input Manager to the New Input System.
*   The `GameEventManager` is replaced by a more robust event system (e.g., ScriptableObject events or UnityEvents).
*   The hardcoded Tags are refactored into constants or serialized properties.

## 22. Non-Goals
This skill does NOT attempt to define:
*   Art direction, material setups, or shader graph specifics (Cartoon FX).
*   Level design geometry or specific layout coordinates.
*   Weapon balancing metrics (damage, fire rates) unless structurally relevant.
