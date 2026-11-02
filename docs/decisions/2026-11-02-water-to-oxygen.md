# Water becomes liquid oxygen

- Status: accepted
- Date: 2026-11-02
- Builds on [liquid oxygen](2026-10-30-liquid-oxygen.md) and [typed ports](2026-10-29-typed-ports.md)

## Context

The user wanted ice melted into water, water poured into a cistern as a canister into a tank, and a station turning it
into oxygen. Liquid oxygen already reads in the pipes: tanks hold it, vaporizers turn it into air.

## Decision

- **A small `Water` module** (decided with the user over a general fluids module): the cisterns' water, pouring, the
  electrolysers. Liquid oxygen stays in Atmosphere's tanks; the electrolyser puts it there through one operation,
  `IAtmosphereOps.Fill`. A general module waits for a third fluid with a named user.
- **The bottle.** Ice smelts into one bottle of water (`water`), at the furnace in 40 ticks. A bottle is used up when poured.
- **The cistern** holds `Water.CisternBottles` (8) bottles, poured in with E (`PourWater`).
- **The electrolyser**, 4×6, three glass columns (variant A, decided with the user): water in on its right, liquid oxygen out
  on its left, draws 5, so one solar panel runs it. It takes water from the cisterns whose output is on its input's network
  and puts liquid into the tanks whose input is on its output's, one unit a tick at full power, a unit of water a unit of
  liquid. The hydrogen is let go.
- **The tank gains an input**, `(3, 2) In liquidOxygen`: an electrolyser's output joins it, where its output port would be
  two outputs (`pipes.bothGive`). A drawn line passing under that cell now ends there, harmless on the maps that do it.
- **The Former's 3 ice → canister stays** (decided with the user): the canister is portable oxygen, the electrolyser the
  base's. The electrolyser gives 4500 a bottle against 3000 an ice the Former's way: a guess until a playtest.
- **Saves:** a module added, format 44, its slice empty on every surface.

## Deliberately not handled

- Cisterns passing water to each other; empty bottles; bottling water again; hydrogen; freezing or boiling.
- Switching an electrolyser off. It shows why it stands by a red lamp and, under the pointer, in words.
