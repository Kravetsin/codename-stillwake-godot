# dev-panel (client)

Development readouts and controls, ported from `apps/game/src/features/dev-panel/` (G-08): a panel in
the top-right corner, hidden at the start, shown and hidden again with the backquote key.

- **Reads**, four times a second and only while shown: frames a second (Godot's own count), the
  world's tick, the hash the client last sent, how long this peer's commands take from sent to run
  (`Session.RoundTrip`), and on the network where this machine stands (`Main`'s line: hosting and
  how many peers, or joined), what a guest holds queued and in reserve, and how many times it was
  resynced; the surface this hero is on, of how many (0076). Every module of that surface by its id, the world's
  globals with the ship's; an open entry shows its slice of the world's save as JSON, the first 20 000 characters of it.
- **Controls:** the world's speed ×4 and back, creative mode (`items`' `SetCreative`, read at the
  click so two quick clicks switch it back), and a meteor on a random wall of the outer hull
  beside a room (`hazards`' `MeteorHit`), with the wall cell behind it away from the room, so a hull two cells thick (0104)
  is breached through; or a line saying none is left. Surfaces (0076): `open planet` opens a planet grown from its seed
  (0077), `open strip` the test strip (`world.open`), `to ship` and `down` beam this hero aboard or down to the surface
  opened last (`world.transfer`), or say none is open.
- **Items** (creative mode): a picker, shut at the start, with a row for every item in `Content.Items.All` by group then
  name (`ItemPicker`), so a new item appears on it unasked. The search keeps the rows whose name or id holds what is
  typed, case ignored; `1`, `10` or `stack` is how many a pick gives. A row clicked, or Enter for the first shown, sends
  `items`' `Spawn` for this hero: what fits goes into its bag. With creative mode off it says so and sends nothing.
  While the search is focused the hero takes no keys (`HeroView`); hiding the panel lets go of it.
- **Sounds:** a board, shut at the start, with a button for every cue in `Sound.All`, so a new cue
  appears on it unasked. Cues have no names; each is called by its first file under `res://audio/`
  without its `-1` and extension (`crew/step`, `CueNames`), and its tune when it plays another's files
  at another pitch (the landing, `crew/step ×0.85`). A one-shot plays at each press, any of its
  files as the game picks them; a loop switches on and off, amber while on. Players come from
  `Cues.Player` on the UI bus, heard the same anywhere and through floating's silence; each is the
  panel's child, made at its cue's first press, so no stream loads before one is asked for.

## Files

| File          | Holds                                          | Godot |
| ------------- | ---------------------------------------------- | ----- |
| `DevPanel.cs` | the panel, its readouts, buttons, item picker and sound board | yes   |
| `logic/CueNames.cs` | what the sound board calls a cue               | no    |
| `logic/ItemPicker.cs` | what the item picker lists for a search  | no    |

## Accepted limitations

- A loop switched on keeps playing while the panel is hidden, until it is switched off: hiding the
  panel is how one hears it over the game.

## Differences from the TypeScript

- Hidden at the start, in the editor and in exported builds alike, where the TypeScript panel was
  open: it covers much of a 1280×720 screen, and players never need it.
- Save and load are the game menu's (`game/menu`), not the panel's.
- A slice shows its saved state only: derived state is never saved, where the TypeScript panel also
  showed a module's derived `Map`s and typed arrays.
- The sound board is new: the TypeScript game had no sound.
