# crew (client): the heroes

This hero and others', client-authoritative (0002): only commands reach the sim.

## Files

| File | Holds |
| --- | --- |
| `logic/Hero.cs` | `Hero`, `Step` |
| `logic/Rig.cs` | joints, limbs, fist |
| `logic/Pose.cs` | poses, head, air |
| `logic/Motion.cs` | lean and landing |
| `logic/Splashes.cs` | water heard |
| `logic/LookTint.cs` | skin, hair tints |
| `logic/Aim.cs` | reach, aim, sends |
| `logic/AxeSwing.cs` | stroke clock |
| `logic/Strokes.cs` | tool strokes |
| `logic/MotionPlay.cs` | its motion |
| `ReachAir.cs` | grinder haze |
| `logic/AirCuts.cs` | dust, brush |
| `logic/BladeTrail.cs` | blade haze |
| `SpearLight.cs` | spear's |
| `logic/SpearFx.cs` | when |
| `logic/SwingArc.cs` | its angle kept |
| `logic/SwingArm.cs` | arm swung |
| `logic/HandOver.cs` | with it |
| `logic/BodyPlay.cs` | its answers |
| `logic/HaftGrip.cs` | far hand |
| `logic/ArmUp.cs` | aim up |
| `logic/HarpoonShot.cs` | harpoon, grapple |
| `logic/HarpoonLine.cs` | its line |
| `logic/Prompt.cs` | what E does |
| `logic/Seating.cs` | seats; hands tied |
| `logic/FogMove.cs` | across fog |
| `logic/Fittings.cs` | modules in hand |
| `SeatedHeroes.cs` | porthole |
| `UsePrompt.cs` | it shown |
| `logic/Tether.cs` | tether |
| `logic/DrillPace.cs` | drill hits |
| `logic/RunSounds.cs` | tools heard |
| `logic/ToolEdge.cs` | cutter beam |
| `logic/ToolRays.cs` | tool hits |
| `WreckPixels.cs` | wrecks' pixels |
| `ToolWork.cs` | tools at work |
| `logic/ToolHaze.cs` | tool hazes |
| `logic/CutterJet.cs` | cutter jet |
| `logic/Motor.cs` | `Run` till agreed |
| `logic/Remote.cs` | heroes sent |
| `logic/RemoteRod.cs` | another's rod |
| `logic/RodFlourish.cs` | its lights |
| `logic/Suits.cs` | suits worn |
| `logic/SuitGlow.cs` | body lights |
| `logic/TrackerBlink.cs` | tracker blink |
| `SuitKeys.cs` | V on the tablet |
| `Figure.cs` | the rig, lit, dressed |
| `logic/BodyFit.cs` | helmets fit |
| `logic/Cape.cs` | a cape's cloth |
| `logic/Spring.cs` | arm's swing |
| `CapeView.cs` | it drawn, lined |
| `HeroView.cs` | keys, steps, camera |
| `HeroView.Hands.cs` | its hands |
| `HeroView.Guns.cs` | its gun |
| `HeroView.Feel.cs` | air, body |
| `logic/GunHand.cs` | gun cooldown |
| `HeroView.Others.cs` | others |
| `logic/CameraFollow.cs` | camera aim |
| `JetPlume.cs` | jetpack plume |
| `logic/PlumeWay.cs` | its way |
| `RodLight.cs` | harpoon light |
| `AimOutline.cs` | aim outline |
| `ModeGlyph.cs` | driver's mark |
| `logic/DriverMode.cs` | its dots |
| `HeroAutopilot.cs` | scripted runs |
| `logic/Report.cs` | Enter reports |
| `Frost.cs` | the cold |
| `Haze.cs` | the heat |
| `logic/ScreenEase.cs` | eased |
| `logic/Danger.cs` | gauge read |
| `logic/AirCues.cs` | air back |
| `logic/CanistersSeen.cs` | tanks |
| `AirSwirl.cs` | fling, breath, leak |
| `SuitGauge.cs` | air row |
| `MealSlots.cs` | meals, purge |
| `logic/Retching.cs` | a retch |
| `logic/RetchSpray.cs` | its bile |
| `InkLayer.cs` | bile drawn |
| `logic/MealTips.cs` | food cards |
| `Thermometer.cs` | degrees |
| `logic/Thermometers.cs` | read |
| `logic/Tunnel.cs` | last breath |
| `TunnelView.cs` | it drawn |
| `logic/VisorFog.cs` | visor fog |
| `VisorFogView.cs` | it drawn |
| `logic/HurtSeen.cs` | hurt |
| `HurtView.cs` | shown |
| `SuitColours.cs` | colours |
| `LookEditor.cs` | look editor |
| `logic/Markers.cs` | markers |
| `MarkerLayer.cs` | arrows |
| `logic/Bodies.cs` | bodies |
| `BodyView.cs` | bodies, beacons |
| `logic/Rams.cs` | rams and arcs |
| `HunterView.cs` | catchers |
| `CatcherFxView.cs` | its light |
| `Glitch.cs` | in bands |
| `logic/CatcherFx.cs` | its fight |
| `logic/CatcherMotion.cs` | moves |
| `logic/Crew.En.cs` | its text |
| `logic/Crew.Ru.cs` | Russian |

