# Each player learns for themself; purchases are the crew's

- Status: accepted; supersedes in part [the-board](2026-11-05-the-board.md) and
  [one-to-understand](2026-11-12-one-to-understand.md) (what is found and known being the crew's)
- Date: 2026-11-21

## Context

What was found and what was known were the crew's (`Knowledge.Obtained`, `Knowledge.Granted`), a rule left from the
analyser that one player held at a time. A thing is now learned by having it. At the last playtest, players who stood
aside got blueprints from nowhere, and only the gatherer saw what they gathered open something. The companion is each
hero's own ([drone-companion](2026-11-21-drone-companion.md)), and a crew-wide board looked wrong beside it.

## Decision

- **Found by holding.** A player has found an item once it has been in their inventory, equipment included, by any road:
  dug, picked up, crafted into hand, taken from a crate or from a friend. A stack in a crate is no one's find.
  `Items` checks every non-creative peer's slots in a derivation system before research walks, in peer and slot order,
  and calls `Knowledge.Find` for an item not yet found. A creative peer finds nothing; what it spawned is found once held
  out of creative.
- **Knowledge keeps a record a peer** (`Learner`: `Found` in the order first held, `Granted` sorted ordinally), saved as
  `learners`, made on first need. The world's `Granted` is what every player knows: the map's `Layout.Known` and what the
  crew bought. Queries: `Knows(peer, id)`, `KnowsAny(id)`, `KnownIds(peer)`, `Found(peer)`, `HasFound(peer, item)`.
  `Version` moves on a find or a grant; `Obtain` no longer moves it.
- **Research walks each learner**: what is understood from a peer's finds is granted to that peer. A craft still needs a
  machine that makes it standing on the surface. `Recipes(peer)`, `Knows(peer, …)` and `Done(peer, node)` take the peer.
- **The market stays the crew's.** Credits are the ship's, and a block bought is known to everyone: one orders, another
  catches the crate. The market's `Reached` and `BuyProblem`, its gates and a trade station's slots are the crew's;
  a bought bag, belt or trade-slot upgrade opens everyone's rows. Magnetic flares arm once anyone knows `ArmedBy`.
- **The client reads the local peer**: the board, the build menu, the Fabricator, the Handbook, the market page, the
  edge's keys and the drone's wheel.

## Deliberately not handled

- **A crafted item's parts** count as found only if they passed through hands: crafting from crates finds the made item,
  not its parts.
- Credits stay the ship's; prices get a later pass.
- No teaching a friend or trading knowledge.
- A peer's record stays when they leave, as their inventory does.
- No migration: everyone in an older save starts from zero; `learners` is an added field meaning "none yet".
- `Knowledge.Obtained` stays crew-wide, for the stretches of space and tests.
