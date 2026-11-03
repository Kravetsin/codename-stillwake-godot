# crew (client): the heroes

This machine's hero and the others'. Heroes are client-authoritative (0002): the controller runs here, and
only E's presses, `Enter` reports, `PickColour`, `Thrust`, `Refill`, `SuitMode`, `Run`, `HitBy`, `Bolted` and the hands'
commands reach the sim; others' heroes come through the host.

## Files

| File | Holds |
| --- | --- |
| `logic/Hero.cs` | the controller: `Hero`, `Step` (0047, 0106) |
| `logic/Rig.cs` | joints and limbs; the fist (0040) |
| `logic/Pose.cs` | poses, the head, the air's blend (0090) |
| `logic/Motion.cs` | lean and landing (0090) |
| `logic/LookTint.cs` | skin, hair on hi-res parts |
| `logic/Aim.cs` | reach, aim, what hands send, a fan |
| `logic/AxeSwing.cs` | the axe's swing (0038-0040, 0075) |
| `SwingSmear.cs` | a downstroke's swipe of light (0075) |
| `logic/SwingArm.cs` | the front arm swinging, aiming |
| `logic/HarpoonShot.cs` | the hand harpoon (0044) |
| `logic/HarpoonLine.cs` | the harpoon's line |
| `logic/Prompt.cs` | what E does at the aim, or why not |
| `logic/Seating.cs` | a hero in a drop pod |
| `logic/FogMove.cs` | across a planet's fog (fog-edges) |
| `SeatedHeroes.cs` | behind the porthole |
| `UsePrompt.cs` | the prompt at the bottom |
| `logic/Tether.cs` | the tether as drawn, a reeled rope |
| `logic/DrillPace.cs` | when the drill hits (0054) |
| `logic/Motor.cs` | `Run` sent till the sim agrees (0055) |
| `logic/Remote.cs` | what is sent of heroes, and kept |
| `logic/RemoteRod.cs` | another's harpoon rod (0065) |
| `logic/RodFlourish.cs` | when its lights play |
| `logic/Suits.cs` | suit colours, what is worn |
| `logic/SuitGlow.cs` | a body's lights |
| `SuitKeys.cs` | V and R on the tablet's edge |
| `Figure.cs` | the sprite rig, lit, dressed; dust (0075, 0090) |
| `logic/Cape.cs` | a worn cape's cloth |
| `logic/Spring.cs` | a held arm's swing |
| `CapeView.cs` | it drawn, with its lining |
| `HeroView.cs` | the layer: keys, steps, rams, the camera |
| `HeroView.Hands.cs` | its hands: E, plans, axe, drill, aim |
| `HeroView.Guns.cs` | its gun fired, a guest's shots flown |
| `logic/GunHand.cs` | a gun's cooldown and shots |
| `HeroView.Others.cs` | others' heroes and what they trail |
| `logic/CameraFollow.cs` | where the camera looks |
| `JetPlume.cs` | a jetpack's plume (0075) |
| `RodLight.cs` | the harpoon's light (0075) |
| `AimOutline.cs` | what the aim outline traces |
| `FaceRim.cs` | a tool's aim glow |
| `logic/AimGlow.cs` | its light, from rays |
| `HeroAutopilot.cs` | scripted runs |
| `logic/Report.cs` | the `Enter` report (0050) |
| `Frost.cs` | frost past the field (0050) |
| `logic/Danger.cs` | what the suit gauge reads |
| `logic/AirCues.cs` | air coming back (0081) |
| `logic/CanistersSeen.cs` | friends' canisters |
| `AirSwirl.cs` | a vortex, breath, leak (0081, 0103) |
| `SuitGauge.cs` | the suit gauge (0058, 0074, 0081) |
| `SuitColours.cs` | the menu's suit colours |
| `LookEditor.cs` | the look editor (looks-are-printed) |
| `logic/Markers.cs` | markers' list and shape |
| `MarkerLayer.cs` | edge arrows |
| `logic/Bodies.cs` | bodies from floor stacks (0061) |
| `BodyView.cs` | bodies, beacons, tips; gone, they crumble (0061) |
| `logic/Rams.cs` | a creature's ram as met here (0103) |
| `logic/Crew.En.cs` | its player text |
| `logic/Crew.Ru.cs` | Russian |

## Guarantees

- Lengths and speeds are thousandths of a cell (`Content.Hero`); the controller steps every 1000/60 ms
  (`Net.Clock`), drawn between; floats only in drawing.
- A step never ends inside a wall, closed door or hatch, or past the grid; afloat without a jetpack the body's middle
  stays within `Hero.Tether` of its anchor. A door shut on the hero pushes it a `Hero.PushStep` to the
  nearest fit, carrying no speed (0089).
