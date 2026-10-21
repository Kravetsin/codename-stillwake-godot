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
[0060](../../docs/decisions/0060-the-farm-a-grow-capsule-is-a-fed-station.md),
[0066](../../docs/decisions/0066-the-handbook.md)), and the record of every kind found. Ported from
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
  `AtStart`, the pieces the first tick puts out; `Found`, the Handbook's record
  ([0066](../../docs/decisions/0066-the-handbook.md)): every kind found, in the order first found, each a `FoundKind`
  (public, with internal setters) `{ Kind, Items, Looks }`, the items seen come out of it and the looks caught or
  broken, each once, in the order first seen.
  All saved. **Not saved, never hashed:** the last 32 catches, each
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
  cells: small salvage near the ship, more and bigger wrecks the farther out, but cable racks about as rare a spawn
  as in the stream (4, 4 and 5), so found circuits are a moderate bonus (0056, amended);
  past 128 the **derelicts** too, and past 256 big and rich rock ([0056](../../docs/decisions/0056-rich-finds-far-out.md)):
  an engine block and a cargo hold one spawn in 79 past 128; with the pod cluster one in 28 past 256.
- **`asteroids`, the asteroid band** (`HullBand(From, To, Lead)`, 0054): two bands of rows, 22 to 44 past the
  hull's top row and as far past its bottom row, beyond the tether's 12 cells; across, the stream's way, from
  `Lead` right of the hull to as far left of it. Two tiers of the asteroid's kinds (0056): small, middling and large
  nearer, and from 72, its far edge, rows 36 to 44 out (every piece spawning `Lead` across, `d` is twice its rows
  there), more large and the ore-rich. Not a stream's box: the far field
  spawns in it as anywhere. A beat every 3 to 5 minutes (`Every` 3600, `Jitter` 2400), at most 2 out.

## Rules

- In the simulation phase, every tick: the clock advances; every piece moves left by its speed, or,
  reeled, toward its reel's point (below); one whose `X` is below its `End` is gone once no hero's view
  holds it, and one past the grid's left edge at once; and one of a zone round the heroes is gone unless
  it stays (below). Then each zone in content order, the stream first, on its beat.
- **No zone on a planet** (0076): on a surface whose kind has no debris (`Surfaces.Kind.Debris` false, the test strip)
  no zone beats and the world's first pieces are not put out: nothing spawns, nothing is drawn from the Rng. Each
  surface's debris drifts round its own heroes only, so a hero on a planet has none of the ship's round it.
- **A hero's view** (`Debris.View`, 60 × 34 cells either way round its reported cell): more than the
  farthest zoom shows on any screen (96 × 54 cells, `ShipCamera.Farthest`). No zone spawns a piece in any hero's view, nor takes one away in it
  but at the grid's edge, so no one sees debris appear or vanish.
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
  the cell `Lead` right of the hull, its **lead**, and ends `Lead` left of it, both kept on the grid. Where
  a hero's view holds the lead, the piece starts one column past that view's right edge instead, pushed on
  past any other hero's view it lands in (`Upstream`; each hero once, the column only growing), kept on the
  grid; so the stream comes in from past the right edge of every screen and drifts past the ship. A hero
  whose view does not hold the start moves nothing, however far ahead. The kind is drawn for the lead.
  The first is due on clock `Debris.Every`. Of the weights' 137 (0041's three times over), 108 are small pieces,
  27 wrecks and 2 soil clods: a spawn every 10 s on average, a wreck one in five, a clod one in 68, some 5 an
  hour on the wreck, where a few spawns are lost at the cap (`SoilClodTests`).
- **The band's beat** (`HullBand`): an asteroid spawns if fewer than `Max` of the band are out and there is a
  hull. Drawn in this order: a row of the band's `2 × (To − From + 1)`, those above the hull first, top down,
  then those below; a kind from its tier; a speed. A row off the grid spawns nothing, its draws taken all the
  same. It starts and ends as the stream's pieces do, `Lead` right and left of the hull, its start pushed
  out of every hero's view as theirs; its kind drawn for the lead.
