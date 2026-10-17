# Plan: Foundations before content — a world that costs what is built, not its size

## Why

Stage 1 content (meteors that breach walls, a radar, an engine) and a bigger build area both lean on
work that today walks every cell of the world. Measured on the experiment branch `exp/big-world`
(debug game build, an RTX 4060 Ti PC; the sim in Release):

| World | Built | Save | Hash | Load | Tick | Tick, a door toggled | Guest hitch each second |
| --- | --- | --- | --- | --- | --- | --- | --- |
| 256×128 | all | 0.5 MB | 2 ms | 0.01 s | 0.3 ms | 1–3 ms | — |
| 1024×512 | 256×128 | 5 MB | 24 ms | 0.12 s | 0.4 ms | 8 ms | 65 ms |
| 2048×1024 | 256×128 | 20 MB | 73 ms | 0.5 s | 1.2 ms | 33 ms | 250 ms |

Loading and joining are fine (a 20 MB world joins in 2.5 s on one PC). What breaks is work done
again and again over the whole area:

1. **The save is ten bytes a cell, empty space included**, and every peer hashes it once a second
   (`Protocol.HashEvery`), so a guest stalls for the length of a hash. The grid's five layers are
   almost all of it.
2. **Rooms are found again over the whole grid** on any change to walls or doors.
3. **The air overlay** walks every cell several times a second.
4. **Godot's buffer of per-instance shader parameters** runs out at about 6,000 machines.

Items 1 and 2 are needed at any size: every module grows the save, and meteors will rebuild rooms on
every breach; the door case is already over the 2 ms budget on the reference world.

## Tasks

### F-1 — The grid saved in runs ✅

The grid's five layers are saved as runs (`Runs` in Core), format 5 (PR #38). The wreck's save went
from 188 KB to 4 KB; a 2048×1024 world with a 256×128 ship from 20 MB to 0.3 MB, its hash from 73 to
4 ms. Accepted: a desync report on the grid names a run, not a cell.

### F-2 — Rooms and their air found again only around a change ✅

The grid, rooms and pipes have a `Version`, never saved; rooms are flooded again only around a
change away from space and name the ids they found again (`ChangedSince`); atmosphere redoes orders,
stranded air, leaks and the survey's counts for those rooms only, keeps the survey between ticks,
and edits up to 32 aired cells in place. The world hash after fuzzed runs, door toggles and random
edits is the same as before. A tick in Release, steady / a door toggled every tick, in ms:

| World | Before | After |
| --- | --- | --- |
| 256×128, the reference world | 0.7 / 2.8 | 0.5 / 0.8 |
| 1024×512, the reference ship in its corner | 0.4 / 8.3 | 0.1 / 0.2 |
| 1024×512, fully built (24k machines, 8k rooms) | 6.7 / 18.4 | 1.2 / 1.7 |
| 2048×1024, 512×256 built | 3.0 / 32.4 | 0.5 / 0.9 |

A change within two cells of space, a breach, still floods the whole grid (F-4).

### F-3 — A machine by its id at once ✅

The grid keeps every machine by its id and each kind's machines in id order, derived and kept
current on every build and removal (`MachineById`, `MachinesOf`); items find a machine's slots, and
furnace, research and air their records, by a binary search or one pass in id order. Power finds a
network's machines from the cables' ends, atmosphere reads the tanks and generators once a tick and
looks for tanks taken apart only when the grid moved, and a door toggle reuses its scratch. No
per-machine closures or per-tick allocations are left in those loops. The world hash after fuzzed
wreck runs, the reference world with door toggles, and factory worlds with random builds, removals,
cables and items moved into furnaces and analyzers (a save and load half-way) is the same as before.
A tick in Release, in ms (steady / a door toggled every tick, or steady alone):

| World | Before | After |
| --- | --- | --- |
| 1024×512, fully built (24k machines) | 1.8 / 2.4 | 0.4 / 0.9 |
| 2048×1024, fully built (98k machines) | 7.0 / 9.5 | 2.0 / 5.9 |
| 256×256, 1,024 furnaces and 1,024 analyzers at work (`BudgetTests`) | 50.5 | 0.5 |
| 512×256, 6k machines: the furnace / research system alone, 1,024 of each | 13.5 / 17.4 | 0.2 / 0.04 |

Power's networks are still found every tick: they now cost what is cabled, and a cache would have to
follow analyzers' slots and resting machines, which move no version.

### F-4 — Rooms flooded within the ship, not the world

- A change beside space floods only the ship's bounding box plus a margin, so a breach costs what
  the ship is, not the build area (today 6 ms on 1024×512, 24 ms on 2048×1024, once).

### F-5 — The air overlay draws only what changed or is seen

- Client only. Its per-cell passes are limited to the cells on screen or the rooms that changed.

### F-6 — The build area: 1024×512

- Decided with the user on 2026-10-17. One migration moves an old wreck into the new grid; the
  new-world map and the camera follow.

## Not in this plan

- **The welcome and resync in pieces.** Only Steam caps a message (512 KB). ENet and the relay take
  32 MB. It belongs to the GodotSteam task (G-09), and after F-1 a realistic world is far below the
  cap anyway.
- **Compressing saves and messages.** After runs the save is small; revisit if the welcome of a big
  colony is slow over the internet.
- **The shader parameter buffer** (item 4): raise the project setting only if a ship ever reaches
  thousands of machines.
- **Chunked worlds, several maps at once, hero cells in the sim:** each comes with the content that
  needs it (planets, creatures), per the architecture audit.
