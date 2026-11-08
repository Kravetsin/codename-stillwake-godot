# The black market opens by the mafia's reputation

- Status: accepted
- Date: 2026-11-08
- Amends [the-board](2026-11-05-the-board.md): the market's later rings open by reputation, not by orders met
- Amends [market-grid](2026-11-03-market-grid.md): every good is shown, only its price ciphered; the order card becomes the wanted card

## Context

Players saw weak first goods, filled the first order, saw only more bags appear as garbage, and gave the market up. What an
order opened was hidden, so it read as a tax, not a goal; orders counted sales from the world's start without saying so.
The user asked for a market that matters as much as research. The whole rework is designed in
[the black market plan](../plans/black-market.md); this is its first step.

## Decision

- **Every good is shown from the start,** picture, name and description. A price past the level is garbage (`Cipher`), and
  buying it is refused with `research.reputation`. As a level opens, its prices decipher on the old clock (`MarketReveal`).
- **Reputation:** a level wants a gate's `Order`. A wanted good counts at its full worth up to its count; its excess, and
  anything else sold, at a seventh (`Content.Research.OffWanted`). Only what is sold after a level opened counts toward the
  next, so one capsule opens one level and reputation past a target is dropped. The user's idea over a cap per kind, which
  a pile of one good would still have farmed.
- **Records in Knowledge, rules in Research:** Knowledge keeps `SoldWorth`, the level and its base; Research raises the level
  each tick, and Knowledge keeps an `Open` only for the next level, as each surface's research calls it.
- **The levels:** the scanner moves to the first, its headline; the second and fourth ask a third good, the fifth more.
  Targets run 1 580, 2 780, 4 930, 4 670 and 3 520 credits' worth; by other goods alone, seven times that.
- **The card** says "The mafia is interested in", a row per wanted good and a bar of the level; what a level opens leaves it,
  for the grid shows it. Locked blueprints sort after the open ones, nearest level first.
- **Saves** of format 56 get the old orders met in turn as their level, counted on from what is sold then; progress toward
  the next is lost, the game being pre-release.

## Deliberately not handled

- A later level's goods sold early count at a seventh: an auto station sells everything priced, and the card shows only the
  next level. A tile may hint at it if a playtest shows it hurting.
- Deliveries, gear as items and the confiscated container: later steps of the plan.
- Levels past the fifth.
