# Knowledge

## Files

| File | Purpose |
| --- | --- |
| `Knowledge.cs` | What the crew and each player know, and what the crew has earned, wherever it did (0076): the blueprints and crafts known, each player's finds, the credits, what was obtained and sold. |

What the crew and each player know, and what the crew has earned, wherever it did ([0076](../../docs/decisions/0076-a-world-is-made-of-surfaces.md),
[trade-and-tech](../../docs/decisions/2026-10-30-trade-and-tech.md),
[own-knowledge](../../docs/decisions/2026-11-21-own-knowledge.md)). A global: one per world, made by the ship's
registry and shared by every surface's, saved second in the ship's `slices`. Records only: no commands, no systems.
`Research` and `Trade`, one a surface, hold the rules.

- **Depends on:** nothing; it is made with the id of every blueprint and craft (`ResearchModule.Ids`), what a map
  with no `Layout.Known` is granted.
- **State** (`KnowledgeState`, members internal): `Granted` — what every player knows, by id, sorted ordinally: the map's
  `Layout.Known` and what the crew bought since; `Learners` — a `Learner(Peer, Found, Granted)` a peer, in peer order, made on
  first need: the items it has held in the order first held, and what it understood from them, sorted ordinally (saved as
  `learners`, omitted while none); `Credits`; `Obtained` — `Tally(Item, Count)`, in the order first obtained; `Sold` — `Tally`s, sorted
  ordinally by item; `SoldWorth`, what everything sold was worth; `Reputation(Level, Base, BaseWorth)`, the mafia's level and what had been sold
  as it opened, the next level's goods and the worth ([mafia-reputation](../../docs/decisions/2026-11-08-mafia-reputation.md));
  `Deliveries` — `Delivery(Id, Good)`, purchases paid for and not yet caught, broken or lost, in the order bought, and `NextDelivery`, ids only growing
  ([mafia-deliveries](../../docs/decisions/2026-11-08-mafia-deliveries.md)). Saved, `Obtained`, `Sold` and `Deliveries`
  omitted when empty; `Lost`, deliveries lost since the load, never. Derived, never saved: where each item stands in `Obtained` and each learner's `Found` as a set, looked up and never
  iterated; `Version`.
- **Files:** `Knowledge.cs` — the module, its state, query and operation interfaces.

## API

- **Queries** (`IKnowledgeQueries`): `Granted(id)`, everyone's; `Knows(peer, id)`, everyone's or the peer's own; `KnowsAny(id)`,
  anyone's; `KnownIds(peer)`, `Learners`, `Found(peer)`, `HasFound(peer, item)`; `GrantedIds`, `Credits`, `Obtained`,
  `ObtainedOf(item)`, `SoldOf(item)`, `SoldWorth`, `Level`, `BaseOf(item)`, `BaseWorth`, `Deliveries`, `InFlight(good)`, `Lost`; `Version`, which moves whenever something is newly found or
  granted, a level opens, or a delivery is queued or ends.
- **Operations** (`IKnowledgeOps`): `Earn(credits)`; `Spend(credits)`, which throws when short, the caller having checked;
  `Grant(id)`, for everyone, and `Grant(peer, id)`, for one peer, again changing nothing; `Find(peer, item)` (`Items`), once; `Obtain(item, count)` (`Items`) and
  `Sell(item, count, worth)` (`Trade`), a count of 0 or less counting for nothing; `Open(level, start, startWorth)` (`Research`), kept only for the
  level after the current one, so every surface may call it on a tick and the world rises once; `Deliver(good)` (`Research`), queued under a new id;
  `Delivered(id, lost)` (`Debris`), an id not on its way let be.

## Invariants

- `Granted` and each learner's `Granted` only grow, hold each id once, sorted; a learner's `Found` holds each item once.
- `Credits` never go negative.
- `Level` only grows, one at a time.
- A delivery id is used once: `NextDelivery` only grows.
- `Obtained` holds each item once, its counts above zero and only growing; `Sold` likewise, sorted.
- Whatever lies in a slot or on the floor has a count or is known from the start (`CountsTests`), a creative peer's spawns aside.
