# Debris

The pieces of debris drifting by, in zones by the hull, in its asteroid band and round every hero,
catching them, reeling them in by harpoon, and breaking wreckage and asteroids with any weapon
([0006](../../docs/decisions/0006-debris-in-the-simulation.md),
[0009](../../docs/decisions/0009-debris-around-the-ship.md),
[0034](../../docs/decisions/0034-wreckage-and-the-axe.md),
[0041](../../docs/decisions/0041-wreckage-of-many-kinds-and-circuits.md),
[0044](../../docs/decisions/0044-the-harpoon-reels-a-piece-to-a-point.md),
[0052](../../docs/decisions/0052-debris-by-zones-around-every-hero.md),
[0054](../../docs/decisions/0054-asteroids-and-the-drill.md), [0055](../../docs/decisions/0055-an-electric-tool-draws-while-it-runs.md),
[0060](../../docs/decisions/0060-the-farm-a-grow-capsule-is-a-fed-station.md)). Ported from
`packages/sim/src/modules/debris`; its rules hold here unchanged, the harpoon and the zones new in the port.

- **Depends on:** `Grid` (`IGridQueries`: its size, its `Version` and the hull's bounds), `Crew`
  (`ICrewQueries`: where the sender's hero is, and every hero's reported cell), `Rooms`
  (`IRoomsQueries`: what is space), `Items` (`IItemsQueries`: `CanDrain`, whether the belt can pay a tick of an
  electric weapon's draw, and `Running`, whether the sender runs it; `IItemsOps`: `GiveOrDrop`, the inventory a
  caught piece goes into; `DropAt`, the floor a broken wreck falls on).
- **State** (`DebrisState`, members internal): `Pieces` in spawn order, so in id order, each a `Piece` (public, with
  internal setters) `{ Id, Zone, Item, X, Y, Speed, Start, End, Damage, Reel, SeenAt }`, of the zone `Zone` (an id of
  `Debris.Zones`), its middle at `X`, `Y` in thousandths
  of a cell (`DebrisModule.Unit`), drifting `Speed` a tick from `Start` till below `End`, `Damage` the hit points weapons took off it,
  `Reel` a harpoon pulling it (a `Reeling { By, ToX, ToY, Left }`: the peer, the point, the ticks still to go) or null,
  `SeenAt` the clock it was last in a hero's keep box (0 for the stream's); `NextId`,
  never reused; `Clock`, ticks run; `Beats`, each zone's `Beat { Zone, Next }`, the clock its next piece is due at;
  `AtStart`, the pieces the first tick puts out. All saved. **Not saved, never hashed:** the last 32 catches, each
  piece and what it gave, written by `Grab` and read by nothing in the sim, only by the client (`CaughtFrom`); forgotten
  on a create or a load. **Derived:** the hull's bounds, followed by the
  grid's log (0051): grown over a structure come, and only when one on its edge goes, its edge lines looked
  at again (never saved; scanned whole after a create, a load, or when the log no longer reaches back).
- **Files:** `Debris.cs` — the module, `Grab`, `Hit`, `Reel` and their rules; `Drift.cs` — the system:
  drift, the zones' beats and spawns, despawn, the hull; `State.cs` — its slice, `Piece`, `Reeling` and
  `Beat`; `Fuzz.cs` — its fuzz generator.
- **Runs** after `Hazards` and before `Furnace` in `Registry.All`, as in TypeScript's `MODULES`.

## Zones

`Content.Debris.Zones`, in order: each a `DebrisZone { Id, Anchor, Every, Jitter, Max, DriftMin,
DriftMax, Tiers }`. Its anchor says where its pieces spawn; its tiers give its kinds by weight by the
distance from the hull's box, `d = max(dx, 2·dy)` in cells past it (rows count double, the world being
half as tall), each tier from its `From` up to the next's (`DebrisModule.TierOf`).

- **`ship`, the stream** (`ByHull(Margin, Lead)`): the stream past the ship, as before zones, its one
  tier `Debris.Kinds`, the only zone with soil clods (0060). Its **box** is the hull grown by `Lead` columns either way and `Margin` rows.
