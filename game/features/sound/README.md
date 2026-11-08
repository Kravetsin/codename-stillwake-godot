# sound (client): what is heard

## Files

| File | Purpose |
| --- | --- |
| `AudioCensus.cs` | Every 10 s a log line: sound players, those playing, fps, the hearing |
| `logic/AudioTally.cs` | What `AudioCensus` counts, as one line |
| `logic/GroundSounds.cs` | What a planet's ground sounds like (0093), chosen from what the sim says is there: the boots' step, push-off and … |
| `logic/Hearing.cs` | The hearing's arithmetic, apart from Godot: how the buses sound at a share of the way from aboard (0) to floating … |
| `logic/Motors.cs` | A motor heard as it runs (0059), apart from Godot: how fast it spins, from still (0) to full (1), linearly over its … |
| `SoundView.cs` | The hearing (ADR 0033, 0042): a click for every button; floating in space, the World bus's low-pass falling as it … |
| `logic/Strikes.cs` | A train of one-shots heard while something works, apart from what it does (0054): the drill's strikes, a hammer … |
| `logic/VoiceStarts.cs` | Which voice of a placed player starts a sound: a second before Godot's next physics tick on a spare voice … |

What is heard (0033), in space (0042), machines at work (0059), drones (0062) and a planet (0093). Presentation only:
nothing in `sim/` or `net/` makes or knows of a sound; each peer plays what it sees.

Elsewhere: `game/Content/logic/Sound.cs` (cues and tunables); `game/default_bus_layout.tres` (Master → UI, Ambient, Music,
Effects → World, Suit, Helmet); `game/shared/` `Cues.cs`, `ActSound.cs`, `Heard2D.cs`, `Voices.cs`; `game/audio/`
(WAV one-shots, OGG loops; `CREDITS.md`: CC0 or marked placeholders).

## Rules

- **Owns** the hearing. `Main` adds `SoundView` first; it asks `HeroView.Floats` whether this player's hero floats in
  space. The title, a world with no hero, the dev worlds and a planet (0076) never float.
- **Floating** (0042): over `Sound.MuffleMs` the World bus's low-pass eases to `MuffledHz` as it fades to silence; the
  Suit bus, what the hero touches, dulls to a thud (low-pass to `SuitMuffledHz`, low shelf up) and rises
  `SuitFloatingDb` (0059). Helmet, the alarm's, is never touched. The reverse back aboard (`Hearing.At`).
