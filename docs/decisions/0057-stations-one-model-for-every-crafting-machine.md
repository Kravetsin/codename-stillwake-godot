# 0057 — Stations: one model for every crafting machine

- Status: accepted; supersedes of [0028](0028-the-lifepod-and-crafting-by-machine-only.md) "every craft
  is the pod's while it is the only station", and of [0023](0023-a-furnace-burns-fuel.md) and
  [0045](0045-a-burner-generator-shares-the-furnaces-fire.md) that the `furnace` module smelts
- Date: 2026-10-17
- Design: "powered production stations" (approved), slices 1 to 3 of 4

## Context

Two machines made things, each its own way. The lifepod's Fabricator lived in `crafting`: a queue paid
when queued, a speed constant (`Crafts.PodSpeed`), a queue size (`Crafting.QueueMost`), its rows in
`Crafts.Rows`, and "a machine crafts when it is a lifepod". The furnace lived in `furnace`: its smelt
progress on the fire record, counted in ticks of fire. The concept wants many more: powered machines for
parts (a former, a circuit assembler), an electric smelter with more heat, and upgrade modules that make a
machine faster or give it a longer queue. Patched one by one, each would copy a speed, a queue, an energy
rule and a panel; the smelter would copy the furnace's smelt rules without its fire (0049).

## Decision

- **A station is a machine with an entry in `Content.Stations`:** its `Speed` in thousandths of full
  speed, its `QueueMost`, its `Rows` (what a queued station makes and where each sits in its panel: one
  list) and its `Heat`. `Crafting` owns every station and reads these in one place (`StationOf`), so an
  upgrade level folds in there; a station's slots stay its machine's, read by `Items`.
- **Two feeds, both in `Crafting`.** *Queued* (the pod, the Former): `Craft` and `Cancel` as before, paid
  from the inventory, the queue shared by the crew. *Fed* (the furnace): its input slot is its queue; work
  on its item drops when the item changes; the input decides the output (`Yields`), where the station's
  heat is at least the item's (`ItemSpec.Heat`, 1 for all yet). A craft at a fed station, or of a recipe not
  in a queued station's rows, is refused `crafting.notHere`.
- **Energy is the machine's:** none (the pod: a full share), `Draws` (its network's satisfaction) or a
  `FuelSlot` (its fire: a full share while it burns). Work a tick is `Speed × share / 1000`, rounded down,
  stations in machine id order. Progress is in thousandths of a tick for both feeds.
- **`Furnace` keeps fires only.** In its construction phase it tells `Crafting` which furnaces are heated
  (`ICraftingOps.Heat`), as it tells `Power` to rest a cold generator; the flag is derived, set before it
  is read every tick, cleared after, never saved. A furnace lights only when `Crafting` says it can smelt
  (`ICraftingQueries.CanSmelt`). One copy of the smelt rules, for the furnace and the smelter to come.
- **The fire is read as the tick begins.** A furnace starts smelting the tick after it lights, and a fire
  that goes out for want of air smelts on its last tick; a fire fed in time relights within its tick and
  misses none. Otherwise timings are unchanged: the pod's, the generator's, and the furnace's between
  those two ticks.
- **`Crafting` runs before `Building`**, which refuses to take apart a station with crafts queued
  (`building.notEmpty`), and rests an idle station that draws (nothing queued, or a unit done waiting for
  room) through `IPowerOps.Rest`, as research rests an analyzer.
- **Format 14:** a step moves each fire's item and progress (in ticks) into crafting's `smelts` (in
  thousandths), drops the fires kept only for them, and puts crafting's slice before building's.

## The Former (slice 2)