- **`far`, the far field** (`AroundHeroes`): round every hero out past every stream's box, from the
  cell its client last reported (`Hero.Cell`, about once a second, 0050). Three tiers, at 0, 128 and 256
  cells: small salvage near the ship, more and bigger wrecks, cable racks most of all, the farther out;
  past 128 the **derelicts** too, and past 256 big and rich rock ([0056](../../docs/decisions/0056-rich-finds-far-out.md)):
  an engine block and a cargo hold one spawn in 83 past 128; with the pod cluster one in 31 past 256.
- **`asteroids`, the asteroid band** (`HullBand(From, To, Lead)`, 0054): two bands of rows, 22 to 44 past the
  hull's top row and as far past its bottom row, beyond the tether's 12 cells; across, the stream's way, from
  `Lead` right of the hull to as far left of it. Two tiers of the asteroid's kinds (0056): small, middling and large
  nearer, and from 72, its far edge, rows 36 to 44 out (every piece spawning `Lead` across, `d` is twice its rows
  there), more large and the ore-rich. Not a stream's box: the far field
  spawns in it as anywhere. A beat every 3 to 5 minutes (`Every` 3600, `Jitter` 2400), at most 2 out.

## Rules

- In the simulation phase, every tick: the clock advances; every piece moves left by its speed, or,
  reeled, toward its reel's point (below); one whose `X` is below its `End` is gone; and one of a zone
  round the heroes is gone unless it stays (below). Then each zone in content order, the stream first,
  on its beat.
- **A beat:** on the clock its `Beat.Next`, the next is due `Every` plus `Below(Jitter + 1)` ticks on;
  a zone with no jitter draws nothing for it, so a beat that spawns nothing takes nothing from the Rng.
  A zone with no beat, one added to content since the world was saved, is given one on its first tick,
  due `Every` on. A new world's first beats are due on their zone's `Every`.
- **The stream's beat:** a piece spawns if fewer than `Max` of the stream are out (the far field's do
  not count) and there is a hull: the bounds of every cell with a structure in it, walked only within the
  grid's `Built` box, so it costs the ship, not the world (0048), and only when the grid's `Version`
  has moved. Drawn with the world's `Rng.Below`, in this
  order: a row among the hull's and `Margin` above and below; a kind by weight from its tier, walking
  its kinds in order; a speed from `DriftMin` to `DriftMax`. It starts in the middle of
  the cell `Lead` right of the hull and ends `Lead` left of it, both kept on the grid.
  The first is due on clock `Debris.Every`. Of the weights' 137 (0041's three times over), 108 are small pieces,
  27 wrecks and 2 soil clods: a spawn every 10 s on average, a wreck one in five, a clod one in 68, some 5 an
  hour on the wreck, where a few spawns are lost at the cap (`SoilClodTests`).
- **The band's beat** (`HullBand`): an asteroid spawns if fewer than `Max` of the band are out and there is a
  hull. Drawn in this order: a row of the band's `2 × (To − From + 1)`, those above the hull first, top down,
  then those below; a kind from its tier; a speed. A row off the grid spawns nothing, its draws taken all the
  same. It starts and ends as the stream's pieces do, `Lead` right and left of the hull.
- **The far field's beat** (`AroundHeroes`), with a hull: for each hero on the grid in id order, while
  fewer than the zone's `Max` (640) are out — so at the cap lower ids spawn first — and skipping, with
  no draw, a hero whose reported cell is in a stream's box or that already has `PerHero` (110) of the
  zone in its `Keep` box (68 × 48 cells either way): a point on the ring round its cell, drawn as a
  side of four (right, left, up, down), an offset along it (up to `RingTo`'s other half-size either way)
  and a depth across it (from `RingFrom` to `RingTo`: 61 to 64 columns, 36 to 42 rows, just past the
  farthest view, so what spawns on the right drifts into sight within a minute); then a kind
  from the tier the point's distance picks, and a speed. A point off the grid, in a stream's box or in
  any hero's `View` (60 × 34, the farthest zoom) spawns nothing, its draws taken all the same. The
  piece starts there, in its cell's middle, `Start` its `X`, `End` 0, so it drifts in from where it spawns, out of sight
  and is gone past the grid's left edge; `SeenAt` the clock. A beat every tick, no jitter.
- **Despawn,** for a zone round the heroes only: a piece in some hero's `Keep` box is seen (`SeenAt` the
  clock) and stays; one outside every keep box stays while within some hero's `Hard` box (100 × 64)
  and seen less than `Linger` (600, 30 s) ago; else it is gone that tick: `Linger` after it was last
  seen, or at once outside every hard box, or when the last hero leaves. The stream's pieces are never
  gone by distance, only past their `End`.
