# dev-panel (client)

Development readouts and controls, ported from `apps/game/src/features/dev-panel/` (G-08): a panel in
the top-right corner, hidden at the start, shown and hidden again with the backquote key.

- **Reads**, four times a second and only while shown: frames a second (Godot's own count), the
  world's tick, the hash the client last sent, how long this peer's commands take from sent to run
  (`Session.RoundTrip`), and on the network where this machine stands (`Main`'s line: hosting and
  how many peers, or joined), what a guest holds queued and in reserve, and how many times it was
  resynced. Every module by its id; an open entry shows its slice of the world's save as JSON, the
  first 20 000 characters of it.
- **Controls:** the world's speed ×4 and back, creative mode (`items`' `SetCreative`, read at the
  click so two quick clicks switch it back), and a meteor on a random wall of the outer hull
  (`hazards`' `MeteorHit`), or a line saying none is left.

## Files

| File          | Holds                         | Godot |
| ------------- | ----------------------------- | ----- |
| `DevPanel.cs` | the panel, its readouts and buttons | yes   |

## Differences from the TypeScript

- Hidden at the start, in the editor and in exported builds alike, where the TypeScript panel was
  open: it covers much of a 1280×720 screen, and players never need it.
- Save and load are the game menu's (`game/menu`), not the panel's.
- A slice shows its saved state only: derived state is never saved, where the TypeScript panel also
  showed a module's derived `Map`s and typed arrays.
