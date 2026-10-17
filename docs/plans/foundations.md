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
- **Slice (a) ✅ — world, zone, centred wreck, migration** (0048): `Content.World` 2048×1024 with a
  1024×512 build zone in its middle; `Layout.InWorld` centres the wreck and carries `Layout.Zone`;
  the grid saves its `Zone` and refuses `grid.outsideZone`; debris finds the hull within `Built`;
  save format 10 moves every saved cell by (928, 464). Dev runs and their scripts keep the wreck as
  drawn (`Registry.WreckMap`); `--world` plays the new world without the title. On the new world the
  grid's sync (~2 ms) and the light's compare (~1 ms) still walk whole layers on a change: slice 0.
- Slice 0: the client's grid, light, starlight, predicted toggles and pipes overlay cost only what is
  built (measure `--perf --size 2048x1024 --built 192x96` before and after).
- **Slice (b) ✅ — the report, the cold, the frost** (0050): the client also sends `Enter` every
  `Crew.ReportEvery` ticks while its hero's body is in another cell than the sim's; crew's `EdgeCost`
  drains the suit past the field (the zone grown by `Field.Margin`), 1 a breath just past it to 45 at the
  edge; a frost shader over the world shows it. `--edge=N` starts the hero N cells past the field.
- Slice (c): the build-mode zone frame.

