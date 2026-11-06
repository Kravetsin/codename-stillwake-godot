# The trade station comes with circuits

- Status: accepted; supersedes [trade-and-tech](2026-10-30-trade-and-tech.md) and [tech-graph](2026-10-30-tech-graph.md) on the
  trade station being of raw salvage
- Date: 2026-11-06

## Context

The trade station cost 4 scrap chunks and 2 hull plates, the furnace's very price, so the board understood it within the first
minutes, beside the furnace and the workbench. In the playtest of 2026-11-06 players building their first furnace and bench
wondered why a sales terminal was offered: there was nothing yet worth selling, and nothing yet to buy.

## Decision

- **The trade station costs 4 plates, 4 wire and a circuit** (`Machines`). Plates and wire are the workbench's; the circuit is
  found, in cable racks and derelicts, two of them understanding it (`Understanding.Enough`). So it is understood about when
  circuits arrive, the first electric machines' time, and still a modest price beside the auto trade station's.
- **The first credits still need nothing bought.** `ContentTests` walks the furnace and the workbench alone, the market left out,
  to the trade station and its cost; and asserts raw salvage alone no longer reaches it.

## Deliberately not handled

- A world whose trade station is already understood keeps it: known stays known ([the-board](2026-11-05-the-board.md)).
- No first-hour line or hint points at the station; the board's thread from the first circuit is the hint.
