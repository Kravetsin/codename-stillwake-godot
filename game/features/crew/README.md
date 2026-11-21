# crew (client): the heroes

This hero and others', client-authoritative (0002).

## Files

| File | Holds |
| --- | --- |
| `logic/Hero.cs` | `Hero` |
| `logic/Rig.cs` | rig |
| `logic/Pose.cs` | poses |
| `logic/Motion.cs` | lean |
| `logic/Splashes.cs` | water |
| `logic/LookTint.cs` | tints |
| `logic/Aim.cs` | aim |
| `logic/AxeSwing.cs` | clock |
| `logic/Strokes.cs` | strokes |
| `logic/MotionPlay.cs` | motion |
| `ReachAir.cs` | grinder |
| `logic/AirCuts.cs` | dust |
| `logic/BladeTrail.cs` | blade |
| `SpearLight.cs` | spear's |
| `logic/SpearFx.cs` | when |
| `logic/SwingArc.cs` | angle |
| `logic/SwingArm.cs` | arm |
| `logic/HandOver.cs` | with it |
| `logic/BodyPlay.cs` | answers |
| `logic/HaftGrip.cs` | grip |
| `logic/ArmUp.cs` | up |
| `logic/HarpoonShot.cs` | harpoon |
| `logic/HarpoonLine.cs` | line |
| `logic/Prompt.cs` | E |
| `logic/Seating.cs` | seats |
| `logic/FogMove.cs` | fog |
| `logic/Fittings.cs` | modules |
| `SeatedHeroes.cs` | porthole |
| `UsePrompt.cs` | shown |
| `logic/Tether.cs` | tether |
| `logic/DrillPace.cs` | drill |
| `logic/RunSounds.cs` | sounds |
| `logic/ToolEdge.cs` | beam |
| `logic/ToolRays.cs` | hits |
| `WreckPixels.cs` | wrecks |
| `ToolWork.cs` | tools |
| `logic/ToolHaze.cs` | hazes |
| `logic/CutterJet.cs` | jet |
| `logic/Motor.cs` | motors |
| `logic/Remote.cs` | sent |
| `logic/RemoteRod.cs` | rods |
| `logic/RodFlourish.cs` | lights |
| `logic/Suits.cs` | suits |
| `logic/Skins.cs` | skins |
| `logic/Appearance.cs` | tab |
| `AppearanceView.cs` | shown |
| `SkinSlot.cs` | slot |
| `HeroDoll.cs` | doll |
| `logic/BladeFx.cs` | effects |
| `logic/FireFx.cs` | fire |
| `logic/RiftFx.cs` | rift |
| `BladeFxView.cs` | drawn |
| `logic/Gestures.cs` | gestures |
| `logic/Wheel.cs` | keys |
| `WheelView.cs` | drawn |
| `logic/Pings.cs` | pings |
| `PingView.cs` | drawn |
| `logic/SuitGlow.cs` | lights |
| `logic/TrackerBlink.cs` | tracker |
| `SuitKeys.cs` | V |
| `Figure.cs` | figure |
| `logic/BodyFit.cs` | fits |
| `LightDrone.cs` | drone |
| `logic/DroneFlight.cs` | body |
| `logic/DroneWill.cs` | mind |
| `logic/DroneModes.cs` | modes |
| `logic/DroneSign.cs` | sign |
| `logic/DroneSwarm.cs` | swarm |
| `DroneMotes.cs` | motes |
| `logic/DroneMotesLook.cs` | look |
| `logic/SheetHoloLook.cs` | projects |
| `logic/PlanPoint.cs` | points |
| `CoreGlow.cs` | core |
| `logic/Meeting.cs` | vision |
| `MeetingView.cs` | drawn |
| `HeroView.Meeting.cs` | meeting |
| `CutsceneView.cs` | grade |
| `logic/Cape.cs` | cape |
| `logic/Spring.cs` | spring |
| `CapeView.cs` | drawn |
| `HeroView.cs` | hero |
| `HeroView.Hands.cs` | hands |
| `HeroView.Guns.cs` | gun |
| `HeroView.Feel.cs` | feel |
| `logic/GunHand.cs` | cooldown |
| `HeroView.Others.cs` | others |
| `logic/CameraFollow.cs` | camera |
| `JetPlume.cs` | plume |
| `logic/PlumeWay.cs` | way |
| `RodLight.cs` | rod |
| `AimOutline.cs` | outline |
| `ModeGlyph.cs` | mark |
| `logic/DriverMode.cs` | dots |
| `HeroAutopilot.cs` | scripted |
| `logic/Report.cs` | reports |
| `Frost.cs` | cold |
| `Haze.cs` | heat |
| `logic/BodyHeat.cs` | body |
| `SuitFxView.cs` | fx |
| `logic/SuitFx.cs` | sums |
| `logic/ScreenEase.cs` | eased |
| `logic/Danger.cs` | gauge |
| `logic/AirCues.cs` | cues |
| `logic/CanistersSeen.cs` | tanks |
| `AirSwirl.cs` | swirls |
| `SuitGauge.cs` | air |
| `MealSlots.cs` | meals |
| `logic/Retching.cs` | retch |
| `logic/RetchSpray.cs` | bile |
| `InkLayer.cs` | ink |
| `logic/MealTips.cs` | cards |
| `Thermometer.cs` | degrees |
| `logic/Thermometers.cs` | read |
| `logic/Tunnel.cs` | tunnel |
| `TunnelView.cs` | drawn |
| `logic/VisorFog.cs` | fog |
| `VisorFogView.cs` | drawn |
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
| `CatcherFxView.cs` | light |
| `Glitch.cs` | bands |
| `logic/CatcherFx.cs` | fight |
| `logic/CatcherMotion.cs` | moves |
| `logic/Crew.En.cs` | text |
| `logic/Crew.Ru.cs` | Russian |

