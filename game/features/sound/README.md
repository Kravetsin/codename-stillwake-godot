# sound (client): what is heard

The first sound pass (ADR 0033), what is heard in space (0042), machines heard at work (0059) and drones (0062). Presentation only: nothing in
`sim/` or `net/` makes or knows of a sound; each peer plays what it sees.

| File                           | Holds                                                                                          | Godot |
| ------------------------------ | ---------------------------------------------------------------------------------------------- | ----- |
| `game/Content/Sound.cs`        | every cue's files and volume, the bus names, the muffle's, the bed's and the alarm's tunables  | no    |
| `game/default_bus_layout.tres` | the buses: Master → UI, Ambient, Music, Effects → World (a low-pass), Suit (a low-pass and a low shelf), Helmet | — |
| `game/shared/Cues.cs`          | a cue's stream (a randomizer over its files, ±6% pitch, or a loop) and its players, at its tune | yes   |
| `game/shared/ActSound.cs`      | an action's sound on Suit: the click aboard, the glove's thump floating; crew and items each hold one | yes   |
| `game/shared/Heard2D.cs`       | a sound placed in the world, as loud as its distance from the screen's middle says              | yes   |
| `Hearing.cs`                   | the buses at a share of the way into space (`Mix`), and the easing of a share                   | no    |
| `Strikes.cs`                   | a train of one-shots at a rate, each its own pitch and volume (`StrikeTrain`); a loop's attack and release (`LoopEnvelope`); a one-shot in step with a picture (`FrameBeat`); one-shots at random moments (`RandomTrain`) | no    |
| `Motors.cs`                    | a motor's spin and load, and from them its pitch scale and volume (`MotorSound`); a touch held on past a flicker (`TouchHold`) | no    |
| `SoundView.cs`                 | the hearing: button clicks, the muffle while floating, the title's music and the bed of space   | yes   |
| `game/audio/`                  | the files, one-shots as WAV and loops as OGG looped on import; `CREDITS.md`, CC0 or marked placeholders. Synthesised loops and pops are rendered offline from the audition's parameters (deterministic seeds; a loop's second of two periods kept, so it closes on itself; −23 dBFS RMS) | — |

- **Owns:** the hearing. `Main` adds `SoundView` first, asking it whether this player's placed hero
  floats out in space (`HeroView.Floats`). The title, a world with no hero yet and the dev worlds
  never float.
- **Floating (0042):** over `Sound.MuffleMs` the World bus's low-pass eases from `ClearHz` to
  `MuffledHz` (in log space) as the bus fades out to silence; over the same time the Suit bus — what
  the hero itself touches — dulls to a thud: its low-pass (24 dB an octave) falls to `SuitMuffledHz`
  and its low shelf under `SuitLowsHz` rises to `SuitLowsGain`, and the bus rises `SuitFloatingDb` (6 dB, 0059), so
  what the hero touches stands out of the bed. Nothing touched leaves the muffle, the jetpack neither (0059).
  Helmet, the alarm's, is never touched.
  The reverse back aboard. `Hearing.At` gives the settings, `SoundView` sets them.
- **Reach:** a sound placed in the world carries as its cue's `Sound.Reach` says: full volume up to
  `Full` cells from the middle of the screen (the hero), then fading as the square of what is left of
  the way to `Silent`, where it stops. A vent hums loud only close by (3 → 10), doors and venting carry far
  (6 → 24). Every station at work carries alike (`Sound.StationReach`, 2 → 10, 0059) and is about as loud as the
  next: about −32 LUFS at its loudest 400 ms within 2 cells, under a step, a door or any tool. Godot's own fall-off has no
  full-volume stretch, so `Heard2D` turns it off and sets the volume each frame; Godot still pans.
- **Music** on the Music bus, each a loop at a share of its volume, stopped once silent: the title's
  track (0035) faded in over `Sound.MusicFadeS` while the title is shown and out once it is gone; the
  bed of space (`Sound.Space`, 0042) faded in over `SpaceInS` while the hero floats and out over
  `SpaceOutS` back aboard, where the vents' hum takes over; 11 dB under the title's track (0059).
- **Reset:** the `AudioServer` outlives `ReloadCurrentScene` (exit to title), so `SoundView._Ready`
  sets the World and Suit buses clear again.
- **Clicks:** every `BaseButton` in the tree, found once on `_Ready` and then by `SceneTree.NodeAdded`,
  its `Pressed` connected once (a reparented button enters the tree again).
