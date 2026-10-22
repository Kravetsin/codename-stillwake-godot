# 0104 — The cell is 8 px

- Status: accepted; supersedes [0005](0005-terraria-scale.md)'s 16 px cell, its reference world and its doorway "three `D`s", and its air
  as [0105](0105-a-room-airs-four-cells-a-beat.md) has it; amends [0007](0007-spacewalks-and-airlocks.md)'s tether anchor
- Date: 2026-10-22
- Design: `designs/cell-8px.md` (the session's scratchpad); its amendments, the user's answers, override its top

## Context

At 16 px a cell is 38% of the hero's height. Building is coarse, the ground is blocky, and walking needs slopes (0069), which
have cost three rounds of foot IK, creature jerks and special collision. Halving the cell gives building and digging the
grain of the hero's own body, and lets a later change replace slopes with steps.

## Decision

- **A cell is 8 px** (`Camera.Cell`, which `ShipCamera.Cell` and `Rig.Cell` read). Every picture keeps its pixels: the hero is
  about 42 px tall as before, a tank 32 px.
- **One rule for every number: lengths double, quantities per area ×4, quantities per cell unchanged.**
  - ×2: the hero (`Hero`: width 1900, height 5250, every speed, gravity 42, jump 830, reach 12000; ratios and ticks kept), the
    creatures (4×4 cells, every speed, reach and box), stacks thrown and falling, the harpoon, the jetpack, debris (margins,
    leads, views, drift, tiers, the asteroid band), the field's margin, the pod's reach, lava's, the resonator's, drones' reach
    and speed, a cable's span, every row, column and wave of a planet type (the first planet 4096×1536; its noise marks, bands
    and chances kept), growths (2×2 at the least: `Flora` `Across` 2, `Tall` ×2), machine footprints and ports (a tank 4×4,
    a vent 2×2, the lifepod 6×6), the camera's farthest view (192×108), lights' reaches; in the client every length in cells
    or thousandths of a cell.
  - ×4: oxygen counted by area (0105).
  - ÷4, rounded to the nearest, a half up: a wall's or a back wall's build ticks a cell (scrap 3, iron 7, soil 3, stone 5,
    alloy 10). Halved, rounded up: a wall's cost a cell (scrap 1), so a wall of the same length, one cell thick, costs about
    what it did. A door costs a plate and a gear a cell, 10 ticks: a doorway of six cells costs six plates, as three did, and
    six gears where it took three, a gear not halving.
  - Unchanged, per cell: strength, leak, what a dug cell yields, weapons' damage and pace, every tick count, hit points.
  - A per-cell falloff in the client (a light's keep) becomes its square root: light falls off over the same pixels.
- **Maps are drawn a character a 16 px cell and doubled** (`Layout.Doubled()`): walls, back walls, machines and ladders fill
  the 2×2 cells of their character; a door fills its block's left column and a hatch its top row, so doorways are one cell
  thick and six long, the other half open (space beside the hull, a ladder it is climbed through); a lone door is a door
  standing in a wall, a hatch between walls across; a floor fills its block's top row; `S` its bottom-left; a conduit its
  top-left and the cells toward neighbouring conduits, so pipes stay one cell wide and meet the doubled ports; materials,
  cables, the zone and plants double with the map. Slopes throw: they are not doubled. The wreck, the strip, the ruins'
  prefabs, the dev maps and test scenarios (by default; `cells: true` reads a map a character a cell) are doubled.
- **The ship world stays 2048×1024 cells**, so it is physically half as big; its zone stays 1024×512.
- **A wall two cells thick still leaks** (Rooms): a solid cell beside a room is hull when the outside is beyond it, or beyond
  the solid cell behind it away from the room; a wall of three leaks nothing. So a doubled hull, a 16 px one, leaks through
  its inner layer, hull leak per length ×2 with the cells, as 0005's ratio had it.
- **Creatures step up and down `Fauna.StepUp` = 2 cells**, the one 16 px cell they stepped, and move in parts of under a cell, so
  a flight of 1500 a tick never passes a wall one cell thick.
- **The reference world is 512×256** (the 256×128 drawing doubled); CLAUDE.md's line is the user's to change.
- **Pictures drawn at 16 px:** what fills a character's cells (a ladder, a floor, iron's back walls) is cut by where it lies;
  what is one cell (pipes, doors, hatches, valves, the hull's edges) is drawn at half its size till it is drawn at 8 px. A
  planet's living edge is a 16 px column's half by its column's parity, laid from two cells over the ground; its details lie
  by 16 px cells.
- **Saves are format 28**; every save before is refused, its steps and fixtures deleted (0031's list shows them grey).

## Rejected

- **Keeping the planet's cell count (2048×768), a quarter of its area:** the user kept its physical size; the measurements were
  within the budgets (the PR's report).
- **Hull walls doubled to one cell thick:** the wreck would lose its 16 px walls; a hull two cells thick leaking through its
  inner layer keeps both the look and the leak.
- **Doors two cells thick:** the user chose doors matching a one-cell hull.
- **Light tuned at the old scale behind a conversion in the shader:** every number would then mean 16 px cells while the
  sim's mean 8; reaches and keeps were converted where they are written instead.

## Consequences

- A breath is a cell of air, now a quarter of what it was: four heroes in the wreck's first room no longer outrun its
  generator; breathing from the suit weighs less against a tank. Retuned after a playtest, as 0005 was.
- The planet's gravity on the hero rounds to 29, not the 28 twice the old 14 would be: a held jump there rises 11.6 cells, not
  12, and clears twelve with the ledge assist.
- Planet surfaces grow fewer trees, bushes and rocks: level ground over their doubled `Level` columns is rarer at the finer
  grain (`FloraTests`); flowers are unchanged.
- One-cell gaps between walls, tiny rooms, become common: visible and harmless. A lone ladder cell is climbed centred on
  itself; a two-wide ladder centred between its columns.
- The planet costs some four times what it did: grown in about 0.6 s, opened on a tick of about 1.7 s, some 290 MB held by the
  sim (the PR's measurements).
- The tether ties at the body's middle as it leaves the hull (0007), not the middle of its cell: at 8 px a cell's middle lies
  below a standing hero's.
- Digging is four times slower per volume till the brush (2×2 a stroke) lands with it.
