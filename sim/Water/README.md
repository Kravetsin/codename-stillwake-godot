# Water

## Files

| File | Purpose |
| --- | --- |
| `Fuzz.cs` | The module's `Fuzz` override: the commands the desync suite draws. |
| `Water.cs` | Water: what cisterns hold, poured in by the bottle, and the electrolysers that split it into liquid oxygen. |

Water ([water](../../docs/decisions/2026-11-02-water-to-oxygen.md)): ice melted into a bottle, poured into a cistern, piped to an
electrolyser, which splits it into the liquid oxygen of the tanks on its output. The numbers are `Content/Water.cs`.

- **Depends on:** `Grid` (`MachinesWith`, `MachineById`, `PortsOf`), `Items` (`ShortOf`, `Take`), `Pipes` (`NetworkAt`), `Power`
  (`Satisfaction`), `Atmosphere` (`OxygenIn`, `Fill`). Run and saved after Atmosphere, last.
- **State** (`WaterState`, all saved): `Cisterns` — the water in each cistern holding any, by machine id; `Making` — each
  electrolyser's work toward its next unit, by machine id.

## Rules

- **Pouring.** `PourWater(Cistern)` takes a bottle (`Water.Item`) from the sender (free in creative) and adds `Water.Bottle`.
  Refused, nothing changed: `water.noCistern` (not a machine with `MachineTrait.HoldsWater`), `water.full` (it would spill
  over `Water.CisternCapacity`), `items.notEnough`. Two pours on one tick apply in stamped order.
- **Cisterns in a line** (tanks-in-a-line), first in the simulation phase: each cistern holding water, in id order, pours up
  to `Water.CisternFlow` a tick into the cisterns whose input is on its output's network, in id order, as each has room; a
  later one pours on in the same tick, so a line fills from its last cistern.
- **Electrolysis**, then, each `MachineTrait.Electrolyses` machine in id order: its cisterns are those whose
  output port is on its input's network, its tanks those whose input port is on its output's. While they hold water and
  have room, its work grows by `Water.ElectrolyserRate` × its power's satisfaction (thousandths); each `Air.LeakEvery × 1000`
  takes a unit of water from the cisterns holding any, in id order, and puts a unit of liquid into the tanks in id order
  (`IAtmosphereOps.Fill`). Units made are `min(finished, water, room)`, rounded down, the rest kept; else the work stands.
  At full power it makes one a tick. What it puts in is breathed from next tick: Atmosphere has already run.
- **A cistern taken apart** takes its water with it, in the construction phase.
- **Melting** is content: ice smelts into a bottle of water (`Items.cs`), at the furnace in 40 ticks.
- **A planet stage:** ice and copper come only of planets, and the electrolyser's electrodes are copper
  (industrial-oxygen), so it is understood and built there first.

## API

- **Queries** (`IWaterQueries`): `WaterIn(cistern)`; `PourProblem(machine, peer)` — why that `PourWater` would be refused;
  `Stalled(electrolyser)` — why it makes nothing: `water.noWater`, `water.tanksFull`, `water.noPower`; null while it works.
- **Commands:** `PourWater(Cistern)`.
- **Fuzz:** mostly a bottle into a cistern the grid has, else into ids 0–5, mostly no cistern.

## Invariants

- A cistern holds between nothing and `Water.CisternCapacity`; only those holding any are listed.
- Water and liquid oxygen together never grow by electrolysis: a unit of water becomes a unit of liquid. Pouring only moves it.

## Deliberately not handled

- No empty bottle comes back; water is never bottled again from a cistern.
- Hydrogen is let go, neither stored nor used. Nothing freezes, boils or spills.
- An electrolyser cannot be switched off; with no water it stalls, drawing its power.
