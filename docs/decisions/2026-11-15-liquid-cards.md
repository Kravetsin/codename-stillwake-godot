# A canister's and a bottle's litres are food's rows

- Status: accepted; supersedes the card line of [liquid-volumes](2026-11-09-liquid-volumes.md)
- Date: 2026-11-15

## Context

A canister's and a water bottle's card was the compact one, its litres a plain line, "3 l · ×4 = 12 l", while food beside
them had the medium card of [food-and-gear-cards](2026-11-14-food-and-gear-cards.md). The user asked for the same look.

## Decision

- **A canister and a bottle get food's card** (`ItemCard.Liquid`): the picture in its window, the name and kind, the rows,
  the price under them.
- **Under «Содержимое»**, with the two bottles' sign (`Tank`): «Объём 3 л»; a stack of more than one also «Всего ×4 12 л»,
  its count as the row's small word (`ItemCards.LiquidRows`, `ItemCards.Counted`).
- **Its edge and kind cyan**, `#4f8aa3` and `#68c3e6`, the `Tank` sign's hue: chosen in the change, not on a pick page. The
  user played it and shipped it so.

## Deliberately not handled

- Tanks and cisterns keep their pointer's tip: they are machines, not items, and have no card.
- Biofuel is still counted, not poured: it has no litres and no rows.
