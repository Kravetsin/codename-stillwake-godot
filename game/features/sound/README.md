# sound (client): what is heard

The first sound pass (ADR 0033). Presentation only: nothing in `sim/` or `net/` makes or knows of a
sound; each peer plays what it sees.

| File                          | Holds                                                                                   | Godot |
| ----------------------------- | --------------------------------------------------------------------------------------- | ----- |
| `game/Content/Sound.cs`       | every cue's files and volume, the bus names, the muffle and the alarm's tunables         | no    |
| `game/default_bus_layout.tres`| the buses: Master → UI, Ambient, Effects → World (a low-pass), Suit                       | —     |
| `game/shared/Cues.cs`         | a cue's stream (a randomizer over its files, ±6% pitch, or a loop) and its players       | yes   |
| `SoundView.cs`                | the hearing: button clicks, the room tone, the muffle out of air and the breath           | yes   |
| `game/audio/`                 | the files, one-shots as WAV and loops as OGG looped on import; `CREDITS.md`, all CC0      | —     |

- **Owns:** the hearing. `Main` adds `SoundView` first, handing it `Ears`: aboard a session, muffled
  while its placed hero's body cell (`HeroView.Current`) is not aired (`Atmosphere.AiredAt`), out while
  passed out. The title, a world with no hero yet and the dev worlds are never muffled.
- **Muffled:** over `Sound.MuffleMs` the World bus's low-pass eases from `ClearHz` to `MuffledHz` (in
  log space) and the bus drops `MuffledDb`, the room tone fades out and the breath fades in on Suit;
  the reverse in air. Passed out, the breath stops. Own steps stay clear: they are on Suit.
- **Reset:** the `AudioServer` outlives `ReloadCurrentScene` (exit to title), so `SoundView._Ready`
  sets the World bus clear again.
- **Clicks:** every `BaseButton` in the tree, found once on `_Ready` and then by `SceneTree.NodeAdded`,
  its `Pressed` connected once (a reparented button enters the tree again).
- **Hooks elsewhere**, each feature owning its players as children, never in a static:
  - `crew/Figure.cs` — a step at each half stride walking or running, a landing from the air; this
    player's hero on Suit, the others' positional (`AudioStreamPlayer2D`) on World.
  - `crew/SuitGauge.cs` — the alarm every `Sound.AlarmEveryS` while the suit runs low; none passed out.
  - `building/BuildingView.cs` — the screwdriver: one positional player, four voices, moved to each
    plan as its work sparks.
  - `atmosphere/AirView.cs` — the hiss: one looped positional player at the venting stream nearest
    the middle of the screen; stopped when nothing vents.

## Deliberately not handled

- Music and a Music bus; volume sliders (the bus names are their contract).
- Reverb by room size, occlusion by walls, Doppler; one hiss however many breaches vent.
- A source silenced because it sits in vacuum: only the listener's air decides.
- Other heroes' breath and alarms; jump, swim, passing out, plans placed and finished, machines,
  doors, debris, inventory and the hand-drawn controls (`SlotBox`, hotbar): later passes.
- A loop still playing as the game quits is reported by Godot as leaked at exit; harmless.
