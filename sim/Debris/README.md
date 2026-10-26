# Debris

## Files

| File | Purpose |
| --- | --- |
| `Debris.cs` | The pieces of debris drifting by, catching them, reeling them in and breaking wreckage with weapons, in zones by … |
| `Drift.cs` | The debris module's system: pieces drift and are gone, and each zone of `Debris.Zones` spawns on its beat, the … |
| `Fuzz.cs` | The module's `Fuzz` override: the commands the desync suite draws. |
| `State.cs` | The saved slice and its records: `Piece`, `Reeling`, `Beat`, `FoundKind`, `DebrisState`. |

The pieces of debris drifting by, in zones by the hull, in its asteroid band and round every hero; catching them,
reeling them in by harpoon, breaking wreckage and asteroids with any weapon; and the Handbook's record of every kind
found (0006, 0009, 0034, 0041, 0044, 0052, 0054, 0056, 0060, 0066).

- **Depends on:** `Grid` (size, `Version`, the log, the hull's bounds), `Crew` (the sender's hero, every hero's
  reported cell), `Rooms` (what is space), `Items` (`CanDrain`, `Running`, `Damage`, `WeaponProblem`; `GiveOrDrop`,
  `DropAt`). Runs after `Hazards` and before `Furnace`. Creatures reads it (`IDebrisQueries`, `Wrecks`) and changes it
  (`Unreel`: the sentry; `Hit`: a shot).
- **State** (`DebrisState`, saved): `Pieces` in spawn (id) order, each `{ Id, Zone, Item, X, Y, Speed, Start, End,
  Damage, Reel, SeenAt }`, positions in thousandths of a cell (`DebrisModule.Unit`); `NextId`, never reused; `Clock`;
  `Beats`, each zone's next due clock; `AtStart`; `Found` (0066), each `FoundKind { Kind, Items, Looks }` in the order
  first found. **Not saved, never hashed:** the last `CaughtKept` (32) catches, read only by the client (`CaughtFrom`),
  forgotten on a load (0066). **Derived:** the hull's bounds, kept by the grid's log (0051).

## Zones

`Content.Debris.Zones`, in order, each a `DebrisZone` whose anchor says where it spawns and whose tiers give its kinds
by weight by distance from the hull's box, `d = max(dx, 2·dy)` (`TierOf`, 0052). Numbers live in content.

- **`ship`, the stream** (`ByHull`): past the ship, within the hull grown by `Lead` columns and `Margin` rows; the only
  zone with soil clods (0060).
- **`far`, the far field** (`AroundHeroes`): round every hero outside every stream's box, from its reported cell (0050,
  0052); farther tiers bring bigger wrecks, derelicts and rich rock (0056).
- **`asteroids`, the band** (`HullBand`, 0054): two bands of rows above and below the hull, crossed the stream's way;
  the far field spawns in it as anywhere.

## Rules

- Each simulation tick: every piece drifts left, or toward its reel's point; one past its `End` is gone once no view
  holds it, one past the grid's left edge at once. Then each zone, in content order, on its beat (0052).
- **No zone on a surface without debris** (`Surfaces.Kind.Debris`, 0076): nothing spawns, nothing is drawn from the
  Rng. Each surface's debris drifts round its own heroes only.
- **A hero's view** (`Debris.View`) is more than the farthest zoom shows. No zone spawns a piece in any hero's view nor
  removes one in it, but at the grid's edge (0052).
- **A beat** is due `Every` plus `Below(Jitter + 1)` on; no jitter draws nothing. A zone with no beat (added since the
  save) gets one on its first tick (0052).
