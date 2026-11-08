# The black market is a grid of goods

- Status: accepted; every good shown with its price ciphered, and the order card made the wanted card, by [mafia-reputation](2026-11-08-mafia-reputation.md)
- Date: 2026-11-03
- Supersedes [market-decrypt](2026-11-02-market-decrypt.md): its rows of five cards, its poison accent and the hack's marks

## Context

The market drawn as a row per order held at most five goods a row, and the user has many more goods in mind. Its prices
were bare numbers nobody could read as credits. Upgrades took 20 of its 21 cards, one per slot. The user asked for a
darknet market whose owners care about privacy and convenience rather than looks, still deciphering what opens. On
[a page](https://claude.ai/artifact/U4pmYEn84hozusteEWsGMK) of four, a terminal, an onion storefront, an icon grid and a
messenger bot, the user picked the icon grid, «Плотная сетка».

## Decision

- **Flat tiles, a picture and a price each,** in as many columns and rows as the view holds: 12 by 5 at 1280x720. Category
  tabs over them with their counts; the name and the rest only in the strip and by the pointer.
- **Only the order being filled shows,** on the left: what it asks, what is sold of it, and what it unlocks as garbage.
  Orders met are gone; everything they opened lies in one catalogue, upgrades first, then blueprints newest first.
- **One tile per kind of upgrade:** the inventory slot, the trade station slot and the belt slot. It shows the next price
  and how many are bought of those opened; once those are bought it waits for the next order, and once every one is
  bought it says so. Its button buys the cheapest one opened, a `BuyBlock` as before: the sim is unchanged, and still
  sells any step opened.
- **The game's coin by every price,** as `CreditLine` writes it. One accent, an orange `#d9512f`, on the order being
  filled, the button and what deciphers; the hack's marks go.
- **The decipher stays:** as an order is met, what it opened deciphers into the catalogue, a tile that gains steps only in
  its count, and the next order's head clears. A click picks a tile and clears it; only the button buys.
- What stays of market-decrypt: the solar panel with the fourth order, orders filled only by selling, the decipher's
  clock (`MarketReveal`), garbage (`Cipher`) and orders seen as one count (`BoardSeen.SeeOrders`).

## Deliberately not handled

- **Scrolling:** a test fails once the goods outgrow the grid at 1280x720, which brings it. A smaller window shows fewer.
- **Contraband:** there is none yet, so there is no tab or lot for it.
- **The candidate's flashes** on a sale and a purchase, and its tabs' sort control: the label says how it is sorted.
- **A ladder bought out of order** in an old save, bag3 before bag2: the tile offers bag2, then bag4.
- **Made at and used in** in the strip: the candidate shows what a blueprint is made from, and that is kept.
