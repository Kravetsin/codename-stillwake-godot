# Black market: reputation and deliveries (design)

Status: design accepted by the user, cut by `design-reviewer`. Numbers are a first pass.

## Problem

Players see weak first goods (bags and slots), fill the first order, see only more of the same appear (as garbage),
and abandon the market. The 12×5 grid looks empty; nothing can be bought twice, so credits stop mattering.

## Player experience

1. The market (M, the tablet's second page) shows every good: picture, name, description. A price the mafia has not
   opened yet is garbage; the button says "not enough reputation".
2. The left card, in place of the order: "The mafia is interested in: glass, silicon, frames", a bar for each and one
   for the level.
3. The crew sells. Wanted goods move the bar fast, anything else slowly. At the target the next level's prices
   decipher. Level 1 already opens the scanner.
4. A purchase arrives in the ship's stream on the trade station's row, its good's icon over it. An upgrade is a parcel
   taken with E; anything else a crate of about three hits by any tool, spilling its items for whoever grabs them.
5. Gear can be bought again and again, one per hero. A confiscated container spills a random mix, sometimes rare.

What goes wrong: nobody goes out, the crate drifts past the stern. It is gone with its credits; the tile is buyable
again.

## Deliberately not handled

- Cosmetics, coordinates and tips, loans, protection rackets, a wheel UI.
- The contraband items themselves (a PR an item). Where it heads: a power driver that draws no charge, a regen suit
  that also keeps out the night's chill. Strictly better or unique, never a trade-off.
- Themed containers, a second container tier, a materials shop.
- Nets and salvage drones catching deliveries.
- An "on its way" state: the tile reads "bought" while in flight.
- A delivery bought while no hero is aboard the ship (the crew on a planet): it comes anyway and is lost past the
  empty ship, the simplest rule; the market says where it arrives.
- Refunds, insurance, reputation that decays, levels past the last.
- Reputation sold past a target, the rest of that capsule included: dropped.
- A refund for gear blueprints bought before the change, and progress toward a level kept across the migration: the
  game is pre-release.
- A later level's goods sold early count at a seventh: an auto station sells everything priced.

## Approach

**Records in `Knowledge`, rules in `Research`.** Knowledge stays a record keeper with no rules
(`sim/Knowledge/README.md`). It gains saved `SoldWorth` (a running sum of the worth sold, which Trade, `Sell`'s only
caller, passes along with each sale) and `Reputation { Level, Base }`, where `Base` is a snapshot taken as the level
opens: `SoldWorth` and the `SoldOf` of the next level's wanted goods. Wanted goods are disjoint across levels, so
progress is derived, never reset, in sevenths (the full formula and terms: [the PR 1 plan](black-market-reputation.md)):

- a wanted good: `sold = SoldOf − Base`; `min(count, sold) × worth × 7`, its excess past the count at ×1;
- everything else: the worth sold since `Base` less the wanted goods' worth, at ×1, never below 0 (a worth changed
  by a later build could otherwise take it under).

`Research.Walk` raises the level once progress reaches `target×7` and snapshots the new `Base` through
`IKnowledgeOps.Open(Level + 1, …)`, which Knowledge keeps only for the level after its own, so two surfaces walking on
one tick raise it once.

**Delivery is one path.** `Knowledge.Deliveries` holds everything paid for and not yet caught or lost: `{Id, Good}`.
`BuyBlock` spends the credits and queues a delivery; it no longer grants. The ship's `Debris` (no other surface's)
spawns each delivery that has no piece yet, the same tick, `Lead` right of the hull on the row of the trade station
with the lowest machine id; with no station standing, deliveries wait queued. Two kinds:

