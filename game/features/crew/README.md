# crew (client): the heroes

This hero and others', client-authoritative (0002): only commands reach the sim.

## Files

| File | Holds |
| --- | --- |
| `logic/Hero.cs` | `Hero`, `Step` |
| `logic/Rig.cs` | joints, limbs, fist |
| `logic/Pose.cs` | poses |
| `logic/Motion.cs` | lean and landing |
| `logic/Splashes.cs` | water heard |
| `logic/LookTint.cs` | tints |
| `logic/Aim.cs` | aim |
| `logic/AxeSwing.cs` | stroke clock |
| `logic/Strokes.cs` | strokes |
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
| `logic/HarpoonShot.cs` | harpoon |
| `logic/HarpoonLine.cs` | its line |
| `logic/Prompt.cs` | what E does |
| `logic/Seating.cs` | seats |
| `logic/FogMove.cs` | across fog |
| `logic/Fittings.cs` | modules |
| `SeatedHeroes.cs` | porthole |
| `UsePrompt.cs` | it shown |
| `logic/Tether.cs` | tether |
| `logic/DrillPace.cs` | drill |
| `logic/RunSounds.cs` | sounds |
| `logic/ToolEdge.cs` | cutter beam |
| `logic/ToolRays.cs` | tool hits |
| `WreckPixels.cs` | wrecks |
| `ToolWork.cs` | tools |
| `logic/ToolHaze.cs` | tool hazes |
| `logic/CutterJet.cs` | jet |
| `logic/Motor.cs` | motors |
| `logic/Remote.cs` | sent |
| `logic/RemoteRod.cs` | rods |
| `logic/RodFlourish.cs` | its lights |
| `logic/Suits.cs` | suits worn |
| `logic/Skins.cs` | skins |
| `logic/Appearance.cs` | their tab |
| `AppearanceView.cs` | it shown |
| `SkinSlot.cs` | a slot |
| `logic/BladeFx.cs` | effects |
| `logic/FireFx.cs` | fire |
| `logic/RiftFx.cs` | rift |
| `BladeFxView.cs` | drawn |
| `logic/Gestures.cs` | gestures |
| `GestureWheelView.cs` | wheel |
| `logic/Pings.cs` | pings |
| `PingView.cs` | drawn |
| `logic/SuitGlow.cs` | lights |
| `logic/TrackerBlink.cs` | tracker |
| `SuitKeys.cs` | V |
| `Figure.cs` | the figure |
| `logic/BodyFit.cs` | fits |
| `logic/Cape.cs` | a cape's cloth |
| `logic/Spring.cs` | spring |
| `CapeView.cs` | drawn |
| `HeroView.cs` | the hero |
| `HeroView.Hands.cs` | its hands |
| `HeroView.Guns.cs` | its gun |
| `HeroView.Feel.cs` | air, body |
| `logic/GunHand.cs` | gun cooldown |
| `HeroView.Others.cs` | others |
| `logic/CameraFollow.cs` | camera |
| `JetPlume.cs` | plume |
| `logic/PlumeWay.cs` | its way |
| `RodLight.cs` | harpoon light |
| `AimOutline.cs` | outline |
| `ModeGlyph.cs` | driver's mark |
| `logic/DriverMode.cs` | its dots |
| `HeroAutopilot.cs` | scripted |
| `logic/Report.cs` | Enter reports |
| `Frost.cs` | the cold |
| `Haze.cs` | the heat |
| `logic/ScreenEase.cs` | eased |
| `logic/Danger.cs` | gauge read |
| `logic/AirCues.cs` | air back |
| `logic/CanistersSeen.cs` | tanks |
| `AirSwirl.cs` | air swirls |
| `SuitGauge.cs` | air row |
| `MealSlots.cs` | meals |
| `logic/Retching.cs` | a retch |
| `logic/RetchSpray.cs` | bile |
| `InkLayer.cs` | bile drawn |
| `logic/MealTips.cs` | cards |
| `Thermometer.cs` | degrees |
| `logic/Thermometers.cs` | read |
| `logic/Tunnel.cs` | tunnel |
| `TunnelView.cs` | it drawn |
| `logic/VisorFog.cs` | fog |
| `VisorFogView.cs` | it drawn |
| `logic/HurtSeen.cs` | hurt |
| `HurtView.cs` | shown |
| `SuitColours.cs` | colours |
| `LookEditor.cs` | editor |
| `logic/Markers.cs` | markers |
| `MarkerLayer.cs` | arrows |
| `logic/Bodies.cs` | bodies |
| `BodyView.cs` | drawn |
| `logic/Rams.cs` | rams |
| `HunterView.cs` | catchers |
| `CatcherFxView.cs` | its light |
| `Glitch.cs` | in bands |
| `logic/CatcherFx.cs` | its fight |
| `logic/CatcherMotion.cs` | moves |
| `logic/Crew.En.cs` | its text |
| `logic/Crew.Ru.cs` | Russian |

