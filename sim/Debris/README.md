# Debris

The pieces of debris drifting past the ship, catching them, reeling them in by harpoon, and breaking
wreckage with an axe
([0006](../../docs/decisions/0006-debris-in-the-simulation.md),
[0009](../../docs/decisions/0009-debris-around-the-ship.md),
[0034](../../docs/decisions/0034-wreckage-and-the-axe.md),
[0041](../../docs/decisions/0041-wreckage-of-many-kinds-and-circuits.md),
[0044](../../docs/decisions/0044-the-harpoon-reels-a-piece-to-a-point.md)). Ported from
`packages/sim/src/modules/debris`; its rules hold here unchanged, the harpoon new in the port.

- **Depends on:** `Grid` (`IGridQueries`: its size and the hull's bounds), `Crew` (`ICrewQueries`:
  where the sender's hero is), `Rooms` (`IRoomsQueries`: what is space), `Items` (`IItemsOps`:
  `GiveOrDrop`, the inventory a caught piece goes into; `DropAt`, the floor a broken wreck falls on).
- **State** (`DebrisState`, members internal): `Pieces` in spawn order, each a `Piece` (public, with
  internal setters) `{ Id, Item, X, Y, Speed, Start, End, Damage, Reel }`, its middle at `X`, `Y` in thousandths
  of a cell (`DebrisModule.Unit`), drifting `Speed` a tick from `Start` till below `End`, `Damage` the hit points axes took off it,
  `Reel` a harpoon pulling it (a `Reeling { By, ToX, ToY, Left }`: the peer, the point, the ticks still to go) or null; `NextId`,
  never reused; `Clock`, ticks run; `NextSpawn`, the clock the next piece is due at; `AtStart`, the pieces the first tick puts out. All saved;
  nothing is derived.
- **Files:** `Debris.cs` — the module, `Grab`, `Hit`, `Reel` and the rules below; `State.cs` — its slice,
  `Piece` and `Reeling`; `Fuzz.cs` — its fuzz generator.
- **Runs** after `Hazards` and before `Furnace` in `Registry.All`, as in TypeScript's `MODULES`.

## Rules

- In the simulation phase, every tick: the clock advances; every piece moves left by its speed, or,
  reeled, toward its reel's point (below); and one whose `X` is below its `End` is gone.
- On the clock `NextSpawn`, the next is due `Debris.Every` plus `Below(Debris.Jitter + 1)` ticks
  on, and a piece spawns if fewer than `Debris.Max` are out and there is a hull: the bounds of every
  cell with a structure in it, found again on each beat. Drawn with the world's `Rng.Below`, in this
  order: a row among the hull's and `Debris.Margin` above and below; a kind by weight, walking
  `Debris.Kinds` in order; a speed from `Debris.DriftMin` to `DriftMax`. It starts in the middle of
  the cell `Debris.Lead` right of the hull and ends `Debris.Lead` left of it, both kept on the grid.
  The first is due on clock `Debris.Every`. Of the weights' 45, 36 are small pieces and 9 wrecks:
  a spawn every 10 s on average, a wreck one in five.
- On the first tick a world begins with its map's `Layout.DebrisAtStart` pieces out (at most
  `Debris.Max`), each drawn as a spawn is, then placed along its way, its `X` drawn evenly from its
  `End` to its `Start`. The wreck sets `Debris.AtStart`, so a player waits for none; test maps set
  none and begin empty.
