# sound (client): what is heard

The first sound pass (ADR 0033), and what is heard in space (0042). Presentation only: nothing in
`sim/` or `net/` makes or knows of a sound; each peer plays what it sees.

| File                           | Holds                                                                                          | Godot |
| ------------------------------ | ---------------------------------------------------------------------------------------------- | ----- |
| `game/Content/Sound.cs`        | every cue's files and volume, the bus names, the muffle's, the bed's and the alarm's tunables  | no    |
| `game/default_bus_layout.tres` | the buses: Master → UI, Ambient, Music, Effects → World (a low-pass), Suit (a low-pass and a low shelf), Helmet | — |
| `game/shared/Cues.cs`          | a cue's stream (a randomizer over its files, ±6% pitch, or a loop) and its players, at its tune | yes   |
| `game/shared/ActSound.cs`      | an action's sound on Suit: the click aboard, the glove's thump floating; crew and items each hold one | yes   |
| `game/shared/Heard2D.cs`       | a sound placed in the world, as loud as its distance from the screen's middle says              | yes   |
| `Hearing.cs`                   | the buses at a share of the way into space (`Mix`), and the easing of a share                   | no    |
| `Strikes.cs`                   | a train of one-shots at a rate, each its own pitch and volume (`StrikeTrain`); a loop's attack and release (`LoopEnvelope`) | no    |
| `SoundView.cs`                 | the hearing: button clicks, the muffle while floating, the title's music and the bed of space   | yes   |
| `game/audio/`                  | the files, one-shots as WAV and loops as OGG looped on import; `CREDITS.md`, CC0 or marked placeholders | — |

- **Owns:** the hearing. `Main` adds `SoundView` first, asking it whether this player's placed hero
  floats out in space (`HeroView.Floats`). The title, a world with no hero yet and the dev worlds
  never float.
- **Floating (0042):** over `Sound.MuffleMs` the World bus's low-pass eases from `ClearHz` to
  `MuffledHz` (in log space) as the bus fades out to silence; over the same time the Suit bus — what
  the hero itself touches — dulls to a thud: its low-pass (24 dB an octave) falls to `SuitMuffledHz`
  and its low shelf under `SuitLowsHz` rises to `SuitLowsGain`. Helmet, the alarm's, is never touched.
  The reverse back aboard. `Hearing.At` gives the settings, `SoundView` sets them.
- **Reach:** a sound placed in the world carries as its cue's `Sound.Reach` says: full volume up to
  `Full` cells from the middle of the screen (the hero), then fading as the square of what is left of
  the way to `Silent`, where it stops. A vent hums loud only close by (3 → 10), a fire crackles a
  little further (3 → 12), doors and venting carry far (6 → 24). Godot's own fall-off has no
  full-volume stretch, so `Heard2D` turns it off and sets the volume each frame; Godot still pans.
- **Music** on the Music bus, each a loop at a share of its volume, stopped once silent: the title's
  track (0035) faded in over `Sound.MusicFadeS` while the title is shown and out once it is gone; the
  bed of space (`Sound.Space`, 0042) faded in over `SpaceInS` while the hero floats and out over
  `SpaceOutS` back aboard, where the vents' hum takes over.
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
  - `crew/HeroView.cs` — this player's drill (0054), on Suit at its bit: while it runs with its bit on a
    piece the sim would let it hit, `Sound.DrillStrike` (a pickaxe on metal cut to 50 ms) at
    `Sound.DrillStrikesPerSecond` (18, a hammer drill's buzz, apart from the damage's 10 hits a second), the
    first at once, each its pitch scale within ±`DrillStrikePitch` (12%) and its volume within
    ±`DrillStrikeVolumeDb` (1.5 dB) (`StrikeTrain`); stopped, voices and all, the frame it stops. Its hits
    play no chop. And this player's jetpack (0047), on Suit: `Sound.Thrust`, a loop, while it thrusts,
    risen over `ThrustAttackS` (40 ms) and fallen over `ThrustReleaseS` (140 ms), linear in amplitude
    (`LoopEnvelope`), then stopped. Others' drills and jetpacks are silent.
  - `power/ChargerLights.cs` — each charging station, on World where it stands: `Sound.ChargerHum`, a
    positional loop (2 → 8 cells) faded in over `HumInS` while it charges and out over `HumOutS`, begun
    somewhere along its 4 s; and `Sound.ChargeFull` once as a battery charging there comes full
    (`Charges.CameFull`), not when one is put in full. The hum is set level with a vent's (about −33 LUFS
    as played).
  - `grid/GridView.cs` — on World, where it stands: a door opening or closing (once, at its top cell)
    and a valve turned, found as the drawing catches up with the grid, so anyone's are heard and this
    player's own at once (0014).
  - `furnace/FurnaceView.cs` — each burning furnace's crackle, a positional loop on its fire, begun at
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
