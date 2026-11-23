# crew (client): the heroes

Client-authoritative (0002).

## Files

| File | Holds |
| --- | --- |
| `logic/Hero.cs` | |
| `logic/Rig.cs` | |
| `logic/Pose.cs` | |
| `logic/Motion.cs` | |
| `logic/Splashes.cs` | |
| `logic/LookTint.cs` | |
| `logic/Aim.cs` | |
| `logic/AxeSwing.cs` | |
| `logic/Strokes.cs` | |
| `logic/MotionPlay.cs` | |
| `ReachAir.cs` | grinder |
| `logic/AirCuts.cs` | dust |
| `logic/BladeTrail.cs` | |
| `SpearLight.cs` | |
| `logic/SpearFx.cs` | when |
| `logic/SwingArc.cs` | |
| `logic/SwingArm.cs` | |
| `logic/HandOver.cs` | |
| `logic/BodyPlay.cs` | answers |
| `logic/HaftGrip.cs` | |
| `logic/ArmUp.cs` | |
| `logic/HarpoonShot.cs` | |
| `logic/HarpoonLine.cs` | |
| `logic/Prompt.cs` | E |
| `logic/Seating.cs` | |
| `logic/FogMove.cs` | |
| `logic/Fittings.cs` | modules |
| `SeatedHeroes.cs` | porthole |
| `UsePrompt.cs` | |
| `logic/Tether.cs` | |
| `logic/DrillPace.cs` | |
| `logic/RunSounds.cs` | |
| `logic/ToolEdge.cs` | beam |
| `logic/ToolRays.cs` | hits |
| `logic/WeaponThrow.cs` | throw |
| `logic/ThrownDrawing.cs` | catch |
| `WreckPixels.cs` | |
| `ToolWork.cs` | |
| `logic/ToolHaze.cs` | |
| `logic/CutterJet.cs` | |
| `logic/Motor.cs` | |
| `logic/Remote.cs` | sent |
| `logic/RemoteRod.cs` | |
| `logic/RodFlourish.cs` | lights |
| `logic/Suits.cs` | |
| `logic/Skins.cs` | |
| `logic/Appearance.cs` | tab |
| `AppearanceView.cs` | |
| `SkinSlot.cs` | |
| `HeroDoll.cs` | |
| `logic/BladeFx.cs` | |
| `logic/FireFx.cs` | |
| `logic/RiftFx.cs` | |
| `BladeFxView.cs` | |
| `logic/Gestures.cs` | |
| `logic/Wheel.cs` | keys |
| `WheelView.cs` | |
| `logic/Pings.cs` | |
| `PingView.cs` | |
| `logic/SuitGlow.cs` | |
| `logic/TrackerBlink.cs` | |
| `Figure.cs` | |
| `logic/BodyFit.cs` | |
| `LightDrone.cs` | |
| `logic/DroneFlight.cs` | body |
| `logic/DroneWill.cs` | mind |
| `logic/DroneTow.cs` | tug |
| `logic/DroneThoughts.cs` | moods |
| `logic/FormsOpen.cs` | opens |
| `logic/DroneModes.cs` | |
| `logic/DroneSign.cs` | |
| `logic/DroneSwarm.cs` | |
| `logic/DroneFlurry.cs` | |
| `logic/WheelFade.cs` | |
| `logic/SheetHoloLook.cs` | |
| `logic/PlanPoint.cs` | |
| `CoreGlow.cs` | |
| `logic/Meeting.cs` | vision |
| `MeetingView.cs` | |
| `HeroView.Meeting.cs` | |
| `CutsceneView.cs` | grade |
| `logic/FormVision.cs` | lesson |
| `FormVisionView.cs` | |
| `HeroView.Vision.cs` | |
| `logic/Cape.cs` | |
| `logic/Spring.cs` | |
| `CapeView.cs` | |
| `HeroView.cs` | |
| `HeroView.Hands.cs` | |
| `HeroView.Guns.cs` | |
| `HeroView.Feel.cs` | |
| `logic/GunHand.cs` | cooldown |
| `HeroView.Others.cs` | |
| `logic/CameraFollow.cs` | |
| `JetPlume.cs` | |
| `logic/PlumeWay.cs` | drawn from nozzle |
| `RodLight.cs` | |
| `AimOutline.cs` | |
| `ModeGlyph.cs` | |
| `logic/DriverMode.cs` | |
| `HeroAutopilot.cs` | |
| `logic/Report.cs` | |
| `Frost.cs` | cold |
| `Haze.cs` | heat |
| `logic/BodyHeat.cs` | |
| `SuitFxView.cs` | |
| `logic/SuitFx.cs` | sums |
| `logic/ScreenEase.cs` | |
| `logic/Danger.cs` | gauge |
| `logic/AirCues.cs` | |
| `logic/CanistersSeen.cs` | tanks |
| `AirSwirl.cs` | |
| `SuitGauge.cs` | air |
| `MealSlots.cs` | |
| `logic/Retching.cs` | |
| `logic/RetchSpray.cs` | bile |
| `InkLayer.cs` | |
| `logic/MealTips.cs` | cards |
| `Thermometer.cs` | degrees |
| `logic/Thermometers.cs` | |
| `logic/CrustFill.cs` | |
| `logic/Tunnel.cs` | |
| `TunnelView.cs` | |
| `logic/VisorFog.cs` | |
| `VisorFogView.cs` | |
| `logic/HurtSeen.cs` | |
| `HurtView.cs` | |
| `SuitColours.cs` | |
| `LookEditor.cs` | |
| `logic/Markers.cs` | |
| `MarkerLayer.cs` | arrows |
| `logic/Bodies.cs` | |
| `BodyView.cs` | |
| `logic/Rams.cs` | |
| `HunterView.cs` | catchers |
| `CatcherFxView.cs` | light |
| `Glitch.cs` | bands |
| `logic/CatcherFx.cs` | fight |
| `logic/CatcherMotion.cs` | moves |
| `logic/Crew.En.cs` | |
| `logic/Crew.Ru.cs` | |