- Pieces pass through the hull and each other: nothing collides.
- `Grab(Piece)` puts the piece into the sender's inventory as one of its salvage item, or, a
  container (`Loot`: a cargo crate, a supply pod), as what it opens into, drawn entry by entry by
  the world's `Rng` as the grab applies (`LootOf`): the one-in roll, then the count. Refused with
  `debris.gone` (no such piece out), `debris.notOutside` (the sender's hero, at the cell its client
  last reported, or the piece's middle is not in space). A full inventory is no reason: what does not
  fit floats at the hero's cell, on the floor of space (`IItemsOps.GiveOrDrop`). A wreck is refused
  with `debris.tooBig`.
- **Wreckage** is a kind with hit points in `Wreckage`: a hull section (15), a cargo container (11),
  a machine block (19), a cable rack (9), a tank cluster (7) and a shield slab (31) (0034, 0041). It
  drifts, spawns and is saved as any piece; each kind breaks into its own `Loot`, a cable rack always
  into exactly one circuit, the only source of them. `Hit(Piece)` takes `Axe.Damage` off them, adding it to the piece's `Damage`;
  the hit that brings `Damage` to its hit points removes it, and its loot (`Loot`, drawn by `LootOf`
  as a container's is) falls on the cell under its middle, still, dropped by `FloorStack.Nobody`
  (`IItemsOps.DropAt`): floating there in space for anyone to pick up at once, the hero who broke it
  too. Refused with `debris.gone`, `debris.notOutside` (as `Grab`) or `debris.notWreckage` (a piece
  caught by hand). The sim does not ask whether the sender holds an axe, how fast it swings nor how
  far it reaches: the client's word, among friends (principle 6). The client sends one `Hit` for each
  wreck a swing's drawn blade touches (0039, 0040), so one swing may hit two wrecks.
- **The harpoon** (0044): `Reel(Piece, ToX, ToY)` reels a piece, small or a wreck, to a point in
  thousandths of a cell, the client's word: `Harpoon.Stop` before the gun. Applying it clamps the point
  to the grid and sets the piece's `Reel` to `{ By = sender, ToX, ToY, Left }`, `Left` the ticks its
  longer axis takes at `Harpoon.ReelSpeed`, rounded up, at least one. Each tick a reeled piece moves
  `X += (ToX − X) / Left` and so `Y`, truncating toward zero, instead of drifting; then `Left` falls by
  one, and at none the reel clears: the piece is then exactly on the point, and drifts on from there
  the next tick. The last `Reel` of a piece wins: a later one, by anyone, replaces the reel, its `Left`
  counted afresh from where the piece is; two on one tick go in the host's order. Refused with
  `debris.gone` or `debris.notOutside`, as `Grab`. `Grab` and `Hit` take a reeled piece as any other,
  and one caught or broken mid-reel is gone with its reel.

## API

- **Queries** (public members; no other module reads debris, so there is no query interface):
  `Pieces` — live and read-only; `PieceAt(cell)` — the first piece whose middle is in the cell;
  `GrabProblem(peer, piece)`, `HitProblem(peer, piece)` and `ReelProblem(peer, piece)` — the refusal
  that peer's `Grab`, `Hit` or `Reel` would get, for the client.
- **Static:** `CellOf(x, y, width)` — the cell under a piece's middle; `LootOf(item, rng)` — what
  catching a piece of that item gives, merged by item in the order its entries first give it.
- **Commands:** `Grab(Piece)`, `Hit(Piece)`, `Reel(Piece, ToX, ToY)`.
- **Fuzz:** a grab, a hit or a reel, one in three each, at a piece that is out three times in four,
  else an id from 0 to `NextId + 1`, gone or never spawned; a reel's point is anywhere from a grid's
  size before the grid to one past it, so the clamp is drawn too. Most are refused, as few fuzzed
  heroes stand in space.

## Invariants

- Piece ids grow in spawn order and are never reused; no more than `Debris.Max` are out.
- Every piece is on the grid, at or right of its `End`, and left of its `Start` unless it was reeled;
  each spawned in the band of the hull it spawned by.
- A reel's point is on the grid and its `Left` above zero.
- A wreck's `Damage` is below its hit points; a piece caught by hand has none.

## Deliberately not handled

- Reach: the sim lets a hero in space grab any piece in space; the client offers only those within
  reach, among friends.
- A hero's reported cell is where its body entered space: inside space it is not reported again, so
  the sim knows the hero is outside, not where.
- Two grabs of one piece on one tick: the host's order decides, and the second is `debris.gone`; so
  for hits after the one that breaks a wreck.
- A wreck half over the hull is judged by its middle cell, as a small piece is: with its middle over
  the ship it cannot be hit, though its end sticks out into space. Accepted: it drifts on and out.
- The axe in hand, the swing's pace (`Axe.SwingTicks`) and reach (the axe's picture, 0040): the client's, among friends. A hit in flight after the
  axe was put away still lands; creative mode needs no axe either.
- The harpoon's shot, its range, the hit test, the hooked rod before the reel and the cooldown: the
  client's (0044). The cooldown is the hero's: every harpoon it holds waits for it. The sim does not
  ask whether a harpoon is held nor where the gun is; a hook not yet reeled is never saved.
- A diagonal reel is up to 41% faster than a straight one: the longer axis sets the time, with no
  square root. A reel goes on after its reeler leaves; reeled over the hull, a piece cannot be grabbed
  (`debris.notOutside`) and drifts back out; reeled left of its `End`, it is gone that tick.

## Differences from TypeScript

- `cellOf(piece, width)` is `CellOf(x, y, width)`, and `lootOf` returns the cost as a list of
  `(Item, Count)`, items' cost everywhere in the port. A piece is always on the grid, so `X / Unit`
  and `Y / Unit` divide non-negative numbers and truncation is the floor.
- The invariants have a test of their own here (`KeepsIdsInSpawnOrder…`): random hulls grown and cut
  down, heroes grabbing in and out of space, reels to points on and off the grid, waits long enough
  to reach the cap.
- The harpoon and `Reel` are new in the port; TypeScript has none.
