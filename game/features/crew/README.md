# crew (client): the hero

This machine's hero, ported from `apps/game/src/features/crew/` (G-07). Heroes are client-authoritative
(0002): the controller runs here, outside the sim, against the ship's cells, and nothing it does is a
command but what E presses and the room it reports entering (`crew`'s `Enter`).

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
| `Report.cs`        | `Report.Of`: the `crew` `Enter` telling the sim where the hero is (`report.ts`)          | no    |
| `Danger.cs`        | `SuitDanger`, `Danger.Of` (`danger.ts`) and `Danger.Read`, what the gauge reads          | no    |
| `SuitGauge.cs`     | the suit bar, red edges when low, the dark when passed out (`SuitGauge.tsx`)              | yes   |

The files without Godot are compiled a second time into `game.tests/` (linked, as `sim/Tests` links
`game/Content/Light.cs`), where their TypeScript tests are ported one for one: `hero.test.ts`,
`pose.test.ts`, `rig.test.ts`, `tether.test.ts`, `report.test.ts`, `danger.test.ts`, and of `aim.test.ts`
the tests of doors, valves, reach and the wreck crossed through a local session.

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
- The suit is the colour drawn into the PNGs (the first suit colour, `e8b04a`); suits per hero (the
  sim's `Hero.Colour`) need a recolouring shader, and the colour picker (`SuitColours.tsx`) a panel.
- The camera is a plain `Camera2D` that `HeroView.Frame` centres on the body's middle; the world's
  camera from G-06 (`ShipCamera.Follow`) takes its place when the two land together.

## The crew module

- `HeroView.Frame` reads the sim's `crew`: the hero wakes at `Spawn`, and again there when its `Out`
  runs down; passed out it takes no orders, its E does nothing, and it falls and lies (`PoseOf`).
- Every frame, after the steps, it sends `Report.Of`'s `Enter` when the sim has no hero for this peer,
  or holds it in another room or on the other side of the air's edge than its body: the same frames
  `index.ts` sends `reportOf`'s.
- `SuitGauge`, a layer of its own made by `Main`, reads `Danger.Read` four times a second.
- The host passes `crew`'s `Leave` as the farewell of a peer whose link closes (`Main.Host`).
- The tether's anchor is the controller's own (0007), sent with the hero once heroes are replicated.

## Not ported yet

- Other peers' heroes (`remote.ts`, sending ~20 a second and drawing them behind), with their figures,
  tethers and passing out: `Client.SendHero` carries no hero yet.
- Doors shown open at once while a toggle is in flight (`predictedGrid`, 0014): `Ship.Of` reads the
  grid as it is.
  hands (`holdsPlan` is false), dust from steps and landings, light on the hero, a helmet lamp.
- `keys.test.ts`: the keys are Godot's input here, read in `HeroView`.
