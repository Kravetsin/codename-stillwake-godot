# crew (client): the heroes

This machine's hero and the others'. Heroes are client-authoritative (0002): the controller runs here against the
ship's cells, and only E's presses, `Enter` reports, `PickColour`, `Thrust`, `Refill`, `Run`, `HitBy` and the hands'
commands reach the sim. Other heroes come through the host.

## Files

| File | Holds |
| --- | --- |
| `logic/Hero.cs` | the controller: `Hero`, `HeroInput`, `Ship`; one `Step` at 60 a second; steps (0106); flight (0047) |
| `logic/Rig.cs` | the figure's joints and limbs; the fist and what it holds (0040) |
| `logic/Pose.cs` | poses, the head's turn, `Jumped`/`Landed`, the air's blend (0090) |
| `logic/Motion.cs` | lean and landing, read from motion (0090) |
| `logic/Aim.cs` | reach, the aim, what the hands send and the tools touch (0039, 0054, 0102) |
| `logic/AxeSwing.cs` | the axe's swing clock, arm and edge (0038-0040); a bite held (0075) |
| `SwingSmear.cs` | the swipe of light of a downstroke (0075) |
| `logic/SwingArm.cs` | the front arm through swings; aiming the harpoon |
| `logic/HarpoonShot.cs` | the hand harpoon: rod, hook, reel, pull, cooldown (0044) |
| `logic/TwoHand.cs` | the two-handed hold, as a rifle (0078) |
| `logic/HarpoonLine.cs` | the harpoon's line as drawn |
| `logic/Prompt.cs` | `Prompts.Of`: what E does at the aim, or why not |
| `UsePrompt.cs` | the prompt at the bottom middle |
| `logic/Tether.cs` | the tether as drawn, a reeled rope |
| `logic/DrillPace.cs` | when the drill hits: on its start tick, then every `Pace` ticks (0054) |
| `logic/Motor.cs` | the `Run` sent while the sim differs from what is wanted (0055) |
| `logic/Remote.cs` | `Seen`, `SentHero`, `RemoteHeroes`, `HeroSender` |
| `logic/RemoteRod.cs` | another hero's harpoon rod played out here (0065) |
| `logic/Suits.cs` | suit colours and shades |
| `Figure.cs` | the sprite rig in a suit, lit and posed; dust (0075, 0090) |
| `HeroView.cs` | the layer: keys, steps, rams, the camera |
| `HeroView.Hands.cs` | its hands at the pointer: E, plans, axe, drill, aim |
| `HeroView.Others.cs` | other heroes, plumes, rods, lines, tethers |
| `logic/CameraFollow.cs` | where the camera looks |
| `JetPlume.cs` | a jetpack's plume (0075) |
| `RodLight.cs` | the harpoon's light (0075) |
| `AimOutline.cs` | what the aim outline traces |
| `HeroAutopilot.cs` | scripted runs and their maps (0089, 0090, 0092) |
| `logic/Report.cs` | the `Enter` report (0050); exert, thrust, refill; `Cold` |
| `Frost.cs` | the frost past the field, by `Report.Cold` (0050) |
| `logic/Danger.cs` | what the suit gauge reads |
| `logic/AirCues.cs` | the moments air comes back (0081) |
| `logic/CanistersSeen.cs` | a friend's canister read off its suit (0081) |
| `AirSwirl.cs` | a canister's vortex, a breath, a ram's leak (0081, 0103) |
| `SuitGauge.cs` | the suit gauge under the hotbar (0058, 0074, 0081) |
| `SuitColours.cs` | the suit colour row in the game menu |
| `logic/Markers.cs` | the markers' list and geometry |
| `MarkerLayer.cs` | edge arrows to home, own body and friends |
| `logic/Bodies.cs` | bodies read from floor stacks' tags (0061) |
| `BodyView.cs` | each body drawn, its beacon (0061) |
| `logic/Rams.cs` | a creature's ram as this client meets it (0103) |
| `logic/Crew.En.cs` | Its player text (`En`) |
| `logic/Crew.Sound.cs` | Its sound cues (`Sound`) |

`logic/` is Godot-free, tested in `game.tests/`.

## Guarantees

- Lengths and speeds are integer thousandths of a cell (`Content.Hero`); the controller steps every 1000/60 ms
  (`Net.Clock`) at any frame rate and is drawn between its last two steps. Floats only in drawing.
