# The black market as Decrypt

The look was picked on a page of candidates: [market-decrypt](../decisions/2026-11-02-market-decrypt.md).

## Problem

The market is a ring map of 20 goods round a hub, half-empty and crude. It should read as the tablet's own interface
hacked, and work exactly as now.

## Player experience

1. The market tab shows one row per group: the order on the left, what it opens as cards on the right. Everything
   unopened is garbage.
2. Poison (`#ff2d78`) marks what can be bought and the order being filled. A click picks; only the strip's button buys.
3. A sale at the station fills the order with the tablet away. The key and tab lamps light, as today.
4. When the market next shows, after 0.2 s (the tablet's rise), the next group deciphers character by character, cards
   in turn, in about 1.6 s.

Going wrong: clicking a sealed card picks nothing, and the pointer says "Not open yet".

| State      | Card                         | Strip's button               |
| ---------- | ---------------------------- | ---------------------------- |
| Not open   | garbage name, price, picture | none, not pickable           |
| Too dear   | dim picture, grey price      | disabled, `research.noCredits` |
| Affordable | poison edge and price        | poison, buys                 |
| Picked     | white edge over its state    | as its state                 |
| Bought     | dim, green `research.bought` | disabled                     |

The strip keeps name, words, and takes/made at/used in where they exist (the solar panel), in slate.

## Out of scope

- Sim, prices, orders, commands.
- The tablet's head, tabs and other pages.
- Contraband cards.
- Paper, objects or wash on the screen.

## Scale

- World: 6 rows, 20 cards.
- Players: each machine draws its own.
- Frequency: 4 reads a second; five deciphers a world today.

## Approach

No sim change: `Done`, `Reached`, `BuyProblem`, `SoldOf` and `BuyBlock` cover it. `MarketPage` is a new `ITabletPage`
and replaces `RingsPage`, which since the tree went serves only the market: a hub map shares nothing with rows. Content's
`TechRing` stays, as the data of a row.

Rows are 64 tall, the one being filled 122, gap 6; what is spare is shared, so six fill the 472 view with or without an
active row. Cards share a row's width and trim long names. A Content test holds the page to its size: at most 6 rows, at
most 5 goods in one. A seventh order fails that test and brings scrolling with it, as nothing needs it today.

One Control per row head and per card, built once and restyled, as `BoardPage` builds its notes: mouse filter and `Tips`
come free. Each draws itself, for per-character colour and the cursor cell, and repaints only when its text changes. The
strip reuses `StripParts`. Faults are a few rects and slipped slices placed by hand in an overlay through
`Tablet.OnScreen`; they are part of the look that was picked.

Font: `Style.Mono` (system Consolas), already the tablet's tabs. No font ships today and none is added. Cell width is
measured, text goes through `T()`, prices are plain digits like `CreditLine`.

## Determinism and co-op

Client only; buying stays `BuyBlock`. `logic/Cipher` is a pure own hash of place id, character index and step, never
`GetHashCode`, and it keeps blanks, so a read repaints identically. Garbage stands still at rest; it moves only while a
row deciphers, on a reveal clock in `Step` that never touches the sim. `Read` (timer) says what is true, behind a change
key. A picture stands as a block of garbage cells until its card is done, then shows.

Remembered: `OrdersSeen` in `BoardSeen`, per machine and world, already kept. Orders open strictly in turn, so a count
names the rows. Every read on screen compares it with the orders met, so an order filled while the market is open plays
at once (auto stations and guests sell with the tablet out). One member replaces `MarketChosen`, so lamp and decipher
agree and the key's lag replays nothing. Rows opened together start together.

Garbage is cosmetic: a card the sim says is reached is pickable and buyable mid-decipher, and picking it clears it.

A late guest or lost file starts as things stand, and nothing plays. A guest present earlier plays it at their own
opening; nothing is sent.

## Trade-offs accepted

- We do not resume a decipher after closing or switching tab: rows show clear, because seeing it once is enough.
- We do not handle combining or CJK text in the character grid: only English exists.
- We do not ship a font for machines without Consolas: they fall back to their monospace, and the release is Windows.
- We do not rescale faults: at other window sizes positions scale, sizes do not.
- We do not scroll: a window under about 720 high clips the last rows, and no seventh order exists.
- We do not build repeat purchases: contraband does not exist yet.

## Tests

- Unit: `BoardSeen`'s count; `Cipher`'s wrap; sim answer to card state; the Content size test. No scenarios: no sim rule
  changes.
- Invariants (FsCheck, `game.tests`): `Cipher` keeps length and blanks, same place same garbage. `MarketReveal` ends all
  clear and plays nothing when seen equals met.
- Recorded: fill, reopen, decipher; an order filled with the market open; all orders done. Read by frame.

## Cost

No sim module, no content. New: `MarketPage`, `Cipher`, `MarketReveal`, the faults. Gone: `RingsPage`, `logic/TechRings`
with its tests (the rest of `TechRingsTests` tests `TechLinks` and is renamed), `ResearchTablet.HubAt`, the `Tabs` loop,
`TreeDemo`'s drag, zoom and hub steps, and what only rings read (`TechTab.Branches`, `TechNode.Branch`,
`research.branch.*`). `TechCamera`, `TechLinks` and `CapsuleFlights` stay. The README, map and glossary change. An ADR
supersedes `2026-10-30-tech-rings`. New i18n keys. Bag, belt and trade-slot pictures exported from the candidate's
`kit.js` through `tools/`, replacing `AsciiArt.Upgrade`'s pixels. About +500/−550.

## Decisions taken by default

- Poison also marks the decipher's cursor and the faults, as drawn; the buy and credit flashes stay slate.
- Consolas, judged on a frame; JetBrains Mono (OFL), as the candidate was drawn, would then cover the whole tablet.
- The candidate's "n/20 open" and thousands commas are dropped: the head and `CreditLine` are shared.
- With nothing picked the strip keeps `research.pickHint`.
