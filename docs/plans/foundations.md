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

### F-1 — The grid saved in runs

- The grid's five layers are written as runs, `[value, count, value, count, …]`, by one converter in
  `sim/Core`, so the save, the hash, the welcome and a load cost what is built, not the area.
- Format 5, with its migration step (layers to runs) and its fixture (0031).
- A budget test: a 1024×512 world with 256×128 built saves no bigger than one and a half times the
  same 256×128 alone. Bytes, not time, so it never flakes.
- Accepted: a desync report on the grid names a run (`grid.structure[57]`), not a cell. It still
  names the layer, and the grid is the module least likely to drift.

**Done when:** the migration and budget tests pass; the 2048×1024 experiment world saves well under
1 MB; a guest no longer stalls each second on the experiment branch.

### F-2 — Rooms found again only around a change

- Rooms are rebuilt only in the region a wall or door change touches, not by a flood from the
  grid's border.
- A budget test: a door toggled every tick stays under 2 ms on the reference world, and on a large
  world with a small ship.

### F-3 — The air overlay draws only what changed or is seen

- Client only. Its per-cell passes are limited to the cells on screen or the rooms that changed.

### F-4 — The final ship grid

- Decided with the user once F-1 and F-2 are in: the build area's size, and one migration that
  moves an old wreck into the new grid.

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
