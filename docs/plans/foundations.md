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

A change within two cells of space, a breach, still flooded the whole grid, until F-4.

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

### F-4 — Rooms flooded within the ship, not the world ✅

The grid keeps the smallest box holding everything built (`Built`, derived): grown by every
placement, moved in past each edge line a removal empties. A change beside space floods only that
box: every cell outside it is space, so a cell on its side sees what one on the grid's edge does,
and no margin is needed. The rooms come out as a full `Find` gives them, and name the ids that
differ, so atmosphere redoes those rooms only instead of the whole grid. Rooms and pipes compare
their layer copies only within the box now and at their last look. The world hash after fuzzed
wreck runs, ships in larger worlds edited inside, on the hull and out in space (a load half-way),
and the reference world with door toggles is the same as before. A hull wall beside space taken
apart and built again every tick, the reference ship (256×128) 8 cells into a world of space, in ms:

| World | Before (Release / Debug) | After (Release / Debug) |
| --- | --- | --- |
| 1024×512 | 11.9 / 28.4 (rooms 10.3, air 1.4) | 1.4 / 4.5 (rooms 1.1, air 0.2) |
| 2048×1024 | 28.7 / 104.6 (rooms 23.7, air 4.5) | 1.2 / 3.5 (rooms 1.0, air 0.2) |

What is left is the ship: a flood of 256×128 costs 0.7 ms. Anything built far out, a back wall or a
pipe in space, widens the box and so the cost.

### F-5 — The air overlay draws only what changed ✅

The air field and its texture cover only what is built and a cell round it (the grid's `Built`),
growing with it; the shader is told where the texture lies, so it draws what a texture of the whole
world did, pixel for pixel. A survey looks again only at the rows whose rooms moved, the rooms whose
openings changed and the cells whose air came or went; a frame eases, writes and uploads only the
cells still on their way, so a still ship uploads nothing. The pulse of draining rooms is laid out
again only when they change. A temporary harness fed the old field and the new the same random
rooms, air, openings and growth for thousands of frames: every texel uploaded was the same. The
perf world (`--perf`, now with `--size` and `--built`), debug build, 20 s of the frame log: the
`hands` section, where the air is drawn, and the frames it pushed over a refresh:

| World | Before: avg / max ms, slow frames | After |
| --- | --- | --- |
| 256×128 | 0.038 / 3.1, none | 0.028 / 1.3, none |
| 1024×512, 256×128 built | 0.129 / 29.2, 77 over 16.7 ms | 0.027 / 10.1 (the first survey), none |
| 2048×1024, 512×256 built | 0.613 / 127.3, 76 over 33 ms | 0.045 / 4.7, none |

Not limited to the cells on screen: the box of what is built already bounds it, and easing cells off
screen keeps what scrolls into view as it would have been.

### F-6 — The build area: 1024×512

- Decided with the user on 2026-10-17. One migration moves an old wreck into the new grid; the
  new-world map and the camera follow.

### F-7 — The grid keeps a log of changed cells

- Today every reader of the grid finds what changed on its own, by comparing layers with a copy:
  rooms, pipes, the tile view, the light, the starlight, the door prediction. F-2, F-4 and F-6's
  slice 0 bounded those compares; the log removes them.
- The grid records each changed cell (and the layers that changed) as it changes, under its version.
  A reader asks for the cells changed since the version it last saw and handles only those; a reader
  too far behind, or a load, rebuilds as today.
- Derived, never saved or hashed. Every reader moves to it in the same change (0049).
- Done when a door toggle, a breach and a wall built cost a handful of cells everywhere, whatever the
  size of the world or the ship, measured on a fully built 1024×512 and on the 2048×1024 world.

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
