```text
 _____ ____    ____  _                 _
|___ /|  _ \  / ___|| |__   ___   ___ | |_ ___ _ __
  |_ \| | | | \___ \| '_ \ / _ \ / _ \| __/ _ \ '__|
 ___) | |_| |  ___) | | | | (_) | (_) | ||  __/ |
|____/|____/  |____/|_| |_|\___/ \___/ \__\___|_|

         Fly. Fight. Survive.
```

[![MIT Licence](https://img.shields.io/badge/licence-MIT-blue.svg)](LICENSE)

A 3D space shooter game built with Unity. Pilot your ship through an asteroid field, destroy enemies, and collect pickups for score.

## What is This?

3D Shooter is a Unity game where you control a spaceship in an asteroid-filled environment. Features include:

- Full 6DOF flight controls (pitch, yaw, roll)
- Raycast-based laser weapons
- Enemy AI with pathfinding and obstacle avoidance
- Procedurally generated asteroid field (1000 asteroids)
- Health/shield system with regeneration
- Score tracking with high score persistence

## Prerequisites

| Requirement | Version |
|-------------|---------|
| Unity Editor | 2021.3 LTS or later |

## Getting Started

1. Clone the repository:
   ```bash
   git clone https://github.com/DarrenBenson/3D-Shooter.git
   ```

2. Open in Unity Hub:
   - Click "Open" and select the cloned folder
   - Unity will import assets automatically

3. Open the main scene:
   - Navigate to `Assets/Scenes/Game.unity`
   - Press Play

## Controls

| Action | Input |
|--------|-------|
| Thrust forward/back | W / S |
| Yaw left/right | A / D |
| Pitch up/down | Arrow Up / Down |
| Roll | Q / E |
| Fire lasers | Left Mouse |
| Boost | Spacebar |

## Game Mechanics

| Mechanic | Details |
|----------|---------|
| Player health | 10 HP, regenerates 1 HP every 2 seconds |
| Enemy spawn | Every 5 seconds |
| Pickup score | 100 points |
| Enemy kill | 50 points |
| Laser cooldown | 2 seconds |

## Project Structure

```
Assets/
  Scripts/           # Game logic (19 C# scripts)
  Scenes/            # Game.unity (main), Test.unity
  Prefabs/           # Reusable game objects
  Materials/         # Laser and trail effects
  Asset Store/       # Third-party assets
```

## Architecture

The game uses an **event-driven architecture** via `GameEventManager.cs`:

- `OnStartGame` - Initialises all systems
- `OnPlayerDestroyed` - Triggers game over
- `OnUpdateHealthBar` - Shield updates
- `OnIncrementScore` - Score additions
- `OnRespawnPickup` - Pickup respawn

Systems subscribe to events rather than holding direct references, enabling loose coupling.

## Asset Credits

- **SpaceshipCollectionPack** - Ship models
- **Gems Ultimate Pack** - Pickup visuals (AurynSky)
- **LowPolyRockPack** - Asteroid models (BrokenVector)
- **Cartoon FX** - Particle effects (JMO Assets)

## Licence

MIT Licence - see [LICENSE](LICENSE) for details.
