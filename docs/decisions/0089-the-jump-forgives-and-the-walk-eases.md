# 0089 — The jump forgives, its height follows Space, the walk eases in and out

- Status: accepted
- Date: 2026-10-21

## Context

The hero's controller was ported from the TypeScript as it was: a jump only from the ground on the exact step Space went
down, always to its full height on one symmetric arc, and a walk at full speed from the first step and dead still on the
next. It reads stiff next to the platformers it is compared with (Celeste, Terraria, Hollow Knight). A research page
compared the controller with a proposed one in a JS copy of it, step for step, and the user approved the proposals with
"numbers: as proposed". This decision is their physics part; ladders, zero-g and the camera are decided apart.

## Decision

All of it stays in the client controller (`game/features/crew/Hero.cs`), one deterministic integer step at 60 a second as
before; every number is a constant in `Stillwake.Content.Hero`.

- **Coyote time and a jump buffer, 100 ms each (6 steps).** A jump still pushes off up to `Coyote` steps after the feet
  left the ground; a press in the air jumps on landing up to `JumpBuffer` steps later. The hero carries both timers and
  whether its jump is rising; a jump spends them, so a press makes one jump.
- **The height follows Space.** The controller reads Space held (`HeroInput.JumpHeld`). Let go while rising, the rise speed
  halves (`JumpCut` 500); near the top (`Apex`, under 60 up or down) with Space held, gravity is 0.6 of itself
  (`ApexGravity`); falling, 1.15 (`FallGravity`). `Jump` goes from 420 to 415, so a held jump still clears three cells and
  not four: 3954 thousandths, against 3990 before. A tap rises 1222.
- **A planet's jump comes down quicker:** under a planet's own gravity a fall gains 1.5 of it (`PlanetFallGravity`), so the
  six-cell jump (6014) takes about a second (61 steps, 33 up and 28 down) and does not float down.
- **The walk eases:** the speed across carries between steps and moves toward what the keys ask by `WalkAccel` 30 (full
  walk in 4 steps, 67 ms), `RunAccel` 32 (full run in 7, 117 ms) and `Stop` 30 (a walk stops in 4, a run in 8, 133 ms).
  In the air each is 0.65 of itself (`AirControl`). Only what stops the body takes its speed: a walk that ends where the
  body drops off an edge (the controller ends it there, so a hole one cell wide is fallen into) keeps it.
- **Others see a tap jump too:** a jump is read from the motion when it rises at a quarter of `Jump`, not half, since a
  tap's rise is halved a step in.

Multipliers are integer thousandths, rounded down, so every step is whole numbers: 21 → 12 at the top and 24 falling
aboard, 14 → 8 and 21 on the planet. The proposal's JS used floats; rounded down, its heights come out the same to the
hundredth of a cell (3.95 and 6.01).

## Deliberately left out

- **Ladders, zero-g, the camera, corner correction and a ledge assist** were on the same page; they are other changes.
- **A door's push carries no speed:** pushed out of a door shut on it, the hero stops beside it rather than walking on.
  The harpoon's pull and a swim do carry theirs into gravity, as a fall carries its speed.
- **Space held while just falling** eases the fall at its first slow steps as at a jump's top, as the proposal does: it
  needs no rule of its own, and nobody is hurt by a fall that starts a few thousandths softer.
- **The timers are not sent to others.** Others' heroes are drawn from sampled positions and speeds (0002); nothing
  there needs them.
- **The run's stop rounds to 8 steps (133 ms)**, a step over the proposal's "about 120": its float rate left 11
  thousandths for the 8th step too.
