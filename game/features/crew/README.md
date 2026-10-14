# crew (client): the hero

This machine's hero, ported from `apps/game/src/features/crew/` (G-07). Heroes are client-authoritative
(0002): the controller runs here, outside the sim, against the ship's cells, and nothing it does is a
command but what E presses.

## Files

| File               | Holds                                                                                   | Godot |
| ------------------ | --------------------------------------------------------------------------------------- | ----- |
| `Hero.cs`          | the controller (`hero.ts`): `Hero`, `HeroInput`, `Ship`, `Point`; one `Step` at 60 a second | no    |
| `Rig.cs`           | the figure's joints and limbs in pixels, `EndOf` and `Reach` (`rig.ts`)                 | no    |
| `Pose.cs`          | `Pose`, `Limbs`, `PoseOf`, `LimbsOf`, the head's turn and facing (`pose.ts`, `lookOf`, `faceOf`) | no    |
| `Aim.cs`           | reach, what the pointer aims at and what E sends (`aim.ts`, doors and valves only)      | no    |
| `Tether.cs`        | the tether as drawn, a reeled rope (`tether.ts`) on `game/render/Rope.cs` (`rope.ts`)   | no    |
| `Figure.cs`        | the rig of `art/hero_*.png` sprites, posed and settled every frame (`figure.ts`)        | yes   |
| `HeroView.cs`      | the layer: keys, steps, drawing between steps, the tether, the aim outline, the camera  | yes   |
| `HeroAutopilot.cs` | scripted runs for recordings, and the gym map with a ladder                             | no    |

The files without Godot are compiled a second time into `game.tests/` (linked, as `sim/Tests` links
`game/Content/Light.cs`), where their TypeScript tests are ported one for one: `hero.test.ts`,
`pose.test.ts`, `rig.test.ts`, `tether.test.ts`, and of `aim.test.ts` the tests of doors, valves,
reach and the wreck crossed through a local session.

## Guarantees

- Lengths and speeds are integer thousandths of a cell, the constants `Stillwake.Content.Hero`'s, the
  controller stepping every 1000/60 ms through `Stillwake.Net.Clock` whatever the frame rate; the
  hero is drawn between its last two steps.
- A step never ends inside a wall, a closed door or past the grid's edge, and out in space the body's
  middle never gets farther than `Hero.Tether` from where the tether is tied (both FsCheck properties).
- Floats only in drawing: the rig, the poses, the tether's rope.

## Differences from the TypeScript

- The body's middle is `Height / 2` rounded down, 1312 above the feet, where the TypeScript has 1312.5:
  positions stay whole numbers. A tether cut back to its limit no longer leaves a half thousandth.
- The far limbs are tinted darker whole, gear included; `figure.ts` darkened only the suit's colour.
- The suit is the colour drawn into the PNGs (the first suit colour, `e8b04a`); suits per hero need
  the crew module and a recolouring shader.
- The camera is a plain `Camera2D` that `HeroView.Frame` centres on the body's middle; the world's
  camera from G-06 (`ShipCamera.Follow`) takes its place when the two land together.

## Seams for the crew module (not ported yet)

- `HeroView.Frame(…, spawn, passedOut, …)`: the spawn is the layout's `S` and `passedOut` is false
  until `crew.spawn()` and `crew.hero(id).out` exist. Passed out the hero takes no orders, falls, and
  wakes at the spawn; `PoseOf` already gives `Lie` and `Tumble`.
- `HeroView.Current`: where the hero is, for `report.ts`'s `crew.enter` with its body cell.
- The tether's anchor is the controller's own (0007), sent with the hero once heroes are replicated.

## Not ported yet

- Other peers' heroes (`remote.ts`, sending ~20 a second and drawing them behind), with their figures
  and tethers: with crew, since `Client.SendHero` carries no hero yet.
- Suit danger (`danger.ts`) and the suit gauge, with crew's suit air.
- Doors shown open at once while a toggle is in flight (`predictedGrid`, 0014): `Ship.Of` reads the
  grid as it is.
- Tanks, crates, plans and debris under the pointer (`aim.ts`), pick-ups, prompts, the plan held in the
  hands (`holdsPlan` is false), dust from steps and landings, light on the hero, a helmet lamp.
- `keys.test.ts`: the keys are Godot's input here, read in `HeroView`.