- **Stream and band beats** spawn while fewer than `Max` of the zone are out and there is a hull (cells with a
  structure, walked within the grid's `Built` box and only when `Version` moved, 0048). Draw order: row, kind by weight
  from the lead's tier, speed. A piece starts `Lead` right of the hull and ends `Lead` left of it, on the grid; a start
  inside a hero's view is pushed one column past its right edge, past every other view it lands in (0052). A band row
  off the grid spawns nothing but takes its draws (0054).
- **The far field's beat** (0052): every tick, each hero on the grid in id order while fewer than `Max` are out,
  skipping without a draw a hero in a stream's box or with `PerHero` in its `Keep` box: a point on the ring round its
  cell (side, offset, depth), a kind from that point's tier, a speed. A point off the grid, in a stream's box or in any
  view spawns nothing, its draws taken. It starts there, `End` 0, `SeenAt` the clock.
- **Despawn**, far pieces only (0052): in some hero's `Keep` box it is seen and stays; else it stays while within some
  `Hard` box and seen less than `Linger` ago. The stream's pieces go only past their `End`.
- **First tick** (no ADR): a world puts out its map's `Layout.DebrisAtStart` pieces of the stream (at most `Max`), each
  drawn as a spawn and placed evenly along its way.
- Nothing collides: pieces pass through the hull and each other (0006).
- **`Grab(Piece)`** (0034): the sender gets the piece's salvage item, or a container's `Loot`, drawn by `LootOf` with
  the world's `Rng` as it applies; what does not fit floats at the hero (`GiveOrDrop`). Refused `debris.gone`,
  `debris.notOutside` (the hero's reported cell or the piece's middle not in space), `debris.tooBig` (a wreck).
- **Wreckage** (0034, 0041, 0054, 0056): a kind with hit points in `Wreckage`; hit points alone say how hard it is,
  and every weapon breaks every kind (0054); a cable rack gives one circuit (0041). `HewWrecks(Tool, X, Y, Heading)`
  (fan-of-rays): each ray of the fan strikes the first wreck whose box (`Wreckage.Sizes`) covers a cell it crosses (first
  spawned there), stopping there or at a wall; one whose middle is not in space is passed (`Struck`). Each struck takes
  `Damage` once (boosted, 0098) through `Hit`, as a shot's wreck (`Wrecks`): the blow kept (`Piece.Struck`: peer, debris
  clock); at its hit points it goes,
  its `Loot` under its middle (`FloorStack.Nobody`).
  Refused `debris.notOutside` (the hero), `debris.nothingToHit`, `items.flat`, then `items.notRunning` (0055). A hit
  spends nothing; `HewProblem` leaves the run out.
- **Found** (0066): a `Grab` that applies and a `HewWrecks` that breaks record the kind, the items `LootOf` drew for it (the
  same roll, overflow too) and the look it wore (`Content.Looks.Of`), each appended the first time; never sorted or shrunk.
- **The harpoon** (0044): `Reel(Piece, ToX, ToY)` clamps the point to the grid and sets `Reel { By, ToX, ToY, Left }`,
  `Left` the longer axis's ticks at `Harpoon.ReelSpeed`, rounded up, at least one. Each tick the piece moves
  `X += (ToX − X) / Left` (and `Y`), truncating toward zero, instead of drifting; at `Left` 0 it is on the point and
  drifts on. The last `Reel` wins; two on a tick go in host order. Refused as `Grab`. A piece caught or broken
  mid-reel is gone with its reel.

## Invariants

- Piece ids grow and are never reused; no zone has more than its `Max` out.
- Every piece is on the grid, at or right of its `End` unless a hero sees it, left of its `Start` unless reeled
  (`AsteroidTests`). None spawns or goes in a hero's view but at the grid's edges; the first tick's aside
  (`DebrisZonesTests`).
- A far piece spawns on the ring of a hero out of every stream's box; `SeenAt` is never past the clock, 0 on the stream's.
- A reel's point is on the grid and its `Left` above zero. A wreck's `Damage` is below its hit points.
- `Found` only grows; each kind once and spawnable, each item once among its loot, each look once and below
  `Looks.CountOf` (`DebrisFoundTests`).
- Positions are never negative: `X / Unit` is the floor.

## Deliberately not handled

- Reach, the weapon in hand, its pace, the harpoon's shot and cooldown: the client's, among friends (0044).
- A stale reported cell (0050), a window over 1920×1080 or a narrow grid may show a spawn (0052).
- A stream pushed right by a hero by its lead is thinned by the cap while the hero stays (0052).
- Heroes share one far field's density; with no hull there is none. A half-broken far wreck can despawn.
- Two grabs of one piece on a tick: host order; the second is `debris.gone`.
- A catch's popup is lost if a load or resync runs it (0066). Picking up a floor stack records nothing.
- A wreck is judged by its middle cell: over the hull it cannot be hit, and drifts on.
- An asteroid reeled out of its band stays out. A diagonal reel is up to 41% faster: no square root (0044).
