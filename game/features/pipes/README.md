# pipes (client): spans, piping and the pipes overlay

## Files

| File | Purpose |
| --- | --- |
| `logic/Flow.cs` | Oxygen as the pipes overlay shows it flowing, along spans. |
| `logic/PipeHands.cs` | The hands at work on pipes, the plan piping (0109): a left drag lays a span from the cell pressed to the cell let … |
| `PipesOverlay.cs` | Every pipe network brought out of the ship while the plan is piping: each network's spans over the ship, slim, in a … |
| `PipesView.cs` | The pipes as they are built (0109): each span a straight pipe from the middle of one end's cell to the other's, at … |
| `logic/Pipes.En.cs` | Its player text (`En`) |

The pipes as built, the piping's hands and the overlay that shows every network while the plan is piping (0109), ported from
`apps/game/src/features/pipes/` (G-08) and made spans. Reads `sim/Pipes` (`Spans`, `Valves`, `Networks`, `NetworkAt`,
`LayProblem`), `IGridQueries` (`Machines`, `PortsOf`, `MachineAt`), `Atmosphere.OxygenIn` and `Power.Satisfaction`; sends
`LayPipe`, `CutPipe` and `MakeValve` (a valve's E is the crew's `SetValve`).

| File              | Holds                                                                                          | Godot |
| ----------------- | ---------------------------------------------------------------------------------------------- | ----- |
| `PipesView.cs`    | every span one continuous straight tube at any angle, 4 px thick, coloured a row at a time from the pipe art's run (0110), no per-cell seams; a flange only at joints, a valve's wheel open or shut, heard turning | yes |
| `PipeHands.cs`    | piping by its menu's card (0110): with the pipe a left drag lays and a right press takes the nearest span away, that span lit whole while hovered; with the valve the left makes a valve on a joint, the right takes it off (tested) | no |
| `Flow.cs`         | oxygen along spans from the sources, its runs and dots, the palette (`flow.ts`, `oxygen.ts`; tested) | no |
| `PipesOverlay.cs` | the overlay while the plan is piping: networks in colours of their own (faded when dry), valves, sockets, dots; the span being laid green or red with its reason, its length and cost by the pointer | yes |

- The overlay shows only while the plan is piping (C, `Tools.OverlayShown`); building, the menu has no pipes, valves or pipe
  overlay. Its buttons are the piping's (`Hands`): the building takes none.
- A press on a machine's cell stands for its nearest port (`PipeHands.CellAt`); anywhere else, the cell itself.
- Oxygen runs from tanks holding some and powered generators; a network none feeds is faded and still. Read every
  250 ms once the world ticked; the dots move every frame.
- Out of the overlay the pipes are `PipesView`'s, lit as the ship is, behind what stands in front (`Layers.Pipes`,
  `Valves`); redrawn only when the spans, valves or grid change.
