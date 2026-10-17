# 0051 — The grid keeps a log of changed cells

- Status: accepted
- Date: 2026-10-17

## Context

Every reader of the grid found out on its own what had changed, by comparing layers with a copy it
kept: rooms (structure and back walls), pipes (conduits and the machine list), the tile view (five
layers), the light (structure, back walls, machines), the starlight (all of it, swept again), the pipes
overlay (valves). F-2, F-4 and F-6's slice 0 bounded those compares to what is built, so a big world
of space costs nothing; but on a fully built 1024×512 ship what is built is the world, and a door
toggled still cost the client 35 ms a frame. Six callers of one need, each solved apart: the case 0049
names.

## Decision

- **The grid logs every write** to a layer or to a machine's cells (`Occupancy`): the version it was
  made under, the cell, the layer and what the cell held before. Every write goes through one method,
  so none goes unlogged. The log is derived: never saved, never hashed, empty after a load.
- **Readers ask `ChangedSince(version)`:** the cells whose layers differ now from what they held then,
  ascending, each with the layers that do (`GridLayers`). The answer is exact: a cell changed and
  changed back is not named, so a reader does what its old compare would have done and the world's
  hash cannot tell the two apart.
- **Null means "look at everything":** for a version the log no longer reaches, or one this grid never
  had. Every reader keeps its old path for it — rooms and pipes find everything again as after a load,
  the views compare within what is built — so the log may forget.
- **A ring of 65,536 writes**, the oldest dropped when full. A door, a breach, a hero's build write a
  handful; the sim's readers look every tick and the client's every frame or few times a second, so
  none comes near it. No reader registers or holds the log back: a simple bound, and "too old →
  look again" handles whatever outruns it.
- **A version names one grid.** Versions start over with a load, which makes a new grid; a reader
  keeps the grid it read with the version (the client's views are made anew with the world, and the
  pipes overlay compares the grid it is handed).
- **Every reader moves to it at once:** rooms, pipes and atmosphere in the sim; the tile view, the light's
  occupancy and lamps, the starlight and the pipes overlay's valves in the client. Starlight, which
  spreads, is worked out again only within a reach of the cells changed, the distance past which what
  crosses a changed cell is fainter than a texel can show, so its texels are exactly those of working
  it all out.

## Consequences

- A door toggled, a breach, a wall built or taken apart costs the readers the cells it changed,
  whatever the size of the world or the ship: on a fully built 1024×512 world a toggled door cost the
  client's light 35 ms a frame and now under 1 ms. With the changed cells in hand, rooms label again
  only the rooms near a change, a breach beside space included: its tick fell from 10.8 to 1.0 ms
  there (F-7).
- Layer copies kept only to compare are gone (rooms' two, pipes' conduits and machine list, the light's
  two). The tile view keeps what it drew, which it draws from and which a toggle in flight shows apart.
- What is left of a change is the readers' own work: atmosphere walks every vent and room to follow
  the rooms changed, and pipes find every network again when a conduit or machine changes.
- A new reader of the grid asks the log and keeps a path for null; it never keeps a copy to compare.
