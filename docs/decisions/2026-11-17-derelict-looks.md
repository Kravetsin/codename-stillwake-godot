# A derelict looks like a ship someone owned and left

- Status: accepted; built; supersedes in part [generated-vessels](2026-10-30-generated-vessels.md) (the engine bells, one
  main engine a vessel, the lamp and soil clod aboard)
- Date: 2026-11-17

## Context

A vessel world showed a grey box of cells: a stepped bow, wall "tentacles" for engine bells, the player's own iron wall
everywhere, nothing outside to take, a dark inside with nothing to tell. One derelict looked like the next. The user asked
how to make ships prettier, more atmospheric and more real, saw a canvas mockup of three generated vessels (the pick:
https://claude.ai/artifact/BDRspiJRSZ3MBMfp4A7LsE) and said to build all of it (2026-11-16), then, after playing, asked for
the mockup's furniture, its coloured containers, a flat lamp and no soil clods (2026-11-17). Only intact abandoned vessels
for now: outside whole, damage inside; wrecked vessels come later. Space stays empty, as in the main world.

## Decision

- **What a hero takes apart, or what fills a cell, is sim; what only shows is the client's,** derived from the vessel
  world's class and seed. Looks are never saved: a vessel world never outlives a load, and a joining guest has the seed.
- **Shape** (`Shipyard.Describe`, stream 2 after the rooms' stream 0, so a seed's rooms stay): the body in modules every
  22–38 columns bulging 0–2 rows up and down (none up under the bridge), a rib between each two, a housing behind a raised
  bridge, the bow a flat run then a straight slope (`BowFlat`, `BowRun`, `BowTip`). Bulges, ribs and housing are hull and
  never cut a room. The bells are gone (`VesselClass.Bells`); every deck below the first holds a main engine.
- **Outside parts are machine rows** `Vessels.Outside` (Hp, footprint, `Gives`, no menu card), laid by `Shipyard` in cells
  with no structure and no back wall, touching the hull: a `vesselNozzle` per engine against the stern, four RCS blocks, a
  mast with a red beacon, a dish; a shuttle's solar panel and belly tank; a freighter's 1–3 radiators, docking collar, keel
  truss on pylons with a coloured `vesselContainer` under 4 in 5 segments, sensor boom with a white strobe. A nozzle gives
  plates and copper, never the `nozzle` item: the Former keeps that gate (the user's pick).
- **Inside,** the mockup's furniture as rows `Vessels.Furniture` by role (bunks, locker, table, life tanks, console, pilot's
  seat, machine block, cable rack, coloured hold cargo), salvage piles still mixed in. The only lamp is `vesselLamp`, flat on
  the ceiling row, needing no power. No soil clod (`salvageClod` kept in no room, so an old world still loads). An inner door
  is laid open 1 in 5; the airlock never.
- **Colours are looks** (`Vessels.Coloured`): a machine with several pictures, the look drawn from stream 2, as salvage's.
- **Skin** (`VesselDress` beside `WreckDress`, GridView's dress for vessel kinds): one grey atlas of 8-cell panels by row
  role (lit top, middle, shaded bottom), rust and pit variants, rib tiles, a diagonal tile per bow slope, a stripe; the hull
  layer tinted by the livery (`VesselLooks.Liveries`, 4). Heavy ink where a side faces space, light where it faces a room.
  Name and code in Latin for every player, painted once as the surface loads into cell pieces (`VesselPaint`).
- **Light** (`VesselLights`, `VesselLamps`, `Beacons`): each stamped lamp red, dead, or the vessel's one dying lamp; the
  airlock's red; consoles' screens 3 in 4 lit, some glitching; a red dot over each door the grid says is shut; a jammed
  door (1 in 3 open ones) drawn half-shut while open; beacons blink outside. No Godot lights. The power bolt skips the
  vessel's own lamps.
- **Signs** (`VesselSigns`): 3–5 on a freighter, 2–3 on a shuttle, from frost, a sparking cable, soot over a burnt machine,
  a fallen ceiling panel, a toppled crate, a burst pipe's gas; pictures and particles, not things.
- **Looks are deterministic:** candidates come only from `Shipyard.Stamp(Describe(class, seed))` in reading order, chosen by
  `LookRng`, a fixed SplitMix64 (never `GetHashCode`, `HashCode`, `GD.Rand*`); the live grid decides only whether each shows,
  and a look goes once a cell it was stamped on changes.

## Consequences

- Yields rose and were re-measured: a shuttle 1600–2600, a freighter 3300–6000.
- Outside parts are reached by jetpack: the open space of a vessel world has no gravity.
- A letter half on a cell that is gone is clipped at the cell's edge.

## Deliberately not handled

- The bow's slope is drawn: its cells stay full cells (no slopes, 0106), so a hero flying along it bumps a hidden corner,
  and the rooms behind it still step.
- A part on a hull cut away floats: machines need no floor.
- Signs hold nothing and the pointer names nothing on them; gas is not air, frost is not cold.
- A jammed door lets light and heroes through as an open one: it is a look.
- Blink and flicker phases run on each client's clock, out of step between screens.
- With every lamp taken, a vessel is dark but for the helmet lamp.
