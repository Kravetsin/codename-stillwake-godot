# Design and plan: each player learns for themself

## Problem

What is found and what is known are the crew's today (`Knowledge.Obtained`, `Knowledge.Granted`). It goes back to the
analyser, which one player at a time held for a minute. Now a thing is learned by having it. At the last playtest the
players who stood aside got blueprints from nowhere. Only the gatherer saw what they gathered open something. The
companion is to grow per hero too (drone-companion), and a crew-wide board makes that look wrong.

## Settled with the user (2026-11-20)

- **Found by holding.** A player has found an item once it has been in their inventory, by any road: dug, picked up,
  crafted into hand, taken from a crate or from a friend. A stack lying in a crate is no one's find.
- **The market sells to the buyer.** Credits stay the ship's. A block bought is known to its buyer alone; each player
  buys their own. Prices get a later pass, a little lower.
- **Into the playtest build**, on `feat/gear-roles`.

## Player experience

1. Two friends start. One breaks the hold's piles; the hull plates and chunks go into their hands, and their board writes
   the notes and grants the first walls. The other's board stays empty.
2. The other takes chunks from the crate the first filled. Their board writes the same notes and grants the walls.
3. One buys the scanner at the market from the ship's credits. When the delivery is caught, by whoever, the scanner opens
   on the buyer's drone wheel only. The other may buy their own.
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

The world's `Granted` becomes the start: what the map's `Layout.Known` grants every player, nothing added to it after.
A peer's record is made on first need, empty. Queries: `Knows(peer, id)` = the start's or the peer's; `KnowsAny(id)` = the start's or
any peer's; `Found(peer)`, `HasFound(peer, item)`. Ops: `Find(peer, item)` and `Grant(peer, id)`, both no-ops when already
there. `Version` moves on either.

**Items** finds in a derivation system, before research walks: each non-creative peer's inventory, slot by slot, in peer order,
`Find` for an item not yet found. A pass catches every road in (a static `Stacks.SetInventory` cannot reach Knowledge, and tests
and the fuzz write inventories too); it costs a set lookup a slot a tick. A creative peer finds nothing; what it spawned is found
once held out of creative. `Version` moves only on a new find or grant; `Obtain` no longer moves it.

**Research** walks each peer with a record: what is understood from that peer's finds is granted to that peer. A craft
still needs a machine that makes it standing on the surface. It walks only when `Version` or the grid moves, as now.
`Knows(peer, …)`, `Done(peer, node)`, `Reached(peer, node)`, `BuyProblem(peer, node)` take the peer. A gate (the mafia's
level) stays the crew's. `BuyBlock` delivers for `c.From`; `Delivery` gains `For`. `Debris.Unpack` grants the good to `For`
and notes it to `For` where the piece broke.

**Upgrades:** a bag or belt upgrade opens rows for its buyer (`Items.Own(peer)` reads the peer's grants). A trade station's
slots are the ship's: open once anyone bought them (`KnowsAny`). `Flares` arm once anyone knows `ArmedBy` (`KnowsAny`).

**Client:** the board, build menu, fabricator, handbook, market page, tablet keys and drone wheel read the local peer.
`Recipes(peer)`.

## Determinism and co-op

Everything is in the sim and saved, sorted by peer, with ordinal string order. Nothing walks a dictionary. `Found` order is
the pass's order, surfaces by id, peers and slots in order. A joiner gets it with the world. Queries never make a record; only
`Find` and `Grant` do. No format bump: `learners` and `Delivery.For` are added fields meaning "none yet". An old save's
`granted` stays what every player knows, and its old deliveries, with no buyer, grant everyone.

## Out of scope

- Per-player credits, or prices: a later pass.
- Teaching a friend, or knowledge trade.
- A peer's record removed on leave: kept, as their inventory is.
- Crew-wide `Obtained` as anything but the stretches' trigger and tests.

## Trade-offs accepted

- **Machines read the crew** (`KnowsAny`): a trade slot bought by one opens the station for all. A machine is the ship's.
- **A crafted item's parts** count as found only if they passed through hands. Craft-from-crates takes from a crate, so
  the parts are not found by the crafter; the made item, given to hand, is.
- **Creative** still knows everything for its peer and finds nothing new by spawning (a creative spawn writes the inventory
  and is found: accepted, dev only).

## Tests

- Scenarios: holding finds and grants to the holder only; a crate's stack is no one's until taken; taking from a crate
  finds it; a bought block goes to the buyer, whoever catches it; a bag upgrade opens the buyer's rows only; a trade slot
  opens for the station; a late joiner knows the start only; the migration step gives old finds to every peer.
- Invariant (FsCheck): a peer's `Granted` grows only; every item in a non-creative peer's inventory is in its `Found`.
- Client: `BoardView` reads the local peer's finds.

## Steps

1. Knowledge's peer records, queries, ops; Items' finds at `SetInventory`; Research walking per peer; the callers taking a
   peer; the save step. Tests above.
2. The client reading the local peer; the market buying for self; frames of the board for two players.
