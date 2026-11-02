# Goods leave by capsule

- Status: accepted
- Date: 2026-10-30
- Supersedes the selling a unit at a time of [trade-and-tech](2026-10-30-trade-and-tech.md)

## Context

A trade station ate what was put in it, two units a second, with nothing to see or decide. The user wanted selling to be an
event: a capsule going off with the goods and coming back with the money.

## Decision

- **`SendCapsule`** takes everything priced from a station at once, for what it is worth then. The credits come
  `Market.CapsuleFlight` ticks later, thirty seconds: the whole trip, not one way.
- **No limit on what a capsule carries** but the station's four slots. The user's call, knowing a full one carries 1000 units
  where selling moved 60 in the same time: the limit is now what the player can make and bring.
- **One capsule a station.** Slots fill while it is away. Building a second station is the way to send more often.
- **Nothing happens to a capsule.** It cannot be lost, and a station taken apart while one is away still gets its credits, so
  no refusal is needed and no player can lose goods to a misclick.
- **The auto trade station** (`autoTradeStation`, in the Electronics ring) launches by itself whenever it can, so drones can
  run selling alone. It is a machine rather than a switch on the old one: it fits the tree's one thing a node, and costs
  something to reach. The plain station keeps being what the start can afford.
- **It flies behind the hull.** A station stands anywhere, with no hatch or roof rule to refuse a place: the capsule is drawn
  under the back walls, going up and astern and shrinking away from the camera, the same in reverse coming back. Because the
  hull hides it at first, the station's screen flashes and a thud is heard as it goes, then a bar runs down till it is home.
  The flight is the client's alone; the sim only counts ticks.

## Consequences

- Credits come much faster once the player has stock; the five hours to the drop pod need another look in the playtest.
- Selling now needs a press: a player who fills a station and walks away earns nothing. The bar and the key say so, and the
  auto station removes it.
- Format 40: the sales' beat is gone from the slice.
- Not done: sounds of its own; the launch reuses a door's thud, the return the charger's blip.
