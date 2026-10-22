# 0100 — Tech branches

- Status: accepted; slice 1 built (a module's effects per host, trades only; the arc module in the charger)
- Date: 2026-10-22
- Design: `designs/tech-branches.md` (the session's scratchpad) and the user's decisions of 2026-10-22, which override its top
- Amends: [0086](0086-modules-one-socket-for-machines-and-tools.md) (`ModuleSpec`'s one set of numbers for every host, and
  "no module in the charger"); [0099](0099-the-arc-bench.md)'s "Next" (the arc module in the charger, built)

## Context

The arc module (0086) did one thing everywhere: +1000 speed, draw and damage, whatever it was fitted into. That held while
every host was a station or the drill, each reading the number it cared about. The user wanted the arc in the charger as
well, five times as fast, and the design names what comes after: each planet type has a technology of its own, a branch,
whose modules do things of that branch's kind (heat, cold, force, growth, light) in different hosts, a tier at a time. One
`Speed` for all could not say "five times in the charger, twice in a station"; a module needs its effects per host.

## Decision

- **A branch is a technology:** Arc and Thermal certain (Thermal next, the crucible's heat module and a burn that lingers);
  Cryo, Kinetic, Bio and Optics later, as their planets come. **One branch per planet type, the first planet always Arc**
  (the savanna of the spires, 0085). A branch's id is its modules' `Tech` (`TechLooks` its icon); its tiers are new kinds of
  effect, each opened by a relic of that branch analysed (0027), as the sky shard opens the arc's first.
- **A module's effects are per host:** `ModuleSpec(Item, Tech, On)`, `On` a list of `HostEffects(Hosts, Effects)`: the hosts
  by kind (an electric machine's kind or an electric tool's item, 0098), and what the module does in each of them. A host is
  in one entry at most. `ModuleSpec.Hosts` lists them all, `In(host)` its effects there, `Adds(host, stat)` its trades on one
  stat summed; `Modules.Fits`, `For` and `Hosted` read `On`.
- **Effects are records, trades only today:** `Trade(Stat, Add)`, `Stat` one of Speed, Draw, Damage, Heat; `Add` in
  thousandths added to 1000 for speed, draw and damage (+1000 twice, +4000 five times), whole levels for heat. A residual
  effect (a hit's shock that lingers, the arc's second tier) is the next kind of `ModuleEffect`, not built.
- **One fold, per host:** `Boost.Plus(module, host)` adds a module's trades in that host; `IItemsQueries.Boost(machine)` folds
  a machine's modules by its kind, `ToolStats.Of(item, modules)` a tool's by its item. Gains round down, costs up, as before.
  `Crafting` and `Power` call the same queries and did not change.
- **The arc module per host** (designer-approved, pinned by `ContentTests`): the electric smelter, the Former and the circuit
  assembler +1000 speed and +1000 draw; the drill +1000 damage and +1000 draw; **the charger +4000 draw**. The charger's draw
  is what it charges (0053), so five times the draw is five times the charging: 3 → 15 a tick, a battery (1800 power-ticks)
  in 120 ticks, 6 s instead of 30, an accumulator (5400) in 360 ticks, 18 s instead of 90. **A fair trade and the player's
  choice** (the user's call): an accumulator still charges without the module, and on a short network the fitted charger
  lowers every user's share, its own too. The numbers each host had before stay exactly (a station's damage and a drill's
  speed were never read).
- **One socket per host for now:** a host takes one of each module, and the arc is the only module, so one socket each.
  Sockets per host (two for the drill and the stations with tool tiers) are a later slice.
- **The charger is a host:** its panel gets the socket under its body, ghost and marks as every host's (0086, 0097), and the
  shift-flight test covers it as it covers every machine that takes a module. **The drone bay is no host yet:** its 4 is
  shared between drones whose own flight time is the logistics' tuning (0062), and a bay five times as hungry would starve
  whatever shares its network while drones dock; it waits for a module of its own kind.
- **The card says what a module does per host, from its records:** a line for each entry of `On`, its trades in their
  order as times, "×2 speed · ×2 power", "×2 damage · ×2 power", "×5 charging · ×5 power" (at a charging host a draw trade
  says both, for there the draw is the charging); a line said twice, once. No host is named (0086) and no module has text
  of its own: a new module or host composes the same way (`card.trade.*`).
- **The charger's panel shows its numbers:** over its bar what its battery holds of full in power units, as the card writes
  it ("2700 / 5400"), and while it charges what goes in a tick ("+15/tick", `IPowerQueries.ChargeRate`, the sim's own
  amount); its header's bolt reads what it draws with its modules (15 with the arc), as a smelter's does. The always-on HUD
  stays wordless.
- **No save-format change:** a save holds a machine's modules by id (`Fitted`, format 24) and a tool's on its stack
  (format 25); what a module does is content, read when the world runs. A charger fitted is one more `Fitted` entry.
- **Dev:** the testbed's charger has three panels of its own (15, what it draws fitted), and its supply crate an arc module
  for every host; `-- --testbed --open=charger --charger=arc|plain [--shots DIR]` fills an empty accumulator, fitted or not,
  the seconds, charge, draw, share and the moment it came full written over the screen.

## Next

- **Slice 2:** `Content.Branches` (a branch's relics by tier), the accumulator and the arc module's place at the arc bench in
  content tests by branch.
- **Slice 3, the arc's second tier:** a residual effect (`Residual(Kind, Per, Every, Ticks)`), afflictions on cracks and
  debris that tick in their owners' simulation phases, storm glass from the far field, the crackle on what is shocked; a
  save-format step.
- **Slice 4:** several modules on a stack (`Stack.Modules`) and sockets per host; then afflictions on creatures, the Thermal
  skeleton, and planet types carrying their branches' relic weights.

## Rejected

- **One set of numbers per module, read by each host as it likes** (0086's): "five in the charger, two in a station" cannot
  be said, and a stat with no meaning in a host (a station's damage) sits there unread.
- **A charger module of its own** (a "fast charge" item): the arc is the branch of electricity; a module per host doubles
  items the way the arc drill did (0098).
- **Words per module on the card:** many modules and hosts; the effect records say it.

## Deliberately not handled

- **The fitted charger does not crackle:** the stations' crackle (`ArcCrackle`) is the crafting feature's, the charger's
  lights the power feature's; its battery filling five times as fast is what shows. A crackle for it waits for the user's
  look pass, with the crackle moved where both features can draw it.
- **A drone bay takes no module**, as said; nor generators, the analyzer, the pod or the furnace.
- **No stat caps and no cross-branch synergies:** one branch exists.
- **The card lists every line wherever it is shown**, not only the open host's: with three lines it reads at a glance.
