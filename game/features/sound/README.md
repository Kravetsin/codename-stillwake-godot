# sound (client): what is heard

The first sound pass (ADR 0033). Presentation only: nothing in `sim/` or `net/` makes or knows of a
sound; each peer plays what it sees.

| File                           | Holds                                                                              | Godot |
| ------------------------------ | ---------------------------------------------------------------------------------- | ----- |
| `game/Content/Sound.cs`        | every cue's files and volume, the bus names, the muffle and the alarm's tunables    | no    |
| `game/default_bus_layout.tres` | the buses: Master → UI, Ambient, Music, Effects → World (a low-pass), Suit          | —     |
| `game/shared/Cues.cs`          | a cue's stream (a randomizer over its files, ±6% pitch, or a loop) and its players  | yes   |
| `game/shared/Heard2D.cs`       | a sound placed in the world, as loud as its distance from the screen's middle says  | yes   |
| `SoundView.cs`                 | the hearing: button clicks and the silence while floating                          | yes   |
| `game/audio/`                  | the files, one-shots as WAV and loops as OGG looped on import; `CREDITS.md`, all CC0 | —     |

- **Owns:** the hearing. `Main` adds `SoundView` first, asking it whether to muffle: while this
  player's placed hero floats out in space (`HeroView.Floats`). The title, a world with no hero yet and
  the dev worlds are never muffled.
- **Muffled:** over `Sound.MuffleMs` the World bus's low-pass eases from `ClearHz` to `MuffledHz` (in
  log space) as the bus fades out to silence; the reverse back aboard. Only Suit is left: the action
  click and the alarm.
- **Reach:** a sound placed in the world carries as its cue's `Sound.Reach` says: full volume up to
  `Full` cells from the middle of the screen (the hero), then fading as the square of what is left of
  the way to `Silent`, where it stops. A vent hums loud only close by (3 → 10), a fire crackles a
  little further (3 → 12), doors and venting carry far (6 → 24). Godot's own fall-off has no
  full-volume stretch, so `Heard2D` turns it off and sets the volume each frame; Godot still pans.
- **Music** (0035): the title's track on the Music bus, faded in over `Sound.MusicFadeS` while the title is
  shown and out once it is gone, stopped when silent; nothing plays in a world yet.
- **Reset:** the `AudioServer` outlives `ReloadCurrentScene` (exit to title), so `SoundView._Ready`
  sets the World bus clear again.
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
  - `crew/SuitGauge.cs` — the alarm every `Sound.AlarmEveryS` while the suit runs low; none passed out.
  - `building/BuildingView.cs` — the screwdriver's ratchet: one positional player, four voices, moved
    to each plan as its work sparks.
  - `debris/DebrisView.cs` — an axe's chop (`Sound.Chop`, 0034) where a piece is struck, whoever
    struck it, found as the drawing sees its damage rise, and a last one where a wreck breaks: one
    positional player, four voices, on Suit, since a wreck is only chopped out in space, where World is
    silent.
  - `atmosphere/AirView.cs` — two looped positional players, each at its source nearest the middle
    of the screen and stopped without one: the hiss at a venting stream, and the ship's hum at a vent
    with oxygen behind it (its room's networks hold some or make some). The hum is the vents', not the
    air's: a room aired by a pod alone, or the oxygen run out, is quiet.

## Deliberately not handled

- Music in a world; volume sliders (the bus names are their contract).
- Reverb by room size, occlusion by walls, Doppler; one hiss however many breaches vent.
- Silence by the room's air: only floating mutes the world (0033).
- Other heroes' clicks and alarms; a drone in space; jump, swim, passing out, plans placed and
  finished, the analyzer and the pod, the inventory and the hand-drawn controls (`SlotBox`, hotbar):
  later passes.
- A loop still playing as the game quits is reported by Godot as leaked at exit; harmless.
