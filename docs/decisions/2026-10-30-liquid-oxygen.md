# Air comes from liquid oxygen

- Status: accepted
- Date: 2026-10-30
- Supersedes, in part, [0016](0016-breathe-where-the-air-is.md): the generator no longer makes air from power alone
- Builds on [typed ports](2026-10-29-typed-ports.md): two oxygen fluids in the pipes

## Context

A powered generator made air from nothing, so oxygen was never something to find, and the wreck's one panel held its
room for ever. The user wants air to come from canisters, salvaged or frozen from ice, and the chain to read in the
pipes.

## Decision

- **Two fluids.** A tank (`MachineTrait.Gas`) holds liquid oxygen (`Fluids.LiquidOxygen`); its port gives it,
  `(0, 2) Out liquidOxygen`. Canisters are poured in as before (`LoadCanister`). Gas is the fluid `air`.
- **The vaporizer** is the old generator, its kind id kept (`generator`) to spare a migration of every slice naming
  it; the player calls it "Oxygen vaporizer". It takes liquid in at `(0, 2)` and gives air out at `(3, 0)`, draws 4,
  and is the only `AirSource`. While powered, with room in its store (`Air.GeneratorStore`) and liquid in the tanks
  on its input's network, it makes up to 1 unit a tick at full power, a unit of liquid becoming a unit of gas, taken
  from those tanks in id order; else its work stands, so nothing is made from nothing and nothing lost.
- **Vents draw only from the stores of the vaporizers feeding their network** (output port on it). A tank piped
  straight to a vent airs nothing, and typed ports refuse that pipe (`pipes.mixedFluid`). A vent is fed by a
  vaporizer's output, supplied while a store feeding it holds gas or a vaporizer feeding it is powered with liquid
  behind, so a room does not flicker to vacuum while the store refills.
- **The tick** gathers the survey, lets the vaporizers make their share, reads tanks and stores again, then draws.
- **Throughput.** One vaporizer fills a room a cell a tick, a quarter of the old speed, and a breach drains a
  canister in about 9000 ticks instead of about 1125: liquid leaves at most at the vaporizers' rate. The air overlay's
  `Lasts` shows that; its `Oxygen` is the liquid behind the vaporizers feeding the room.
- **A room in vacuum says why**: no vent, not piped to a vaporizer, the tank behind it dry, or no power.
- **Saves** keep their bytes with a new meaning (format 39): a tank's oxygen is liquid, a generator's store and work its
  gas and progress. Old ships piped tank to vent lose their supply and the player re-pipes; nothing was released.
- **The wreck**, until its crate comes: a tank in the torn room B, one canister's worth in it, piped to the
  vaporizer, which is piped to the vent.

Rejected: liquid and gas kept only inside machines, with one pipe fluid (the chain would not read in the pipes); a new
kind id for the vaporizer (a migration of every slice for a name).

## Deliberately not handled

- **Boil-off, temperature, pressure:** liquid keeps for ever in a tank.
- **Liquid oxygen as an item:** a canister is the only way it moves by hand.
- **Switching a vaporizer off:** it runs while powered with liquid behind and room in its store.
- **Old saves' tank–vent spans** are not re-piped by a migration: they show `notPiped`.
- **No warning before a tank runs dry on a slow leak:** `Lasts` and `dry` show in the air overlay.
