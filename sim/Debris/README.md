# Debris

The pieces of debris drifting past the ship, and catching them
([0006](../../docs/decisions/0006-debris-in-the-simulation.md),
[0009](../../docs/decisions/0009-debris-around-the-ship.md)). Ported from
`packages/sim/src/modules/debris`; its rules hold here unchanged.

- **Depends on:** `Grid` (`IGridQueries`: its size and the hull's bounds), `Crew` (`ICrewQueries`:
  where the sender's hero is), `Rooms` (`IRoomsQueries`: what is space), `Items` (`IItemsOps`:
  `GiveOrDrop`, the inventory a caught piece goes into).
- **State** (`DebrisState`, members internal): `Pieces` in spawn order, each a `Piece` (public, with
  internal setters) `{ Id, Item, X, Y, Speed, Start, End }`, its middle at `X`, `Y` in thousandths
  of a cell (`DebrisModule.Unit`), drifting `Speed` a tick from `Start` till below `End`; `NextId`,
  never reused; `Clock`, ticks run; `NextSpawn`, the clock the next piece is due at. All saved;
  nothing is derived.
- **Files:** `Debris.cs` — the module, `Grab` and the rules below; `State.cs` — its slice and
  `Piece`; `Fuzz.cs` — its fuzz generator.
- **Runs** after `Hazards` and before `Furnace` in `Registry.All`, as in TypeScript's `MODULES`.

## Rules

- In the simulation phase, every tick: the clock advances; every piece moves left by its speed, and
  one whose `X` is below its `End` is gone.
- On the clock `NextSpawn`, the next is due `Debris.Every` plus `Below(Debris.Jitter + 1)` ticks
  on, and a piece spawns if fewer than `Debris.Max` are out and there is a hull: the bounds of every
  cell with a structure in it, found again on each beat. Drawn with the world's `Rng.Below`, in this
  order: a row among the hull's and `Debris.Margin` above and below; a kind by weight, walking
  `Debris.Kinds` in order; a speed from `Debris.DriftMin` to `DriftMax`. It starts in the middle of
  the cell `Debris.Lead` right of the hull and ends `Debris.Lead` left of it, both kept on the grid.
  The first is due on clock `Debris.Every`.
- Pieces pass through the hull and each other: nothing collides.
- `Grab(Piece)` puts the piece into the sender's inventory as one of its salvage item, or, a
  container (`Loot`: a cargo crate, a supply pod), as what it opens into, drawn entry by entry by
  the world's `Rng` as the grab applies (`LootOf`): the one-in roll, then the count. Refused with
  `debris.gone` (no such piece out), `debris.notOutside` (the sender's hero, at the cell its client
  last reported, or the piece's middle is not in space). A full inventory is no reason: what does not
  fit floats at the hero's cell, on the floor of space (`IItemsOps.GiveOrDrop`).

## API

- **Queries** (public members; no other module reads debris, so there is no query interface):
  `Pieces` — live and read-only; `PieceAt(cell)` — the first piece whose middle is in the cell;
  `GrabProblem(peer, piece)` — the refusal that peer's `Grab` would get, for the client.
- **Static:** `CellOf(x, y, width)` — the cell under a piece's middle; `LootOf(item, rng)` — what
  catching a piece of that item gives, merged by item in the order its entries first give it.
- **Commands:** `Grab(Piece)`.
- **Fuzz:** a piece that is out three times in four, else an id from 0 to `NextId + 1`, gone or never
  spawned. Most grabs are refused, as few fuzzed heroes stand in space.

## Invariants

- Piece ids grow in spawn order and are never reused; no more than `Debris.Max` are out.
- Every piece is on the grid, between its `Start` and `End`, in the band of the hull it spawned by.

## Deliberately not handled

- Reach: the sim lets a hero in space grab any piece in space; the client offers only those within
  reach, among friends.
- A hero's reported cell is where its body entered space: inside space it is not reported again, so
  the sim knows the hero is outside, not where.
- Two grabs of one piece on one tick: the host's order decides, and the second is `debris.gone`.

## Differences from TypeScript

- `cellOf(piece, width)` is `CellOf(x, y, width)`, and `lootOf` returns the cost as a list of
  `(Item, Count)`, items' cost everywhere in the port. A piece is always on the grid, so `X / Unit`
  and `Y / Unit` divide non-negative numbers and truncation is the floor.
- The invariants have a test of their own here (`KeepsIdsInSpawnOrder…`): random hulls grown and cut
  down, heroes grabbing in and out of space, waits long enough to reach the cap.
