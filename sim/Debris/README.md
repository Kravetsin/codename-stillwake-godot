# Debris

## Files

| File | Purpose |
| --- | --- |
| `Debris.cs` | The module, its queries and operations: `Grab`, `Catch`, `HewWrecks`, `HewPieces`, `Reel`, `Hit`, `Found`. |
| `Drift.cs` | The system: pieces drift, are caught or gone; deliveries and each zone spawn. |
| `Fuzz.cs` | The commands the desync suite draws. |
| `Nets.cs` | The debris nets: the catch, `Collect`, a net gone dropping what it held. |
| `State.cs` | The saved slice and its records: `Piece`, `Reeling`, `Beat`, `FoundKind`, `NetEntry`, `DebrisState`. |

Pieces drifting by in zones and the mafia's deliveries; caught by hand or net, reeled in, wrecks broken; kinds found (0006, 0009, 0034, 0041, 0044, 0052, 0054, 0056, 0060, 0066).

- **Depends on:** `Grid` (size, `Version`, the log, the hull's bounds, machines and their traits), `Crew` (heroes;
  `Loot` of what one breaks), `Knowledge` (deliveries), `Rooms` (what is space), `Items` (weapons and bags;
  `GiveOrDrop`, `GiveWhatFits`, `DropAt`). Runs after `Hazards` and before `Furnace`. Creatures reads it
  (`Breaks`) and changes it (`Unreel`, `Hit`).
- **State** (`DebrisState`, saved): `Pieces` in id order, each `{ Id, Zone, Item, X, Y, Speed, Start, End, Damage,
  Cut, Reel, Struck, SeenAt, Delivery }`, in thousandths of a cell; `NextId`, never reused; `Clock`; `Beats`, each zone's next due
  clock; `AtStart`; `Found` (0066), `FoundKind { Kind, Items, Looks }` in the order first found; `Held`, every net's
  entries `{ Net, Id, Kind, Cell, Items }` in catch order, not written while empty. **Unsaved, unhashed:** the last
  `CaughtKept` (64) grabs and collected entries, by catcher (`CaughtFrom`). **Derived:** the hull's bounds (0051); the
  grid version the nets were last checked at.

## Zones

`Content.Debris.Zones`, in order, each an anchor and tiers of kinds by weight by distance from the hull's box,
`d = max(dx, 2·dy)` (`TierOf`, 0052): **`ship`**, the stream (`ByHull`: the hull grown by `Lead` columns and `Margin`
rows; the only one with clods, 0060); **`far`** (`AroundHeroes`: round every hero out of every stream's box,
farther ones bigger, 0050, 0056); **`asteroids`** (`HullBand`, 0054: rows above and below).

## Rules

- Each tick: a gone net drops its catch; each piece drifts left or to its reel's point; one past its
  `End` is gone once no view holds it, past the grid's left edge at once; then the catch, in id order; then each zone,
  in content order, on its beat (0052).
- **No zone on a surface without debris** (`Surfaces.Kind.Debris`, 0076): nothing spawns or is drawn.
- **A hero's view** (`Debris.View`, past the farthest zoom): no zone spawns or removes a piece in one but at the grid's edge (0052).
- **A beat** is due `Every` plus `Below(Jitter + 1)` on; no jitter draws nothing. A zone with no beat gets one (0052).
- **Stream and band beats** spawn while fewer than `Max` of the zone are out and there is a hull (structure within
  `Built`, 0048). Draws: row, kind, speed. A piece starts `Lead` right of the hull, ends `Lead` left
  of it; a start in a hero's view is pushed past every view it lands in (0052). A band row off the grid spawns nothing but takes its draws (0054).
- **The far field's beat** (0052): each tick, each hero on the grid in id order while fewer than `Max` are out, but
  one in a stream's box or with `PerHero` in its `Keep` box (no draw): a point on its ring (side, offset, depth), a kind
  by its tier, a speed; off the grid, in a stream's box or a view, none. `End` 0, `SeenAt` the clock.
- **Despawn**, far pieces only (0052): seen in some hero's `Keep` box, it stays; else while in a `Hard` box,
  seen under `Linger` ago.
- **First tick:** `Layout.DebrisAtStart` of the stream goes out (at most `Max`), spread along its way. Nothing collides (0006) but a net.
- **`Grab(Piece)`** (0034): the sender gets the item, or a container's (crate, pod, fridge) `Loot`, the rest floating at the
  hero (`GiveOrDrop`). Refused `debris.gone`, `debris.notOutside` (either not in space), `debris.tooBig` (a wreck).
