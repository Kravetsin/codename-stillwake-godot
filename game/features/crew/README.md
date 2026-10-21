# crew (client): the heroes

This machine's hero and the others', ported from `apps/game/src/features/crew/` (G-07). Heroes are
client-authoritative (0002): the controller runs here, outside the sim, against the ship's cells, and
nothing it does is a command but what E presses, the room it reports entering (`crew`'s `Enter`) and
the suit colour picked (`PickColour`), a jetpack's thrust (`Thrust`) and a canister breathed
(`Refill`). Other heroes come through the host beside the simulation.

## Files

| File               | Holds                                                                                   | Godot |
| ------------------ | --------------------------------------------------------------------------------------- | ----- |
| `Hero.cs`          | the controller (`hero.ts`): `Hero`, `HeroInput`, `Ship` (with its walls' shapes, 0069), `Point`; one `Step` at 60 a second, up and down slopes; the jetpack's flight (`Flown`, `ThrustToward`, 0047) | no    |
| `Rig.cs`           | the figure's joints and limbs in pixels, `EndOf` and `Reach` (`rig.ts`); the fist and an item held in it (`FistOf`, `HeldAt`, 0040) | no    |
| `Pose.cs`          | `Pose`, `Limbs`, `PoseOf`, `LimbsOf`, the head's turn and facing (`pose.ts`, `lookOf`, `faceOf`); `Jumped` and `Landed`, when the boots are heard pushing off and coming down; the air's poses blended by the speed up or down (0090) | no    |
| `Motion.cs`        | what the figure reads from the motion over time (0090): the lean of its acceleration, the landing's depth and length by the fall, the time since a jump and falling (`Momentum`); a landing's ring by the fall | no    |
| `Aim.cs`           | reach, what the pointer aims at and what the hands send (`aim.ts`): doors, valves, tanks, crates, plans, debris; what the axe's blade touches (`Struck`, 0039), what the drill's bit touches (`Touched`, 0054) | no    |
| `AxeSwing.cs`      | the fire axe's swing: one clock for the blade drawn and the strike, its point, this hero's and others'; the arm and the edge through it (`ArmAt`, `EdgeAt`, `Lead`) (0038, 0039, 0040); a swing that bit drawn held on contact (`Shown`, 0075) | no    |
| `SwingSmear.cs`    | the swipe of light an axe's downstroke leaves round the shoulder (0075) | yes   |
| `SwingArm.cs`      | the front arm with the axe: straight through a swing, eased from rest into it, out of it and into the next (0040); the arm aiming the harpoon too | no    |
| `HarpoonShot.cs`   | the hand harpoon (0044): the rod's flight, hook, reel and way back, the presses each state takes, the hero's cooldown, the point reeled to (`ReelOf`), the arm that aims the barrel (`ArmFor`) | no    |
| `TwoHand.cs`      | the two-handed hold (0078): an item with `HeldLook.Both` held as a rifle across the chest, turned at its pivot toward the aim within `Up` and `Down`, the near fist on its grip and the far one on its foregrip (`AngleFor`, `At`, `Arms`) | no    |
| `HarpoonLine.cs`   | the harpoon's line as drawn: the tether's space rope (`Tether.Space`), slack in flight and hooked, wound taut reeling | no    |
| `Prompt.cs`        | `Prompts.Of`: what E does at the aim, in words, and why the sim would refuse it (`prompt.ts`) | no    |
| `UsePrompt.cs`     | the prompt at the bottom middle, red with the reason when refused (`CrewPanel.tsx`'s `UsePrompt`) | yes   |
| `Tether.cs`        | the tether as drawn, a reeled rope (`tether.ts`) on `game/render/Rope.cs` (`rope.ts`)   | no    |
| `DrillPace.cs`     | when the drill hits, on the world's ticks: the tick it starts, then every `Pace` ticks while it runs, no burst after a stall (0054) | no    |
| `Motor.cs`         | an electric tool in the hand (0055): what this client wants it to run (`Wanted`) and the `Run` sent while the sim differs (`RunOf`) | no    |
| `Remote.cs`        | `Seen` with its hands (`Busy`, `Handwork`) and its harpoon's rod (`RodSeen`), `SentHero` (and its wire string), `RemoteHeroes`, `HeroSender` (`remote.ts`)     | no    |
| `RemoteRod.cs`     | another hero's harpoon played out here from the rod it sends: flight, hook, reel, way back, its cues and its gun loaded (0065) | no    |
| `Suits.cs`         | the suit colours, `Suits.Of`, their shades and the colour the PNGs are drawn in (`suits.ts`, `art.ts`) | no |
| `Figure.cs`        | the rig of `art/hero_*.png` sprites in a suit (`SuitArt`), lit, posed and settled every frame, the plan's sheet in its hands, dust from its boots (`figure.ts`; luminous, 0075: fine motes and a veil a running step, grains, motes, veils and a flat ring on landing; a heavy landing's wider ring and streaks, 0090) | yes |
| `HeroView.cs`      | the layer: keys, steps, drawing between steps, others' heroes, tethers, the hands, the aim outline, the camera | yes |
| `CameraFollow.cs`  | where the camera looks: led by the hero's steps, its height eased after it, alike at any frame rate (`Followed`) | no |
| `JetPlume.cs`      | a jetpack's plume (0075): its core of light, streaks, motes, pool of light, shimmer | yes |
| `RodLight.cs`      | the harpoon's light (0075): the shot's glint and gas, the rod's trail and wake, the bite, the taut line's bead, the zip home | yes |
| `AimOutline.cs`    | what the aim outline traces: a machine's picture and socket, a whole doorway (door or hatch, 0067), a valve's wheel, a plan's own pictures | yes |
| `HeroAutopilot.cs` | scripted runs for recordings, and the gym map with a ladder; `Jetpack()`, the flight out of the testbed's airlock (`-- --jetpack`, `game/dev/JetpackDemo.cs`); `HillsRun`, the walk over `-- --hills`; `Feel`, `-- --feel` with `--hills`, `--strip` or `--planet`: tap and held jumps, a run, a coyote jump, traced every step (0089); `PosesRun` in the `Hall`, `-- --poses --autopilot --lit`: a sprint reversed and stopped, a hop, a held jump, falls of 8 and 21 cells, the drill held through jumps (0090); `LaddersRun` in `Ladders`, `-- --ladders --autopilot --lit`, and `StripLaddersRun` with `--strip`: a ladder grabbed running and mid-jump, slid, leapt off, pulled up out of (0092) | no    |
| `Report.cs`        | `Report.Of`: the `crew` `Enter` telling the sim where the hero is (`report.ts`), on a room's change and as its report every `Crew.ReportEvery` ticks (0050); `ExertOf` and `ThrustOf`, what it breathes and vents; `RefillOf`, a canister breathed once; `Cold`, how hard the cold bites at a cell | no    |
| `Frost.cs`         | the frost past the field: one full-screen shader quad over the world, under the HUD (0050) | yes   |
| `Danger.cs`        | `SuitDanger`, `Danger.Of` (`danger.ts`) and `Danger.Read`, what the gauge reads, room air included | no    |
| `AirCues.cs`       | the moments air comes back, each once (0081): breathing room air again, the danger letting go | no    |
| `CanistersSeen.cs` | a friend's canister read off its suit jumping more than air can add in a breath (0081) | no    |
| `AirSwirl.cs`      | air round a hero (0081): a canister's vortex rising round the body from the feet, a breath of room air's motes off the visor | yes |
| `SuitGauge.cs`     | the suit gauge under the hotbar, as low as the slot size puts it (0058), on a screen in its tab under the wrist computer's cuff (0074): the canister and fifteen cells, red and pulsing when low, moving smoothly, a hit flashing and draining (0081); a canister's surge, a wave on breathing again with a soft inhale, red edges when low letting go in a pale flash (0081), the dark when passed out (`SuitGauge.tsx`) | yes   |
| `SuitColours.cs`   | the suit colours, a row in the game menu's settings: the one worn ringed, those taken dimmed, the rest a click away (`SuitColours.tsx`) | yes |
| `Markers.cs`       | the markers' list (`Marker`: target, colour, icon) and their geometry: on the screen or off, clamped to its edge, slid out of the HUD, turned toward the target, faded in | no |
| `MarkerLayer.cs`   | the arrows at the screen's edge to home, to this player's body and to friends, drawn from `Markers`, the distance in the one tooltip | yes |
| `Bodies.cs`        | `Pile`, `Bodies.Of` and `Latest`: the bodies on the floor read from its stacks' tags, this player's newest (0061) | no |
| `BodyView.cs`      | each body drawn on its stacks: the figure lying limp in its suit, the beacon blinking and lighting round it (0061) | yes |

The files without Godot are compiled a second time into `game.tests/` (linked, as `sim/Tests` links
`game/Content/Light.cs`), where their TypeScript tests are ported one for one: `hero.test.ts`,
`pose.test.ts`, `rig.test.ts`, `tether.test.ts`, `report.test.ts`, `danger.test.ts`, `remote.test.ts`,
`suits.test.ts`, `prompt.test.ts` (`PromptTests`) and `aim.test.ts` (`AimTests`, the harpoon rod's contact
too); `AxeSwingTests`, `HarpoonTests`, `JetpackTests`, `MarkersTests`, `BodiesTests` and `CameraFollowTests` have no TypeScript original.

## Guarantees

- Lengths and speeds are integer thousandths of a cell, the constants `Stillwake.Content.Hero`'s, the
  controller stepping every 1000/60 ms through `Stillwake.Net.Clock` whatever the frame rate; the
  hero is drawn between its last two steps.
- A step never ends inside a wall, a closed door or past the grid's edge, and out in space the body's
  middle never gets farther than `Hero.Tether` from where the tether is tied, with no jetpack worn (both
  FsCheck properties). The one way in is a door shut on the hero, by anyone: then each step, keys aside,
  pushes it `Hero.PushStep` (a cell in the 200 ms a door slides) toward the nearest place it fits beside
  the doorway, on top of it or under it, the way it faces first on a tie: a hatch shut under its feet
  lifts it onto it, one shut over its head drops it below (`ShutOutTests`). Its own client pushes it, as
  every hero moves (0002), and its next report says where it went. With no such place (doors shut on
  both sides of it, which no map makes) it stays.
- The hero walks against doors as this peer shows them (`Shared.Predicted.Grid`, 0014): its own
  toggles in flight already made, so it walks through a door opened on the frame E was pressed.
- Floats only in drawing: the rig, the poses, the tether's rope, others' heroes between samples.
- Every hero is lit where it stands, as the ship is (`render/Lit.cs`); tethers are not.
- Drawn over the machines, the floor's stacks and the furnace's fire, as the crew's layer stacked in
  TypeScript (`render/Layers.cs`): every tether, then others' heroes, then this machine's.
- A tether's rope steps 60 times a second and is drawn between its last two steps, as the hero is
  (`render/Rope.cs`'s `DrawnAt`).

## Holes and floors

The TypeScript had none of these rules; they came from playtest 1, where a hole one cell wide let one
player down and not the other, and a floor could be jumped up through but not dropped through.

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
- **Down drops through floors** (`Hero.OnFloors`): standing on floors alone, down (S or the down arrow),
  walking or not, steps the feet a thousandth into the floor's row, and a row the feet are in is never
  landed on, so the hero falls through: no timer, nothing kept. Held, it drops through every floor it lands
  on. A wall, closed door or ladder under any part of the feet holds it; jump wins over down; down by a
  ladder grabs and climbs it as before, and a ladder that ends on a floor is climbed down onto it, not through
  (step off the ladder, then drop). There is no crouch. Others see it by the positions sent, as any fall.

## Slopes (0069)

The grid's walls may be cut to a shape ([0069](../../../docs/decisions/0069-a-cell-has-a-shape.md)); the hero's
`Ship` reads it beside the structure (`ShapeAt`, the grid's own), and the geometry is the sim's (`Shape.Solid`,
`Shape.TopAt`, `Shape.BottomAt`, `Shape.ClosedSides`, integer thousandths).

- **Whole cells hold the whole box; a floor slope (◢ ◣) holds the middle of the feet** on its diagonal, and a
  slope beside the middle holds the box at its point nearest the middle (so a hole at a ramp's top is walked onto
  and caught as any, `Hero.Caught`). Mixed footing stands on the higher, but a corner no more than half a width
  above the ground under the middle gives way to it (`Hero.Landing`): off a plateau down a slope the feet follow
  the slope rather than hang on the corner and drop half a cell.
- **Walking** (`Hero.Walk`) moves in parts that end as the box's leading edge reaches its column's last thousandth
  or its middle crosses into another column; after each the feet step up onto ground no more than half a width
  above them, the head permitting, and stick to ground that fell by no more than the part walked. The next column's
  walls are judged with the feet lifted to where they are as the edge reaches it, so a ramp never cuts a step short.
  Up and down a ramp the hero stays grounded every step at its speed on the flat, walking or running, with no jump
  and no hop; `Figure` keeps the stride going from walking to running and back.
- **The low ledge rule:** walking, a wall whose top is no more than half a width above the feet does not stop the
  box. At 45° the leading edge meets the next column exactly half a width above the feet, so the equal case
  passes (`≤`); with `<` every ramp would stop at its top. Whole cells only ever sit at whole rows, so on flat
  ground nothing changes.
- **A slope's closed sides** (◢'s right, ◣'s left, both bottoms) stop the box like a wall; through its open side
  the box goes in and the feet climb. The box may overlap only its lowest half width — the uphill side of a slope,
  a ledge being stepped onto; a step, climb, swim or pull that would end with more of it in anything, or the feet
  in anything, is not made (`Hero.Clear`).
- **Ceiling slopes** (◤ ◥, part 2 of 0069): the head follows the diagonal (`Shape.BottomAt`). Rising — a jump, a
  ladder — the box's top stops where it first meets a ceiling slope's diagonal, in the row the head is in too
  (`Hero.MoveY`); through a ceiling slope's open side (◤'s right, ◥'s left) the box goes in under it till the head
  meets the diagonal, in a column it is in already too (`Hero.MoveX`), so the hero stands and walks under a bevelled
  corner as far as the open half gives the head room. The head never overlaps the solid half: the lowest half width
  is still the only band that may overlap anything. A ceiling slope's top is whole to the feet, and its closed sides
  stop the box like a wall. No slide along the diagonal: a head that meets it stops, as under a flat ceiling.
  **Swimming and pulled**, every shape is whole. **Down on a slope** does nothing; down through floors and the
  one-cell-hole catch work as before.
- Tested in `SlopeTests`: a hill up and down grounded every step both ways, walking and running; mixed footing;
  closed sides and a ceiling slope on the floor stopping the hero; climbing a slope from its open side; the hole at a
  ramp's top from either side; a ladder at a ramp's foot; swimming into a slope; a jump into a ceiling slope from its
  open side stopping at the diagonal; a corridor three high with bevelled top corners, walked into either way, and a
  run of ◤ along a ceiling; and a property that, on random ships of walls, slopes, floors, ladders and doors,
  whatever the keys, nothing above the box's lowest half width or the feet is ever in a wall's solid, whole or a
  slope's half, nor a closed door.
- `-- --hills` is a room of slopes to walk (`game/dev/HillsDemo.cs`), aired and lit, stacks lying on its slopes;
  with `--autopilot` the hero walks it (`HeroAutopilot.HillsRun`). `-- --slopes` (`game/dev/SlopesDemo.cs`) has a
  corridor three high with its far top corner cut; with `--autopilot` the hero walks in under it (`SlopesRun`).

## The jump and the walk's feel (0089)

- **Coyote time and the jump buffer:** a jump still pushes off the ground up to `Hero.Coyote` (6) steps after the
  feet left it, and a press of Space in the air jumps on landing up to `Hero.JumpBuffer` (6) steps later. The hero
  keeps the timers (`Hero.Airborne`, `Hero.Buffered`); a jump spends both, so one press makes one jump and no second
  press in the air jumps again. A hero made in mid-air counts as not having stood.
- **As high as Space is held:** `HeroInput.JumpHeld` is Space held down (the press's step counts as held). Let go
  while a jump rises (`Hero.Rising`), the rise speed is halved once (`Hero.JumpCut`); below `Hero.Apex` up or down
  with Space held, gravity eases to `Ship.ApexGravity` (12 aboard); coming down it is `Ship.FallGravity` (24 aboard,
  21 on a planet: `Hero.FallGravity`, `Hero.PlanetFallGravity` of the kind's own). Held to its top, a jump of
  `Hero.Jump` 415 rises 3954 thousandths in 22 steps and lands 20 later; a tap, 1222 in 11. On a planet: 6014 in 33,
  28 down, 61 steps in all. A fall not jumped eases too with Space held; a jump off a ladder is cut as any.
- **The walk's start and stop:** the speed across is kept between steps (`Hero.Vx`, what the step moved, or what it
  walked where the walk ended as the body dropped off an edge or into a hole: only what stops the body takes it) and eased
  toward what the keys ask (`Hero.Paced`): `WalkAccel` 30 a step (full walk in 4), `RunAccel` 32 (full run in 7),
  `Stop` 30 with no key or turning (a walk stops in 4, a run in 8, a run let go of Shift slows to a walk in 4). In
  the air each is `AirControl` (650 thousandths) of it, rounded down: 19, 20, 19. Speed carries off a ledge, a jump,
  the harpoon's pull and a swim into gravity; a door's push leaves none. Slopes are walked at whatever the speed:
  `Walk` takes any step, so the parts that end at a column's last thousandth are as before (`SlopeTests` walk the
  hill and the stairs from standing).
- Others see a jump from the motion (`Poses.Jumped`) once it rises at a quarter of `Hero.Jump`, so a tap's halved
  rise is heard pushing off too. The script holds Space 40 steps after each press (`HeroAutopilot`), jumping full height.
- Tested in `HeroFeelTests`: each timer to its last step and one past, a spent coyote jump, a tap under half a held
  jump, the eased top, the airtimes aboard and on a planet, the walk's and the run's step counts, turning, the air's share.

## The figure answers its motion (0090)

- **Lean:** on the floor the body leans `HeroMotion.Lean.Gain` (0.006) radians per thousandth of a cell a step gained a
  step, the change smoothed by 0.3 a step (by time, so a friend's speed seen a send at a time leans it as far), at most
  0.25 forward and 0.22 back. At speed it is upright again but for the run's own 0.15. The tilt settles in 60 ms.
- **No skid:** a stop or a turn only leans the body. The skid pose and its streaks were taken out: tapping left and
  right made the legs dance on the spot.
- **Landing:** the knees sink `Poses.Land.Sink` times the fall over 420 (0.35 to 1.8; ×0.6 landing at over 60 across),
  springing back in 150 ms plus 110 by the fall over 600. A hop (229) sinks 0.55, a held jump (398) 0.95, eight cells
  (597) 1.42 and heavy (from 480): its ring goes out to 34 px, at 0.45 for 260 ms, and two light streaks race out either
  side (`Luminous.LandStreak`); a lighter landing's ring goes out 3 + 26 × the fall over 1200 (at least 0.2 of it).
- **Air:** from `Poses.Airborne.Rise` to `Fall` as the speed goes from -150 to 300, smoothstepped; within 60 of still
  the knees tuck toward `Top`, up to half; for 110 ms after a jump the legs push straight (`Push`); falling faster
  than 300 for over 450 ms the arms flail, fully 300 ms later (the front arm calm while it holds an item); moving
  faster than 100 across the body tips 0.1 into its way. A two-handed item keeps both fists on its grips throughout.
- All of it is read from `X`, `Y`, `Vx`, `Vy`, `Facing` and the pose, which others' heroes send: friends lean
  and land as their peer draws them. Tested in `MotionTests`.

## Others' heroes

- **Another surface (0076):** moved by the sim to another surface, this hero stands where it arrived, the harpoon's
  rod back in the gun; `Main` draws the registry of the surface it is on and puts away the open crate, the plan and the
  effects.
- **A planet's rules (0076):** `Ship.Of` takes the kind of the surface (`Registry.Rules`): the hero floats only where
  `IRoomsQueries.Afloat` says, never on a planet, its open sky included, so no swim, tether, thrust or muffle there;
  and a fall gains the kind's gravity a step (`Ship.Gravity`, `Surfaces.Kind.Fall` of `Hero.Gravity`: 14 on the
  test strip, against 21 aboard), so a jump clears six cells where aboard it clears three (`PlanetHeroTests`), and
  comes down at half as much again (`Hero.PlanetFallGravity`, 0089).
  `--strip --autopilot` plays the strip (`HeroAutopilot.PlanetRun`): out of the shelter, onto the cliff, down the
  shaft into the cave.

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
- Only those on this hero's surface (`World.SurfaceOf`, 0076), each in its suit (`Suits.Of`, the world's `Players`), posed by how it moves, lying while the sim
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
  pipe or door still outlines its cell, a wall cut to a shape its solid half (`render/ShapeMasks.cs`, 0069). Traced once per aim: a door or plan added beside the one aimed at
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
- The camera no longer sits on the body's middle (movement review, item 12; `CameraFollow`, numbers in
  `game/Content/Camera.cs`). It leads the way the hero moved its last step, never the way it faces, so turning
  to aim at the pointer moves nothing: `LeadShare` of the screen's width at a run, at every zoom (2 cells of the
  farthest zoom's 96: the playtest found 6 flew back and forth, so only a light sense of motion is left), the lead
  turning slowly over `LeadTurnMs`, so a turn or a stop drifts, and its size easing over `SmoothMs`, whatever the
  frame rate. The hero itself is
  followed exactly across, so the lead shows whole and nothing trails. Up and down under gravity the view always
  eases after the body's middle over `SmoothMs`, through jumps too (the playtest of #137 found a height held
  until landing made the eye hunt), never more than `Catch` cells from it however fast it jumps or falls. Floating
  in space the lead is up and down as well, on the same exact follow. Wherever the hero is put anew (a session's
  start, waking at the pod, another surface, passing out where the sim has it) the camera is put straight on it.
  Every ease ends no slower than `SettleSpeed` (half the lead a second) and lands exactly on its aim: an eased tail
  crawled, its last screen pixels most of a second apart, and the stop twitched in. The camera is drawn on whole screen
  pixels, an eighth of one off them (`CameraFollow.OnPixels`), so the world shifts all at once rather than a texel
  column at a time at zooms that are not whole, and this player's hero is drawn a whole number of pixels from the
  camera, so a steady lead holds it still on screen. Others' heroes stand where they are, between pixels.

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
- **On a planet** nothing floats, so the jetpack thrusts nothing and draws nothing (0076); its card says so.
- Every frame the client sends `Thrust` while its own thrust (any step of the frame) differs from the sim's
  (`Report.ThrustOf`), as it does `Exert`: on with the first thrusting step, off on Shift let go, aboard,
  the jetpack taken off, the suit empty or passed out.
- **The plume** (`JetPlume`, 0075): a hot thruster's, from the jetpack's nozzle as the figure draws it
  (`Figure.Nozzle`): a short tapered core of light, white at the nozzle and blue toward its tip, flickering in
  length, drawn behind the heroes; 90 blue-white streaks and 24 blue motes a second blown the other way (`Luminous.Thrust`,
  `ThrustMote`); a blue pool of light held at it (`Flares.Hold`); heat shimmer in it; a faint ring as it lights. Against
  the thrust for this hero, at once (0014); for others while the sim says they thrust (`Hero.Thrusting`), against the way they move, down
  while still. The figure wears the jetpack over its air pack (`Figure.Jetpack`, `art/hero_jetpack.png`, a
  placeholder) whenever the sim has one on that hero's back.
- **A canister breathed:** a right click over the world with a canister in hand (the hand free, awake, no
  plan worked under the pointer; `Buttons.TakeRightClick`), or `Ui.Breathe` from a right click on one in the
  inventory, sends `Refill` when the sim would take it now (`CrewModule.RefillProblem`) and none is in
  flight (`Report.RefillOf`). Only then is it felt (0081): the UI's click (`Ui.Click`); air filling the suit, heard in
  the helmet (`Sound.AirFill`, a synthesised placeholder); a vortex of air round the whole body (`AirSwirl.Vortex`: a
  faint flat ring at the feet, then fine streaks and motes spiralling up round it to above the head, fading out there,
  about a second, all light so it reads in the dark, a faint pale light on the body, `Fx.Vortex*`); and
  `Ui.Refilled`: the items feature pulses the slot, the suit gauge
  surges (a sky wash, a white sweep along the lit cells, motes rising thick, `WristLook.Surge*`). A friend's canister
  is seen in its suit jumping by more than a breath of room air and the pod's can add (`CanistersSeen`): the vortex round
  it and the hiss on World where it is. A canister into a friend's suit within 5 units of full shows nothing there.
- **Air coming back** (`AirCues`, read with the gauge four times a second): coming out of thin air or vacuum into room
  air or the pod's with a suit to refill, one soft wave across the cells (`WristGauge.Wave`), a soft inhale in the
  helmet (`Sound.Inhale`) and a few pale motes off the visor (`SuitGauge.BreathesAgain`, wired by `Main` to
  `HeroView.BreathesAgain`), never twice within `WristLook.AgainGapS`; a low suit leaving danger without passing out,
  by a canister or room air, lets the red edges go in `LetGoInS` to a pale cyan edge that fades over `LetGoOutS`,
  never the whole screen. `-- --breathe` records a canister breathed in the torn room and the walk in to the pod;
  `-- --breathe-air` only the walk.

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
  energy tab (`EnergyBar`, 0053, 0081) sits right of the gauge, always: the two fill the hotbar's
  width (`Slots.GaugeRowOf`). The energy tab is added first, so the red edges and the dark cover it too.
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
  The downstroke leaves a swipe of light (0075, `SwingSmear`): a band round the shoulder from where the
  stroke began to the blade's edge as drawn, at most 1.6 radians behind it, clear at its tail and brightest
  at the blade, a thin white rim on its outer edge, added over the figure; after the stroke it shrinks onto
  the blade and dies away within a few frames. Every hero's swings leave it. A swing of this player's that
  bit a wreck (hits sent) is drawn stopped on contact for three 60 Hz frames (`AxeSwing.Shown`), the rest of
  it hurried to end on time: the swing's clock, and every hit it times, run on untouched, the sim never
  sees it. Others' heroes' swings are not held, for a peer is not told which bit.
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
  with a harpoon in hand. The harpoon's light (`RodLight`, 0075), this hero's and others' alike (0065): each shot a
  glint at the muzzle and a jet of cold gas, nine fine streaks along the barrel and two puffs of vapour (`HarpoonShot.Puff`);
  the flying rod a thin warm trail of light behind it and motes in its wake; its bite on metal a glint, eight sparks
  sprayed back and a faint ring (on a crate or pod the ring only); the line going taut, a bead of light running down
  it to the gun; a reel over, a streak zipping home along the line, a glint, a ring and a flash at the hand. The line and the rod are drawn over the heroes (`Layers.Hero`), the line
  unlit as the tethers are.
- **The drill (0054):** with `Ui.Wielded` the drill, the hero facing the pointer, the figure holds it in both hands as a
  rifle (0078, `TwoHand`): the near hand on the loop of frame under its rear, the far one on the foregrip under the front of
  its motor (`HeldLook.Drill`, `art/hero_drill.png`, the tube hammer picked by the user, its long bit the edge, `Aimed`),
  the bit's line through the pointer as far as the hold turns, so its reach is the drawn bit's: some cell and a half from the shoulder. Held on the left button (not a hold begun
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
  "[Hold LMB] Break", red with "Batteries flat" (`items.flat`) once the belt cannot pay. Flat, it winds down
  and sends nothing; aboard it spins and touches nothing. The energy bar shows while it is held (0053). Every
  weapon works on every wreck alike: the axe chops an asteroid as any wreck, the drill bores a hull section.
  **The arc drill** (`arcDrill`, 0075) is the drill with arcs on its coil: held, run, aimed and heard alike
  (`HeldLook.IsDrill`), its own `Weapons.ArcDrill` pace and damage (15 a second to the drill's 10), its picture the
  drill's with the coil tinted (`art/hero_arc_drill*.png`, a placeholder). Found, never made: only the testbed's
  supply crate and `-- --asteroid --arc-drill` hold one till planet ruins do.
  **Its light** (`render/DrillLight`, 0075, shared with the drill's item card, which throws it into particles of its own):
  only the tool's own; what its hits knock out of what they strike is that object's, thrown where it is struck
  (0094: `HitBurst`, below and in `features/effects`), so the bit throws nothing, whether it reaches the hit or only
  spins short of it. The arc drill's coil (`HeldLook.Coil`, the
  copper wound round the stock) besides, while it spins, as fine as a fire's embers (slice 3): hair-thin fine arcs
  (`Bolts`, `fine`), most arched over the stock's top into the dark from one turn to another, a few faint across its
  bright face, 33 a second at full spin; sparks crawling along its top (a short stroke of light, `Flares.Trail`), 10 a
  second, now and then leaping off where they end; 7 leaps a second of one or two tiny blue streaks (`Luminous.ArcSpark`);
  3 micro-glints a second; a faint blue haze and light over it pulsing at 2.6 Hz. Every rate swells and ebbs together on
  slow noise, bursts from a third of itself to twice, and is times `Fx.Crackle`; the plain drill has none.
  Heard at its bit (0059, `features/sound`): its motor whenever it spins, grains of a drill running free, spun up
  and down with the bit's picture (`HeldLook.SpinUpMs`) and sagging on a piece; and while its bit works on a piece,
  held 0.1 s past a graze, 18 short strikes a second, each its own pitch and loudness (`StrikeTrain`), silent the
  moment it stops.
- **Digging** (0077): with the axe or a drill held and natural ground under the pointer, in reach (`Aiming.Reaches`, as
  plans are reached), the aim is that cell: "[Hold LMB] Dig", red with "Too hard" on bedrock or "Batteries flat", the
  cell outlined (its slope's half for a slope). The axe's strike sends one `Dig` at the cell of its swing's point
  (`Aiming.DigAt`) and bites, drawn stopped a moment as on a wreck; the drill sends one at its pace (`DrillPace`), its
  works flag and strikes firing on ground as on a wreck. Nothing traces the blade or the bit, which only find wrecks in
  open space: a shaft, a cave or the strip's shelter dig alike. E digs nothing. Each dig is heard and seen as it is sent,
  and a friend's as its swing or its drill's pace is played here (`Struck`, 0093, 0094): by what it lands on, on that
  cell's face turned toward the hero who struck (`Impacts.On`), its look a share of the axe's for a faster tool
  (`Impacts.ShareAt`), the whole for the hit that breaks or fells it. Its cracks and breaks are building's (`DigView`).
  `Ui.Bits` hands `debris` every running drill's point as drawn, where a wreck it bites is struck.
- **The helmet lamp** (0077): worn on the head, its bulb shows on the helmet's brow (`Figure.HeadLamp`, a placeholder
  of three warm pixels, drawn unlit), this hero's and every friend's shown; `HeroView.HeadLamps()` says where each
  shines from this frame and the way its head looks (`Figure.HeadLampToward`), which `Main` hands the light
  (`LightView.HeadLamps`): a beam that way.
- **What is dug at stays readable** (0077): the dig target's outline is drawn over the hero and its tool (`Layers.Dug`),
  and the drill, always full size, is held two-handed across the chest (0078, below), close to the body, so it hides little of
  the cells before the hero; the drill and arc drill alike, and `Aim.Touched` traces the same bit. A landing's dust spreads along the floor only as far as it runs open
  (`Figure.FloorRoom`, from `HeroView`), and dust along the floor is drawn behind the walls (`Luminous.Low`), so none shows through a shaft's walls.
- **The two-handed hold** (0078, `TwoHand`): an item with `HeldLook.Both` (the drill and the arc drill now; two-handed weapons and
  tools to come, as a harpoon gun or an electric spear) is held as a rifle, its mount (`TwoHands.Mount`, between the hands) on a pivot across the middle of the chest,
  turned there so its edge's line runs through the pointer, but at most `TwoHand.Up` up and `Down` down (55° and 85°),
  past which it points as far as it turns; the pivot moves between `TwoHand.Keys`, forward aimed up and up and out aimed
  down, so aimed at the cells under the feet the drill stands before the legs as a jackhammer, the body unchanged. Both arms reach for it (`Rig.Reach`): the near (front) fist on `HeldLook.Grip`, its elbow back as a
  trigger arm's, the far (back) one on `TwoHands.Foregrip`, drawn behind the body and the item; neither ever crosses the
  helmet nor lifts its elbow above the shoulder, which bounds the clamp, as the far arm's reach and the floor do. The
  hands take the grips from the pose over 120 ms, the item's turn settling as a limb does. Standing, walking, climbing or
  afloat alike; a friend's hero shows it aimed at the point it drills while it runs, else resting `TwoHand.Rest` down.
  `Aim.Touched` traces the bit of the same hold, the body upright. `-- --pose-sheet=<dir>` (`game/dev/PoseSheet.cs`) draws
  the hold eight ways round the shoulder, near and far, standing and on a ladder, facing either way, into PNGs and quits.
- **Ladders** (0077, 0092): up grabs one walking, running, falling or rising, but not for `Hero.Regrab` 12 steps after a jump or
  a walk off one, so a jump off with up held goes on; down grabs standing or falling with no side held. The hero eases onto its
  middle (`LadderCentring` 450 of the way a step, about 5 steps) and climbs up at 110 a step (6.6 cells a second), down at 150
  (9), with Shift sliding at 300 (18). A side held in mid-air turns the hero on it, ready to jump that way, side then jump or
  jump then side; on the ground a side walks off unless up is held; Space with a side leaps off at 0.9 of the run and 0.85 of
  the jump, straight up as from the floor. At the top the body rises to it and stands; the figure has pulled itself up meanwhile
  onto `Hero.LedgeAbove`, hands on the ledge, the front foot stepping up (`Poses.PullUp`). The limbs' cycle is 4 cells climbed,
  read from the hero's height (`HeroMotion.Ladder`): a hand over the other every 2 cells; a slide, read from `Vy`, raises the
  hands to the rails and throws `Luminous.RailSpark`s. A friend's hero shows all of it from `Climbing`, `X`, `Y` and `Vy`. The
  hands hold tools and the plan while climbing (`Poses.HoldsIn`), so a hero digs from a ladder. `-- --ladders --autopilot --lit`
  records it (`HeroAutopilot.LaddersRun`; with `--strip`, the strip's shaft).
- **Ledge assist** (0092): a jump (`Hero.Leaping`) pressed toward a whole wall whose top its feet are short of by no more than
  the drawn foot clears (`Hero.MantleReach`, from `Poses.FootLift`: 297 at the top, 278 rising, 36 falling fast), rising no
  faster than 110 a step, is raised up to `MantleRise` 110 a step and put `MantleAcross` 60 onto the top, standing; the figure
  steps up in the pull-up's end (`Hero.MantleLedge`, read from any hero's position). A held jump now gets onto four cells
  aboard, not five; on the planet six, not seven. `-- --ledges --autopilot --lit` records it (`HeroAutopilot.LedgesRun`).
- The pointer and the left button are the world's (`render/WorldButtons`): the mouse, or a script's.
  The left press is read apart from the card's (`Buttons.TakeClick`, `keys.ts`'s `takeClick`).
- A press on a door or valve is built from it as shown (`Shared.Predicted.Grid`, passed to `Aiming.AimAt`): it asks
  for the other state (`SetDoor`/`SetValve`, 0071), so with this peer's own set in flight the next press undoes
  it, and the prompt says what the press asks for, as `promptOf`'s `shown` did.

## Not ported yet

- `keys.test.ts`: the keys are Godot's input here, read in `HeroView`.
