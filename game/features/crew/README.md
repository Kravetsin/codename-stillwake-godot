# crew (client): the heroes

This machine's hero and the others', ported from `apps/game/src/features/crew/` (G-07). Heroes are
client-authoritative (0002): the controller runs here, outside the sim, against the ship's cells, and
nothing it does is a command but what E presses, the room it reports entering (`crew`'s `Enter`) and
the suit colour picked (`PickColour`). Other heroes come through the host beside the simulation.

## Files

| File               | Holds                                                                                   | Godot |
| ------------------ | --------------------------------------------------------------------------------------- | ----- |
| `Hero.cs`          | the controller (`hero.ts`): `Hero`, `HeroInput`, `Ship`, `Point`; one `Step` at 60 a second | no    |
| `Rig.cs`           | the figure's joints and limbs in pixels, `EndOf` and `Reach` (`rig.ts`)                 | no    |
| `Pose.cs`          | `Pose`, `Limbs`, `PoseOf`, `LimbsOf`, the head's turn and facing (`pose.ts`, `lookOf`, `faceOf`) | no    |
| `Aim.cs`           | reach, what the pointer aims at and what the hands send (`aim.ts`): doors, valves, tanks, crates, plans, debris | no    |
| `Prompt.cs`        | `Prompts.Of`: what E does at the aim, in words, and why the sim would refuse it (`prompt.ts`) | no    |
| `UsePrompt.cs`     | the prompt at the bottom middle, red with the reason when refused (`CrewPanel.tsx`'s `UsePrompt`) | yes   |
| `Tether.cs`        | the tether as drawn, a reeled rope (`tether.ts`) on `game/render/Rope.cs` (`rope.ts`)   | no    |
| `Remote.cs`        | `Seen`, `SentHero` (and its wire string), `RemoteHeroes`, `HeroSender` (`remote.ts`)     | no    |
| `Suits.cs`         | the suit colours, `Suits.Of`, their shades and the colour the PNGs are drawn in (`suits.ts`, `art.ts`) | no |
| `Figure.cs`        | the rig of `art/hero_*.png` sprites in a suit (`SuitArt`), lit, posed and settled every frame, the plan's sheet in its hands, dust from its boots (`figure.ts`) | yes |
| `HeroView.cs`      | the layer: keys, steps, drawing between steps, others' heroes, tethers, the hands, the aim outline, the camera | yes |
| `HeroAutopilot.cs` | scripted runs for recordings, and the gym map with a ladder                             | no    |
| `Report.cs`        | `Report.Of`: the `crew` `Enter` telling the sim where the hero is (`report.ts`)          | no    |
| `Danger.cs`        | `SuitDanger`, `Danger.Of` (`danger.ts`) and `Danger.Read`, what the gauge reads          | no    |
| `SuitGauge.cs`     | the suit bar, red edges when low, the dark when passed out (`SuitGauge.tsx`)              | yes   |
| `SuitColours.cs`   | the suit colours under the bar: the one worn ringed, those taken dimmed, the rest a click away (`SuitColours.tsx`) | yes |

The files without Godot are compiled a second time into `game.tests/` (linked, as `sim/Tests` links
`game/Content/Light.cs`), where their TypeScript tests are ported one for one: `hero.test.ts`,
`pose.test.ts`, `rig.test.ts`, `tether.test.ts`, `report.test.ts`, `danger.test.ts`, `remote.test.ts`,
`suits.test.ts`, `prompt.test.ts` (`PromptTests`) and `aim.test.ts` (`AimTests`).

## Guarantees

- Lengths and speeds are integer thousandths of a cell, the constants `Stillwake.Content.Hero`'s, the
  controller stepping every 1000/60 ms through `Stillwake.Net.Clock` whatever the frame rate; the
  hero is drawn between its last two steps.
- A step never ends inside a wall, a closed door or past the grid's edge, and out in space the body's
  middle never gets farther than `Hero.Tether` from where the tether is tied (both FsCheck properties).
- The hero walks against doors as this peer shows them (`Shared.Predicted.Grid`, 0014): its own
  toggles in flight already made, so it walks through a door opened on the frame E was pressed.
- Floats only in drawing: the rig, the poses, the tether's rope, others' heroes between samples.
- Every hero is lit where it stands, as the ship is (`render/Lit.cs`); tethers are not.
- Drawn over the machines, the floor's stacks and the furnace's fire, as the crew's layer stacked in
  TypeScript (`render/Layers.cs`): every tether, then others' heroes, then this machine's.

## Others' heroes

- This hero is sent (`Client.SendHero`, a string net never reads: `SentHero.Encode`, JSON) every 50 ms
  while it moves, turns its head, ties or lets go its tether or takes the plan out, and every second
  standing still, stamped with its controller step's time.
- Each other hero is drawn 150 ms behind its newest sample, between the two around then, placed by the
  sender's stamps; it walks on 100 ms along its last speed when samples run out, then stands; a gap
  wider than three cells is a jump; one silent for three seconds is hidden with its tether.
- Each in its suit (`Suits.Of`, the sim's `Hero.Colour`), posed by how it moves, lying while the sim
  says it is out, its head turned as sent, the plan in its hands as sent; its tether drawn too. They
  sway the cables as this hero does (`HeroView.Others`).
- `Main` takes the heroes the client heard each frame and hands them to `HeroView.Frame`.

## Differences from the TypeScript

- The body's middle is `Height / 2` rounded down, 1312 above the feet, where the TypeScript has 1312.5:
  positions stay whole numbers. A tether cut back to its limit no longer leaves a half thousandth.
  Others' heroes between samples are rounded to whole thousandths too.
- Suits are recoloured from the PNGs, drawn in `Suits.Drawn` (`e0a030`, an older first suit): each of
  its four shades becomes the same shade of the suit, once per part and colour (`SuitArt`). A figure
  whose colour changes is recoloured, not made anew.
- The camera is a plain `Camera2D` that `HeroView.Frame` centres on the body's middle; the world's
  camera from G-06 (`ShipCamera.Follow`) takes its place when the two land together.

## The crew module

- `HeroView.Frame` reads the sim's `crew`: the hero wakes at `Spawn`, and again there when its `Out`
  runs down; passed out it takes no orders, its E does nothing, and it falls and lies (`PoseOf`).
- Every frame, after the steps, it sends `Report.Of`'s `Enter` when the sim has no hero for this peer,
  or holds it in another room or on the other side of the air's edge than its body: the same frames
  `index.ts` sends `reportOf`'s.
- `SuitGauge` and `SuitColours`, layers of their own made by `Main`, read the sim four times a second.
- The host passes `crew`'s `Leave` as the farewell of a peer whose link closes (`Main.Host`).
- The tether's anchor is the controller's own (0007), sent with the hero.

## The hands

- One aim (`Aiming.AimAt`) for everything the hands work under the pointer: a plan in the cell (the
  left button held), a crate, furnace or analyzer, a tank, a door, a valve (E), else the nearest piece
  of debris out in space (E or the left button). Pieces with a grab in flight are passed over
  (`Shared.Predicted.Grabbing`).
- With the hand free and the hero awake, what E would work is outlined (a machine's whole footprint,
  over the machines, under the heroes and the plans as the TypeScript layers stacked: `Layers.Aim`) and `UsePrompt` says what E does, red with the sim's reason. A piece of
  debris is outlined by the debris feature, through `Ui.AimedPiece`; a crate opened is shown by the
  items feature, through `Ui.OpenCrate`, and closed here once out of reach or passed out.
- With anything else in hand (`Ui.Hand`, 0022) the hero holds the plan's sheet (`Figure`'s `sheet`,
  `HeroView.HoldsPlan`, sent to the others with the hero), and E and the left button do nothing here:
  a press is spent, a hold works no plan.
- The pointer and the left button are the world's (`render/WorldButtons`): the mouse, or a script's.
  The left press is read apart from the card's (`Buttons.TakeClick`, `keys.ts`'s `takeClick`).
- Door and valve prompts read them as shown (`Shared.Predicted.Grid`), as `promptOf`'s `shown` did:
  with this peer's own toggle in flight, the next press undoes it.

## Not ported yet

- `keys.test.ts`: the keys are Godot's input here, read in `HeroView`.
- A helmet lamp: the TypeScript game has none.
