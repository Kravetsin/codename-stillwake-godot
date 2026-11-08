# Plan: PR 1, reputation replaces orders

The first PR of [black-market](black-market.md); where the two differ, this plan wins. Every good on the market is
shown, its price ciphered until the mafia's reputation reaches its level; levels rise by selling, wanted goods at full
worth, anything else at 1/7. Purchases are still granted at once (deliveries are PR 2).

## Rules (settled with the user)

- Every good of every level is shown from the start: picture, name, description. A price past the level is garbage,
  and buying it is refused as "not enough reputation".
- A level asks for a few wanted goods, each a count. A wanted good counts at its full worth up to its count; past it,
  and every other good sold, at a seventh.
- Only what is sold after a level opened counts toward the next. Reputation past a target is dropped.
- Unchanged: credits, prices, upgrades crew-wide, the grid's look, the decipher's clock, buying granting at once.

## Terms

- **Level** `L`: `Knowledge.Level`, 0 for a new world. The market tab's ring `r` is open when `r <= L`; ring 0 always.
- **Gate**: the `TechNode` heading ring `r > 0` (`order1` … `order5`); its `Order` of `(item, count)` is what level `r`
  wants. **Next** is the gate of ring `L + 1`, null once `L` is the last ring.
- `Done(gate)` is `gate.Ring <= L`. `Reached(node)` keeps its code (every `Need` done): a block's need is its ring's
  gate, so a block is reached when `block.Ring <= L`, and a gate when its ring is `L + 1` or less.
- **Base**: what was sold when level `L` opened, the start Next is counted from: the `SoldOf` of each of Next's wanted
  goods (`Base`) and the `SoldWorth` (`BaseWorth`).
- `Tally(Item, Count)` is Knowledge's existing record (`Knowledge.cs`).

## Steps

### Content

1. `sim/Content/Research.cs`, the orders in full: order1 scrapChunk 40, iron 20, planks 20 (unchanged); order2 plate
   30, gear 20, wire 30; order3 glass 20, silicon 20, frame 10 (unchanged); order4 circuit 15, coil 10, tube 30; order5
   copper 40, mica 20, hide 15. The scanner moves from order2's ring to order1's. `public const int OffWanted = 7`, with
   its summary: what anything not wanted is divided by.
2. Content tests: every ordered item has a `Market.PriceOf`; every order's target is above 0; the gates' rings run
   1, 2, 3 … with none missing.

### Knowledge (records only)

3. `KnowledgeState` gains saved `long SoldWorth` and `Reputation { int Level; List<Tally> Base (sorted ordinally);
   long BaseWorth }`, JSON `soldWorth` and `reputation.{level, base, baseWorth}`; a new world 0, empty, 0. `long`, as
   a late world's sales in sevenths can pass an `int`.
4. `IKnowledgeOps.Sell(item, count, worth)` also adds `worth` to `SoldWorth`. `IKnowledgeOps.Open(level, base,
   baseWorth)` is kept only when `level == Level + 1`, so every surface's research may call it on one tick and the
   world rises once; it bumps `Version`, so the client notices. `Sell` does not.
5. `IKnowledgeQueries`: `SoldWorth`, `Level`, `BaseOf(item)` (0 for an item not in `Base`), `BaseWorth`.

### Trade

6. `Trade.Launch` passes `held.Count × Market.WorthOf(held.Item)`: what the capsule earns for it. Only priced items
   are sold, so the worth is never 0.

### Research (the rules)

7. `Done(gate)` and `Reached` as in Terms; a gate's order is no longer read by `Done`.
8. Query `Standing()` returns `Standing(TechNode? Next, IReadOnlyList<Want> Wanted, long Points, long Target)`,
   `Want(Item, Sold, Count)`, `Sold = SoldOf − BaseOf`. In sevenths, with `w` an item's `Market.WorthOf`:
   - `Target = Σ count × w × 7` over Next's order;
   - `Points = Σ (min(Sold, count) × w × 7 + max(Sold − count, 0) × w)` over Next's order, plus
     `max(0, SoldWorth − BaseWorth − Σ Sold × w)`: everything else, never below 0, as a later build may change a worth.
   - Past the last level: `Next` null, `Wanted` empty, `Points` and `Target` 0.
9. `Walk`, in the derivation phase, every tick, before its check of `_knowledgeSeen` and `_gridSeen`: if `Next` is set
   and `Points >= Target`, `Open(L + 1, base, SoldWorth)` where `base` is the `SoldOf` of each wanted good of the gate
   of ring `L + 2`, or empty when there is none. The base starts the new level at 0, so one capsule opens one level.
10. `BuyProblem`: a block not reached is `research.reputation`; a gate or no such node stays `research.locked`.

### Callers of what changed

