# crew (client): the heroes

This hero and others', client-authoritative (0002): only commands reach the sim.

## Files

| File | Holds |
| --- | --- |
| `logic/Hero.cs` | the controller: `Hero`, `Step` (0047, 0106) |
| `logic/Rig.cs` | joints and limbs; the fist (0040) |
| `logic/Pose.cs` | poses, the head, the air's blend (0090) |
| `logic/Motion.cs` | lean and landing (0090) |
| `logic/Splashes.cs` | water heard |
| `logic/LookTint.cs` | skin, hair tints |
| `logic/Aim.cs` | reach, aim, what hands send |
| `logic/AxeSwing.cs` | a stroke's clock (0038-0040, 0075) |
| `logic/Strokes.cs` | each tool's stroke |
| `logic/MotionPlay.cs` | its motion |
| `ReachAir.cs` | a tool's reach shown |
| `logic/AirCuts.cs` | its dust and brush |
| `logic/SwingArc.cs` | its angle kept |
| `logic/SwingArm.cs` | the front arm swung |
| `logic/ArmUp.cs` | when an aim is up |
| `logic/HarpoonShot.cs` | harpoon, grapple (0044) |
| `logic/HarpoonLine.cs` | its line |
| `logic/Prompt.cs` | what E does |
| `logic/Seating.cs` | seats; hands tied |
| `logic/FogMove.cs` | across fog (fog-edges) |
| `SeatedHeroes.cs` | behind the porthole |
| `UsePrompt.cs` | that prompt shown |
| `logic/Tether.cs` | the tether |
| `logic/DrillPace.cs` | when the drill hits (0054) |
| `logic/RunSounds.cs` | tools heard |
| `logic/ToolEdge.cs` | the cutter's beam |
| `logic/ToolRays.cs` | what a tool hits |
| `WreckPixels.cs` | wrecks' opaque pixels |
| `ToolWork.cs` | tools at work |
| `logic/ToolSparks.cs` | sparks off a wreck |
| `logic/CutterJet.cs` | the cutter's jet |
| `logic/Motor.cs` | `Run` till the sim agrees |
| `logic/Remote.cs` | heroes sent and kept |
| `logic/RemoteRod.cs` | another's harpoon rod (0065) |
| `logic/RodFlourish.cs` | when its lights play |
| `logic/Suits.cs` | suit colours, what is worn |
| `logic/SuitGlow.cs` | a body's lights |
| `logic/TrackerBlink.cs` | the tracker's blink |
| `SuitKeys.cs` | V on the tablet's edge |
| `Figure.cs` | the rig, lit, dressed (0075, 0090) |
| `logic/Cape.cs` | a worn cape's cloth |
| `logic/Spring.cs` | a held arm's swing |
| `CapeView.cs` | it drawn, lined |
| `HeroView.cs` | keys, steps, rams, camera |
| `HeroView.Hands.cs` | its hands |
| `HeroView.Guns.cs` | its gun |
| `logic/GunHand.cs` | a gun's cooldown |
| `HeroView.Others.cs` | others' heroes |
| `logic/CameraFollow.cs` | the camera's aim |
| `JetPlume.cs` | a jetpack's plume |
| `RodLight.cs` | harpoon light |
| `AimOutline.cs` | the aim's outline |
| `ModeGlyph.cs` | the driver's mode mark |
| `logic/DriverMode.cs` | its dots |
| `HeroAutopilot.cs` | scripted runs |
| `logic/Report.cs` | Enter reports (0050) |
| `Frost.cs` | frost past the field (0050) |
| `logic/Danger.cs` | the gauge's read |
| `logic/AirCues.cs` | air back (0081) |
| `logic/CanistersSeen.cs` | others' tanks |
| `AirSwirl.cs` | fling, breath, leak (0081, 0103) |
| `SuitGauge.cs` | the suit gauge (0081) |
| `logic/Tunnel.cs` | the last breath |
| `TunnelView.cs` | it drawn |
| `logic/VisorFog.cs` | a low suit's fog |
| `VisorFogView.cs` | it drawn |
| `logic/HurtSeen.cs` | hurt |
| `HurtView.cs` | shown |
| `SuitColours.cs` | menu's colours |
| `LookEditor.cs` | the look editor |
| `logic/Markers.cs` | markers |
| `MarkerLayer.cs` | edge arrows |
| `logic/Bodies.cs` | bodies (0061) |
| `BodyView.cs` | bodies, beacons, tips (0061) |
| `logic/Rams.cs` | rams and arcs |
| `HunterView.cs` | catchers as heroes, fighting |
| `CatcherFxView.cs` | its light |
| `Glitch.cs` | in bands |
| `logic/CatcherFx.cs` | its picks |
| `logic/Crew.En.cs` | its text |
| `logic/Crew.Ru.cs` | Russian |

## Guarantees

