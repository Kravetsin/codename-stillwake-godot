# Plan: Creative mode per player

## Why

Creative mode and instant building are world settings today. `items` keeps one `Creative` flag per surface and
`building` keeps one `Instant` flag; `SetCreative` and `SetInstant` flip them for everyone, although the host already
stamps each command with its sender. In co-op, one developer testing a machine turns the game into creative mode for
the other.

**Decided with the user** (2026-11-07): each player switches creative mode for themself. **When one player turns it on,
the other does not notice.** Every rule that player B meets (what B pays, what B's inventory opens, what B knows, B's
air, what B gets back when B takes something apart, which hazards come) is the same whether player A is creative or not.
B still sees what A does in the world, as B sees any teammate's work: walls going up, machines appearing.

## The rule

- `items` keeps `Creative` as a list of peers, ascending, instead of a flag. `SetCreative(On)` adds or removes `c.From`.
  The query is `Creative(int peer)`; the world-wide `Creative` property is removed, so the compiler finds every reader.
- **A creative peer pays as if it had the items.** `Take`, `TakeStock` and `ShortOf…` for a creative peer return the
  full cost as taken, take nothing from its slots or crates, and never refuse for lack. Whatever records a payment
  (`Pipes`' and `Power`'s `Paid`, a plan's `Delivered`, a craft's entry) records it as paid. So a pipe laid by A and cut by
  B gives B its tubes back, exactly as if A had paid for them. Today creative returns nothing taken, so `Paid` is false and
  B would get nothing back: B would notice.
- **Shared blueprints.** `LackOf` and `Owes` lose their creative clause and become world truths. A creative worker pays
  the part it begins from nothing: `PaymentOf` picks the first item the plan still lacks, whatever its stock holds, and
  adds it to `Delivered`. B working the same plan after A owes only the parts after A's. The invariant
  `Delivered <= cost`, `Progress <= BuildTicks * paid / parts` then holds always: the creative exception in
  `sim/Building/README.md` goes.
- **Research.** `Knows(id)`, `Knows(placement)` and `Done(node)` become world truths with no creative clause. Peer-aware
  overloads `Knows(int peer, …)` and `Done(int peer, node)` add `Creative(peer)`. Command checks (`PlacePlan`, `Craft`)
  use the sender's. This fixes a leak: `Hazards/Flares.cs` arms magnetic flares from `Knows(MagneticFlares.ArmedBy)`,
  so today creative mode arms them for everyone.
- **Slots.** `Own` becomes `Own(peer)`: a creative peer's inventory opens every slot, the others' as their upgrades say. A
  trade station's slots (`InputsOf`) belong to the machine and count upgrades only: today creative opens them for
  everyone.
- **Air** (`Crew.cs`): a creative peer's hero keeps full air and takes no room air, as today, per hero.
- **Death:** `LeaveBody(peer)` leaves nothing for a creative peer only.
- **Spawn:** refused `items.notCreative` unless the sender is creative.
- **Instant building** becomes per peer too, by the same reasoning: `building` keeps `Instant` as a list of peers;
  `SetInstant(On)` sets it for `c.From`; a blueprint placed by an instant peer is built at once, free, and
  `building.heroInside` is checked for it. B's blueprints wait for work as before.
- **Surfaces:** `Inherit` copies both lists to a new surface.
- **Client:** every reader asks for the local peer: `DevPanel` (both buttons, the item picker's note), `InventoryPanel`'s
  creative label (`Reading.Creative`), `FabricatorPanel`, `Crafts`, `Tools.Known`, `StripParts`, `MarketPage`, the
  building ghost (`Building.Problem(cell, placement)` gains the peer). Nothing shows that another player is creative.

## Steps, one PR each, in order

### C1: creative mode per player: done

What stays true is in [creative-per-player](../decisions/2026-11-07-creative-per-player.md) and the module READMEs.

### C2: instant building per player
- `sim/Building`: `State.Instant` → `List<int>`, ascending; `Instant(int peer)`; `SetInstant` per sender; the instant
  path in `PlacePlan` and `building.heroInside` read the placer. `Inherit` copies it.
- **Save:** its format step: `building.instant: true` → the ids of every peer in `items.inventories`.
- `DevPanel`: the button reads the local peer.
- **Tests:** A instant, B not: A's blueprint is built at once, B's waits for work; `building.heroInside` only for A's
  wall; save, load, new surface. The C1 invariant test gains `SetInstant(true)` from A.
- **Docs:** the ADR from C1 gains instant building; `building` and `dev-panel` READMEs.

## Deliberate omissions

- **Items A makes from nothing reach B.** A can spawn a stack and drop it, or build and let B take apart, and B gets
  real items. That is A acting in the world, as any teammate who shares; creative mode is a developer's tool among
  friends (principle 6).
- **A's refunds make items.** A cancels a craft or cuts a pipe and gets back what it "paid" from nothing. Harmless: A can
  spawn anything anyway.
- **A creative hero takes no room air,** so a room shared with A holds a little more for B. It is the same as A not
  breathing; B cannot tell it from a hero standing in a suit.
- **No permission check.** Any peer can switch itself into creative mode; the host does not decide (co-op among friends,
  principle 6).
- **No indicator of another player's creative mode.** By the requirement.
- **The testbed** makes only the peer that loads it creative; a guest joining it switches creative mode on from the dev
  panel.