- **Fog** (fog-edges): in a planet's fog the World bus's low-pass eases toward `MuffledHz` by the fog at the hero
  (`FogShare.Column`, `Hearing.At`'s `fogged`), as floating dulls it; its gain and the Suit bus are left alone.
- **Reach** (0033, 0059): a placed sound is full up to its cue's `Reach.Full` cells from the screen's middle, then fades
  as the square of the way left to `Silent`. Every station at work carries alike (`Sound.StationReach`) and about as
  loud as the next. `Heard2D` turns Godot's fall-off off and sets the volume each frame; Godot still pans.
- **Starts**: Godot starts a placed sound on the next physics tick, a second play before it replacing the first. So
  physics ticks 60 a second (the client has none; at 20 a drill lost half its hits), and `Heard2D` gives a second
  play in one tick a spare voice (`VoiceStarts`), up to its `voices`.
- **Tool idle** (0068): a tool working nothing (`DrillWhir`, `Swing`) plays at the one `Sound.ToolIdleDb`.
- **Music** (0035, 0042) on Music, loops stopped once silent: the title's track while the title shows; the bed of
  space (`Sound.Space`) while the hero floats, the vents' hum taking over aboard.
- **Reset** (0033): the `AudioServer` outlives `ReloadCurrentScene`, so `SoundView._Ready` sets World and Suit clear.
- **Clicks** (0033, 0081): every `BaseButton` in the tree (found on `_Ready`, then by `NodeAdded`, connected once) and
  whatever asks through `Ui.Clicked` (inventory actions, a canister breathed): `Sound.Click` on UI, once a frame at most.
- **Loops begin anywhere along themselves**, so two never play in step.

## Hooks elsewhere

Each feature owns its players as children, never in a static.

- `crew/Figure.cs` (0033, 0093): steps each half stride; a push-off leaving the deck or a ladder (`Poses.Jumped`), not
  walking off an edge; a landing (`Poses.Landed`). On a planet by `Figure.Footing` (`GroundSounds.FootingOf`), soft or
  heavy by `HeroMotion.Landing.Heavy`. On World; others' positional, their jumps from their sent motion.
- `crew/HeroView.cs`, `items/ItemsView.cs` (0033, 0042): on Suit (`ActSound`), `Sound.Act` for a crate opened, a
  canister loaded, debris caught, an item taken; floating, the glove's thump (`ActInSpace`).
- `crew/HeroView.cs`, harpoon (0044, 0065): own on Suit: the shot, the bite (`Chop` on metal, the action's thump on a
  crate or pod), the drum loop while it reels (never on a miss), `Sound.Ready` as the cooldown ends with it in hand.
  Others' on World at `Sound.ToolReach`, silent while this hero floats; no ready cue.
- `crew/HeroView.cs`, drill (0054, 0059): on Suit, its whir as grains in a `StrikeTrain`, never a looped recording,
  rate and pitch by `MotorSound` (`Sound.DrillMotor`), spun with the bit's picture; loaded on a piece the sim would let
  it hit, held `DrillTouchHoldS` (`TouchHold`). Its hits on a wreck are `Chop`, `DrillHitDb` quieter (0093).
- `crew/HeroView.cs`, axe (0068): `Sound.Swing` every swing, its peak mid downstroke. Jetpack (0047, 0059): `Sound.Thrust` loop on Suit while it thrusts (`LoopEnvelope`). A canister breathed
  (`AirFill`) and room air again (`Inhale`) on Helmet (0081), a friend's on World. Others' swings, drills, jetpacks:
  silent.
- `power/ChargerLights.cs` (0059): `ChargerHum` while charging; `ChargeFull` as a battery comes full (`CameFull`).
- `logistics/LogisticsView.cs` (0062): each drone's rotors (`DroneWhir`, `DroneMotor`), tuned by id (`Whir.cs`), never in
  unison; `DroneDock` at lift-off and touch-down; `DroneGrab` as its clamp takes or puts. A low drone: its amber lamp.
- `crafting/StationSounds.cs` (0059): told each frame by `StationView` whether it works (`CraftingModule.Works`) and
  its picture step. The Former: a stroke on the picture its head comes down on (`FrameBeat`), over its motor loop
  (`FormerSpin`), never pounding. The smelter: its hum (`LoopEnvelope`) and pops at random moments (`RandomTrain`).
- `grid/GridView.cs` (0014): a door opening or closing, a valve turned, as the drawing catches up: anyone's, own at once.
- `furnace/FurnaceView.cs` (0059): each fire's crackle loop.
- `crew/SuitGauge.cs` (0033): on Helmet, the alarm while the suit runs low.
- `building/BuildingView.cs` (0033): the screwdriver's ratchet as a plan's work sparks.
- `debris/DebrisView.cs` (0034, 0042): on Suit, this player's own chop as a wreck's damage rises, and the break its own
  hit made (`OwnHits.cs`: a `Hit` of this peer's that left `InFlight`). Another's is silent: a wreck is only in space.
- On a planet (0093, 0094), on World but own hits on Suit: `crew/HeroView.cs` plays every tool's hit as what it hits
  (`GroundSounds.HitAt`), heard on the face turned toward the hero (`Impacts.On`); the drill at its hit pace, a
  friend's from `Items.Running` and its `Busy` bit; lava's hiss (`LavaBurn`). `building/DigView.cs` a wall broken, by
  material. `flora/FloraView.cs`, `flora/CaveView.cs`, `ruins/HomesView.cs` a growth felled, by kind (`Unheard`
  none); cave drips. `grid/LavaView.cs` the rumble at the nearest open lava and its bubbles.
- `hazards/MeteorView.cs`: `Break` or `Chop`.
- `atmosphere/AirView.cs` (0033): the hiss at the nearest venting stream, the hum at the nearest vent with oxygen.

## Deliberately not handled

- Music beyond the title and space.
- Reverb, occlusion, Doppler; one hiss for every breach. Silence by the room's air (0033).
- Others' clicks, alarms, swings; swim, passing out, plans, the analyzer, the pod, the inventory: later passes.
- A jump with its head at a ceiling is silent; another's jump heard over ten steps into its rise is missed.
- The circuit assembler is silent till its audition. A station taken down mid-work stops at once; the Former strikes
  from its first picture, before its motor is at speed.
- On a planet (0093): ladders, cave air and wind; a friend's drill's whir; rugs sound as the floor under them; the
  deck's landing has one loudness. A wall broken without a crack first would be silent; no weapon does that.