- Lengths, speeds: thousandths of a cell (`Content.Hero`); a step 1000/60 ms (`Net.Clock`).
- A step never ends in a wall, shut door or hatch, or past the grid; afloat with no jetpack the middle stays within
  `Hero.Tether` of its anchor. A door shut on it pushes it `Hero.PushStep` to the nearest fit, no speed (0089).
- Doors are walked as this peer shows them (0014); a press sends `SetDoor`/`SetValve` (0071), `SetPlatform`.
- **Holes, floors:** falls into a hole it fits; down drops through floors only; jump wins.
- **Steps** (0106): a walk steps a cell up or down where it fits; two up stop it, drawn eased.
- **Jump feel** (0089): coyote time, buffer, short hop, eased apex.
- **The figure answers its motion** (0090, hero-gait), as sent.
- **Rammed** (0103) **or arced** (`Rams.Hits`, once; beams burn): `HitBy`/`Bolted` sent, thrown, keys masked.
- **Surfaces** (0076): `Ship.Of` rules: nothing floats on a planet, gravity the kind's `Fall`; fog glides (`FogMove`)
  over a seam (fog-edges).
- **Camera:** leads, eases, snaps. **Markers** (0061): home, last body, friends heard, deliveries (lost: red), one's catcher. **Hurts**
  (hurts-are-shown) tint edges and gauge.
- **Water** (liquids): swum `SwimDepth` deep, left by Space or shore; feet `WadeDepth` deep splash.
- **Swim, haul, jetpack** (0047, tether-haul): afloat through floors, tied where it left the hull (0007);
  Shift hauls in along the tether's bends (`Wraps`); a jetpack unties, Shift thrusts, `Thrust` sent while unlike the sim's.
- **A canister** (0047, 0081): right click or R sends `Refill`, V `SuitMode` (regen-suit) if the sim takes it.
- **The crew module:** made at `Spawn`; passed out: no orders, lies at the sim's cell, takes none (0061). `Enter` goes when the sim's room or field side
  differ, every `Crew.ReportEvery` ticks off its cell, each tick if threatened; none out (0050); a closed link's host: `Leave`.
- **Dressed as worn**: helmet, body, hair, clothes, a lamp; the colour on the near shin; a switched body lit by
  `SuitOn` and charge (`SuitGlow`), lit round (hero-lights).
- **Tracker** blinks by `SignalOf` (`TrackerBlink`); **catchers** are heroes in their gear (`CreatureLooks.Heroes`), untracked;
  killed in view, they tumble (tracker-catchers).

## Others' heroes

- Sent (`SentHero`) every 50 ms while it changes, each second still, at once on a rod's change (0002, 0065); hands by
  item and `Busy` (0037).
- Drawn 150 ms behind (0013), drill aim and all, walking 100 ms past the last, hidden after 3 silent s; only those
  here (0076). A friend's tool strikes per number heard (0038), from the hero sent with it; its rod plays out by `RemoteRod`
  (0065); its drill or driver runs as the sim says (0055).

## The hands

- **What the hand holds shows** (`HeldLook`); a builder twists a plan or a motor (`Twist`, `Driven`).
- **Aimed only in use** (aim-on-use): up while used and `LingerMs` after, facing the pointer; shots and hits wait for it.
- **Left is the front, right the back** (0107, 0108): a screwdriver works plans or repairs on the left;
  the plan, wiring, lays cables; a tool on the right, nothing.
- **One aim** (`Aiming.AimAt`): a plan (back walls first), machine (0099), tank, door, valve, a wreck
  pointed at, else the nearest small piece. Outlined green or red, `UsePrompt` says why; plan in hand, E idle. Armed, small pieces.
- **Hits are rays** (tool-reach, hits-pass-wrecks): a fan (`ToolRays`) to a wall, through wrecks; ground rings, sparks.
- **Struck tools** (0038, swing-motions): swung by sector, hitting as the blade crosses the kept aim.
- **Harpoon, grapple** (0044, grapple-climbs): reel pieces, pull to walls; hung, refire; Space/RMB let go.
- **A gun** (hold-to-fire): left held fires each cooldown; a `Charge` one fires let go full; flat, clicks dry. Afloat it kicks (`Recoil`).
- **Drill, grinder, cutter** (0054, 0055): left held runs it (`Run`), hitting each `Pace` ticks, the cutter by its
  beam (`ToolEdges`), sparking (`ToolWork`).
- **Power driver** (driver-at-work, driver-order): charged, works a 2×2 square (`WorkedSquare`); right click toggles (mode-glyph).
- **Seated** (`Seating.Held`): no tool; E leaves. **Ladders, ledges** (0092): grab, climb, slide, leap off; a hatch open climbs, shut floors (0112); a jump a foot
  short mantles.

## Deliberately not handled

- Friends are marked only while heard, unnamed; markers one way overlap.
- A body is dressed as its peer now (gone, the first colour); it and a seated hero show no suit lights.
- Others' swings are not held on contact; a friend's gun hangs, aim unsent.