## Guarantees

- Lengths, speeds: thousandths of a cell (`Content.Hero`); step 1000/60 ms.
- A step never ends in a wall, shut door, hatch, off the grid; afloat, no jetpack, the middle stays within
  `Hero.Tether` of its anchor. A shut door pushes it `Hero.PushStep` to a fit (0089).
- Doors go as this peer shows them (0014); a press sends `SetDoor`/`SetValve` (0071), `SetPlatform`.
- **Holes, floors, steps** (0106): falls in holes it fits; down drops through; jump wins; a cell up or down, two stop it.
- **Jump** (0089): coyote, buffer, short hop.
- **The figure answers its motion** (0090, hero-gait).
- **Rammed** (0103) **or arced** (`Rams.Hits`): `HitBy`/`Bolted` sent, thrown, keys masked.
- **Surfaces** (0076): `Ship.Of` rules, gravity the kind's `Fall`; fog glides a seam (`FogMove`, fog-edges).
- **Camera:** leads, eases, snaps. **Markers** (0061): home, last body, friends heard, deliveries, a catcher. **Hurts**
  (hurts-are-shown) tint edges and gauge.
- **Water** (liquids): swum `SwimDepth` deep, left by Space or shore; wading splashes.
- **Swim, haul, jetpack** (0047, tether-haul, jet-cruise): afloat through floors, tied where it left the hull;
  Shift hauls in along its bends (`Wraps`); a jetpack unties; Shift vents unless `Hero.Cruising`.
- **A canister** (0047, 0081): RMB, R send `Refill`, V `SuitMode` (regen-suit) if taken. Cooling greys the O₂ sign
  in a ring; too soon, red (canister-mark).
- **Meals:** RMB sends `Eat`, chewed; refused, slots blink (hud-vitals); the 4th held 3 s, `Purge` (food-tuning).
  **Degrees** over them (the-thermometer).
- **Crew:** made at `Spawn`; passed out, lies at its cell, takes no orders (0061). `Enter` goes when the sim's room or side
  differ, each `Crew.ReportEvery` ticks off its cell, each tick threatened; none out (0050); host gone: `Leave`.
- **Dressed as worn**, colour on the shin; a switched body lit by `SuitOn`, charge (hero-lights).
  **Limbs bend** whole (bent-limbs); **helmets** turn (helmets-fit-the-suit), collar under a raised arm.
- **Tracker** blinks (`SignalOf`); **catchers**: heroes in gear, untracked, tumble killed (tracker-catchers).

## Others' heroes

- Sent (`SentHero`) every 50 ms while it changes, each second still, at once on a rod's change (0002, 0065); hands by
  item and `Busy` (0037).
- Drawn 150 ms behind (0013), walking 100 ms past the last, hidden 3 silent s on; only those
  here (0076). A friend's tool strikes per number heard (0038), from the hero sent with it; its rod plays out by `RemoteRod`
  (0065); its drill, driver run as the sim says (0055).

## The hands

- **What the hand holds shows** (`HeldLook`); a builder twists a plan, a motor (`Twist`, `Driven`).
- **Aimed only in use** (aim-on-use): up in use, `LingerMs` after, at the pointer; shots and hits wait for it.
- **Left is the front, right the back** (0107, 0108): left, a screwdriver works plans, repairs;
  the plan, wiring, lays cables; right, a tool, nothing.
- **One aim** (`Aiming.AimAt`): a plan (back walls first), machine (0099), tank, door, valve, a wreck
  pointed at, else the nearest small piece. Outlined green/red, `UsePrompt` why; plan in hand, E idle. Armed: small ones.
- **Hits are rays** (tool-reach, hits-pass-wrecks): a fan (`ToolRays`) to a wall, through wrecks; ground rings.
- **Sparks are the haze's** (spark-rule): a haze throws its ground's look.
- **Struck tools** (0038, swing-motions): by sector, hit as the blade crosses the aim, leaned into.
- **Harpoon, grapple** (0044, grapple-climbs): reel pieces, pull to walls; hung, refire; Space/RMB let go.
- **A gun** (hold-to-fire): held fires each cooldown; a `Charge` one on release, full; flat, clicks. Kicks afloat, shoves standing (`Recoil`, body-answers).
- **Drill, grinder, cutter** (0054, 0055): held runs it (`Run`), hits a `Pace`, the cutter's by beam (`ToolEdges`).
- **Power driver** (driver-at-work, driver-order): charged, works a 2×2 square (`WorkedSquare`); RMB toggles (mode-glyph).
- **Seated** (`Seating`): no tool; E leaves. **Ladders, ledges** (0092): grab, climb, slide, leap off; a hatch open climbs, shut floors (0112); a jump a foot short mantles.

## Deliberately not handled

- Friends are marked only while heard, unnamed; markers one way overlap.
- A body is dressed as its peer now (gone, the first colour); it and a seated hero show no suit lights.
- Others' swings do not hold on contact; a friend's gun hangs, unaimed, unrecoiled.
