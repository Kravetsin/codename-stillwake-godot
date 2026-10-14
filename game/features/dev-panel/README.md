# dev-panel (client)

Development readouts and controls, ported from `apps/game/src/features/dev-panel/` (G-08): a panel in
the top-right corner, open from the start, hidden and shown again with the backquote key.

- **Reads**, four times a second and only while shown: frames a second (Godot's own count), the
  world's tick, the hash the client last sent, how long this peer's commands take from sent to run
  (`Session.RoundTrip`), and on the network where this machine stands (`Main`'s line: hosting and
  how many peers, or joined), what a guest holds queued and in reserve, and how many times it was
  resynced. Every module by its id; an open entry shows its slice of the world's save as JSON, the
  first 20 000 characters of it.
- **Controls:** save and load the world to and from the dev slot (`user://saves/dev.json`, through
  `Main`), the world's speed ×4 and back, creative mode (`items`' `SetCreative`, read at the click so
  two quick clicks switch it back), and a meteor on a random wall of the outer hull (`hazards`'
  `MeteorHit`), or a line saying none is left.

## Files

| File          | Holds                         | Godot |
| ------------- | ----------------------------- | ----- |
| `DevPanel.cs` | the panel, its readouts and buttons | yes   |

## Differences from the TypeScript

- A slice shows its saved state only: derived state is never saved, where the TypeScript panel also
  showed a module's derived `Map`s and typed arrays.
- Load works only in a world of this machine's own: hosting or joined, the ENet link would stay with
  the old session. The TypeScript load left a joined session; nobody but a developer loads, and they
  can quit and start alone.
- The save is a file in Godot's user folder, the TypeScript's an IndexedDB entry; G-09 brings saves as
  files for players.
