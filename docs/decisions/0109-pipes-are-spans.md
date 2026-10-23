# 0109 — Pipes are straight spans, laid with the wrench

- Status: accepted; supersedes the conduit layer of the grid and the pipe and valve cards of the plan menu (0107, 0108)
- Date: 2026-10-23
- Design: agreed by both developers in chat; no design file

## Context

Pipes were a layer of the grid, a code a cell, planned cell by cell from the menu and built by the screwdriver. At 8 px
cells (0104) a pipe across a ship is dozens of plans, and laying air was the slowest chore aboard. Cables already work
better: laid point to point in one stroke from the inventory (0017, 0108).

## Decision

**A pipe is a span**, straight, between two nodes: a machine's port or a joint on any cell of the build zone. At any angle,
diagonals too, at most `Content.Piping.MaxSpan` (12) long, its length the square root of the squared distance of its
cells, an integer, rounded up. It costs a tube per started 4 cells (`CellsPerItem`), from the inventory, refunded when cut.
A joint takes as many spans as are laid to it: a branch. A valve is a joint that opens and closes (`MakeValve`, a tube and
a gear; `SetValve` by E, as before). Pipes pass through walls and leave rooms sealed.

**The networks** join ports through joints and spans; a shut valve carries nothing. Air flows as before between the
machines on a network: only what connects matters, not the geometry.

**The module.** `pipes` owns the spans and valves, saved, and finds the networks from them (`sim/Pipes`), depending on
`grid` and `items` as `power` does; the grid loses its conduit layer (`Conduit`, `ConduitAt`, `SetValve`, `GridLayers.Conduit`),
`building` its conduit placements and the hazards their pipe breaking. One shared module for spans with `power` was
weighed and not taken: cables sag and move, end on anchors with a node rule of their own and carry no state like a valve;
the two share a shape, not code, and a shared module would serve neither better today.

**The wrench** (`wrench`, `Content.HandTools.Wrench`) mirrors the stripper (0108): in every hero's kit, crafted at the
lifepod of scrap. Held, the pipes overlay shows; the left button drags a span from the cell pressed (a machine's cell
standing for its nearest port) to the cell let go, its length and cost by the pointer; Ctrl and a click turns a joint into
a valve or back; the right button takes the nearest span away. The plan menu's air tab keeps only its machines.

**Maps** draw pipes as before (`-`, `v`, `x`); loading turns each straight run between nodes (ends, corners, branches,
valves, ports) into a span, cut every 12 cells by a joint, so every map connects the machines it did.

**Drawn** as the pipe art's straight run along the span at its 4 px thickness, a flange at each joint, a valve its wheel.

**Saves** go to format 29; older ones are refused, nothing having been released (`Migrations.Oldest`).

## Rejected

- **Keeping cell pipes beside spans:** two ways to pipe, and the plans' cost of a long run stays.
- **Pipes sagging like cables:** a pipe is rigid; a straight line also reads as one.
- **The sim checking the wrench is held:** the screwdriver and the stripper are not checked either.
- **A meteor cutting the spans through the wall it breaks:** which spans cross a cell is geometry the networks never need.

## Accepted limitations

- Spans crossing each other do not join; only shared ends do.
- A machine with spans on its ports may be taken apart: its ports become joints, and a new machine there joins them.
- The wrench's icon is a placeholder (`tools/ship-art/hand-tools`); the hero holds no picture of it; the pipe art repeated
  along a diagonal shows its rivets slanted.
- Pipes in a dark room are dark: they are lit as the ship is.
