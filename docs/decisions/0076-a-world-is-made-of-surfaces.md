# 0076 — A world is made of surfaces

- Status: accepted; slice 1 built, slices 2–4 planned
- Date: 2026-10-20

## Context

Heroes are to beam down to planets (sorties). A planet needs its own ground, gravity, air, floor stacks,
plans and machines, and every module holds the state of one grid. A planet drawn as a far region of the
ship's grid was turned down: its rules (gravity everywhere, thin air, what may be built) would leak into
every module as "unless this cell is on the planet". A spike that ran a second full `Registry` beside the
ship's had the right shape and six open problems: routing commands, what crosses between the two, ticking,
saving, ids, and the client's views.

## Decision

- **A world is made of surfaces.** A surface is one grid run by a `Registry` of its own: every surface has
  every module, the same code, its own state and derived state, so a module can never read another
  surface. Surface 0 is the ship: the modules the world is made with, never closed. A surface has an id
  (never reused), a kind naming its rules in content (gravity, air, sky, debris zones, what may be built),
  and the seed its ground grows from (0 for the ship, whose grid is its layout). The kind changes rules,
  never the set of modules.
- **Routing is the core's job.** `World` holds where each peer is (`SurfaceOf(peer)`; a peer not listed is
  on the ship) and a handler table per surface: a command goes to the table of its sender's surface. The
  client stamps each command it sends with the surface it is on (`Command.Surface`); one whose sender
  stands on another surface by its tick is refused `world.elsewhere` before any module sees it: its cells
  are another grid's. An unstamped command — the host's farewell, tests — goes wherever its sender is.
- **Every surface runs every tick,** in id order, after the tick's commands, all sharing the world's one
  `Rng`. Nothing sleeps: an idle planet costs next to nothing, and nothing runs on one for long (no farms,
  no furnaces allowed there).
- **Saves keep the ship where it was.** The top-level `slices` are surface 0's, byte for byte as before
  surfaces, so a ship-only world saves and hashes as it did. Other surfaces, and where peers are, will go
  under a key of their own when there are any.
- **Only the hero and their inventory cross.** A transfer carries a hero's record (suit, air, thrust) and
  inventory with its running tool: the two modules that own per-peer state, Crew and Items, give them up
  on departure and take them on arrival. Drones, cables, pipes, debris, floor stacks and reels never cross.
- **What is the player's, not the grid's, is global** (slice 2): Players (suit colours, so two heroes on
  different surfaces never clash once together) and Knowledge (blueprints granted and analysed), one per
  world, records only, no systems; surfaces may read and change them, they never read surfaces. The
  Handbook keeps reading the ship's Debris; creative mode stays a per-surface dev flag, copied when a
  surface opens.
- **The player's side,** as decided: dying on a planet wakes the hero at the ship's pod, the body staying
  on the planet while it exists; a player who leaves while on a planet rejoins on the ship at the pod; the
  current jetpack, an air-thrust suit for space, does not work on planets; the first planet has lower
  gravity than the ship.

## Slices

| Slice | Delivers | Status |
| --- | --- | --- |
| 1 | Surfaces in the core: the ship as surface 0, `SurfaceOf`, a handler table per surface, the client's stamp, `world.elsewhere`; no change to saves or the game | built |
| 2 | Globals with the second surface: Players and Knowledge, a format bump moving colours and research into them | planned |
| 3 | Surface 1 behind a dev flag: world commands to open a surface and move a hero between them (applied at command time), carrying hero and inventory, the client switching its views and filtering heroes, sounds and in-flight commands by surface, the fuzz drawing transfers | planned |
| 4 | Planet rules from the kind: gravity for stacks and the hero, thin air outdoors, sky light, no debris zones, the build list (`building.notHere`) | planned |
| 5 | The ground generator and digging: its own design | later |

## Deliberately not handled

- **Closing a surface** — discarding a planet when a sortie ends — waits for the pod and the jump, its
  first callers; until then a surface once opened stays.
- **Sleeping or catching up a surface:** every surface runs; an outpost left alone keeps running.
- **A planet saved as seed and edits:** saved whole.
- **Predicting a transfer, markers for friends on other surfaces, chunks or streaming of a big ground.**
- **A command sent just before its hero beams up** is refused `world.elsewhere`: the player presses again
  where they now are.