11. A test helper `Reach(world, level)` that sells each level's order in turn with its worth and runs a tick between,
    since sales before a level opens no longer count. It replaces the direct `Sell` calls in `MarketOrdersTests`,
    `ResearchTests` (75, 91, 104, 277, 338: 91 now reaches the scanner at level 1), `PowerDriverTests` (55),
    `CountsTests` (353–393, which pass a worth), `UpgradesTests` (115, through `Done`) and `game/dev/TreeDemo.cs`
    (58–77, its order texts).

### Save

12. `World.SaveFormat` 56 → 57; its step in `sim/Registry/Migrations.cs` writes only the root `slices.knowledge` (the
    world's, not a surface's):
    - the old five orders and the new ones frozen in the step, never read from `Content`;
    - `reputation.level`: the old orders met in sequence, each by `sold`, stopping at the first unmet (the old rule
      reached a ring only through the one before);
    - `reputation.base`: the `sold` of the new order after that level, empty past the last; `reputation.baseWorth` and
      `soldWorth` 0. Both sums start at the migration, so "everything else" never goes negative;
    - a save with no `sold` (nothing sold, not written) reads as empty.
    Progress toward the next level is lost: the game is pre-release.

### Client

13. `MarketPage`: the order card becomes the wanted card: "The mafia is interested in", a row per wanted good
    (picture, name, `Sold/Count`, a bar), a bar for the level from `Points / Target`. What the order opens leaves the
    card: the grid shows it. Past the last level the card says every level is open. The page's redraw key
    (`MarketPage.cs` 148–150) takes `Level` and `Points`, so selling only scrap moves the bar.
14. Tiles: every good of every ring in the catalogue (`MarketGoods`), the shelves' order unchanged; a tile past the
    level shows its picture and name clear and its price as garbage, the strip's button `T("research.reputation")`. An
    upgrade tile whose next step is past the level (`MarketTile.Waiting`) says it waits for reputation.
15. `MarketReveal`: a level opened deciphers its tiles' prices and the card's head, on the same clock.
    `BoardSeen.SeeOrders` and `TabletKey`'s lamp count `Level`. Levels already met when the page is first seen (a
    migrated save, a guest joining) show clear, without a decipher.
16. i18n (`Research.En.cs`, `Research.Ru.cs`): `research.reputation` "Not enough reputation" / "Недостаточно
    репутации"; `research.market.wanted` "The mafia is interested in" / "Мафию интересует"; `research.market.waits`
    and `nextOrder` reworded to reputation; `order`, `unlocks`, `allFilled` and `filled` reworded or removed as the
    card uses them. `research.locked` stays for gates.

## Deliberately not handled

- Selling a later level's goods early wastes them past a seventh: an auto station sells everything priced. The card
  shows only the next level. If a playtest shows it hurting, a tile may hint at it.
- A level's progress lost in the migration.

## Tests

- `ReputationTests` (scenario), porting `MarketOrdersTests` one for one where the rule survives:
  - a wanted good fills its share at full worth; its excess at 1/7; an unwanted good at 1/7;
  - the level rises at its target exactly, not one seventh short;
  - sales before a level opened do not count toward the next;
  - one huge capsule opens one level, the next only by selling again;
  - "everything else" stays at 0 when a worth is lowered under what was sold;
  - `research.reputation` for a block past the level; `research.locked` for a gate and an unknown id;
  - a world with a planet surface opens a level once;
  - sold part-way to a level, saved, loaded and played on: the same hash as a world that kept running.
- `MigrationsTests`: format-56 saves, migrated and loaded with `World.Load`: one meeting the old first two orders loads
  at level 2 with level 3's base; one with no `sold` at level 0; one part-way to a level at that level, from 0.
- Invariant (`[Property]`): over random sales the level only grows and, after a walk, `Points < Target`; shown to bite
  by letting `Open` take any level once.
- `game.tests`: the wanted card's rows and bar from a `Standing`, in `logic/`.

## Docs

- ADR `docs/decisions/2026-11-08-mafia-reputation.md`: reputation replaces orders, prices ciphered rather than goods;
  amends `2026-11-05-the-board` and `2026-11-03-market-grid`. `tools/adr-index.ps1`.
- READMEs: `sim/Research` (Rules with the points formula; Refusals; API; `Open` called once a surface a tick and kept
  once), `sim/Knowledge` (its records), `sim/Trade` (`Sell` carries worth), `game/features/research` (Files).
  `docs/concept.md`'s market line; `docs/glossary.md`: reputation / репутация.

## Seen in the game

A dev room for the user: a trade station, credits, a stack of each level-1 and level-2 wanted good and some scrap.
Sequences: open the market at level 0 (every tile, prices garbage past it) → sell level 1's goods → the scanner's
price deciphers → sell scrap only and watch the bar move at 1/7 → buy the scanner.

## Done when

`tools/test.ps1` green, `dotnet format Stillwake.sln --verify-no-changes` clean, docs guard green, the user has played
the room. PR to `main` from `feat/market-reputation`, carrying this plan and the design.
