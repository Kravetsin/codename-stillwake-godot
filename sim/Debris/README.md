# Debris

## Files

| File | Purpose |
| --- | --- |
| `Debris.cs` | The module, its queries and operations: `Grab`, `Catch`, `HewWrecks`, `HewPieces`, `Unreel`, `Hit`, `Found`. |
| `Drift.cs` | The system: pieces move, are caught or gone; zones and deliveries spawn. |
| `Lines.cs` | Lines and the host player's feed: `Hook`, `Wind`, `LetGo`, `Moved`. |
| `Fuzz.cs` | The commands the desync suite draws. |
| `Nets.cs` | The nets: the catch, `Collect`, a net gone. |
| `State.cs` | The saved slice and its records: `Piece`, `Line`, `Beat`, `FoundKind`, `NetEntry`, `DebrisState`. |

Pieces drifting by in zones and the mafia's deliveries; caught by hand or net, towed, wrecks broken; kinds found (0009, 0034, 0041, 0052, 0054, 0056, 0060, 0066, debris-host-physics).

- **Depends on:** `Grid` (size, `Version`, the log, the hull, machines), `Crew` (heroes;
  `Loot` of what one breaks), `Knowledge` (deliveries, `Did`), `Rooms` (what is space), `Items` (`GiveOrDrop`,
  `GiveWhatFits`, `DropAt`), `Sky` (the stretch running). Runs after `Hazards`, before `Furnace`. Creatures reads it
  (`Breaks`) and changes it (`Unreel`, `Hit`, `TakeBursting`).
- **State** (`DebrisState`, saved): `Pieces` in id order, each `{ Id, Zone, Item, X, Y, Speed, Start, End, Damage,
  Cut, Struck, SeenAt, Delivery, Angle, Spin, Vx, Vy }`, in thousandths of a cell; `NextId`, never reused; `Clock`; `Beats`, each zone's next due
  clock; `AtStart`; `Found` (0066), `FoundKind { Kind, Items, Looks }` in the order first found; `Held`, every net's
  entries `{ Net, Id, Kind, Cell, Items }` in catch order; `Bursting`; `Lines { By, Piece, HookX, HookY, Length, Winds }`, in hook order. **Unsaved:** the last `CaughtKept` grabs and collected
  entries, by catcher (`CaughtFrom`). **Derived:** the hull's bounds (0051); the grid version nets were checked at; lines by hero and by piece.

## Zones

