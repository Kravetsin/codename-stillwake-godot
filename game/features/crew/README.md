# crew (client): the heroes

This machine's hero and the others', ported from `apps/game/src/features/crew/` (G-07). Heroes are
client-authoritative (0002): the controller runs here, outside the sim, against the ship's cells, and
nothing it does is a command but what E presses, the room it reports entering (`crew`'s `Enter`) and
the suit colour picked (`PickColour`), a jetpack's thrust (`Thrust`) and a canister breathed
(`Refill`). Other heroes come through the host beside the simulation.

## Files

| File               | Holds                                                                                   | Godot |
| ------------------ | --------------------------------------------------------------------------------------- | ----- |
| `Hero.cs`          | the controller (`hero.ts`): `Hero`, `HeroInput`, `Ship`, `Point`; one `Step` at 60 a second; the jetpack's flight (`Flown`, `ThrustToward`, 0047) | no    |
| `Rig.cs`           | the figure's joints and limbs in pixels, `EndOf` and `Reach` (`rig.ts`); the fist and an item held in it (`FistOf`, `HeldAt`, 0040) | no    |
| `Pose.cs`          | `Pose`, `Limbs`, `PoseOf`, `LimbsOf`, the head's turn and facing (`pose.ts`, `lookOf`, `faceOf`); `Jumped` and `Landed`, when the boots are heard pushing off and coming down | no    |
| `Aim.cs`           | reach, what the pointer aims at and what the hands send (`aim.ts`): doors, valves, tanks, crates, plans, debris; what the axe's blade touches (`Struck`, 0039), what the drill's bit touches (`Touched`, 0054) | no    |
| `AxeSwing.cs`      | the fire axe's swing: one clock for the blade drawn and the strike, its point, this hero's and others'; the arm and the edge through it (`ArmAt`, `EdgeAt`, `Lead`) (0038, 0039, 0040) | no    |
| `SwingArm.cs`      | the front arm with the axe: straight through a swing, eased from rest into it, out of it and into the next (0040); the arm aiming the harpoon too | no    |
| `HarpoonShot.cs`   | the hand harpoon (0044): the rod's flight, hook, reel and way back, the presses each state takes, the hero's cooldown, the point reeled to (`ReelOf`), the arm that aims the barrel (`ArmFor`) | no    |
| `HarpoonLine.cs`   | the harpoon's line as drawn: the tether's space rope (`Tether.Space`), slack in flight and hooked, wound taut reeling | no    |
| `Prompt.cs`        | `Prompts.Of`: what E does at the aim, in words, and why the sim would refuse it (`prompt.ts`) | no    |
| `UsePrompt.cs`     | the prompt at the bottom middle, red with the reason when refused (`CrewPanel.tsx`'s `UsePrompt`) | yes   |
| `Tether.cs`        | the tether as drawn, a reeled rope (`tether.ts`) on `game/render/Rope.cs` (`rope.ts`)   | no    |
| `DrillPace.cs`     | when the drill hits, on the world's ticks: the tick it starts, then every `Pace` ticks while it runs, no burst after a stall (0054) | no    |
| `Motor.cs`         | an electric tool in the hand (0055): what this client wants it to run (`Wanted`) and the `Run` sent while the sim differs (`RunOf`) | no    |
| `Remote.cs`        | `Seen` with its hands (`Busy`, `Handwork`) and its harpoon's rod (`RodSeen`), `SentHero` (and its wire string), `RemoteHeroes`, `HeroSender` (`remote.ts`)     | no    |
| `RemoteRod.cs`     | another hero's harpoon played out here from the rod it sends: flight, hook, reel, way back, its cues and its gun loaded (0065) | no    |
| `Suits.cs`         | the suit colours, `Suits.Of`, their shades and the colour the PNGs are drawn in (`suits.ts`, `art.ts`) | no |
| `Figure.cs`        | the rig of `art/hero_*.png` sprites in a suit (`SuitArt`), lit, posed and settled every frame, the plan's sheet in its hands, dust from its boots (`figure.ts`) | yes |
| `HeroView.cs`      | the layer: keys, steps, drawing between steps, others' heroes, tethers, the hands, the aim outline, the camera | yes |
| `AimOutline.cs`    | what the aim outline traces: a machine's picture and socket, a whole doorway, a valve's wheel, a plan's own pictures | yes |
| `HeroAutopilot.cs` | scripted runs for recordings, and the gym map with a ladder; `Jetpack()`, the flight out of the testbed's airlock (`-- --jetpack`, `game/dev/JetpackDemo.cs`)                             | no    |
| `Report.cs`        | `Report.Of`: the `crew` `Enter` telling the sim where the hero is (`report.ts`), on a room's change and as its report every `Crew.ReportEvery` ticks (0050); `ExertOf` and `ThrustOf`, what it breathes and vents; `Cold`, how hard the cold bites at a cell | no    |
| `Frost.cs`         | the frost past the field: one full-screen shader quad over the world, under the HUD (0050) | yes   |
| `Danger.cs`        | `SuitDanger`, `Danger.Of` (`danger.ts`) and `Danger.Read`, what the gauge reads          | no    |
| `SuitGauge.cs`     | the suit bar under the hotbar, as low as the slot size puts it (0058), red edges when low, the dark when passed out (`SuitGauge.tsx`) | yes   |
| `SuitColours.cs`   | the suit colours, a row in the game menu's settings: the one worn ringed, those taken dimmed, the rest a click away (`SuitColours.tsx`) | yes |
| `Markers.cs`       | the markers' list (`Marker`: target, colour, icon) and their geometry: on the screen or off, clamped to its edge, slid out of the HUD, turned toward the target, faded in | no |
| `MarkerLayer.cs`   | the arrows at the screen's edge to home, to this player's body and to friends, drawn from `Markers`, the distance in the one tooltip | yes |
| `Bodies.cs`        | `Pile`, `Bodies.Of` and `Latest`: the bodies on the floor read from its stacks' tags, this player's newest (0061) | no |
| `BodyView.cs`      | each body drawn on its stacks: the figure lying limp in its suit, the beacon blinking and lighting round it (0061) | yes |

The files without Godot are compiled a second time into `game.tests/` (linked, as `sim/Tests` links
`game/Content/Light.cs`), where their TypeScript tests are ported one for one: `hero.test.ts`,
`pose.test.ts`, `rig.test.ts`, `tether.test.ts`, `report.test.ts`, `danger.test.ts`, `remote.test.ts`,
`suits.test.ts`, `prompt.test.ts` (`PromptTests`) and `aim.test.ts` (`AimTests`, the harpoon rod's contact
too); `AxeSwingTests`, `HarpoonTests`, `JetpackTests`, `MarkersTests` and `BodiesTests` have no TypeScript original.

## Guarantees

- Lengths and speeds are integer thousandths of a cell, the constants `Stillwake.Content.Hero`'s, the
  controller stepping every 1000/60 ms through `Stillwake.Net.Clock` whatever the frame rate; the
  hero is drawn between its last two steps.
- A step never ends inside a wall, a closed door or past the grid's edge, and out in space the body's
  middle never gets farther than `Hero.Tether` from where the tether is tied, with no jetpack worn (both
  FsCheck properties).
- The hero walks against doors as this peer shows them (`Shared.Predicted.Grid`, 0014): its own
  toggles in flight already made, so it walks through a door opened on the frame E was pressed.
- Floats only in drawing: the rig, the poses, the tether's rope, others' heroes between samples.
- Every hero is lit where it stands, as the ship is (`render/Lit.cs`); tethers are not.
- Drawn over the machines, the floor's stacks and the furnace's fire, as the crew's layer stacked in
  TypeScript (`render/Layers.cs`): every tether, then others' heroes, then this machine's.
- A tether's rope steps 60 times a second and is drawn between its last two steps, as the hero is
  (`render/Rope.cs`'s `DrawnAt`).

## Holes and floors

The TypeScript had neither rule; both came from playtest 1, where a hole one cell wide let one player
down and not the other.

- **A hole one cell wide is fallen into** (`Hero.Caught`): the body, 950 wide, fits a cell with 50
  thousandths to spare, but a step walks 120 and runs 220, so a walk used to pass over that window or land
  in it by where the hero happened to start, and tapping the keys only ever reached positions a step apart.
  Now a step taken standing stops at the first place on its way where the body fits over a cell it can
  fall into (nothing solid, no floor, no ladder top), and the hero falls in. A hole is crossed by jumping
  it, as a wider one is. Wider holes and ledges are fallen off where they were, at the same place.
- **Down over a hole slides in** (`Hero.Slid`): standing with the body's middle over such a cell and
  nothing else pressed, down moves the hero up to a walk's step a frame toward where it fits, and it falls.
  With the middle beside the hole, down does nothing. Not a narrower body: to never step past the window it
  would have to be 780 wide, against Terraria's proportions (0005); the stop keeps the body and is exact.

## Others' heroes

- This hero is sent (`Client.SendHero`, a string net never reads: `SentHero.Encode`, JSON) every 50 ms
  while it moves, turns its head, ties or lets go its tether, takes the plan or a tool out or its hands
  begin or end work, and every second standing still, stamped with its controller step's time; at once,
  whatever the 50 ms, when its harpoon's rod changes state.
- **Its harpoon (0065):** while the rod is out of the gun, `Seen.Rod` (`HarpoonShot.Seen`, `RodSeen`, JSON
  `"harpoon"`, left out while in the gun): its state and one point that holds through it, the end of the range
  flying, where it sticks from its piece's middle with the piece's id, where it sticks in the structure, none
  on the way back. Each peer plays it out (`RemoteRod`) from the sample drawn: the rod flies from the gun drawn
  here at `Harpoon.RodSpeed`, never past `Harpoon.Range`; stuck, it sits where sent, riding its piece as drawn
  here; free, or its piece gone here, it snaps back at `Harpoon.ReturnSpeed`; in the gun at once when sent so.
  Drawn as this hero's (`HeroView.DrawRod`: the rod, the line slack or taut, the puff at the shot), the gun
  aimed at the rod and loaded 5 s after the shot seen. Heard on World where it is, at `Sound.ToolReach`: the
  shot, the rod's bite (the chop, the glove's thump on a crate or pod), the drum at the gun while it reels or
  pulls; no ready cue. About 50 bytes a send while the rod is out, none while it is in.
- Its hands go with it (0037): the item it wields by id (`Seen.Wields`, `Ui.Wielded`, none passed out),
  and what they are busy with and where (`Seen.Busy`: `Work` the plan the screwdriver turns at, `Chop`
  the point the latest swing is aimed at while a swing plays, with the swings begun (`Busy.Swings`), `Beam` the first cell
  the planner beams in, sent while the beam fades, `Drill` the point the drill aims at while it runs), a point in thousandths of a cell. Both are left out
  of the JSON while empty, as are the swings while none, so an idle hero is written as before; the kind
  of work goes by name, and one not known makes no hero. A new tool needs no new field.
- Each other hero is drawn 150 ms behind its newest sample, between the two around then, placed by the
  sender's stamps; it walks on 100 ms along its last speed when samples run out, then stands; a gap
  wider than three cells is a jump; one silent for three seconds is hidden with its tether.
- Each in its suit (`Suits.Of`, the sim's `Hero.Colour`), posed by how it moves, lying while the sim
  says it is out, its head turned as sent, the plan in its hands as sent; its tether drawn too. They
  sway the cables as this hero does (`HeroView.Others`).
- Its hands as sent, by the same `HeroView.ShowHands` as this hero's: the axe held and swinging
  toward its point, the screwdriver turning at the plan, a beam once to the cell sent (`Busy.BeamsAnew`), the harpoon
  hanging while its rod is in the gun and aimed at the rod while it is out; the drill aimed at its point while it runs
  (`Handwork.Drill`), else hanging, and spinning while the sim says that hero runs it (`IItemsQueries.Running`, 0055). Taken
  from the sample at or before the moment drawn, never between two; but the axe swings once for each
  swing number as it is heard, not 150 ms behind (`AxeSwing.Follow`, 0038), toward the point sent with
  that number, the last chop heard kept while its hands are idle (`Busy.Chopping`, 0039), so its blade
  meets the wreck as the hit lands: within a few hundredths of a second on a LAN and at 100 ms lag. The chop and
  the screwdriver are heard from the world, where the wreck takes damage (`DebrisView`) and the plan progresses
  (`BuildingView`), once whoever works them; the figures play no tool sounds but the harpoon's (below).
- `--aside` steps this hero off the spawn, so a recording of another peer's hero there sees it whole
  (`play.cmd --host --wreck` with `--join --aside` recorded; or `--host --aside` recorded with
  `--join --plan-demo --max-fps 20` beside it).
- `Main` takes the heroes the client heard each frame and hands them to `HeroView.Frame`, with the
  session's progress between ticks, where the debris a rod sticks in is drawn.

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

## The jetpack and the swim (0047)

- In space the hero swims at `Hero.Swim`, 30 thousandths of a cell a step (about 1.8 cells a second), on its
  tether; Shift there does not swim faster. Aboard, walking and running are as they were, and only running
  exerts (`Report.Exerting`).
- **With a jetpack worn** (`IItemsQueries.Equipped`, the sim's inventory, `HeroInput.Jetpack`) the hero ties
  no tether and keeps its speed in space (`Hero.Flown`): Shift held, awake, with air in the suit, thrusts
  toward the movement keys or, with none held, toward the pointer (`Hero.ThrustToward`), gaining
  `Jetpack.Accel` a step up to `Jetpack.Top`; without thrust it slows by `Jetpack.Drag` a step toward what
  the keys swim. A fall carried out into space is cut to `Jetpack.Top`; walls and the grid's edge stop it as
  they stop anything, and what they stop it loses. Taken off out there, the hero swims on with no tether.
- Every frame the client sends `Thrust` while its own thrust (any step of the frame) differs from the sim's
  (`Report.ThrustOf`), as it does `Exert`: on with the first thrusting step, off on Shift let go, aboard,
  the jetpack taken off, the suit empty or passed out.
- **The plume:** puffs of grey air (`Effect.Puff`, `PuffInSpace` out there) every `HeroView.PlumeEveryMs`
  from the jetpack's nozzle as the figure draws it (`Figure.Nozzle`), against the thrust for this hero, at
  once (0014); for others while the sim says they thrust (`Hero.Thrusting`), against the way they move, down
  while still. The figure wears the jetpack over its air pack (`Figure.Jetpack`, `art/hero_jetpack.png`, a
  placeholder) whenever the sim has one on that hero's back.
- **A canister breathed:** a right click over the world with a canister in hand (the hand free, awake, no
  plan worked under the pointer; `Buttons.TakeRightClick`), or `Ui.Breathe` from a right click on one in the
  inventory, sends `Refill` when the sim would take it now (`CrewModule.RefillProblem`) and none is in
  flight, and plays the hiss through the suit (`Sound.Breathe`, a placeholder).

## The crew module

- `HeroView.Frame` reads the sim's `crew`: the hero is made at `Spawn`, the lifepod's hatch, and
  again there when its `Out` runs down; passed out it takes no orders, its E does nothing, and it falls and lies (`PoseOf`).
  Passing out it is moved to the cell the sim has it in, still, when that cell is on the grid: it dies there, and
  lies where its body falls (0061).
- Every frame, after the steps, it sends `Report.Of`'s `Enter` when the sim has no hero for this peer,
  or holds it in another room or on the other side of the air's edge than its body: the same frames
  `index.ts` sends `reportOf`'s. Besides, as the hero's report (0050), it sends one whenever the
  sim holds the hero in another cell than its body's and `Crew.ReportEvery` ticks of the world have gone
  since the last `Enter` it sent (`HeroView._reportedAt`); none while passed out, so a report landing
  after it wakes cannot put it back out past the field.
- `Danger.Read` counts the lifepod's air (`ICrewQueries.ByPod`) as room air: a low suit by the pod
  fills, so the gauge shows it filling and no danger.
- `SuitGauge`, a layer of its own made by `Main`, and `SuitColours`, a row `Main` hands the game menu
  (`game/menu`), read the sim four times a second, the row only while shown. The items feature's
  energy bar (`EnergyBar`, 0053) sits right of the gauge, drawn as it is, while an electric tool is held.
- The host passes `crew`'s `Leave` as the farewell of a peer whose link closes (`Main.Host`).
- The tether's anchor is the controller's own (0007), sent with the hero.

## The cold past the field (0050)

- **The frost** (`Frost`, a layer of its own made by `Main`, `ScreenLayers.Frost`: over the world, under the
  HUD): frost creeping in from the screen's edges, the corners first, its front ragged and the ice veined,
  with a faint static over it changing every frame. Its strength is `Report.Cold` at the body's own cell,
  the crew's `EdgeCost` over `Field.EdgeCost` eased out (2s − s²): 0 inside the field, a thin rim just past
  the line, a band at half depth, most of the screen at the edge. Read four times a second and eased
  toward over a second; hidden while 0, so inside the field it costs nothing. One `ColorRect` with a
  shader: four octaves of value noise twice a pixel.
- It reads the body's cell, the drain the reported one: the frost shows a second before the suit pays.
- `--edge=N` (dev, with `--world`) starts the hero N cells past the field's left line on the world's middle
  row, to see the cold at a depth.

## Markers: home and friends off the screen

Space has no landmarks, so whatever this player may want to find again is pointed at from the screen's
edge while it is off the screen (`MarkerLayer`, a layer of its own made by `Main`, `ScreenLayers.Hud`).

- **One list** (`Markers.Of`): each `Marker` a target in cells, a colour and an icon. **Home** first: the
  crew's `Spawn`, the lifepod's hatch where a hero is made again, in a pale white (`Markers.HomeColour`)
  with the pod's silhouette (`art/machine_lifepod.png` drawn white, mipmapped). The sim keeps one spawn,
  the first pod's, so with several pods home is the one the hero is made at. Then each **friend** heard
  lately (`HeroView.Others`, now by peer) at its body's middle, in its suit's colour with a dot. Between them
  **this player's body**, the newest it left (`Bodies.Latest`, 0061), half a cell above where its stacks lie, in
  its suit's colour with a figure lying; none to older bodies, none to friends' bodies, none once it is picked
  up or gone.
- **The look:** a dark badge ringed in its colour holding its icon, a notched chevron outside the ring toward
  the target. Its middle stays `Slots.Margin` plus its reach in from the edge, where the line from the
  screen's middle to the target crosses (`Markers.Place`), turned toward the target from where it lands.
  Its size follows the slots' size (Esc → Settings): ×1.4 at large.
- **On the screen or off** (`Markers.OnScreen`, its edges counted on it): gone the moment the target is on
  the screen, faded in over `Markers.FadeInS` once it leaves (`Markers.Fade`).
- **Clear of the HUD:** slid along its edge to the nearest place clear of every panel grown by its reach:
  the hotbar, the open inventory with the machine under it and the equipment (`InventoryPanel.Covers`),
  the suit's gauge, the build menu or its chip (`BuildMenu.Covers`), the air chip (`OverlayBar.Covers`),
  a station's panel (`FabricatorPanel.Covers`) and the prompt for E (`HeroView.PromptCovers`), as `Main`
  hands them over.
- **How far:** only by the pointer, the one tooltip (`TipSpots`): "Lifepod", "Your body" or "Crewmate" and the cells
  between this hero's middle and the target, rounded.
- Placed every frame after the hero moves the camera, read from the camera's own position and zoom, so it
  never lags a frame behind; nothing of it reaches the sim.

Deliberately not handled:

- A friend is pointed at only while heard (`RemoteHeroes`), as it is drawn; one silent for three seconds
  loses its marker with its figure.
- Two markers whose targets lie the same way overlap at the edge; friends are few and suits apart.
- The energy bar is not kept out of: it lies farther in from the top and the left than a marker ever goes.
- Friends have no names in the tip: the game has none to show yet.

## Death and the body (0061)

- Passing out is death: the sim leaves what the hero carried but its gear where it fell (`Items`' bodies). The
  hero lies limp in its suit at the sim's cell while out (above), its inventory closes and lets go of the stack
  in hand, and it picks nothing up (`HeroSpot.Out`), so it never takes back the body it lies on.
- **The body** (`BodyView`, made by `HeroView`, drawn under the floor's stacks, `Layers.Bodies`): on each body
  (`Bodies.Of`, at the middle of its stacks, carried on between ticks as the stacks are) the hero's own figure
  lying on its back in the owner's suit, aboard and afloat alike, found already lying; and on its chest a beacon
  blinking in the suit's colour, a flash every 1.2 s, each peer's out of step, drawn unlit over the floor
  (`Layers.Glows`) and lighting what is round it through a `Glow` while lit. A hero's newest body is not drawn
  while that hero is still out: it lies there itself. A placeholder of the hero's sprites until a picture of a
  body is picked.
- **Taking it back** is walking over its stacks, as any stack is picked up, with the usual popups; a friend may.
- **The marker** to this player's newest body: above, in the markers' list.
- `--die` (dev, `game/dev/DieDemo.cs`): the hero comes in out of the pod's reach in the wreck's torn room on an
  almost empty suit, carrying resources and its gear, and dies two breaths later; with `--world --edge=N` it dies
  past the field instead, far enough for the marker.

Deliberately not handled:

- Bodies of a peer who left are drawn in the first suit colour: the sim frees a colour with its hero.
- The body's look is the hero's own sprites, posed: no picture of its own yet.

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
  the line to its point in 12%, eased back, still at either end (`AxeSwing.Lift`). The hero faces that point till the swing ends, swimming
  the other way too (`HeroInput.Swings`). It strikes the frame the blade crosses the line, drawn that
  same frame (the hands are used before the figure is shown): out in space, every wreck the downstroke
  touches then gets one `Hit` the sim would take (`Aiming.HitsOf`), the jolt a tick later. What it
  touches is the path of the axe's drawn edge (0040): held as the figure holds it (`AxeSwing.EdgeAt`:
  the arm straight, the fist at its end, the grip, the hold, `HeldLook.FireAxe`), from its middle
  crossing the line to `AxeSwing.Through` past it, from the shoulder of the body upright, sampled every
  2° as segments against each wreck's aim box (`Aiming.Struck`); never the raised stretch over the head.
  It reaches 1.62 cells ahead of the shoulder.
  Aboard it plays and touches nothing; at air it touches nothing. Once begun it plays through and
  strikes, let go or not; presses during it do nothing; held as it ends, the next begins at once,
  toward the pointer then, so clicking is no faster than holding. Putting the axe away, the hand taken,
  working a plan, climbing or passing out stops it unstruck. A frame counts at most 100 ms, so a stall
  owes no burst of hits. Only the first wreck a swing begun now would touch is aimed at (`Hit`, outlined,
  prompt "[Hold LMB] Chop"), wherever the pointer is; one out of the blade's reach is not, though the
  pointer is on it. A small piece is still caught: by E, or by a click on it within reach, which with
  its hold swings nothing. E never chops. Bare-handed a wreck is aimed at with `Grab`, red: too big. The figure holds the axe (`art/hero_axe.png`) in its front fist
  by the haft, a third up it, square to the forearm, the hand drawn over it (`HeldLook.FireAxe`, 0040);
  swinging, the arm is straight and turns about the shoulder as one piece (`AxeSwing.ArmAt`), past the
  line to the point by `AxeSwing.Lead`, so the blade crosses it. Hanging, it keeps its pose's bent arm.
  Where a swing begins, ends or follows another toward a new point, the arm drawn the frame before is
  kept as an offset that eases out over the wind-up's length, 175 ms (`SwingArm`): never a jump, and
  the swing's own arm by its downstroke. Others' figures go the same way, through `Figure.Show`, and
  see it hang and swing too (0037, 0038).
- **The harpoon (0044):** with `Ui.Wielded` the harpoon, in space or aboard, the arm is straight and turned
  so the barrel's line runs through the pointer (`HarpoonShot.ArmFor`, eased in by `SwingArm`), the hero
  facing it, swimming the other way too; it fires aboard as out in space. The figure holds it as a pistol by its stock
  (`HeldLook.Harpoon`, `art/hero_harpoon.png`, its rod in the barrel, `hero_harpoon_loaded.png`, only
  while it is ready) and reports its muzzle and barrel as drawn (`Figure.Muzzle`, `Barrel`). A fresh
  click (never a hold) fires the rod (`art/hero_harpoon_rod.png`) from the muzzle along the barrel at
  `Harpoon.RodSpeed` out to `Harpoon.Range`; each frame the segment its point moved is tested against
  every piece's box, as drawn between ticks (`Shared.PieceDrawn`): a wreck's aim box, a small piece's
  picture a cell across, each with the quarter-cell margin (`Aiming.Hooks`), that the sim would let this
  hero reel; and walked cell by cell for a wall, floor or closed door (`Aiming.Grapples`), the nearest of
  them winning (`Aiming.RodMeets`). In a piece it sticks 0.15 of a cell inside the picture and rides it;
  the line hangs slack. The next click sends `Reel` to `Harpoon.Stop` before the muzzle on the way to the
  piece (`HarpoonShot.ReelOf`) and the line goes taut till the sim's reel is over, then the rod snaps back
  at `Harpoon.ReturnSpeed`; a click while reeling sends it again from where the hero is, and so does the
  point before the muzzle moving over `Harpoon.FollowBeyond` from the last sent, at most every `Harpoon.FollowTicks`. A miss, a
  piece gone or refused, or one drifting past the range unreeled frees the line, the rod snapping back;
  put away or passed out, it is in the gun at once. **The grapple:** stuck in the structure, the next
  click pulls the hero's middle toward `HarpoonShot.PullTo`, `Harpoon.PullStop` short of the hook, at
  `Harpoon.PullSpeed` (`HeroInput.Pull`, `Hero.PullStep` a step, keys and gravity aside, walls and the
  tether stopping it), the gun aimed at the hook and the hero facing it; there, or stopped short, it hangs
  (`Rod.Hanging`), facing and aiming at the pointer again, till a jump, a movement key or a click lets go and
  it drops; a cell that stops gripping (a door opened) frees it too. Clicks in flight, pulled, on the way
  back or in the cooldown do nothing; with the rod out a click is the gun's, never a grab.
  The cooldown, `Harpoon.CooldownTicks` as real time from the shot, is the hero's: one `HarpoonShot`
  for every harpoon it holds. With the harpoon no wreck is aimed at (`Aiming.AimAt`'s `gun`); a small
  piece within reach is still caught by E or a click while the rod is in the gun, and the piece reeled in
  is caught with E. Heard through the suit (0042): the shot, the rod biting where it struck (the chop on
  metal or structure, the glove's thump on a crate or pod), the drum while reeling or pulled, a ready cue as the cooldown ends
  with a harpoon in hand. Each shot blows a puff of grey air from the muzzle, most along the barrel and one
  off either side (`HarpoonShot.Puff`, `Effect.Puff` on the shared particles): braked aboard in a third of a
  second, wider and longer out in space; and others' too, as their rods leave (0065). The line and the rod are drawn over the heroes (`Layers.Hero`), the line
  unlit as the tethers are.
- **The drill (0054):** with `Ui.Wielded` the drill, the arm aims it as it aims the harpoon, the bit's line
  through the pointer (`HarpoonShot.ArmFor`), the hero facing it; the figure holds it as a pistol by the loop of frame
  under its rear (`HeldLook.Drill`, `art/hero_drill.png`, the tube hammer picked by the user, its long bit the edge,
  `Aimed`), so its reach is the drawn bit's: some two and a half cells from the shoulder. Held on the left button (not a hold begun
  by a grab), it **runs** while the belt can pay a tick of its draw (`IItemsQueries.CanDrain` of its `Draw`, always
  in creative mode), aboard or at nothing too (`Motor.Wanted`, 0055): the client sends `Run(drill, On)` every frame
  till the sim runs it, and `Run(drill, Off)` every frame till it stops once let go, put away, passed out or flat
  (`Motor.RunOf`, as `Thrust`), the run before the frame's hits; the sim draws its power every tick it runs, so the
  energy bar falls while it runs at nothing. Its bit spins, the picture cycling through itself and
  `hero_drill_spin1..3.png` (`HeldLook.Spin`, `Figure.Spinning`), 18 frames a second at full speed, up in 120 ms and
  winding down over 700 ms once it stops, to rest on its picture; the lamp on its rear cap lights while it runs
  (`HeldLook.Lamp`: its pixel drawn unlit over the frames, which draw it dim, and a `Glow` giving a little green light), and on the tick it starts and every `Weapons.Drill.Pace`
  ticks after (`DrillPace`) it sends a `Hit(Piece, "drill")` for each wreck its bit touches that the sim would
  take: the bit's segment, held as the figure draws it from the shoulder of the body upright, against each
  wreck's aim box (`Aiming.Touched`), out in space only. The aim marks the first such wreck with the axe's prompt,
  "[Hold LMB] Break", red with "Batteries flat" (`debris.flat`) once the belt cannot pay. Flat, it winds down
  and sends nothing; aboard it spins and touches nothing. The energy bar shows while it is held (0053). Every
  weapon works on every wreck alike: the axe chops an asteroid as any wreck, the drill bores a hull section.
  Heard at its bit (0059, `features/sound`): its motor whenever it spins, grains of a drill running free, spun up
  and down with the bit's picture (`HeldLook.SpinUpMs`) and sagging on a piece; and while its bit works on a piece,
  held 0.1 s past a graze, 18 short strikes a second, each its own pitch and loudness (`StrikeTrain`), silent the
  moment it stops.
- The pointer and the left button are the world's (`render/WorldButtons`): the mouse, or a script's.
  The left press is read apart from the card's (`Buttons.TakeClick`, `keys.ts`'s `takeClick`).
- Door and valve prompts read them as shown (`Shared.Predicted.Grid`), as `promptOf`'s `shown` did:
  with this peer's own toggle in flight, the next press undoes it.

## Not ported yet

- `keys.test.ts`: the keys are Godot's input here, read in `HeroView`.
- A helmet lamp: the TypeScript game has none.
