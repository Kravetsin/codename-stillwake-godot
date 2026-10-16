# sound (client): what is heard

The first sound pass (ADR 0033). Presentation only: nothing in `sim/` or `net/` makes or knows of a
sound; each peer plays what it sees.

| File                          | Holds                                                                                   | Godot |
| ----------------------------- | --------------------------------------------------------------------------------------- | ----- |
| `game/Content/Sound.cs`       | every cue's files and volume, the bus names, the muffle and the alarm's tunables         | no    |
| `game/default_bus_layout.tres`| the buses: Master → UI, Ambient, Effects → World (a low-pass), Suit                       | —     |
| `game/shared/Cues.cs`         | a cue's stream (a randomizer over its files, ±6% pitch, or a loop) and its players       | yes   |
| `SoundView.cs`                | the hearing: button clicks and the silence out of air                                     | yes   |
| `game/audio/`                 | the files, one-shots as WAV and loops as OGG looped on import; `CREDITS.md`, all CC0      | —     |

- **Owns:** the hearing. `Main` adds `SoundView` first, asking it whether to muffle: while
  this player's placed hero's body cell (`HeroView.Current`) is not aired (`Atmosphere.AiredAt`). The title, a world with no hero yet and the dev worlds are never muffled.
- **Muffled:** over `Sound.MuffleMs` the World bus's low-pass eases from `ClearHz` to `MuffledHz` (in
  log space) as the bus fades out to silence; the reverse in air. Only Suit is left:
  what the hands touch and the alarm.
- **Reset:** the `AudioServer` outlives `ReloadCurrentScene` (exit to title), so `SoundView._Ready`
  sets the World bus clear again.
- **Clicks:** every `BaseButton` in the tree, found once on `_Ready` and then by `SceneTree.NodeAdded`,
  its `Pressed` connected once (a reparented button enters the tree again).
- **Hooks elsewhere**, each feature owning its players as children, never in a static:
  - `crew/Figure.cs` — a step at each half stride walking or running, a landing from the air; all on
    World: this player's hero heard the same anywhere, the others' positional (`AudioStreamPlayer2D`).
  - `crew/HeroView.cs` — on Suit, what this hero's hands do: a clank for a grabbed piece of debris, a
    metal box for a crate opened or a canister loaded into a tank.
  - `grid/GridView.cs` — on World, where it stands: a door opening or closing (once, at its top cell)
    and a valve turned, found as the drawing catches up with the grid, so anyone's are heard and this
    player's own at once (0014).
  - `items/ItemsView.cs` — on Suit, parts dropped into a pouch for each item picked up.
  - `crew/SuitGauge.cs` — the alarm every `Sound.AlarmEveryS` while the suit runs low; none passed out.
  - `building/BuildingView.cs` — the screwdriver: one positional player, four voices, moved to each
    plan as its work sparks.
  - `atmosphere/AirView.cs` — two looped positional players, each at its source nearest the middle
    of the screen and stopped without one: the hiss at a venting stream, and the ship's hum at a vent
    with oxygen behind it (its room's networks hold some or make some). The hum is the vents', not the
    air's: a room aired by a pod alone, or the oxygen run out, is quiet.

## Deliberately not handled

- Music and a Music bus; volume sliders (the bus names are their contract).
- Reverb by room size, occlusion by walls, Doppler; one hiss however many breaches vent.
- A source silenced because it sits in vacuum: only the listener's air decides.
- Other heroes' hands and alarms; a drone in vacuum; jump, swim, passing out, plans placed and finished, machines,
  doors, debris, inventory and the hand-drawn controls (`SlotBox`, hotbar): later passes.
- A loop still playing as the game quits is reported by Godot as leaked at exit; harmless.
