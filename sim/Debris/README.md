# Debris

## Files

| File | Purpose |
| --- | --- |
| `Debris.cs` | The module, its queries and operations: `Grab`, `Catch`, `HewWrecks`, `HewPieces`, `Reel`, `Hit`, `Found`. |
| `Drift.cs` | The system: pieces drift, are caught or gone; each zone spawns on its beat. |
| `Fuzz.cs` | The commands the desync suite draws. |
| `Nets.cs` | The debris nets: the catch, `Collect`, a net gone dropping what it held. |
| `State.cs` | The saved slice and its records: `Piece`, `Reeling`, `Beat`, `FoundKind`, `NetEntry`, `DebrisState`. |

Pieces drifting by in zones; caught by hand or net, reeled in, wrecks broken; the
Handbook's record of kinds found (0006, 0009, 0034, 0041, 0044, 0052, 0054, 0056, 0060, 0066).

- **Depends on:** `Grid` (size, `Version`, the log, the hull's bounds, machines and their traits), `Crew` (the sender's
  hero, every hero's reported cell), `Rooms` (what is space), `Items` (`CanDrain`, `Running`, `Damage`, `WeaponProblem`,
  `FullFor`; `GiveOrDrop`, `GiveWhatFits`, `DropAt`). Runs after `Hazards` and before `Furnace`. Creatures reads it
  (`Breaks`) and changes it (`Unreel`, `Hit`).
- **State** (`DebrisState`, saved): `Pieces` in id order, each `{ Id, Zone, Item, X, Y, Speed, Start, End, Damage,
  Cut, Reel, Struck, SeenAt }`, in thousandths of a cell; `NextId`, never reused; `Clock`; `Beats`, each zone's next due
  clock; `AtStart`; `Found` (0066), `FoundKind { Kind, Items, Looks }` in the order first found; `Held`, every net's
  entries `{ Net, Id, Kind, Cell, Items }` in catch order, not written while empty. **Unsaved, unhashed:** the last
  `CaughtKept` (64) grabs and collected entries, by catcher (`CaughtFrom`). **Derived:** the hull's bounds (0051); the
  grid version the nets were last looked at.

## Zones

`Content.Debris.Zones`, in order, each an anchor and tiers of kinds by weight by distance from the hull's box,
`d = max(dx, 2·dy)` (`TierOf`, 0052): **`ship`**, the stream (`ByHull`: the hull grown by `Lead` columns and `Margin`
rows; the only one with clods, 0060); **`far`** (`AroundHeroes`: round every hero out of every stream's box, 0050;
farther tiers bigger wrecks, derelicts, rich rock, 0056); **`asteroids`** (`HullBand`, 0054: rows above and below).

## Rules

- Each tick: a net gone drops what it held; each piece drifts left, or toward its reel's point; one past its
  `End` is gone once no view holds it, past the grid's left edge at once; then the catch, in id order; then each zone,
  in content order, on its beat (0052).
- **No zone on a surface without debris** (`Surfaces.Kind.Debris`, 0076): nothing spawns or is drawn from the Rng.
- **A hero's view** (`Debris.View`) exceeds the farthest zoom. No zone spawns or removes a piece in any
  hero's view but at the grid's edge (0052).
- **A beat** is due `Every` plus `Below(Jitter + 1)` on; no jitter draws nothing. A zone with no beat gets one (0052).
- **Stream and band beats** spawn while fewer than `Max` of the zone are out and there is a hull (cells with a
  structure, within `Built`, a vessel's not, 0048). Draws: row, kind, speed. A piece starts `Lead` right of the hull, ends `Lead` left
  of it; a start in a hero's view is pushed past every view it lands in (0052). A band row off the grid spawns nothing but takes its draws (0054).
- **The far field's beat** (0052): every tick, each hero on the grid in id order while fewer than `Max` are out,
  skipping without a draw a hero in a stream's box or with `PerHero` in its `Keep` box: a point on its ring (side,
  offset, depth), a kind from that point's tier, a speed. Off the grid, in a stream's box or in a view, nothing spawns,
  its draws taken. `End` 0, `SeenAt` the clock.
- **Despawn**, far pieces only (0052): seen in some hero's `Keep` box, it stays; else while within some `Hard` box and
  seen less than `Linger` ago.
