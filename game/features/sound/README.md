# sound (client): what is heard

The first sound pass (ADR 0033), and what is heard in space (0041). Presentation only: nothing in
`sim/` or `net/` makes or knows of a sound; each peer plays what it sees.

| File                           | Holds                                                                                          | Godot |
| ------------------------------ | ---------------------------------------------------------------------------------------------- | ----- |
| `game/Content/Sound.cs`        | every cue's files and volume, the bus names, the muffle's, the bed's and the alarm's tunables  | no    |
| `game/default_bus_layout.tres` | the buses: Master → UI, Ambient, Music, Effects → World (a low-pass), Suit (a low-pass and a low shelf), Helmet | — |
| `game/shared/Cues.cs`          | a cue's stream (a randomizer over its files, ±6% pitch, or a loop) and its players              | yes   |
| `game/shared/Heard2D.cs`       | a sound placed in the world, as loud as its distance from the screen's middle says              | yes   |
| `Hearing.cs`                   | the buses at a share of the way into space (`Mix`), and the easing of a share                   | no    |
| `SoundView.cs`                 | the hearing: button clicks, the muffle while floating, the title's music and the bed of space   | yes   |
| `game/audio/`                  | the files, one-shots as WAV and loops as OGG looped on import; `CREDITS.md`, CC0 or marked placeholders | — |

- **Owns:** the hearing. `Main` adds `SoundView` first, asking it whether this player's placed hero
  floats out in space (`HeroView.Floats`). The title, a world with no hero yet and the dev worlds
  never float.
- **Floating (0041):** over `Sound.MuffleMs` the World bus's low-pass eases from `ClearHz` to
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
  bed of space (`Sound.Space`, 0041) faded in over `SpaceInS` while the hero floats and out over
  `SpaceOutS` back aboard, where the vents' hum takes over.
- **Reset:** the `AudioServer` outlives `ReloadCurrentScene` (exit to title), so `SoundView._Ready`
  sets the World and Suit buses clear again.
- **Clicks:** every `BaseButton` in the tree, found once on `_Ready` and then by `SceneTree.NodeAdded`,
  its `Pressed` connected once (a reparented button enters the tree again).
- **Hooks elsewhere**, each feature owning its players as children, never in a static:
  - `crew/Figure.cs` — a step at each half stride walking or running, a landing from the air; all on
    World: this player's hero heard the same anywhere, the others' positional (`AudioStreamPlayer2D`).
  - `crew/HeroView.cs` and `items/ItemsView.cs` — on Suit, the one soft action click (`Sound.Act`): a
    crate opened, a canister loaded, debris caught, an item picked up.
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
    since the last frame. Another hero's hit is silent: a wreck is only out in space (0041).
  - `atmosphere/AirView.cs` — two looped positional players, each at its source nearest the middle
    of the screen and stopped without one: the hiss at a venting stream, and the ship's hum at a vent
    with oxygen behind it (its room's networks hold some or make some). The hum is the vents', not the
    air's: a room aired by a pod alone, or the oxygen run out, is quiet.

## Deliberately not handled

- Music in a world beyond the spacewalk; volume sliders (the bus names are their contract).
- Reverb by room size, occlusion by walls, Doppler; one hiss however many breaches vent.
- Silence by the room's air: only floating mutes the world (0033).
- Other heroes' clicks and alarms; a sound for the swing itself; jump, swim, passing out, plans placed
  and finished, the analyzer and the pod, the inventory and the hand-drawn controls (`SlotBox`,
  hotbar): later passes.
- The action click in space: a high click, it all but vanishes through the muffle (0041).
- A loop still playing as the game quits is reported by Godot as leaked at exit; harmless.