`Content.Debris.Zones`, in order, each an anchor and tiers of kinds by distance from the hull's box, `d = max(dx, 2·dy)`
(`TierOf`, 0052): **`ship`** (`ByHull`: the hull grown by `Lead` columns, `Margin` rows; alone with clods, 0060);
**`far`** (`AroundHeroes`, round heroes out of every stream's box, 0050, 0056); **`asteroids`** (`HullBand`, 0054);
each stretch's (`ByHull`, stretches-of-space).

## Rules

- Each tick: a gone net drops its catch; a line whose hero is gone, out or in a room goes; each piece moves by `Vx`,
  `Vy` (`-Speed`, 0 as spawned), turning by `Spin`; one past its `End` is gone once no view holds it, off the grid at once;
  then the catch, in id order; then each zone, in content order, on its beat (0052).
- **Tumble** (debris-tumble): `Angle`, `Spin` from the id (`TumbleOf`); a wreck is met by its turned box (`Shape`).
- **No zone on a surface without debris** (`Surfaces.Kind.Debris`, 0076): nothing spawns.
- **A hero's view** (`Debris.View`): no zone spawns or removes a piece in one but at the grid's edge (0052).
- **A beat** is due `Every` plus `Below(Jitter + 1)` on. A zone with no beat gets one (0052).
  A stretch's zone beats only while it runs, out of the streams' box else; the `ship` stream's is `Thins` times longer meanwhile.
- **Stream and band beats** spawn while fewer than `Max` of the zone are out and there is a hull (0048). Draws: row, kind, speed. A piece starts `Lead` right of the hull, ends `Lead` left
  of it; a start in a hero's view is pushed past every view it lands in (0052). A band row off the grid spawns nothing but takes its draws (0054).
- **The far field's beat** (0052): each tick, each hero on the grid in id order while fewer than `Max` are out, but
  one in a stream's box or with `PerHero` in its `Keep` box: a point on its ring, a kind
  by its tier, a speed; off the grid, in a stream's box or a view, none. `End` 0, `SeenAt` the clock.
- **Despawn**, far pieces only (0052): on a line or seen in some hero's `Keep` box, it stays; else while in a `Hard` box,
  seen under `Linger` ago.
- **First tick:** `Layout.DebrisAtStart` of the stream goes out (at most `Max`), spread along its way.
- **`Grab(Piece)`** (0034): the sender gets the item, or a container's `Loot`, the rest at the
  hero (`GiveOrDrop`). Refused `debris.gone`, `debris.notOutside`, `debris.tooBig`.
- **Wreckage** (0034, 0041, 0054, 0056): a kind with hit points in `Wreckage`; every weapon breaks every kind.
  `HewWrecks(Tool, X, Y, Heading)`: each fan ray strikes the first wreck whose turned box
  it crosses, or stops at a wall; `HewPieces(Tool, Pieces)` (tool-edge) each listed, reach unchecked.
  One whose middle is not in space is passed. Each takes `Damage` once (0098) through `Hit`; at its hit points its `Loot` falls
  under its middle, anyone's; a `Bursts` kind queues its burst for Creatures. Salvaging hits add to `Cut`; broken with some,
  `Below(hp) < Cut` wins each line half again.
  Refused `debris.notOutside`, `debris.nothingToHit`, `items.flat`, then `items.notRunning` (0055).
- **Deliveries** (mafia-deliveries): on the ship, before the beats, the first queued one with
  no piece out spawns at the stream's start on a trade station's row, zone `delivery`, never netted. A
  `parcel` is grabbed, a `deliveryCrate` or the container broken, only the container found; past its `End`, lost.
- **Found** (0066): every catch (grab, break, net, drone) records the kind, the items `LootOf` drew and the
  look worn (`Content.Looks.Of`), each once.
- **Lines** (debris-host-physics): `Hook(Piece, HookX, HookY)` hooks it there (clamped to its box), as long as the hero's
  span, at most `Harpoon.Range`; one a hero, one a piece, the last wins; refused `debris.gone`, `debris.notOutside`.
  `Wind(On)`, `LetGo` refused `debris.noLine`. A piece gone takes its line (`Unreel` too). The sim pulls and collides nothing but a net.
- **`Moved(ForTick, Bodies, Lines)`**, the host player's only (`debris.notHost`): bodies' poses and motions run on from
  `ForTick` (off the grid, gone); lines' lengths where still on that piece.
- **The catch:** a moved piece of a kind in `Debris.NetCatches`, on no line, is caught by a `Catches` machine at its middle
  with under `NetHolds` entries: an entry, a container's loot rolled then, `Found` recorded; no `CaughtFrom`. A salvage drone's `Catch(piece)`
  takes one so, counted obtained.
- **`Collect(Machine)`**: that net's entries in catch order each give what fits (`GiveWhatFits`, counted obtained), reported
  as a grab's (`CaughtFrom`); an emptied one goes. Refused `debris.noNet`, `debris.netEmpty`, `debris.bagFull`.
- **A net gone**: when the grid's `Version` moved, each entry whose net is no `Catches` machine
  drops its items at its `Cell`, in catch order. A net built again is empty.

## Invariants

- Piece ids grow and are never reused; no zone has more than its `Max` out.
- Every piece is on the grid, at or right of its `End` unless seen, left of its `Start` unless fed
  (`AsteroidTests`). None but a delivery spawns or goes in a view but at the grid's edges (`DebrisZonesTests`).
- Each delivery is one queued entry until caught, broken or gone, no piece naming another (`DeliveriesTests`).
- A far piece spawns on the ring of a hero out of every stream's box; `SeenAt` is never past the clock, 0 on the stream's.
- At most one line a hero and one a piece, each on a piece out (`LinesTests`). A wreck's `Cut` ≤ `Damage` < its hit points.
- `Found` only grows; each kind once and spawnable, each item once among its loot, each look once (`DebrisFoundTests`).
- After the drop every entry's net stands, none holds more than `NetHolds`; `Collect` and the drop make and lose
  nothing (`NetTests`). No position is negative.

## Deliberately not handled

- Reach, aim and the harpoon's shot: the client's (0044).
- A stale reported cell, a big window or a narrow grid may show a spawn; heroes share one far field's density (0052). A half-broken far wreck can despawn.
- A catch's popup is lost to a load or resync (0066); a floor stack picked up is not found.
- A wreck is judged by its middle cell: over the hull it cannot be hit. An asteroid towed out of its band stays out.
- Nets: no drone touches one; a piece through two is caught by the first; a full net raises no alert.