- On the first tick a world begins with its map's `Layout.DebrisAtStart` pieces of the stream out (at most
  its `Max`), each drawn as a spawn is, then placed along its way, its `X` drawn evenly from its
  `End` to its `Start`. The wreck sets `Debris.AtStart`, so a player waits for none; test maps set
  none and begin empty.
- Pieces pass through the hull and each other: nothing collides.
- `Grab(Piece)` puts the piece into the sender's inventory as one of its salvage item, or, a
  container (`Loot`: a cargo crate, a supply pod, every other one with one or two seeds, 0060), as what it opens into, drawn entry by entry by
  the world's `Rng` as the grab applies (`LootOf`): the one-in roll, then the count. Refused with
  `debris.gone` (no such piece out), `debris.notOutside` (the sender's hero, at the cell its client
  last reported, or the piece's middle is not in space). A full inventory is no reason: what does not
  fit floats at the hero's cell, on the floor of space (`IItemsOps.GiveOrDrop`). A wreck is refused
  with `debris.tooBig`.
- **Wreckage** is a kind with hit points in `Wreckage`: a hull section (15), a cargo container (11),
  a machine block (19), a cable rack (9), a tank cluster (7), a shield slab (31) (0034, 0041), a soil clod (8,
  0060), an asteroid
  (120, 0054), a small (50), a large (200) and an ore-rich one (160), an engine block (180), a cargo hold (140) and a
  pod cluster (110) (0056). It drifts, spawns and is saved as any piece; each kind breaks into its own `Loot`, a cable rack always
  into exactly one circuit, their sure source (an engine block or a pod cluster may hold one or two), a soil clod
  always into 1 to 3 seeds and 1 to 2 soil, the seed's second source beside supply pods, an asteroid into
  3 to 5 rock, the others by their size. Hit points alone say how
  hard a kind is: **every weapon breaks every kind** (0054). `Hit(Piece, Tool)` takes the weapon's
  `Weapons.Of(Tool).Damage` off them, adding it to the piece's `Damage`;
  the hit that brings `Damage` to its hit points removes it, and its loot (`Loot`, drawn by `LootOf`
  as a container's is) falls on the cell under its middle, still, dropped by `FloorStack.Nobody`
  (`IItemsOps.DropAt`): floating there in space for anyone to pick up at once, the hero who broke it
  too. Refused with `debris.gone`, `debris.notOutside` (as `Grab`), `debris.notWreckage` (a piece
  caught by hand), **`debris.flat`**: the weapon is electric (its item has a `Draw`, the drill) and no
  battery on the sender's belt holds a tick of its draw (`IItemsQueries.CanDrain`), or, for the command only,
  **`debris.notRunning`**: the weapon is electric and the sender's hero is not running it (`IItemsQueries.Running`,
  0055). A hit spends nothing: the run pays by the tick in `Items`, hitting or not. `HitProblem`, what the client
  asks before it sends, leaves the run out, so the first hit goes in the same frame as the `Run` that starts it;
  the host applies them in that order. A `Tool` that deals no damage is our bug and throws: the client sends
  hits only with a weapon. The sim does not ask whether the sender holds the weapon, how fast it strikes nor
  how far it reaches: the client's word, among friends (principle 6). The client sends one `Hit` for each
  wreck a swing's drawn blade touches (0039, 0040), so one swing may hit two wrecks; the drill one for each
  its bit touches every `Pace` ticks while it runs, after its `Run`.
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
  `GrabProblem(peer, piece)`, `HitProblem(peer, piece, tool)` and `ReelProblem(peer, piece)` — the refusal
  that peer's `Grab`, `Hit` or `Reel` would get, for the client; `CaughtFrom(piece)` — what catching that piece put
  into its catcher's inventory (`IItemsOps.GiveOrDrop`'s answer, what floated left out), or null for no catch among the
  last `CaughtKept` (32): the client's popups for its own catches (`features/items`, `OwnCatches`).
- **Static:** `CellOf(x, y, width)` — the cell under a piece's middle; `LootOf(item, rng)` — what
  catching a piece of that item gives, merged by item in the order its entries first give it.