- `parcel`, for upgrades: `Grab` grants the upgrade, crew-wide as today (the user's call).
- `deliveryCrate`, `DeliveryHp` 15: breaking it grants a blueprint or drops gear (`DropAt(Nobody)`). The confiscated
  container is the same crate drawing from a `Loot` table as it breaks.

Catching, breaking or losing a piece removes its delivery, so `Debris` takes `IKnowledgeOps` (no cycle: Knowledge is
built first, Research before Debris, so the queue has to live in Knowledge).

**Bought:** an upgrade or blueprint granted or in flight cannot be bought again; a lost one simply is not either, so
it can. Gear is never "bought".

**Gear:** `powerDriver`, `scanner`, `heavyHelmet`, `plasmaCutter`, `regenSuit`. Their crafts leave `Crafts` and the
workbench; their worth becomes a hand price in `Market`'s raw table. `salvageDrone` and `solarPanel` stay crew
blueprints.

**Refusals:** `research.reputation` replaces `research.locked` for goods; `research.bought` and `research.noCredits`
stay.

### Reputation levels (first pass)

`Content.Research`'s rings keep their shape and their `Order` of (item, count): a ring's gate is the level, what it
orders the wanted goods. The target is what the order is worth, `Σ count × worth`; a wanted good fills its own share
(`count × worth`) at full worth, its excess and anything else at `OffWanted = 7`, 1/7 of their worth. The card counts
wanted goods in pieces ("glass 12/20") as today. The scanner moves from the second order to the first, to be its
headline; the second and fourth orders gain a third good, the fifth asks more. Worths are `Market.WorthOf`'s today.

| Level | Sell to reach it | Target (worth) | Opens | Headline |
| --- | --- | --- | --- | --- |
| 0 | — (open from the start) | — | bag1, bag2, tradeSlots1 | — |
| 1 | scrapChunk 40, iron 20, planks 20 (as today) | 1 580 | bag3, bag4, tradeSlots2 | **scanner** (1 500) |
| 2 | plate 30, gear 20, wire 30 | 2 780 | bag5, bag6, belt1, tradeSlots3–4 | **powerDriver** (3 000) |
| 3 | glass 20, silicon 20, frame 10 (as today) | 4 930 | bag7, bag8, tradeSlots5, confiscated container | **heavyHelmet** (5 000) |
| 4 | circuit 15, coil 10, tube 30 | 4 670 | solarPanel, bag9, tradeSlots6, canister1, salvageDrone | **plasmaCutter** (4 200) |
| 5 | copper 40, mica 20, hide 15 | 3 520 | bag10, bag11, belt2 | **regenSuit** (12 000) |

Selling only what is not wanted, a level takes seven times its target: some 11 000 credits of other goods for level 1.
Gear costs worth ×2 an item from PR 3. The container costs 1 500; its loot is parts of about that worth, one in twelve
holding a powerDriver or a scanner, one in thirty a plasmaCutter.

### Market page

- The order card becomes the wanted card: the three goods, a bar each, a bar for the level.
- Every tile always shown, its price ciphered until its level; `MarketReveal` deciphers a level's prices as it opens.
- Gear and container tiles never read "bought".
- In the world the delivery's piece wears its good's icon, and an edge marker points to it while it is out of view.
  The parcel's and crate's looks are the user's pick, on an Artifact.

### Save migration

- PR 1: a `SaveFormat` bump; `Level` = the old orders met in sequence, `Base` = the `sold` of the next level's goods,
  sums from 0; progress toward the next level is lost. Details in the PR 1 plan.
- PR 3: gear crafts leave `Granted`; gear already made stays. No refund.

## Determinism and co-op

- Everything enters through `BuyBlock` and `SendCapsule`. A delivery spawns in the purchase's tick with no Rng draw; a
  container's loot is drawn as it breaks, as a wreck's is. Points are integers in sevenths.
- The station is chosen by lowest machine id. Two purchases on one tick go in host order: the second may be refused
  for `noCredits`, or `bought` if both named the same upgrade step.
- Credits are the crew's; whoever grabs or breaks a delivery receives it. "Each hero buys their own" is a convention
  among friends, and a guest may pick up gear another paid for.
- A player joining mid-session gets deliveries and pieces in the save.

## Tests

- Scenarios: a wanted good fills its share at full worth, its excess and other goods at 1/7; sales before a level
  opens do not count; `research.reputation`; a purchase spawns a piece on the station's row the same tick, held while
  no station stands, never on a planet; a parcel grants its upgrade; a crate drops its gear or grants its blueprint; a
  lost delivery makes its good buyable again; two buys of the same step; migrating a save mid-orders.
- Invariants: credits never negative; the level only grows; after a walk progress is below `target×7`; every purchase
  is exactly one delivery until caught, broken or gone.

## Build order

`Knowledge`, `Research`, `Debris` and `Trade` change, no new module. Each PR playable:

1. Reputation replaces orders: the wanted card, prices ciphered by level, the level migration. Purchases still granted
   at once.
2. Deliveries: parcels and crates with the icon and the edge marker; the looks picked first.
3. Gear becomes items, with its migration. After 2, or gear could not be had.
4. The confiscated container.
5. Contraband, a PR an item.

## Settled with the user

- Upgrades stay crew-wide.
- A delivery bought with nobody aboard still comes, and is lost past the empty ship.
- The lane is the station's own row, crossing the hull: a delivery is caught ahead of the bow or astern, the harpoon
  reeling it in.
- The edge marker ships with deliveries.