- **The first powered station:** 2×2, draws 3, speed 1000 (five times the pod's), a queue of eight. It
  makes the parts and components — wire, plate, gear, tube, frame, coil, rotor — not tools; the pod keeps
  all twelve at a fifth of the speed, and its list stops growing: new crafts go to machines.
- It costs 2 frames, a rotor, 2 gears and a circuit, so it opens once those four are analysed (0027).
- **It rests while idle:** nothing queued, or a unit done waiting for room. A machine whose slots only give
  out (`Inputs` 0) draws whenever its module does not rest it; the old rule, "while its first slot holds
  something", is for machines that work on what is put in them.
- `CraftingModule.Works(machine)`: a unit at work, not done, with a share of energy. What the client shows
  without words: the Former's flywheel turns, its gears with it, its press head beats on the die; its
  panel's bolt is lit and its screen runs. Idle, full or without power it stands still, the bolt dark, the
  screen dark when power is what it lacks.
- Its panel is the Fabricator's, made general for any queued station: its name, rows, queue size and
  speed come from its station; a station that draws shows its bolt and what it draws.
- Placeholder art matching its recipe: two frame posts, a rotor flywheel on top, gears driving a press
  head onto a die with a plate on it; the moving parts in four pictures over it. No sound of its own yet:
  new sounds go through the user's audition.
- The testbed has one in room two, cabled to the burner generator and pressing plates; a third panel on
  the roof feeds the charging station so every user stays at full power.

## The electric smelter, heat, silicon and glass (slice 3)

- **A second fed station:** 2×2, draws 4, speed 2000 (twice the furnace), heat 2; an input and three
  outputs, no fuel, no fire. It costs a frame, 2 coils, 4 plates and a circuit, so it opens once those four
  are analysed. Like the Former it rests while idle: nothing in its input it can smelt, or no room for
  what it makes.
- **Heat:** a smelt runs where the station's heat is at least the item's. The furnace is heat 1, the
  smelter heat 2. Heat is read in `StationOf` alone, so machine upgrades fold in there later (a module a
  level, a smelter's level its heat); none are built yet.
- **Silicon:** rock smelts into 2 silicon in 160 ticks at speed 1000, at either station (80 at the
  smelter); silicon into glass in 120, at heat 2, so in the smelter only. Glass waits for the assembler
  and the solar cell (slice 4). Smelting now chains: salvage into materials, and silicon on into glass.
- **Too hot for a station** (decided by the user): the item is taken into the input, so the player sees
  the right kind of machine was used, and the station shows it cannot work it: `TooCold` on
  `ICraftingQueries`, `crafting.tooCold` from the furnace's `At`. A cold-blue star pulses where it would
  work — the furnace's mouth, the smelter's grate — lighting a little round it; its panel's mark turns cold
  blue, and over that mark the one tooltip names the station hot enough ("Needs more heat · Electric
  smelter"). Nothing smelts, nothing is lost, and a furnace does not light for it. Replaced by what it can
  work, it works.
- `Works(machine)` now covers fed stations: what it can smelt and a share of its energy. A furnace's fire
  reaches crafting only within a tick, so the client shows a furnace's fire from `Furnace.At`.
- **Art:** a placeholder matching its recipe: a plated box on a frame, two copper coils behind a grate, a
  small control board, two insulators. Working, the coils glow, three pictures drawn unlit over it, and
  each coil lights the room round it in its orange. Idle, the coils stand dull copper and give no light.
  No sound: the furnace's crackle is a fire's, and a new sound goes through the user's audition.
- **Its panel** is the furnace's without the fuel, plus the Former's bolt and draw.
- The testbed has one in room two, smelting rock, on a fourth roof panel of its own; the ship is three
  cells longer for it.

## Rejected

- **The furnace keeps its smelting, the smelter its own:** two copies of one rule, and upgrades would
  have to find both.
- **Crafting smelts after the fire burns** (in the events phase, from a flag set by the burn): every tick
  of the old timing kept, but a fire relit at the end of its fuel would ask whether the input still holds
  something before that tick's smelt took it, and light a plank for nothing; and the events phase is for
  events.
- **The furnace asks crafting to smelt from inside its burn:** `Crafting` would run after `Furnace`, which
  depends on it, or the two would call each other within a tick.
- **Refusing a too-hot item at the input** (`items.tooCold`): the player would not learn which machine it
  wants; the user chose to take it in and show the station cold.
- **The too-cold reason as the furnace panel's red line:** it is not a fault to fix there but a pointer to
  another machine; the mark says it without words and the tooltip in words, on demand.
- **A station's slots in its station entry:** `Items` reads every machine's slots, crates included; a
  second place would drift from the first.

## Consequences

- A fire's first tick is lost to smelting: about one in sixty of a chunk's work, once per fire lit.
- New machines that make things are a machine spec, a station entry, art and a panel: no module.
- Building depends on crafting, and furnace on crafting; crafting depends on power.