- **Commands:** `Grab(Piece)`, `Hit(Piece, Tool)`, `Reel(Piece, ToX, ToY)`.
- **Lookups:** `Grab`, `Hit` and `Reel` find their piece by a binary search of the pieces, in id order.
  No spatial index: the box tests and each hero's share are pieces times heroes a tick, some 0.12 ms at
  600 pieces and four
  heroes in a debug build (`BudgetTests`).
- **Density:** `DebrisDensityTests` flies a hero by jetpack and hovers one, and counts the far pieces in the
  default zoom's view (40 × 22) and the farthest's (120 × 68): hovering some 4.7 and 40, a new piece in the
  default view every 11 s; flying at 15 cells a second some 4 and 31, never empty more than 1.4 s. Pieces
  spread evenly hold about a ninth as many in the default view as in the farthest.
- **Fuzz:** a grab, a hit with a weapon drawn among `Weapons.All` or a reel, one in three each, at a piece that is out three times in four,
  else an id from 0 to `NextId + 1`, gone or never spawned; a reel's point is anywhere from a grid's
  size before the grid to one past it, so the clamp is drawn too. Most are refused, as few fuzzed
  heroes stand in space. The far field needs none of its own: crew's fuzz enters heroes anywhere on
  the big world, and the wreck's desync test checks far pieces spawn.

## Invariants

- Piece ids grow in spawn order and are never reused; no more than its zone's `Max` of a zone are out.
- Every piece is on the grid, at or right of its `End`, and left of its `Start` unless it was reeled;
  each of the stream spawned in the band of the hull it spawned by, each asteroid in the asteroid band of
  the hull it spawned by, on its lead (`AsteroidTests`).
- A far piece spawns on the ring of a hero out past every stream's box that held fewer than `PerHero`,
  never in a stream's box nor in any hero's view; `SeenAt` is never past the clock, and 0 on the stream's.
- A reel's point is on the grid and its `Left` above zero.
- A wreck's `Damage` is below its hit points; a piece caught by hand has none.

## Deliberately not handled

- Reach: the sim lets a hero in space grab any piece in space; the client offers only those within
  reach, among friends.
- A hero's reported cell is up to a second and a round trip behind its body (0050): a ring drawn round
  a stale cell may spawn near the screen's edge for a hero flying fast; it shows at once, at full strength.
- Heroes together share one far field's density; at the far field's cap lower ids spawn first; a point
  off the grid or in a stream's box wastes that hero's beat; the tiers move with the hull.
- Far pieces drift through the stream's box without counting toward its cap. A half-chopped wreck left
  30 s out of every keep box is gone with its damage.
- With no hull there is no far field: its distances are from the hull's box.
- A hero stopping where no far field was waits some 45 s for the first piece in the default view: nothing
  spawns in the farthest view, so pieces come in by the drift, under a cell a second.
- A hero that hovered a while and then climbs or dives meets its ring's top or bottom band: a share's quarter
  in a few rows, up to some 20 pieces at once in the default view where the field holds 4. Accepted: a
  passing cloud; thicker bands thin the field everywhere else (`DebrisDensityTests` bounds it).
- Two grabs of one piece on one tick: the host's order decides, and the second is `debris.gone`; so
  for hits after the one that breaks a wreck.
- What a catch gave is kept for the client outside the saved state, as an event would be: a grab that runs as a
  world is loaded or a guest resynced shows no popup. Saving it would put a client's concern in every save and hash.
- A wreck half over the hull is judged by its middle cell, as a small piece is: with its middle over
  the ship it cannot be hit, though its end sticks out into space. Accepted: it drifts on and out.
- The weapon in hand, its pace (`Weapon.Pace`) and reach (its picture, 0040): the client's, among friends. A hit
  in flight after the weapon was put away still lands; creative mode needs no weapon either. The drill's hits in
  flight meet the belt and the run only as they apply: one sent as the belt runs flat is refused `debris.flat`, one
  sent after the run was stopped `debris.notRunning`.
- An asteroid reeled out of its band stays out: the band is where they spawn, not where they must be.
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
- The harpoon and `Reel` are new in the port; TypeScript has none. So are the zones and the far field
  (0052), with their own scenarios and invariant test in `DebrisZonesTests`, and the asteroid band and the
  weapons (0054), in `AsteroidTests`; the asteroid's kinds and the derelicts (0056) in `RichFindsTests`, a sortie's
  finds measured in `DebrisDensityTests`; the soil clod (0060) in `SoilClodTests`.
