# Design: Water (ice → bottle → cistern → electrolyser → liquid oxygen)

Status: first pass built on `feat/water` (2026-11-02), what stays true in ADR
[water-to-oxygen](../decisions/2026-11-02-water-to-oxygen.md) and `sim/Water/README.md`. The ink pictures are drawn in
`tools/anime-art` (`machines.js`, `items.js`). Nothing is left. The electrolyser draws 5 (one panel), not a stated 6.
The ports below were moved after play: every machine takes on its left and gives on its right
([ports-left-to-right](../decisions/2026-11-02-ports-left-to-right.md)).

Builds on typed ports (ADR `2026-10-29-typed-ports`, in `main`: ports In/Out of one fluid; the cistern `cistern` 4×4,
`(0, 2) In water`, `(3, 2) Out water`, holding nothing yet) and on the liquid-oxygen plan (`docs/plans/liquid-oxygen.md`,
decided with the user 2026-10-29): tanks hold `liquidOxygen`, the generator becomes the "oxygen vaporizer", and the
electrolyser gives `Out liquidOxygen` into tanks.

**Decided with the user** (2026-10-29/30):
- Ice melts into water; water is a glass bottle with a cap and a drop; bottles are poured into the cistern as a canister
  is into a tank; the electrolyser is variant A, three glass columns, water in by pipe, liquid oxygen out to tanks.
- **A small `Water` module**, not a general `Fluids` one: the cistern's store, pouring bottles and the electrolyser.
  Liquid oxygen stays in Atmosphere's tanks; the electrolyser adds to them through one Atmosphere operation. A general
  module is extracted when a third fluid has a named user.
- **Order:** L-1 (and L-2, independent), then water in one PR.
- **The Former's 3 ice → canister stayed** (removed since, [no-ice-canister](../decisions/2026-11-06-no-ice-canister.md)): the canister is portable oxygen (jetpack, suits), the electrolyser the
  base's automated path.
- **The electrolyser is 4×6**, so its three columns read tall.

## Rules as the player meets them

1. **Melting.** Ice gets `Yields: [("water", 1)], SmeltTicks: 40, Heat: 1`: it melts at the furnace in 40 ticks and the
   electric smelter in 20 through the existing smelt path; nothing new in Crafting or Furnace.
2. **The bottle.** Item `water`, a glass bottle with a cap and a drop; used up when poured, as a canister is (an empty
   bottle would need its own item and glass to make it). Its stack is set beside ice's with a stated reason.
3. **Pouring.** E on a cistern holding a bottle pours it: `PourWater(Machine)`, taking one bottle from the sender (free
   in creative) and adding `Water.Bottle`. Refused, state untouched: `water.noCistern` (not a cistern; on the
   electrolyser its text says to pipe water in from a cistern), `water.full`, `items.notEnough`. A canister aimed at a
   cistern, or a bottle at a tank, is refused the same way, so the prompt picks by held item. Two heroes pouring on one
   tick apply in stamped order; the second may be `water.full`.
4. **The cistern** holds up to `Water.CisternCapacity`, a whole number of bottles stated as `n × Bottle`. Its sight
   glass shows the level. Its line stays teal and still (typed-ports ADR).
5. **The electrolyser**, 4×6, Air tab, `In water` and `Out liquidOxygen` at cells chosen with the art, `Draws` a stated
   number. At its turn, in machine id order, `Making` grows by `Water.ElectrolyserRate` × its power satisfaction
   (thousandths), only if a cistern on its input's network holds water and a tank on its output's network has room;
   each whole unit finished takes 1 water from those cisterns (lowest id first) and adds 1 liquid oxygen to those tanks
   (lowest id first, through an Atmosphere op); units made `min(finished, water, room)`, rounded down, the rest kept in
   `Making`. Else `Making` stands: nothing from nothing. Its own constants, not air's: 1 unit a tick at full power.
   **The tank gains `(3, 2) In liquidOxygen`** (without it, electrolyser Out → tank Out is `pipes.bothGive`).
   Stalled, it shows why — no water, tanks full, no power — in its handbook line or a tint; bubbles only while making.
   Hydrogen is art: bubbles from the right column.

Numbers (`sim/Content/Water.cs`): `Bottle` a placeholder (half a canister, 4500) retuned after a playtest; the cistern a
whole number of bottles; the rate 1 unit a tick. The electrolyser giving more per ice than the Former is a guess, not a
balance claim.

## Saves, co-op, determinism

`WaterState`: the cisterns' amounts and the electrolysers' `Making`, by machine id. A module added: a format bump
(the next free number), empty step. Pouring is a command the host stamps; nothing new for co-op. Integers only; one
division, rounded down, remainder kept; machines walked in id order; networks read from Pipes each tick (no survey
cache until `BudgetTests` asks for one).

## Deliberately not handled

- **Cistern to cistern passing** (deferred): nothing asks for chained cisterns yet; the cistern's In port stays unused.
- **The electrolyser's own store and `Piping.Flow`**: it pushes straight into tanks.
- **A general `Holds` / `Pours` / `Converts` data model**: traits and constants until a third fluid.
- **Empty bottles; water outside machines; hydrogen; temperature; bottling water back out; drones pouring** — none has
  a player need yet.
- **Taking a cistern apart loses its water** (up to the capacity), as a tank loses its liquid in L-1: one rule for stores.
- **Cutting a pipe mid-work** is safe: nothing is in flight; units move whole within a tick.
- **The tank's new In port** sits on L-1's liquid line in the wreck and `BudgetTests`' tile; harmless (no passing),
  checked by `SeatProblem` and the generic suites when the plan is written.
- **Switching the electrolyser off**: cut its cable, as the vaporizer.