- **First tick:** `Layout.DebrisAtStart` of the stream goes out (at most `Max`), spread along its way.
- Nothing collides (0006) but a net.
- **`Grab(Piece)`** (0034): the sender gets the salvage item, or a container's `Loot` drawn by `LootOf`;
  what does not fit floats at the hero (`GiveOrDrop`). Refused `debris.gone`, `debris.notOutside` (hero or piece not in space), `debris.tooBig` (a wreck).
- **Wreckage** (0034, 0041, 0054, 0056): a kind with hit points in `Wreckage`; every weapon breaks every kind; a cable
  rack gives one circuit. `HewWrecks(Tool, X, Y, Heading)`: each fan ray strikes the first wreck whose box covers a cell
  it crosses, stopping there or at a wall; `HewPieces(Tool, Pieces)` (tool-edge) each listed, reach unchecked.
  One whose middle is not in space is passed. Each struck takes `Damage` once (0098) through `Hit`; at its hit points
  its `Loot` falls under its middle, anyone's.
  Salvaging hits add to `Cut`; broken with some, it draws `Below(hp) < Cut`: won, each line half again.
  Refused `debris.notOutside`, `debris.nothingToHit`, `items.flat`, then `items.notRunning` (0055).
- **Found** (0066): every catch (grab, break, net, salvage drone) records the kind, the
  items `LootOf` drew (the same roll, overflow too) and the look worn (`Content.Looks.Of`), each the first time.
- **The harpoon** (0044): `Reel(Piece, ToX, ToY)` clamps the point to the grid and sets `Reel { By, ToX, ToY, Left }`,
  `Left` the longer axis's ticks at `ReelSpeed`, rounded up, at least one. Each tick `X += (ToX − X) / Left` (and `Y`),
  truncating; at `Left` 0 it drifts on. The last `Reel` wins. Refused as `Grab`. Caught or broken, its reel goes too.
- **The catch:** a moved piece is caught when its kind is in `Debris.NetCatches`, it is not reeled and its middle's cell
  holds a `Catches` machine with fewer than `NetHolds` (10) entries: it leaves `Pieces` for an entry, a container's
  loot rolled then (a full net draws nothing), `Found` recorded; no `CaughtFrom`. A salvage drone's `Catch(piece)`
  takes one so, counted obtained: its loot, or null if gone.
- **`Collect(Machine)`**: entries of that net in catch order, each giving what fits (`GiveWhatFits`, counted obtained),
  on past one that does not, each give reported as a grab's (`CaughtFrom`); an emptied one goes. Refused `debris.noNet`
  (no `Catches` machine), `debris.netEmpty`, `debris.bagFull` (nothing fits).
- **A net gone** (a meteor, a screwdriver): when the grid's `Version` moved, every entry whose net is no `Catches`
  machine drops its items at its `Cell` (`DropAt(Nobody)`), in catch order (its cost: `hazards`, or the
  worker). Ids are never reused: a net built again is empty.

## Invariants

- Piece ids grow and are never reused; no zone has more than its `Max` out.
- Every piece is on the grid, at or right of its `End` unless seen, left of its `Start` unless reeled
  (`AsteroidTests`). None spawns or goes in a hero's view but at the grid's edges (`DebrisZonesTests`).
- A far piece spawns on the ring of a hero out of every stream's box; `SeenAt` is never past the clock, 0 on the stream's.
- A reel's point is on the grid and its `Left` above zero. A wreck's `Cut` ≤ `Damage` < its hit points.
- `Found` only grows; each kind once and spawnable, each item once among its loot, each look once (`DebrisFoundTests`).
- After the drop every entry's net stands, none holds more than `NetHolds`; `Collect` and the drop make and lose
  nothing (`NetTests`). Positions are never negative.

## Deliberately not handled

- Reach, what an edge touches, the weapon in hand, its pace, the harpoon's shot and cooldown: the client's (0044).
- A stale reported cell, a window over 1920×1080 or a narrow grid may show a spawn; a stream pushed right by a hero is
  thinned by the cap; heroes share one far field's density (0052). A half-broken far wreck can despawn.
- Two grabs of one piece on a tick: host order; the second is `debris.gone`.
- A catch's popup is lost to a load or resync (0066). Picking up a floor stack records nothing.
- A wreck is judged by its middle cell: over the hull it cannot be hit. An asteroid reeled out of its band stays out. A
  diagonal reel is up to 41% faster (0044).
- Nets: no drone touches one (no slots); a piece through two is caught by the first; a partly collected container
  keeps its look; a full net raises no alert.
