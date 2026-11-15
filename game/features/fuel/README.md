# fuel (client): fuel up the stores' glasses and the pointer's word on them

## Files

| File | Purpose |
| --- | --- |
| `FuelLooks.cs` | The biofuel of each store whose kind has a sight glass (`{kind}_level`: the fuel tank, the crew shuttle), up that glass by any level. |
| `logic/Fuel.En.cs` | Its player text (`En`): the refusals, the draw key, the pointer's tip, how long it lasts. |
| `logic/Fuel.Ru.cs` | Russian |
| `logic/FuelShows.cs` | What the pointer says of a store: its fuel of its capacity and how long it lasts at its draw. |

Fuel as the player sees it ([fuel-liquid](../../../docs/decisions/2026-11-16-fuel-liquid.md)). Reads `sim/Fuel` (`FuelIn`, `Lasts`)
and the grid's machines by trait (`HoldsFuel`); sends nothing itself: the stores' panel (items feature) sends `DrawFuel`.

- **Without a word:** a store with a sight glass in its picture shows its fuel up it, from the bottom to any level, to a hi-res
  pixel (`GlassLevel`, `game/shared`): rust orange, the user's pick. Stores with none (the pod, the generator, the engine) show
  nothing. Read four times a second.
- **By the pointer:** `Biofuel · 12.50/100.00 l · 12 cans` and `Lasts 12 min at this draw`, or `Nothing drawn from it`, through the
  shared `MachineTips`. The panel says the same under its draw key.