## Guarantees

- Lengths, speeds: cell/1000 (`Content.Hero`); step 1000/60 ms.
- A step never ends in a wall, shut door, hatch, off-grid; afloat, unjetted, within `Hero.Tether`; shut doors push
  it `Hero.PushStep` (0089). Doors go as shown (0014); presses send `SetDoor`, `SetValve` (0071), `SetPlatform`.
- **Holes, floors, steps** (0106): falls in holes it fits; down drops through; jump wins; a cell up, down; two stop.
- **Jump** (0089): coyote, buffer, short hop; **figure answers motion** (0090, hero-gait).
- **Rammed** (0103), **arced** (`Rams.Hits`): `HitBy`/`Bolted` sent, thrown, keys masked.
- **Surfaces** (0076): `Ship.Of` rules, gravity its `Fall`; fog glides a seam (fog-edges).
- **Camera** leads, eases, snaps; **markers** (0061): home, body, friends, deliveries, catchers, pings; **hurts** (hurts-are-shown).
- **Water** (liquids): swum `SwimDepth` deep, left by Space, shore; wading splash
- **Swim, haul, jetpack** (0047, tether-haul, jet-cruise): afloat through floors, tied; Shift hauls along bends
  (`Wraps`); jetpack unties; Shift vents unless `Hero.Cruising`.
- **Canister** (0047, 0081): RMB, R `Refill`; V `SuitMode` (regen-suit); O₂ sign grey cooling (canister-mark).
- **Dials** (hud-dials): meals (RMB `Eat`), purge held 3 s (food-tuning), the body.
- **Body** (body-temperature): `BodyHeat` from `Felt`, `Heat`; dying a last breath, `Succumb`; `SpeedOf`.
- **Crew** made at `Spawn`; out, lies, no orders (0061). `Enter` on a new room, side, each `Crew.ReportEvery` ticks
  off its cell, each tick threatened; none out (0050); host gone: `Leave`.
- **Dressed as worn**, colour on shin, `SuitOn` lit (hero-lights); **limbs** bent whole (whole-limbs); **hand** glove
  or bare (hand-at-wrist); **helmets** still, the head turns (gear-roles); **skins** (hero-skins, fancy-skins).
- **Tracker** blinks (`SignalOf`); **catchers** drawn as heroes, tumble killed (tracker-catchers).

## Others

- Sent (`SentHero`) each 50 ms moving, 1 s still, at once on a rod's change (0002, 0065); hands by `Busy` (0037).
- Drawn 150 ms behind (0013), walking 100 ms past the last, hidden 3 s quiet; only those here (0076). Tools strike
  per hit heard (0038), rods play out (0065), drills, drivers run as told (0055).

## The hands

- **Held item shows** (`HeldLook`); plans, motors twist (`Twist`, `Driven`).
- **Aimed in use** (aim-on-use): up, `LingerMs` after, at the pointer; shots, hits wait.
- **Left front, right back** (0107, 0108): left, screwdriver works plans, repairs; plan, wiring, lays cables.
- **One aim** (`Aiming.AimAt`): plan (back walls first), machine (0099), tank, door, valve, wreck, else nearest small
  piece; outlined green/red, `UsePrompt` why; plan in hand, E idle; armed, small ones.
- **Hits are rays** (tool-reach, hits-pass-wrecks): a fan (`ToolRays`) to walls, through wrecks; **sparks** the haze's
  (spark-rule). **Struck** (0038, swing-motions): by sector, as the blade crosses the aim.
- **Harpoon, grapple** (0044, grapple-climbs): reel, pull to walls; hung, refire; Space/RMB let go.
- **Gun** (hold-to-fire): held fires each cooldown, `Charge` on release; kicks (body-answers).
- **Drill, grinder, cutter** (0054, 0055): held runs (`Run`), hits a `Pace`, the cutter by beam (`ToolEdges`).
- **Power driver** (driver-at-work, driver-order): charged, a 2×2 `WorkedSquare`; RMB toggles (mode-glyph).
- **Gestures** (gestures-for-words): H points, pings (crew-pings), held a wheel, the drone signs. **Drone**
  (light-drone): E on the core pairs (drone-companion, drone-meeting); F next mode, held a wheel; projects the bag,
  plans (drone-frame); trails, warns (drone-motion).
- **Seated** (`Seating`): no tool; E leaves. **Ladders, ledges** (0092, 0112): grab, climb, slide, leap; hatches
  climbed, short jumps mantle.

## Deliberately not handled

- Friends marked only while heard, unnamed; markers overlap.
- A body dressed as its peer now; it, a seated hero: no suit lights.
- Others' swings never hold on contact; a friend's gun hangs unaimed.
