# Design and plan: each player learns for themself

## Problem

What is found and what is known are the crew's today (`Knowledge.Obtained`, `Knowledge.Granted`). It goes back to the
analyser, which one player at a time held for a minute. Now a thing is learned by having it. At the last playtest the
players who stood aside got blueprints from nowhere. Only the gatherer saw what they gathered open something. The
companion is to grow per hero too (drone-companion), and a crew-wide board makes that look wrong.

## Settled with the user (2026-11-20)

- **Found by holding.** A player has found an item once it has been in their inventory, by any road: dug, picked up,
  crafted into hand, taken from a crate or from a friend. A stack lying in a crate is no one's find.
- **The market stays the crew's.** Credits are the ship's, and a block bought is known to every player: one orders, another
  catches the crate, and a buyer-only grant would make that catch meaningless. Prices get a later pass, a little lower.
- **No old saves.** Everyone starts from zero; no migration.
- **Into the playtest build**, on `feat/gear-roles`.

## Player experience

1. Two friends start. One breaks the hold's piles; the hull plates and chunks go into their hands, and their board writes
   the notes and grants the first walls. The other's board stays empty.
2. The other takes chunks from the crate the first filled. Their board writes the same notes and grants the walls.
3. One buys the scanner at the market from the ship's credits. The other catches the delivery; the scanner opens on both
   drone wheels.
4. A friend joining an hour in knows what the map starts with. They catch up by taking from the crates.

**Gone wrong:** a player who never touches a material never learns it, however full the crates.
That is the point: they learn by handling.

## Approach

**Who uses it.** Now: the board, building, crafting, the drone's modes, bag and belt rows, the market. Next: the companion's
forms (each hero's own), and a price pass on the market.

**Knowledge** (global, saved) keeps its crew records: `Credits`, `Obtained` (where a stack is born, for the stretches of
space), `Sold`, `Reputation`, `Deliveries`. It adds a record a peer, `Peers`, sorted by peer:

- `Found`: the items the peer has held, each once, in the order first held (the board's order).
- `Granted`: blueprints, crafts and upgrades known to the peer, sorted ordinally.

The world's `Granted` is what every player knows: the map's `Layout.Known` and the crew's purchases.
A peer's record is made on first need, empty. Queries: `Knows(peer, id)` = the start's or the peer's; `KnowsAny(id)` = the start's or
any peer's; `Found(peer)`, `HasFound(peer, item)`. Ops: `Find(peer, item)` and `Grant(peer, id)`, both no-ops when already
there. `Version` moves on either.

**Items** finds in a derivation system, before research walks: each non-creative peer's inventory, slot by slot, in peer order,
`Find` for an item not yet found. A pass catches every road in (a static `Stacks.SetInventory` cannot reach Knowledge, and tests
and the fuzz write inventories too); it costs a set lookup a slot a tick. A creative peer finds nothing; what it spawned is found
once held out of creative. `Version` moves only on a new find or grant; `Obtain` no longer moves it.

**Research** walks each peer with a record: what is understood from that peer's finds is granted to that peer. A craft
still needs a machine that makes it standing on the surface. It walks only when `Version` or the grid moves, as now.
`Knows(peer, …)` and `Done(peer, node)` take the peer (its creative mode); the market's `Reached` and `BuyProblem` stay the
crew's, as do gates. `Debris.Unpack` grants a caught good to everyone and notes it to the catcher.

**Upgrades:** `Items.Own(peer)` reads the peer's knowledge; a bought bag, belt or trade-slot upgrade is everyone's.
`Flares` arm once anyone knows `ArmedBy` (`KnowsAny`).

**Client:** the board, build menu, fabricator, handbook, market page, tablet keys and drone wheel read the local peer.
`Recipes(peer)`.

## Determinism and co-op

Everything is in the sim and saved, sorted by peer, with ordinal string order. Nothing walks a dictionary. `Found` order is
the pass's order, surfaces by id, peers and slots in order. A joiner gets it with the world. Queries never make a record; only
`Find` and `Grant` do. No format bump: `learners` is an added field meaning "none yet", and old saves are not kept anyway.

## Out of scope

- Per-player credits, or prices: a later pass.
- Teaching a friend, or knowledge trade.
- A peer's record removed on leave: kept, as their inventory is.
- Crew-wide `Obtained` as anything but the stretches' trigger and tests.

## Trade-offs accepted

- **Purchases are the crew's**: whoever catches a crate, everyone learns what was in it. Only finds are a player's own.
- **A crafted item's parts** count as found only if they passed through hands. Craft-from-crates takes from a crate, so
  the parts are not found by the crafter; the made item, given to hand, is.
- **Creative** still knows everything for its peer and finds nothing new by spawning (a creative spawn writes the inventory
  and is found: accepted, dev only).

## Tests

- Scenarios: holding finds and grants to the holder only; a crate's stack is no one's until taken; taking from a crate
  finds it; a bought block is everyone's, whoever catches it; a bought bag upgrade opens everyone's rows; a late joiner
  knows the start only.
- Invariant (FsCheck): a peer's `Granted` grows only; every item in a non-creative peer's inventory is in its `Found`.
- Client: `BoardView` reads the local peer's finds.

## Steps

1. Knowledge's peer records, queries, ops; Items' finds pass; Research walking per peer; the callers taking a peer. Tests above.
2. The client reading the local peer; frames of the board for two players.