**Slice 0 — the client costs what is built ✅.** Nothing of the client walks the whole grid on a
regular path any more. The grid view and the light look again only when the grid's `Version` moved,
and then compare their copies only within what is built now and was at their last look
(`game/shared/BuiltRows.cs`); the first drawing draws only cells with something in them. `AfterToggles`
answers just the toggled cells, so a toggle in flight no longer copies two layers (16 MB on 2048×1024)
a tick. Starlight is worked out over `Built` and 4 cells round it and the occupancy texture covers
`Built`, both placed by their corner; past them every cell is open, which is what the edge shows. The
pipes overlay looks for valves within `Built`. `BuiltBoundsTests` holds the bounded starlight and
grid compare to the whole-grid ones over random ships and edits; movie frames of the gallery and four
perf views (a toggled door, the ship's edge, whole worlds) are byte for byte the same, and the wreck's
differ in one pixel by one step (the starlight texture's float placement, `features/light`). The perf
world, debug build, whole world on screen, 20 s of the frame log (avg / max ms; frames over 16.7 ms;
section max ms), and `--toggle` (new: the first doorway toggled every tick, one always in flight):

| World, built | Load, first sync | Frames | grid / light max | `--toggle`: frame avg, over 16.7 ms; light / grid avg |
| --- | --- | --- | --- | --- |
| 2048×1024, 192×96 | 2.2 s, 146 → 2.2 s, 17 ms | 0.42 / 18.4, 1 → 0.39 / 15.8, 0 | 2.3 / 1.2 → 0.03 / 0.03 | 150 ms, every frame; 129 / 18.6 → 0.41 ms, 1 of 46,656; 0.014 / 0.004 |
| 2048×1024, 256×128 | 2.3 s, 142 → 2.1 s, 27 ms | 0.47 / 18.6, 1 → 0.44 / 15.5, 0 | 3.6 / 1.4 → 0.07 / 0.04 | 150 ms, every frame; 129 / 18.9 → 0.48 ms, 1 of 40,345; 0.026 / 0.005 |
| 1024×512, all of it | 4.0 s, ~340 → 4.1 s, ~360 ms | 20.9 / 45, all → 20.5 / 44, all | 0.9 / 0.7 → 0.04 / 0.01 | 69 ms → 68 ms; 34 / 5.1 → 36 / 1.0 |

The fully built world is drawn whole at zoom 0.1 and bound by rendering (18 ms of render CPU), and its
`Built` is the world, so only the grid's toggle cost moved; its starlight is still swept whole on every
change (35 ms). The single 15–19 ms frame of every run, before and after alike, is spent in the
debris and sim sections, not in what this slice touched.

### F-7 — The grid keeps a log of changed cells ✅

The grid logs every write to a layer or a machine's cells — the version, the cell, the layer, what it
held — in a ring of 65,536 writes, derived, never saved or hashed (0050). `ChangedSince(version)` names
the cells that differ since then, ascending, with the layers that do, exactly: a cell changed and
changed back is not named. Null when the log no longer reaches back or the version is no version of
this grid; every reader then looks at everything again, as on its first look. Every reader moved to
it in the same change (0049): rooms, pipes and atmosphere (its vents and tanks looked at again only
when a machine's cell changed) in the sim; the tile view (machines too), the light's occupancy
texture and lamps, the starlight and the pipes overlay's valves in the client. Their layer copies are
gone. Starlight is worked out again only within 45 cells of a change, past which what crossed it is
fainter than a texel shows, from what lies within 90: its texels are those of working it all out,
byte for byte. Rooms, with the cells changed in hand, no longer flood the ship's box for a change
beside space: they label again only the cells beside a change and the rooms within two cells of it;
where a cell stops passing space, a walk through the space beside it tells a pocket closed off, which
becomes a room.

The world hash, every room and every pipe network after fuzzed wreck runs, the reference world with
doors toggled, and edits inside, on the hull and in space of a 256×128 ship in 1024×512, a 192×96 one
in 2048×1024 and a fully built 1024×512 (a load half-way) are the same as before; so are the movie
frames of the gallery and four perf views (`--toggle` on the reference, 2048×1024 and fully built
worlds, and the ship's edge), pixel for pixel. A tick in Release (JIT tiering off, so runs agree), mean
ms over 200 ticks; a door toggled, or a hull wall beside space taken apart and built again, every tick:

| World | Steady | Door: before → after | Breach: before → after |
| --- | --- | --- | --- |
| 272×144, the reference ship 8 in | 0.04 | 0.10 → 0.06 | 0.70 → 0.07 |
| 2048×1024, the reference ship 8 in | 0.04 | 0.10 → 0.07 | 0.69 → 0.07 |
| 1024×512, fully built (8k rooms) | 0.61 | 1.50 (max 12.7) → 1.00 | 10.8 (max 34) → 1.04 |

On the fully built world all that is left of a door or a breach, 0.4 ms, is atmosphere's own: it
walks every vent (16k) to see whose room moved and every room to pick the changed ones; rooms and the
rest take 0.04 ms. The client, debug build, `--perf`, 20 s of the frame log (frame avg; light and grid
sections avg / max; starlight's work per change avg):

| World, `--toggle` | Frame avg | Light | Grid | Starlight a change |
| --- | --- | --- | --- | --- |
| 1024×512, fully built | 65.0 → 29.8 | 35.2 / 43.9 → 0.90 / 2.3 | 0.93 / 1.4 → 0.03 / 0.4 | 32.8 → 1.5 |
| 2048×1024, 192×96 built | 0.40 → 0.39 | 0.013 / 3.0 → 0.007 / 1.7 | 0.004 / 11.2 → 0.003 / 0.07 | 1.5 → 0.9 |
| 256×128, the reference world | 1.61 → 1.57 | 0.09 / 4.2 → 0.03 / 1.6 | 0.006 / 8.9 → 0.004 / 0.06 | 2.6 → 0.9 |

The fully built world is drawn whole at zoom 0.1 and bound by rendering, about 18 ms of render CPU,
before as after; steady, it runs at 20–22 ms a frame either way. A change's starlight costs its window,
about 181 cells square, and an upload of the whole texture (2 MB there): the same on every world.

Not done here: atmosphere's walk over every vent and room on a change (above); pipes find every
network again over the whole grid when a conduit or machine changes (a pipe built, not a door or a
breach); the air overlay compares the rooms per cell row by row within what is built, not the grid.

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