- A step never ends inside a wall, closed door or hatch, or past the grid; afloat without a jetpack the body's middle
  stays within `Hero.Tether` of its anchor (FsCheck). A door shut on the hero pushes it `Hero.PushStep` a step to the
  nearest fit, carrying no speed (0089, `ShutOutTests`).
- Doors are walked as this peer shows them, own toggles in flight made (0014). A press asks for the other state
  (`SetDoor`/`SetValve`, 0071).
- **Holes and floors:** a standing step stops where the body fits over a hole and falls in; a hole a step
  deep is walked down (0106). Down over a hole slides in. Down on floors drops through them; a wall, door, hatch or
  ladder under the feet holds; jump wins over down. No crouch.
- **Steps** (0106): a walk steps up or down one cell where the box fits; two up stop it. Drawn eased, as a ramp.
- **Jump and walk feel** (0089): coyote time and jump buffer; the rise cut when Space is let go; eased apex; speed
  eased toward the keys, `AirControl` of it in the air (`HeroFeelTests`).
- **The figure answers its motion** (0090), read from sent `X`, `Y`, `Vx`, `Vy`, so friends move alike.
- **Rammed** (0103): contact is this client's word (`Rams.Contact`), sending `HitBy`; thrown at once, keys masked
  `Hero.Reel` steps.
- **Surfaces** (0076): `Ship.Of` takes the surface's rules: nothing floats on a planet, gravity is the kind's `Fall`.
- **Camera** (`Content/logic/Camera.cs`): leads the way the hero last moved, never its facing; eases height; snaps
  on a new place; on whole pixels.
- **Jetpack and swim** (0047): afloat the hero swims on its tether; with a jetpack it ties none and Shift thrusts;
  `Thrust` is sent while own thrust differs from the sim's. The plume shows at once (0014).
- **A canister** (0047, 0081): a right click sends `Refill` when the sim would take it and none is in flight.
- **The crew module:** made at `Spawn`; passed out it takes no orders, lies at the sim's cell, closes the inventory
  and picks nothing up (0061). `Enter` is sent when the sim's room or field side differs, and every
  `Crew.ReportEvery` ticks off its cell, none while out (0050). The host sends `Leave` for a closed link. The tether is
  tied where the body's middle last was in the hull (0007, 0104).
- **Markers** (bodies 0061): home, own newest body and friends heard, at the screen's edge, clear of the HUD.

## Others' heroes

- Sent (`SentHero`) every 50 ms while it changes, each second still, at once on a rod's change (0002, 0065); hands
  by item id and `Busy` kind (0037).
- Drawn 150 ms behind (0013), walking on 100 ms past the last sample, hidden after three silent seconds; only those
  on this surface (0076). A friend's axe swings once per swing number heard (0038); its rod plays out by `RemoteRod`
  (0065); its drill spins while the sim says it runs (0055).

## The hands

- **Left is the front, right the back** (0107, 0108): the screwdriver works plans on the left button; the stripper
  lays cables; the axe or drill on the right digs back walls only (`building.builtWall` for a ship's).
- **One aim** (`Aiming.AimAt`): a plan, a machine, tank, door or valve (E), a wreck under the pointer, else the nearest
  small piece in space. Outlined green or red; `UsePrompt` says why. With a plan in hand
  (`Ui.Hand`, 0022) the hero holds the sheet and E does nothing.
- **The axe** (0034, 0038-0040): left held swings; it strikes as the drawn edge crosses the line, one `Hit` per wreck
  touched in space; played through once begun. E never chops.
- **The harpoon** (0044): a click fires; stuck in a piece the next sends `Reel`, in structure it pulls the hero.
- **The drill** (0054, 0055, 0078): held as a rifle; left held sends `Run` until the sim runs it, and hits every
  `Pace` ticks on wrecks its bit touches.
- **Digging** (0077, 0093, 0094): with axe or drill, the brush (`Builds.Brush`, Ctrl for one cell) sends one `Dig` per
  cell the sim would take; a strike is heard and seen.
- **Ladders and ledges** (0092): grab, centre, climb, slide, leap off; an open hatch is climbable, a closed one a floor
  (0112). A jump short of a ledge by the foot's lift is mantled.

## Deliberately not handled

- Friends are pointed at only while heard; markers one way overlap; friends have no names.
- A body of a peer who left is drawn in the first suit colour; a body's look is the hero's sprites.
- Others' swings are not held on contact: a peer is not told which bit.