- **The far field's beat** (`AroundHeroes`), with a hull: for each hero on the grid in id order, while
  fewer than the zone's `Max` (640) are out — so at the cap lower ids spawn first — and skipping, with
  no draw, a hero whose reported cell is in a stream's box or that already has `PerHero` (110) of the
  zone in its `Keep` box (68 × 48 cells either way): a point on the ring round its cell, drawn as a
  side of four (right, left, up, down), an offset along it (up to `RingTo`'s other half-size either way)
  and a depth across it (from `RingFrom` to `RingTo`: 61 to 64 columns, 36 to 42 rows, just past the
  farthest view, so what spawns on the right drifts into sight within a minute); then a kind
  from the tier the point's distance picks, and a speed. A point off the grid, in a stream's box or in
  any hero's view spawns nothing, its draws taken all the same. The
  piece starts there, in its cell's middle, `Start` its `X`, `End` 0, so it drifts in from where it spawns, out of sight
  and is gone past the grid's left edge; `SeenAt` the clock. A beat every tick, no jitter.
- **Despawn,** for a zone round the heroes only: a piece in some hero's `Keep` box is seen (`SeenAt` the
  clock) and stays; one outside every keep box stays while within some hero's `Hard` box (100 × 64)
  and seen less than `Linger` (600, 30 s) ago; else it is gone that tick: `Linger` after it was last
  seen, or at once outside every hard box, or when the last hero leaves. The keep box holds the view, so
  none is gone in sight. The stream's pieces are never gone by distance, only past their `End`.
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
  into exactly one circuit, their sure source (an engine block 1 in 6, a pod cluster 1 in 3, may hold one), a soil clod
  always into 1 to 3 seeds and 1 to 2 soil, the seed's second source beside supply pods, an asteroid into
  3 to 5 rock, the others by their size. Hit points alone say how
  hard a kind is: **every weapon breaks every kind** (0054). `Hit(Piece, Tool)` takes the weapon's
  damage for its sender off them (`IItemsQueries.Damage`: `Weapons.Of(Tool).Damage`, boosted by the module fitted into
  the sender's tool, 0098), adding it to the piece's `Damage`;
  the hit that brings `Damage` to its hit points removes it, and its loot (`Loot`, drawn by `LootOf`
  as a container's is) falls on the cell under its middle, still, dropped by `FloorStack.Nobody`
  (`IItemsOps.DropAt`): floating there in space for anyone to pick up at once, the hero who broke it
  too. Refused with `debris.gone`, `debris.notOutside` (as `Grab`), `debris.notWreckage` (a piece
  caught by hand), **`items.flat`**: the weapon is electric (its item has a `Draw`, the drill) and no
  battery on the sender's belt holds a tick of its draw, or, for the command only,
  **`items.notRunning`**: the weapon is electric and the sender's hero is not running it (`IItemsQueries.WeaponProblem`, shared with digging,
  0055). A hit spends nothing: the run pays by the tick in `Items`, hitting or not. `HitProblem`, what the client
  asks before it sends, leaves the run out, so the first hit goes in the same frame as the `Run` that starts it;
  the host applies them in that order. A `Tool` that deals no damage is our bug and throws: the client sends
  hits only with a weapon. The sim does not ask whether the sender holds the weapon, how fast it strikes nor
  how far it reaches: the client's word, among friends (principle 6). The client sends one `Hit` for each
  wreck a swing's drawn blade touches (0039, 0040), so one swing may hit two wrecks; the drill one for each
  its bit touches every `Pace` ticks while it runs, after its `Run`.
- **Found** (0066): a `Grab` that applies (by hand, or a piece reeled in and caught) and a `Hit` that breaks a
  wreck record its kind in `Found`, appended the first time, and every item `LootOf` drew for it, appended to that
  kind's `Items` the first time each is seen, in the order drawn: the same roll that gives the loot, no second one,
  and what floats off a full inventory too, as it was seen. A small piece caught as itself (a scrap chunk, a hull
  plate) records itself. The look the piece wore, `Content.Looks.Of(kind, id)` (its id round its kind's looks, 0
  for a kind with one; the one function the client draws it by), is appended to the kind's `Looks` the first time.
  Nothing else records: a hit that does not break, a reel, a refused command, a floor stack picked up. One record
  for the world, whoever caught or broke it; never sorted, never shrunk.
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
  last `CaughtKept` (32): the client's popups for its own catches (`features/items`, `OwnCatches`); `Found` — every
  kind found and what came out of it, live and read-only: the Handbook's (`features/debris`, `HandbookPage`).
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
- Every piece is on the grid, at or right of its `End` unless a hero sees it, and left of its `Start` unless
  it was reeled; each of the stream spawned in the band of the hull it spawned by, each asteroid in the
  asteroid band of the hull it spawned by, on its lead or pushed past every view that held it (`AsteroidTests`).
- No piece of any zone spawns in a hero's view, but one by the hull pushed to the grid's last column, nor
  is gone in one, but past the grid's left edge (`DebrisZonesTests`); the world's first pieces aside.
- A far piece spawns on the ring of a hero out past every stream's box that held fewer than `PerHero`,
  never in a stream's box; `SeenAt` is never past the clock, and 0 on the stream's.
- A reel's point is on the grid and its `Left` above zero.
- A wreck's `Damage` is below its hit points; a piece caught by hand has none.
- `Found` only grows: no kind leaves it, nor an item a kind's `Items`, and none moves. Each kind is in it once and
  can spawn in some zone; each item once, among its kind's loot, or the kind itself when it has none; each kind has
  at least one look, each once and below its `Content.Looks.CountOf` (`DebrisFoundTests`).

## Deliberately not handled

- Reach: the sim lets a hero in space grab any piece in space; the client offers only those within
  reach, among friends.
- A hero's reported cell is up to a second and a round trip behind its body (0050): a ring drawn round
  a stale cell, or a stream pushed past a stale view, may spawn near the screen's edge for a hero flying
  fast; it shows at once, at full strength.
- The view is a 1920×1080 screen's: a bigger window at the farthest zoom sees past it, and may see a piece
  appear at its edge. A grid too narrow to push a start past a view, a test's or a dev map's, starts it in
  its last column, in sight; the world's 2048 columns leave the ship's views far from its edges.
- A stream pushed right has farther to drift and holds more pieces on its way, so while a hero hovers by
  its lead the cap thins it, by up to some two fifths with the wreck's hull, a hero 60 columns ahead.
  Accepted: it lasts only while the hero stays there, and a higher `Max` would thicken the stream for all.
  Pushed past the stream's box, its pieces drift through the far field there, as the far field's drift
  through the stream.
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
- A world saved before the Handbook (format 17) begins with nothing found: nothing in a save says which kind an
  item came out of. A hull plate picked up off the floor is not one caught: picking up records nothing. Two
  catches of one kind on one tick are recorded in the host's order. Nothing says who found a kind: the record
  is the world's, as what the analyzer learns is.
- What a catch gave is kept for the client outside the saved state, as an event would be: a grab that runs as a
  world is loaded or a guest resynced shows no popup. Saving it would put a client's concern in every save and hash.
- A wreck half over the hull is judged by its middle cell, as a small piece is: with its middle over
  the ship it cannot be hit, though its end sticks out into space. Accepted: it drifts on and out.
- The weapon in hand, its pace (`Weapon.Pace`) and reach (its picture, 0040): the client's, among friends. A hit
  in flight after the weapon was put away still lands; creative mode needs no weapon either. The drill's hits in
  flight meet the belt and the run only as they apply: one sent as the belt runs flat is refused `items.flat`, one
  sent after the run was stopped `items.notRunning`.
- An asteroid reeled out of its band stays out: the band is where they spawn, not where they must be.
- The harpoon's shot, its range, the hit test, the hooked rod before the reel and the cooldown: the
  client's (0044). The cooldown is the hero's: every harpoon it holds waits for it. The sim does not
  ask whether a harpoon is held nor where the gun is; a hook not yet reeled is never saved.
- A diagonal reel is up to 41% faster than a straight one: the longer axis sets the time, with no
  square root. A reel goes on after its reeler leaves; reeled over the hull, a piece cannot be grabbed
  (`debris.notOutside`) and drifts back out; reeled left of its `End`, it is gone that tick, or once no hero
  sees it.

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
  finds measured in `DebrisDensityTests`; the soil clod (0060) in `SoilClodTests`; the Handbook's record (0066) in
  `DebrisFoundTests`.
