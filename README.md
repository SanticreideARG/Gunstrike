# GunStrike

2D platformer with full-rotation aiming (Soldat-style), ballistic physics and ragdoll characters — built in pure **.NET 9** without a game engine.

## Stack

| Layer | Library |
|---|---|
| Rendering / Window | [Raylib-cs](https://github.com/chrisdill/raylib-cs) 6.1 |
| Physics (Box2D) | [Aether.Physics2D](https://github.com/nkast/Aether.Physics2D) 2.1 |

## Features (current)

- **3-layer parallax** background system
  - Plane 1 — Sky (scroll factor 0.04)
  - Plane 2 — Mid distance: Mountains / Dunes / Buildings (0.20)
  - Plane 3 — Map with full physics (1.00)
- **Ragdoll character** — 10 rigid-body segments, RevoluteJoints, 275 px tall
- Toggle between **animated** and **ragdoll** physics modes (`R` key)
- **Full-rotation aiming** via mouse
- Smooth-follow camera

## Controls

| Key | Action |
|---|---|
| `A` / `D` | Move |
| `Space` | Jump |
| `Mouse` | Aim |
| `LMB` | Shoot *(coming soon)* |
| `R` | Toggle ragdoll |

## Build & Run

```bash
dotnet run --project src/GunStrike.Game
```

Requires **.NET 9 SDK**.

## Project Structure

```
src/GunStrike.Game/
  Core/           GameLoop, GameConstants
  Physics/        PhysicsWorld (Aether wrapper, unit conversion)
  Rendering/      GameCamera, LevelRenderer, ParallaxLayer/System
  Input/          InputHandler
  Entities/       BodyPart, RagdollBuilder, PlayerEntity
  Assets/         (sprites, levels — WIP)
```

## Roadmap

- [ ] Ballistic projectile system
- [ ] Ground contact listener
- [ ] Walk-cycle keyframe animation
- [ ] Sprite-based character rendering
- [ ] JSON tilemap level loading
- [ ] Enemies with ragdoll death