- **Wreckage** (0034, 0041, 0054, 0056): a kind with hit points in `Wreckage`; every weapon breaks every kind.
  `HewWrecks(Tool, X, Y, Heading)`: each fan ray strikes the first wreck whose box covers a cell
  it crosses, stopping there or at a wall; `HewPieces(Tool, Pieces)` (tool-edge) each listed, reach unchecked.
  One whose middle is not in space is passed. Each takes `Damage` once (0098) through `Hit`; at its hit points its `Loot` falls
  under its middle, anyone's. Salvaging hits add to `Cut`; broken with some, `Below(hp) < Cut` wins each line half again.
  Refused `debris.notOutside`, `debris.nothingToHit`, `items.flat`, then `items.notRunning` (0055).
- **Deliveries** ([mafia-deliveries](../../docs/decisions/2026-11-08-mafia-deliveries.md)): on the ship, before the beats, the first queued one with
  no piece out spawns at the stream's start on a trade station's row, spaced by `DeliveryGap`, zone `delivery`, never netted. A
  `parcel` is grabbed, a `deliveryCrate` or the container broken, only the container found; past its `End`, lost.
- **Found** (0066): every catch (grab, break, net, salvage drone) records the kind, the
  items `LootOf` drew (the same roll, overflow too) and the look worn (`Content.Looks.Of`, by id, alike on all peers), each once.
- **The harpoon** (0044): `Reel(Piece, ToX, ToY)` pulls it to the point, clamped to the grid, over the longer
  axis's ticks at `ReelSpeed`, at least one (`Reel { By, ToX, ToY, Left }`; `X += (ToX − X) / Left`, truncating), then it drifts on.
  The last wins; refused as `Grab`; caught or broken, its reel goes too.
- **The catch:** a moved piece of a kind in `Debris.NetCatches`, not reeled, is caught by a `Catches` machine at its middle
  with under `NetHolds` (10) entries: an entry, a container's loot rolled then, `Found` recorded; no `CaughtFrom`. A salvage drone's `Catch(piece)`
  takes one so, counted obtained: its loot, or null if gone.
- **`Collect(Machine)`**: that net's entries in catch order each give what fits (`GiveWhatFits`, counted obtained), reported
  as a grab's (`CaughtFrom`); an emptied one goes. Refused `debris.noNet`
  (no `Catches` machine), `debris.netEmpty`, `debris.bagFull` (nothing fits).
- **A net gone** (a meteor, a screwdriver): when the grid's `Version` moved, each entry whose net is no `Catches` machine
  drops its items at its `Cell` (`DropAt(Nobody)`), in catch order. A net built again is empty.

## Invariants

- Piece ids grow and are never reused; no zone has more than its `Max` out.
- Every piece is on the grid, at or right of its `End` unless seen, left of its `Start` unless reeled
  (`AsteroidTests`). None but a delivery spawns or goes in a view but at the grid's edges (`DebrisZonesTests`).
- Each delivery is one queued entry until caught, broken or gone, no piece naming another (`DeliveriesTests`).
- A far piece spawns on the ring of a hero out of every stream's box; `SeenAt` is never past the clock, 0 on the stream's.
- A reel's point is on the grid and its `Left` above zero. A wreck's `Cut` ≤ `Damage` < its hit points.
- `Found` only grows; each kind once and spawnable, each item once among its loot, each look once (`DebrisFoundTests`).
- After the drop every entry's net stands, none holds more than `NetHolds`; `Collect` and the drop make and lose
  nothing (`NetTests`). No position is negative.

## Deliberately not handled

- Reach, what an edge touches, the weapon in hand, its pace, the harpoon's shot and cooldown: the client's (0044).
- A stale reported cell, a window over 1920×1080 or a narrow grid may show a spawn; a stream pushed right by a hero is
  thinned by the cap; heroes share one far field's density (0052). A half-broken far wreck can despawn.
- Two grabs of one piece on a tick: host order; the second is `debris.gone`.
- A catch's popup is lost to a load or resync (0066); a floor stack picked up is not recorded.
- A wreck is judged by its middle cell: over the hull it cannot be hit. An asteroid reeled out of its band stays out.
- Nets: no drone touches one (no slots); a piece through two is caught by the first; a partly collected container
  keeps its look; a full net raises no alert.
