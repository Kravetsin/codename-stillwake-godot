# pipes (client): the pipes overlay

P brings every pipe network out of the dimmed ship in a colour of its own, ported from
`apps/game/src/features/pipes/` (G-08). Reads `sim/Pipes` (`Networks`), `IGridQueries`
(`ConduitLayer`, `ChangedSince`, `Built`, `Machines`, `PortsOf`), `Atmosphere.OxygenIn` and `Power.Satisfaction`.

| File              | Holds                                                                  | Godot |
| ----------------- | ---------------------------------------------------------------------- | ----- |
| `Flow.cs`         | steps from the sources, runs and dots of oxygen, the palette (`flow.ts`, `oxygen.ts`) | no    |
| `PipesOverlay.cs` | the overlay: dim, network colours (faded when dry), valves, sockets, dots | yes   |
| `Valves.cs`       | the cells holding a valve, kept up with the grid's log                | no    |

- Oxygen runs from tanks holding some and powered generators; a network none feeds is faded and still.
- Read every 250 ms once the world ticked; the dots move every frame. `flow.test.ts` is ported.
- Valves are kept up with the grid's log (`Valves`, 0051): only the cells changed since the last read are
  looked at. A grid not seen before, or one whose log no longer reaches back, is looked at within its
  `Built` only, where every conduit is: a big world of space costs nothing. `BuiltBoundsTests` holds
  them to a whole look.
- Out of the overlay the grid draws the pipes (`features/grid`).