## Guarantees

- Lengths, speeds: thousandths of a cell (`Content.Hero`); step 1000/60 ms.
- A step never ends in a wall, shut door, hatch, off the grid; afloat, unjetted, within `Hero.Tether` of its anchor; a
  shut door pushes it `Hero.PushStep` to a fit (0089). Doors go as shown here (0014); presses send `SetDoor`, `SetValve`
  (0071), `SetPlatform`.
- **Holes, floors, steps** (0106): falls in holes it fits; down drops through; jump wins; a cell up or down, two stop it.
- **Jump** (0089): coyote, buffer, short hop; **the figure answers its motion** (0090, hero-gait).
- **Rammed** (0103), **arced** (`Rams.Hits`): `HitBy`/`Bolted` sent, thrown, keys masked.
- **Surfaces** (0076): `Ship.Of` rules, gravity the kind's `Fall`; fog glides a seam (`FogMove`, fog-edges).
- **Camera** leads, eases, snaps; **markers** (0061): home, last body, friends heard, deliveries, a catcher, pings;
  **hurts** tint edges, gauge (hurts-are-shown).
- **Water** (liquids): swum `SwimDepth` deep, left by Space or shore; wading splashes.
- **Swim, haul, jetpack** (0047, tether-haul, jet-cruise): afloat through floors, tied where it left the hull; Shift hauls
  along bends (`Wraps`); a jetpack unties; Shift vents unless `Hero.Cruising`.
- **Canister** (0047, 0081): RMB, R `Refill`; V `SuitMode` (regen-suit); O₂ sign grey cooling, red too soon (canister-mark).
- **Meals:** RMB `Eat`; refused, slots blink (hud-vitals); 4th held 3 s, `Purge` (food-tuning); degrees over (the-thermometer).
- **Crew** made at `Spawn`; passed out, lies, takes no orders (0061). `Enter` when the sim's room or side differ,
  each `Crew.ReportEvery` ticks off its cell, each tick threatened; none out (0050); host gone: `Leave`.
- **Dressed as worn**, colour on the shin, a switched body lit by `SuitOn` (hero-lights); **limbs** one picture, bent
  (whole-limbs); the **hand** its own, glove or bare (hand-at-wrist); **helmets** turn (helmets-fit-the-suit), collar under a raised
  arm; **skins** over worn items, an effect replacing the haze (hero-skins).
- **Tracker** blinks (`SignalOf`); **catchers**: heroes in gear, untracked, tumble killed (tracker-catchers).

## Others' heroes

- Sent (`SentHero`) each 50 ms while changing, each second still, at once on a rod's change (0002, 0065); hands by item
  and `Busy` (0037).
- Drawn 150 ms behind (0013), walking 100 ms past the last, hidden at 3 silent s; only those here (0076). Tools strike
  per number heard (0038), rods play out (`RemoteRod`, 0065), drills, drivers run as the sim says (0055).

## The hands

- **What the hand holds shows** (`HeldLook`); a builder twists a plan, a motor (`Twist`, `Driven`).
- **Aimed only in use** (aim-on-use): up in use and `LingerMs` after, at the pointer; shots, hits wait.
- **Left front, right back** (0107, 0108): left, a screwdriver works plans, repairs; the plan, wiring, lays cables.
- **One aim** (`Aiming.AimAt`): a plan (back walls first), machine (0099), tank, door, valve, wreck, else the nearest small
  piece; outlined green/red, `UsePrompt` why; plan in hand, E idle. Armed: small ones.
- **Hits are rays** (tool-reach, hits-pass-wrecks): a fan (`ToolRays`) to a wall, through wrecks; ground rings. **Sparks**
  are the haze's (spark-rule). **Struck tools** (0038, swing-motions): by sector, hit as the blade crosses the aim.
- **Harpoon, grapple** (0044, grapple-climbs): reel pieces, pull to walls; hung, refire; Space/RMB let go.
- **A gun** (hold-to-fire): held fires each cooldown, a `Charge` one full on release, flat, clicks; kicks (`Recoil`, body-answers).
- **Drill, grinder, cutter** (0054, 0055): held runs it (`Run`), hits a `Pace`, the cutter's by beam (`ToolEdges`).
- **Power driver** (driver-at-work, driver-order): charged, a 2×2 `WorkedSquare`; RMB toggles (mode-glyph).
- **Gestures** (gestures-for-words): F tap points, pings (crew-pings); hold: a wheel (yes, no, wave); item stowed.
- **Seated** (`Seating`): no tool; E leaves. **Ladders, ledges** (0092, 0112): grab, climb, slide, leap off; a hatch open
  climbs, shut floors; a short jump mantles.

## Deliberately not handled

- Friends marked only while heard, unnamed; markers one way overlap.
- A body dressed as its peer now (gone: first colour); it and a seated hero show no suit lights.
- Others' swings do not hold on contact; a friend's gun hangs, unaimed, unrecoiled.
