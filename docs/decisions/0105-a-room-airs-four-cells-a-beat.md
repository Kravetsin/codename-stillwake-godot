# 0105 — A room airs four cells a beat

- Status: accepted; supersedes [0005](0005-terraria-scale.md)'s "a room fills one cell a tick" and its rejection of filling several cells a beat
- Date: 2026-10-22

## Context

0005 aired a room one cell a tick, the fastest its rule allowed, and rejected filling several cells a beat as a rule change no
one had asked for. The 8 px cell (0104) makes every room four times the cells: at a cell a tick, a room of the same size would
fill four times slower, and a breach mended would take four times as long to air again.

## Decision

- **A filling room airs `Air.CellsPerFill` = 4 cells a fill beat** (`TicksPerCell` stays 1): its first four empty cells in its
  order, each paid with a unit from the first of its vents that can pay, as one was. A room of the same size fills as fast as
  it did.
- Oxygen counted by area is ×4 with the cells: `TankCapacity` 36000, `Canister` 9000, `GeneratorRate` 12000, `GeneratorStore`
  160, `VentLoss` 24, `VentDrain` 8. A cell of air is still one unit; per cell, `DoorLeak` and every material's leak are
  unchanged, so a hull leaks by its length ×2 and a back wall by its area ×4.
- A hero's breath, the suit and its refill are unchanged: a hero's needs do not grow with the ship's cells.

## Rejected

- **A cell a tick, rooms four times slower to fill:** a mended breach would stay in vacuum four times as long for no reason
  the player could see.
- **`TicksPerCell` below 1:** ticks are whole; the rate needs cells per beat.

## Consequences

- A tick airs up to four cells a room: the aired list takes more edits a tick, within its in-place limit.
- A breath weighs a quarter of what it did against a room (0104's consequences).