## Guarantees

- Lengths, speeds: cell/1000 (`Content.Hero`); step 1000/60 ms.
- A step never ends in a wall, shut door, hatch, off-grid; afloat, unjetted, within `Hero.Tether`; shut doors push
  it `Hero.PushStep` (0089). Doors go as shown (0014); presses send `SetDoor`, `SetValve` (0071), `SetPlatform`.
- **Holes, floors, steps** (0106): falls in holes it fits; down drops through; jump wins; a cell up, down; two stop.
- **Jump** (0089): coyote, buffer, short hop; **figure answers motion** (0090, hero-gait).
- **Rammed** (0103), **arced** (`Rams.Hits`): `HitBy`/`Bolted` sent, thrown, keys masked; none spared.
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
- **Left** (0107, 0108): screwdriver works plans, repairs; plan, wiring, lays cables.
- **Right, a throw** (companion-fights): `WeaponThrow` hits, the drone hauls; caught in the hand held out.
- **One aim** (`Aiming.AimAt`): plan (back walls first), machine (0099), tank, door, valve, wreck, else nearest small
  piece; outlined green/red, `UsePrompt` why; plan in hand, E idle; armed, small ones.
- **Hits are rays** (tool-reach, hits-pass-wrecks): a fan (`ToolRays`) to walls, through wrecks; **sparks** the haze's
  (spark-rule). **Struck** (0038, swing-motions): by sector, as the blade crosses the aim.
- **Harpoon, grapple** (0044, grapple-climbs): wind held, pull to walls; hung, refire; Space/RMB let go.
- **Gun** (hold-to-fire): held fires each cooldown, `Charge` on release; kicks (body-answers).
- **Drill, grinder, cutter** (0054, 0055): held runs (`Run`), hits a `Pace`, the cutter by beam (`ToolEdges`).
- **Power driver** (driver-at-work, driver-order): charged, a 2×2 `WorkedSquare`; RMB toggles (mode-glyph).
- **Gestures** (gestures-for-words): H points, pings (crew-pings), held a wheel, the drone signs. **Drone**
  (light-drone, drone-threads, drone-lessons): E on the core pairs (drone-companion, drone-meeting); F next form, held a wheel
  (thread-icons); window corners, flurry (drone-windows); in its player's colour (drone-colour); springs after it,
  perches, shines in open air (drone-spring); flown one way, tugs (drone-tow); warns (drone-motion), feels
  (drone-thoughts); a lesson held in a vision (form-vision).
- **Seated** (`Seating`): no tool; E leaves. **Ladders, ledges** (0092, 0112): grab, climb, slide, leap; hatches
  climbed, short jumps mantle.

## Deliberately not handled

- Friends marked only while heard, unnamed; markers overlap.
- A body dressed as its peer now; it, a seated hero: no suit lights.
- Others' swings never hold on contact; a friend's gun hangs unaimed.
