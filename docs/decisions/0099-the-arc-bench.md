# 0099 — The arc bench: a vise that fits tools, the arc branch's station

- Status: accepted; built
- Date: 2026-10-22
- Design: `designs/tool-bench.md` (the session's scratchpad), its review and the user's decisions of 2026-10-21 and 2026-10-22,
  which override its top; the arc bench pick page (design "vise", «Тиски с осколком»); the tech-branches design's addition
- Amends: [0098](0098-the-tool-bench-modules-in-tools.md)'s slice 2 (the bench is the arc bench, of a shard, a station too);
  [0086](0086-modules-one-socket-for-machines-and-tools.md)'s arc module, now made at the arc bench, not the assembler

## Context

0098 put a module on a tool's stack, and nothing in play fitted one: the bench was its slice 2, a plain workbench. The user
made it arc tech: a bench built round a sky shard, opened with the arc module when a shard is analysed, and a station of
the arc's own crafts. Batteries also felt like a step down once the arc doubled a drill's draw (a battery ran a fitted drill
15 s): the arc's first craft beyond the module is a better battery, the accumulator. Tools will be many (cutters, laser
guns, a spear); the bench must take any with no picture drawn per tool.

## Decision

- **The machine** `arcBench`: 2×1, no power, built of a sky shard, a frame, 2 plates and a gear in 160 ticks; aboard only, as
  every machine (planets refuse machines). A shard in its cost: analysing one opens it with the arc module (0027), as every
  other item of its cost comes from the pod, the Former or salvage. Its slots: the vise, then two outputs.
- **The vise holds one tool a module fits** (`MachineSpec.Tunes`; `Modules.Hosted(item)`: the drill today), nothing else:
  a move or swap leaving another item there is refused `items.notTool`. `Fit`/`Unfit` with the bench as their machine work
  on that tool's stack (`Stack.Module`), never on `Fitted`: refused `items.noTool` (empty vise), `items.notModule` (a module
  the tool takes not), `items.fitted` (it holds one: a stack holds one module) and `items.notFitted`. A bench holding a tool
  is not taken apart (`building.notEmpty`, as any machine holding items). The fuzz draws all of it, moves of tools into a
  vise among them.
- **The arc branch's station:** every craft with a shard in its cost sits at the bench and nowhere else (`ContentTests`): the
  arc module (moved from the assembler, its cost unchanged) and the accumulator. Speed 1000 unpowered, a queue of four,
  **paid by hand only**: its one input is its vise, so a craft from the inputs is refused `crafting.noInputs`, as at the pod.
- **Drones never touch it:** a `Tunes` machine is no target and no source (`Logistics.Takes`, `OutputsOf`), so not servable.
- **The accumulator:** a shard, a battery, 2 copper and a circuit in 200 ticks; worn on the belt, a stack of one, gear,
  holding three batteries' charge (`Charge.Accumulator`, 5400 power-ticks). Every rule that reads `Capacity` takes it: the belt
  drains it as a battery, the charger and a bay fill it, its card and slot bar show it, its empty picture shows at none.
  The battery in its cost is lost with its charge, as a drone's (`ContentTests` names both).
  The wrist's energy tab sums every store on the belt and counts against a belt of three accumulators (`Charges.BeltMost`,
  16 200); it had counted against three batteries, so two accumulators and a battery read 5 400, not 12 600.
- **Its look** is the user's pick, "vise": a ship's bench, copper vise jaws on it, a chip of a shard in the fixed jaw
  (`tools/ship-art/arc-bench/paint.js`). The picture is two cells tall over a one-cell footprint: **a machine picture taller
  than its footprint rises over the cells above** (`ShipTiles.RiseOf`; the tile's origin, a plan's ghost and cut, the aim's
  outline and a plan's dashes all lay it so), with no number for it but the picture's height.
- **Any tool sits on it by its own data:** a held look declares `HeldLook.Clamp`, the pixel the jaws close on; the bench lays
  it on `BenchLook.Clamp`, the jaws' middle, and draws the tool's composed held picture (`HeldParts`, `HeldArt`, its modules
  on it) under the jaws (`arcBench_front`). A clamped tool may overhang the picture's sides by 4 px and the table's front by 2
  (the drill's bit and loop do by 1 and 2), never above it (`BenchLook.Fits`); `BenchLookTests` checks every held tool.
- **Seen** (`BenchView`): the shard's cracks alight, unlit, breathing; its glow and its violet-white light (0096's colour,
  4 cells, 0.75), a small arc off its tip now and then. A module going onto the tool — read from the sim's stack, so a
  friend's fit shows too — is a burst of 650 ms: arcs from the shard's tip and cracks into the tool's first mount every
  ~90 ms, its glow brighter and a soft flash, toned down from the page so the bench stays seen. The panel is the queued
  station's (`FabricatorPanel`): recipes, the vise under "Tool" with the sockets of the tool in it (`Reading.Host`), the queue
  and the outputs; every shift flies (`ShiftFlightsTests` covers the vise and its sockets). Its build card's thumbnail is its
  picture. The accumulator's icon is a placeholder in the 16 px item style, black glass between copper bands, a violet-white
  crack alight; cold when empty.
- **Dev:** the testbed has a bench by room two's door, a plain drill in its vise, and an arc module and an accumulator in
  the supply crate; `-- --testbed --bench` scripts a fit and a queue for recordings (`--open=charger` an accumulator charging,
  `--dark` every lamp out).

## The shard economy

A spire's cradle gives 4 shards, a home's niche 1 (0096). The bench, each arc module and each accumulator take one. A first
spire is the bench, a module for the drill and two more (a station's module, an accumulator); every later one, four upgrades.

## Next

- **The arc module in the charger** (the user's decision): it fills ~5× faster, so an accumulator, 1800 ticks (90 s) at the
  charger's 3 today, fills in about 18 s; it needs effects per host kind (`ModuleSpec` has one `Speed` for all).
- The bench's own mark ("tunes") on the vise's empty slot; card DPS; a sound for the burst, through the user's audition.

## Deliberately not handled

- **A machine built in the cell over the bench** overlaps the vise's picture: the footprint is one row, as the user drew it.
- **A tool on the bench in hand:** the vise's tool is the bench's, not carried; `ToolStack` never sees it.
- **The fitted tool's crackle on the bench:** it crackles only while it runs, in a hand.
- **Shift-clicking a non-tool with the bench open** sends it toward the vise and the sim refuses it (the slot shakes), as a
  stone shifted into a charger.
- **A burst seen by a peer joining mid-burst:** it starts only on a change seen, so a load shows the fitted tool still.
