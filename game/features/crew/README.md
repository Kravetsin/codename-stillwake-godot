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
| `Aim.cs`           | reach, what the pointer aims at and what the hands send (`aim.ts`): doors, valves, tanks, crates, plans, debris; what the axe's blade touches (`Struck`, 0039) | no    |
| `AxeSwing.cs`      | the fire axe's swing: one clock for the blade drawn and the strike, its point, this hero's and others' (0038, 0039) | no    |
| `Prompt.cs`        | `Prompts.Of`: what E does at the aim, in words, and why the sim would refuse it (`prompt.ts`) | no    |
| `UsePrompt.cs`     | the prompt at the bottom middle, red with the reason when refused (`CrewPanel.tsx`'s `UsePrompt`) | yes   |
| `Tether.cs`        | the tether as drawn, a reeled rope (`tether.ts`) on `game/render/Rope.cs` (`rope.ts`)   | no    |
| `Remote.cs`        | `Seen` with its hands (`Busy`, `Handwork`), `SentHero` (and its wire string), `RemoteHeroes`, `HeroSender` (`remote.ts`)     | no    |
| `Suits.cs`         | the suit colours, `Suits.Of`, their shades and the colour the PNGs are drawn in (`suits.ts`, `art.ts`) | no |
| `Figure.cs`        | the rig of `art/hero_*.png` sprites in a suit (`SuitArt`), lit, posed and settled every frame, the plan's sheet in its hands, dust from its boots (`figure.ts`) | yes |
| `HeroView.cs`      | the layer: keys, steps, drawing between steps, others' heroes, tethers, the hands, the aim outline, the camera | yes |
| `AimOutline.cs`    | what the aim outline traces: a machine's picture and socket, a whole doorway, a valve's wheel, a plan's own pictures | yes |
| `HeroAutopilot.cs` | scripted runs for recordings, and the gym map with a ladder                             | no    |
| `Report.cs`        | `Report.Of`: the `crew` `Enter` telling the sim where the hero is (`report.ts`)          | no    |
| `Danger.cs`        | `SuitDanger`, `Danger.Of` (`danger.ts`) and `Danger.Read`, what the gauge reads          | no    |
| `SuitGauge.cs`     | the suit bar, red edges when low, the dark when passed out (`SuitGauge.tsx`)              | yes   |
| `SuitColours.cs`   | the suit colours, a row in the game menu's settings: the one worn ringed, those taken dimmed, the rest a click away (`SuitColours.tsx`) | yes |

The files without Godot are compiled a second time into `game.tests/` (linked, as `sim/Tests` links
`game/Content/Light.cs`), where their TypeScript tests are ported one for one: `hero.test.ts`,
`pose.test.ts`, `rig.test.ts`, `tether.test.ts`, `report.test.ts`, `danger.test.ts`, `remote.test.ts`,
`suits.test.ts`, `prompt.test.ts` (`PromptTests`) and `aim.test.ts` (`AimTests`); `AxeSwingTests` has no
TypeScript original.

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
- A tether's rope steps 60 times a second and is drawn between its last two steps, as the hero is
  (`render/Rope.cs`'s `DrawnAt`).

## Others' heroes

- This hero is sent (`Client.SendHero`, a string net never reads: `SentHero.Encode`, JSON) every 50 ms
  while it moves, turns its head, ties or lets go its tether, takes the plan or a tool out or its hands
  begin or end work, and every second standing still, stamped with its controller step's time.
- Its hands go with it (0037): the item it wields by id (`Seen.Wields`, `Ui.Wielded`, none passed out),
  and what they are busy with and where (`Seen.Busy`: `Work` the plan the screwdriver turns at, `Chop`
  the point the latest swing is aimed at while a swing plays, with the swings begun (`Busy.Swings`), `Beam` the first cell
  the planner beams in, sent while the beam fades), a point in thousandths of a cell. Both are left out
  of the JSON while empty, as are the swings while none, so an idle hero is written as before; the kind
  of work goes by name, and one not known makes no hero. A new tool needs no new field.
- Each other hero is drawn 150 ms behind its newest sample, between the two around then, placed by the
  sender's stamps; it walks on 100 ms along its last speed when samples run out, then stands; a gap
  wider than three cells is a jump; one silent for three seconds is hidden with its tether.
- Each in its suit (`Suits.Of`, the sim's `Hero.Colour`), posed by how it moves, lying while the sim
  says it is out, its head turned as sent, the plan in its hands as sent; its tether drawn too. They
  sway the cables as this hero does (`HeroView.Others`).
- Its hands as sent, by the same `HeroView.ShowHands` as this hero's: the axe hanging and swinging
  toward its point, the screwdriver turning at the plan, a beam once to the cell sent (`Busy.BeamsAnew`). Taken
  from the sample at or before the moment drawn, never between two; but the axe swings once for each
  swing number as it is heard, not 150 ms behind (`AxeSwing.Follow`, 0038), toward the point sent with
  that number, the last chop heard kept while its hands are idle (`Busy.Chopping`, 0039), so its blade
  meets the wreck as the hit lands: within a few hundredths of a second on a LAN and at 100 ms lag. The chop and
  the screwdriver are heard from the world, where the wreck takes damage (`DebrisView`) and the plan progresses
  (`BuildingView`), once whoever works them; the figures play no tool sounds.
- `--aside` steps this hero off the spawn, so a recording of another peer's hero there sees it whole
  (`play.cmd --host --wreck` with `--join --aside` recorded; or `--host --aside` recorded with
  `--join --plan-demo --max-fps 20` beside it).
- `Main` takes the heroes the client heard each frame and hands them to `HeroView.Frame`.

## Differences from the TypeScript

- The aim is outlined along the pictures of what E works, a screen pixel wide (`render/Outline.cs`), where
  the TypeScript boxed its cells two pixels of the world wide. A mark on a wall, back wall, floor, ladder,
  pipe or door still outlines its cell. Traced once per aim: a door or plan added beside the one aimed at
  shows in its outline when the pointer next moves to another cell.
- The body's middle is `Height / 2` rounded down, 1312 above the feet, where the TypeScript has 1312.5:
  positions stay whole numbers. A tether cut back to its limit no longer leaves a half thousandth.
  Others' heroes between samples are rounded to whole thousandths too.
- Suits are recoloured from the PNGs, drawn in `Suits.Drawn` (`e0a030`, an older first suit): each of
  its four shades becomes the same shade of the suit, once per part and colour (`SuitArt`). A figure
  whose colour changes is recoloured, not made anew.
- Afloat the hero stays upright (0036), where the TypeScript swims head first: it leans into a swim
  along the floor (`Poses.Adrift`, at most about 25°), the legs trail and the free hand paddles, and
  still it drifts calmly, never panicking. `pose.test.ts`'s swimming tests are replaced by the upright
  ones.
- The camera is a plain `Camera2D` that `HeroView.Frame` centres on the body's middle; the world's
  camera from G-06 (`ShipCamera.Follow`) takes its place when the two land together.

## The crew module

- `HeroView.Frame` reads the sim's `crew`: the hero is made at `Spawn`, the lifepod's hatch, and
  again there when its `Out` runs down; passed out it takes no orders, its E does nothing, and it falls and lies (`PoseOf`).
- Every frame, after the steps, it sends `Report.Of`'s `Enter` when the sim has no hero for this peer,
  or holds it in another room or on the other side of the air's edge than its body: the same frames
  `index.ts` sends `reportOf`'s.
- `Danger.Read` counts the lifepod's air (`ICrewQueries.ByPod`) as room air: a low suit by the pod
  fills, so the gauge shows it filling and no danger.
- `SuitGauge`, a layer of its own made by `Main`, and `SuitColours`, a row `Main` hands the game menu
  (`game/menu`), read the sim four times a second, the row only while shown.
- The host passes `crew`'s `Leave` as the farewell of a peer whose link closes (`Main.Host`).
- The tether's anchor is the controller's own (0007), sent with the hero.

## The hands

- One aim (`Aiming.AimAt`) for everything the hands work under the pointer: a plan in the cell (the
  left button held), a crate, furnace or analyzer, a tank, a door, a valve (E), else a wreck whose
  picture is under the pointer (`DebrisLooks.Wrecks`, reached at its nearest edge), else the nearest
  small piece of debris out in space (E or the left button). Pieces with a grab in flight are passed over
  (`Shared.Predicted.Grabbing`).
- With the hand free and the hero awake, what E would work is outlined along its own pictures, a
  screen pixel wide at 1080 lines whatever the zoom (`render/Outline.cs`), green, or red when the sim
  would refuse: a crate, furnace, analyzer or tank's whole picture with its socket; the whole doorway,
  which opens as one; a valve's wheel; a plan's own pictures, joined to the plans beside it as the
  building draws them; a mark's machine, else its cell. Over the machines, under the heroes and the
  plans as the TypeScript layers stacked (`Layers.Aim`); traced again only when the aim, its verdict or
  its cell changes. `UsePrompt` says what E does, red with the sim's reason. A piece of
  debris is outlined by the debris feature, through `Ui.AimedPiece`; a crate opened is shown by the
  items feature, through `Ui.OpenCrate`, and closed here once out of reach or passed out.
- With anything else in hand (`Ui.Hand`, 0022) the hero holds the plan's sheet (`Figure`'s `sheet`,
  `HeroView.HoldsPlan`, sent to the others with the hero), and E and the left button do nothing here:
  a press is spent, a hold works no plan.
- **The axe (0034, 0039):** `Ui.Wielded`, set by `items` from the chosen hotbar slot while the hand
  is free, says whether the hero wields the fire axe. With it, the left button held or clicked anywhere
  begins a swing (`AxeSwing`, 0038) toward the pointer as it was then (`AxeSwing.At`, fixed for the
  swing): `Axe.SwingTicks` long as real time, raised back over the shoulder for 35% of it, down through
  the line to its point in 12%, eased back. The hero faces that point till the swing ends, swimming
  the other way too (`HeroInput.Swings`). It strikes the frame the blade crosses the line, drawn that
  same frame (the hands are used before the figure is shown): out in space, every wreck the downstroke
  touches then gets one `Hit` the sim would take (`Aiming.HitsOf`), the jolt a tick later. The
  downstroke is the sector from the line to `AxeSwing.Through` past it, `AxeSwing.Edge` wider either
  side, out to `Axe.Reach` (2 cells) from the shoulder of the body upright, sampled every 5° at three
  distances against each wreck's aim box (`Aiming.Struck`); never the raised stretch over the head.
  Aboard it plays and touches nothing; at air it touches nothing. Once begun it plays through and
  strikes, let go or not; presses during it do nothing; held as it ends, the next begins at once,
  toward the pointer then, so clicking is no faster than holding. Putting the axe away, the hand taken,
  working a plan, climbing or passing out stops it unstruck. A frame counts at most 100 ms, so a stall
  owes no burst of hits. Only the first wreck a swing begun now would touch is aimed at (`Hit`, outlined,
  prompt "[Hold LMB] Chop"), wherever the pointer is; one out of the blade's reach is not, though the
  pointer is on it. A small piece is still caught: by E, or by a click on it within reach, which with
  its hold swings nothing. E never chops. Bare-handed a wreck is aimed at with `Grab`, red: too big. The figure holds the axe (`art/hero_axe.png`) hanging
  from its front hand. Others see it hang and swing too (0037, 0038).
- The pointer and the left button are the world's (`render/WorldButtons`): the mouse, or a script's.
  The left press is read apart from the card's (`Buttons.TakeClick`, `keys.ts`'s `takeClick`).
- Door and valve prompts read them as shown (`Shared.Predicted.Grid`), as `promptOf`'s `shown` did:
  with this peer's own toggle in flight, the next press undoes it.

## Not ported yet

- `keys.test.ts`: the keys are Godot's input here, read in `HeroView`.
- A helmet lamp: the TypeScript game has none.
