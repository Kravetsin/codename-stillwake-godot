# Suits are worn

- Status: accepted; built (the sim, the gauge and the frost; the person under the suit and the looks come next, `docs/plans/suits.md`)
- Date: 2026-10-27
- Supersedes [0028](0028-the-lifepod-and-crafting-by-machine-only.md)'s "heroes are made with a full suit" and, for the
  suit, [0057](0057-stations-one-model-for-every-crafting-machine.md)'s "the pod's list stops growing"

## Context

The suit was the hero itself: `Crew.Suit`, 45 units of air every hero had. It could not come off, so no other suit could
exist, and there was no person inside it. The user wants suits to take off and put on, more of them later (light, heavy,
thermal, a glass "aquarium" helmet), and a person with a face under them, whose look the player picks.

## Decision

- **The suit is an item** (`Content.Suit`, the item `suit`), worn in a sixth equipment slot, `suit`, appended last so the
  older slots keep their indices. On and off is the ordinary `Move`. An item's `Tank` is the air it holds; a later suit is a
  row of `Items`, adding fields when it needs them.
- **One pool of air.** `Hero.Air` (was `Suit`) is what the hero holds; its capacity is the worn suit's `Tank`, or
  `Crew.Lungs` (20) bare. Every danger, the jetpack's thrust and every refill (room, pod, canister) work on that pool as they
  worked on the suit: nothing else had to change. A pool of lungs beside the suit's would have doubled every rule.
- **The cut.** Each tick a conscious hero's air is cut to its capacity: a suit taken off loses what the lungs cannot hold;
  one put on keeps the air, which then fills. No hook runs from Items to Crew.
- **The cold, bare.** Bare where the cell is not aired (space, a vented room, a planet's open air), a breath costs
  `Crew.BareCold` (1) more, the edge's cold added past the field. On thin air it is paid on the beat the shortfall is (0076),
  so thin air stays one rule; the user may revisit it after playing. `ICrewQueries.Cold(peer)` gives the cold for the frost,
  which shows at least `Report.BareFrost` on a bare hero in the cold, before the field's edge.
- **Printing.** Every hero Crew makes (a first `Enter`, one after `Leave`, waking) starts with its air full to capacity. A
  peer's first hero has an empty inventory, so it is printed bare; the suit is `Gear`, so a hero that died suited wakes
  suited. A harder mode losing everything is not built.
- **The basic suit is free at the pod:** a craft costing nothing, 20 ticks (five seconds at the pod's fifth of speed),
  known from the start. It is a convention: the hero has to go and take it.
- **Saves:** format 35. Every peer inventory gains the slot; every hero's, and every heroless peer's aboard, gets the suit,
  since every hero wore one; `suit` became `air`.

## Consequences

- Tests: a scenario's heroes come in suited unless it says `Bare`, so old tests read as before; the bare ones say so.
- The gauge reads air over capacity; a hit on it is more than the dearest breath, since bare lungs are small enough for a
  breath to look like one.

## Deliberately not handled

- **A stored suit holds no air:** swapping in vacuum loses what the lungs cannot hold. Suits are swapped aboard.
- **Running bare by the pod in vacuum** drains a unit a breath (2 + 1 against the pod's 2); standing, it holds a unit short.
- **The cold is paid up to a second late**, as the edge's is: the reported cell (0050).
- **Free suits pile up** if crafted over and over: they are worth nothing.
- **The start room is not aired at once:** a new hero, bare, sees the frost for its first seconds by the pod, which holds
  its air.