- **Hooks elsewhere**, each feature owning its players as children, never in a static:
  - `crew/Figure.cs` — boots on the hollow metal deck: a step at each half stride walking or running;
    a jump's push-off (`Sound.Jump`) as the hero leaves the deck or a ladder rising at least half a
    jump's speed (`Poses.Jumped`), so walking off an edge, falling, makes none; a landing coming down
    from the air (`Poses.Landed`), not at a jump's top where the controller leaves a step without
    speed. The landing is the steps' own files, 4 dB louder and at 0.85 of their pitch (`Cue.Tune`).
    All on World: this player's hero heard the same anywhere, the others' positional
    (`AudioStreamPlayer2D`), their jumps read from their sent motion as their own peer's are; nothing
    new goes over the wire.
  - `crew/HeroView.cs` and `items/ItemsView.cs` — on Suit, through `ActSound`: the one soft action click
    (`Sound.Act`) for a crate opened, a canister loaded, debris caught, an item picked up; while this
    player's hero floats (`HeroView.Floats`, which the hearing reads too) the glove's low thump
    (`Sound.ActInSpace`) in its place, dulled by the Suit muffle like all there is (0042).
  - `crew/HeroView.cs` — this player's harpoon (0044), on Suit: the shot (`Sound.Shot`) as the rod
    leaves; the rod biting where it struck, the frame it does, `Sound.Chop` on metal (a wreck or salvage)
    or the action's thump (`ActSound`) on a crate or pod; the drum (`Sound.Drum`, a loop) from the click
    that reels till the piece is in or the line comes free, never on a miss; and `Sound.Ready` as the
    hero's cooldown ends with a harpoon in hand, the gun ready, not the rod back. No sound of the line
    going taut; others' harpoons are silent.
  - `crew/HeroView.cs` — this player's drill (0054, 0059), on Suit at its bit: its motor whenever it spins
    (`Sound.DrillWhir`), grains of a drill running free, never a looped recording: `DrillWhirGrainsPerSecond` (9) a
    second times the motor's speed, each its pitch ±`DrillWhirPitch` (2%) about that speed (`StrikeTrain`), the speed and
    volume from `MotorSound` with `Sound.DrillMotor`: spun up and down with the bit's picture (`HeldLook.SpinUpMs`, 120 and
    700 ms), pitch 0.35 + 0.65·spin, full volume from two thirds of its spin, and on a piece 8% lower and 3 dB quieter,
    eased in over 60 ms and out over 150 ms. While it runs with its bit on a piece the sim would let it hit, held on
    `DrillTouchHoldS` (0.1 s) after the bit last was (`TouchHold`), so grazing an edge never breaks into bursts,
    `Sound.DrillStrike` (a pickaxe on metal cut to 50 ms) at
    `Sound.DrillStrikesPerSecond` (18, a hammer drill's buzz, apart from the damage's 10 hits a second), the
    first at once, each its pitch scale within ±`DrillStrikePitch` (12%) and its volume within
    ±`DrillStrikeVolumeDb` (1.5 dB) (`StrikeTrain`); stopped, voices and all, the frame the drill stops or the hold
    runs out. Its hits play no chop. And this player's jetpack (0047), on Suit, muffled out in space
    as all there is: `Sound.Thrust`, a loop, while it thrusts, played loud (+3 dB) so its hiss comes through the muffle
    about 6 dB under the bed of space (0059),
    risen over `ThrustAttackS` (40 ms) and fallen over `ThrustReleaseS` (140 ms), linear in amplitude
    (`LoopEnvelope`), then stopped; and a canister breathed (`Sound.Breathe`), on Suit as well. Others' drills and
    jetpacks are silent.
  - `power/ChargerLights.cs` — each charging station, on World where it stands: `Sound.ChargerHum`, a
    positional loop faded in over `HumInS` while it charges and out over `HumOutS`, begun
    somewhere along its 4 s; and `Sound.ChargeFull` once as a battery charging there comes full
    (`Charges.CameFull`), not when one is put in full. The hum is a station's (0059).
  - `logistics/LogisticsView.cs` — drones (0062), on World where they are, a station's reach, all quieter than a
    station at work (−32 LUFS): each drone's rotors (`Sound.DroneWhir`, the audition's synth "Fan", −36 LUFS standing),
    one positional loop a drone, begun somewhere along its 2 s, its motor (`MotorSound` with `Sound.DroneMotor`) spun up
    over 0.5 s from 0.4 of its pitch as it lifts off and down over 0.7 s once it touches down or drops, its volume full
    from two thirds of its spin. From the drone's drawn speed, 0 to 1 of a cruising drone's: its pitch 6% higher and its
    volume 2 dB louder at full speed; a wobble of three slow sines (0.7, 1.9 and 4.3 Hz, shares 0.5, 0.3, 0.2, each
    drone its own phases) sways its pitch ±12 cents and its volume ±0.5 dB; each drone is tuned within ±4% by its id,
    so a few never hum in unison (`logistics/Whir.cs`). A thud (`Sound.DroneDock`, a soft impact and a latch 60 ms
    after it, −33 LUFS) as a drone lifts off its pad and as it touches down on one, the same sound both ways, as the user
    picked; a click (`Sound.DroneGrab`, a small plastic switch, −35 LUFS) as its clamp takes a stack and as it puts some
    or all of it in (`Served.Gripped`). No warning for a low drone: its blinking amber lamp says it.
  - `crafting/StationSounds.cs` — each station at work (0059), on World at its middle, told every frame by
    `StationView` whether it works (`CraftingModule.Works`) and which step of its pictures it shows, each machine its
    own players. The Former: `Sound.FormerStroke` once each time its pictures pass the one its head comes down on (the
    third of four, every 0.44 s; `FrameBeat`, so a slow frame still strikes once), its pitch ±`FormerStrokePitch` (15%)
    and volume ±`FormerStrokeVolumeDb` (1.5 dB); under it `Sound.FormerMotor`, a loop driven by `MotorSound` with
    `Sound.FormerSpin`: spun up over 0.6 s from half its pitch, run down over 0.8 s after it stops, kicked 3% lower at
    each stroke and back over 120 ms. The electric smelter: `Sound.SmelterHum`, a loop risen over `SmelterHumInS` and
    fallen over `SmelterHumOutS` (`LoopEnvelope`), and its melt's pops (`Sound.SmelterCrackle`, six synthesised one-shots)
    at random moments, `SmelterCracklesPerSecond` (2) on average (`RandomTrain`), each ±`SmelterCracklePitch` (10%) and
    up to `SmelterCrackleLowDb` (10 dB) quieter. Loops begin somewhere along themselves. Each station as loud as the
    next; the Former's stroke only 3 dB over its motor, so it never pounds.
  - `grid/GridView.cs` — on World, where it stands: a door opening or closing (once, at its top cell)
    and a valve turned, found as the drawing catches up with the grid, so anyone's are heard and this
    player's own at once (0014).
  - `furnace/FurnaceView.cs` — each burning furnace's or burner generator's crackle, a positional loop on its fire, a station's (0059), begun at
    a random point so two fires never crackle in step.
  - `crew/SuitGauge.cs` — on Helmet, the alarm every `Sound.AlarmEveryS` while the suit runs low; none
    passed out.
  - `building/BuildingView.cs` — the screwdriver's ratchet: one positional player, four voices, moved
    to each plan as its work sparks.
  - `debris/DebrisView.cs` — on Suit, where the wreck is: this player's own axe's chop (`Sound.Chop`,
    0034) as the drawing sees the wreck's damage rise, and the break where a wreck its own hit broke
    bursts. Own is told by `debris/OwnHits.cs`: a `Hit` of this peer's that has left `Client.InFlight`
    since the last frame. Another hero's hit is silent: a wreck is only out in space (0042).
  - `atmosphere/AirView.cs` — two looped positional players, each at its source nearest the middle
    of the screen and stopped without one: the hiss at a venting stream, and the ship's hum at a vent
    with oxygen behind it (its room's networks hold some or make some). The hum is the vents', not the
    air's: a room aired by a pod alone, or the oxygen run out, is quiet.

## Deliberately not handled

- Music in a world beyond the spacewalk; volume sliders (the bus names are their contract).
- Reverb by room size, occlusion by walls, Doppler; one hiss however many breaches vent.
- Silence by the room's air: only floating mutes the world (0033).
- Other heroes' clicks and alarms; a sound for the swing itself; swim, passing out, plans placed
  and finished, the analyzer and the pod, the inventory and the hand-drawn controls (`SlotBox`,
  hotbar): later passes.
- A jump with its head against a ceiling at once has no speed to read and is silent; another's jump
  heard more than ten steps into its rise (a send held up that long) is missed.
- A loop still playing as the game quits is reported by Godot as leaked at exit; harmless.
- The circuit assembler is silent till its audition. A station taken down mid-work stops at once, without running
  down. The Former strikes from its first picture down, not once its motor is at speed as the audition did: the
  stroke follows the head seen, and the head moves at once.