- Doors are walked as this peer shows them (0014). A press asks for the other state
  (`SetDoor`/`SetValve`, 0071); on a launch platform `SetPlatform`, carrying `Landing.Problem`.
- **Holes and floors:** the body falls into a hole it fits; a step-deep one is walked down (0106). Down drops through
  floors, never a wall, door, hatch or ladder; jump wins over down.
- **Steps** (0106): a walk steps a cell up or down where the box fits; two up stop it. Drawn eased.
- **Jump and walk feel** (0089): coyote time, jump buffer, a short hop on letting Space go, eased apex,
  `AirControl` aloft.
- **The figure answers its motion** (0090), read from what is sent, so friends move alike.
- **Rammed** (0103) **or crossed by an arc** (`Rams.Crosses`, once): `HitBy`/`Bolted` sent, thrown, keys masked a while.
- **Surfaces** (0076): `Ship.Of` takes its rules: nothing floats on a planet, gravity is the kind's `Fall`;
  in fog it glides, `FogMove` across a seam (fog-edges).
- **Camera:** leads, eases height, snaps.
- **Jetpack and swim** (0047): afloat the hero swims, tethered, through floors; a jetpack, untied, Shift thrusts,
  `Thrust` sent while it differs from the sim's.
- **A canister** (0047, 0081): right click or R sends `Refill`, V `SuitMode` (regen-suit), if the sim takes it; both
  keys on the tablet's edge, dim while refused.
- **The crew module:** made at `Spawn`; passed out it takes no orders, lies at the sim's cell, closes the inventory
  and picks nothing up (0061). `Enter` goes when the sim's room or field side differs, every `Crew.ReportEvery`
  ticks off its cell, each tick while threatened; none while out (0050). The tether is tied where the
  body's middle last was in the hull (0007, 0104); the host sends `Leave` for a closed link.
- **Markers** (0061): home, own newest body, friends heard.
- **Dressed as worn**: helmet and body apart, later ones in their own pictures; bare, hair and clothes; a lamp on a
  helmet; the colour on the near shin; a switched body lit by `SuitOn` and charge (`SuitGlow`).

## Others' heroes

- Sent (`SentHero`) every 50 ms while it changes, each second still, at once on a rod's change (0002, 0065); hands
  by item id and `Busy` kind (0037).
- Drawn 150 ms behind (0013), walking 100 ms past the last sample, hidden after 3 silent s; only those
  on this surface (0076). A friend's axe swings once per swing number heard (0038); its rod plays out by `RemoteRod`
  (0065); its drill or power driver runs as the sim says (0055).

## The hands

- **What the hand holds shows** (`HeldLook`); a builder twists a plan, or a motor (`Twist`, `Driven`).
- **Left is the front, right the back** (0107, 0108): a screwdriver works plans or repairs on the left;
  the plan, wiring, lays cables; a tool on the right, nothing.
- **One aim** (`Aiming.AimAt`): a plan, machine (its rise, 0099), tank, door or valve (E), a wreck under the
  pointer, else the nearest small piece afloat. Outlined green or red; `UsePrompt` says why. Plan in hand: E idle.
- **A tool's hit is a fan of rays** (`Aiming.FanOf`): a `Hew`, in space a `HewWrecks`, sent if it
  strikes something; heard at the cell nearest the pointer.
- **The axe** (0034, 0038): left held swings; it hits as the drawn edge crosses the line; played through.
- **The harpoon** (0044): a click fires; stuck in a piece the next sends `Reel`, in structure it pulls the hero.
- **A gun:** a click sends `Fire` from the muzzle, each gun its own cooldown; one with `Charge` fires let go full; afloat
  a shot kicks the hero back (`Recoil`, space-recoil); a guest flies its shot (`StepOf`).
- **The drill** (0054, 0055, one-arm-holds): left held sends `Run` till the sim runs it; it hits every `Pace` ticks.
- **The power driver** (driver-at-work): runs only at work; charged, the 2×2 square over plans or marks
  (`WorkedSquare`, `SharesOf`), heard there (`BiteLoop`), its ratchet silent (`Ui.Driven`). Right click: square or cell.
- **Ladders and ledges** (0092): grab, centre, climb, slide, leap off; an open hatch is climbable, a closed one a floor
  (0112). A jump a foot short of a ledge is mantled.

## Deliberately not handled

- Friends are marked only while heard; markers one way overlap; friends have no names.
- A body is drawn as its peer is dressed now; once it left, the first colour.
- A body or a seated hero shows no suit lights.
- Others' swings are not held on contact. A friend's gun hangs, its aim unsent.
