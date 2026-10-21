# 0086 — Modules: one socket for machines and tools

- Status: accepted; slice 1 built (the arc module in electric stations); the arc on the drill, then unlock modules, next
- Date: 2026-10-21
- Design: `designs/modules.md` (the session's scratchpad) and its review, which overrides its top; the user's decisions
- Amends: [0085](0085-ruins-on-the-first-planet.md)'s "the arc coil does nothing yet": it is what the arc module is made of

## Context

The ruins (0085) give the arcs' relic, the arc coil, and nothing took it. The concept wants the arcs' tech to be what a
sortie is for: something the ship cannot make on its own that makes what it has better. Two kinds of thing want it: the
electric stations (faster, hungrier) and the electric tools (the drill hitting harder). [0057](0057-stations-one-model-for-every-crafting-machine.md)
already read every station's numbers in one place, `StationOf`, "so an upgrade folds in there", and [0062](0062-drones-keep-machines-going.md)
the bay's in `BayOf`. Later modules are of another kind: unlock modules that let a station do what it could not (the
crucible, a heat module for alloy; an optics bench; a spore bed). Built as one-off patches, each would carry its own
slot rules, save field and panel; so the mechanic is one socket, designed for those named users, built one slice at a
time (0049).

## Decision

- **A module is an item with an entry in `Content.Modules`:** `ModuleSpec(Item, Tech, Hosts, Speed, Draw, Damage, Heat)`.
  What it adds is in thousandths added to 1000 (+1000 is twice), so modules add up, not multiply; `Heat` adds whole
  levels to a fed station's heat, for the unlock modules to come. Its `Hosts` are the kinds of thing it fits, machines
  today and a tool next; `Modules.For(kind)` the sockets a host has. Its `Tech` (the arc module's `arc`) names its
  technology, and with it the icon the client draws for it (`TechLooks`).
- **Compatibility is shown by the tech's icon, never by a list** (the user's call: modules will be many, and so will
  what takes them). The socket's ghost is the icon of the tech it takes; with a machine open, every slot holding a
  module it takes carries that icon as its mark, as a fire's flame marks what it burns (`SlotMark.Fits`); the module's
  card ends in a strip of its icon and what it does in one line ("×2 speed · ×2 power"). No card, tip or chip names a
  machine it fits, and an empty socket's tip is its name alone. A tool that takes a module (the drill, next) gets the
  same: the marks ask `Modules.Fits(item, host)` of a host kind, which a tool's item id can be.
- **Valheim's way:** a machine holds at most one of each module, fitted and taken out by hand, never moved by drones,
  never loot (made at a station only), kept in death as gear. No on and off: a module fitted works.
- **The arc module**, `arcModule`: a stack of one, gear, made at the circuit assembler of an arc coil, 2 coils, 2 circuits
  and a frame in 200 ticks; opened as every craft is, once its cost is analysed (0027), so the arc coil now asks for the
  analyzer. No scroll gates it yet: deep vaults and their scrolls are the ruins' next slice. It fits the **electric
  stations** (the electric smelter, the Former, the circuit assembler) and adds +1000 speed, +1000 draw, +1000 damage
  (the damage for the drill, next): **twice as fast for twice the power.** The furnace (planks, a starter likely to be
  retired) and the pod (paid by hand) take none: the user's call.
- **Fitted is the items' state, beside the slots, not in them:** `ItemsState.Fitted`, a list of `Fitted(Machine, Module)`
  sorted by machine then module (ordinal), written to a save only while one is fitted. Nothing that reads a machine's
  slots (moves, sorts, smelts, a queued station's outputs, drones) sees a module, so none of them changed. Format 24, an
  identity step: it keeps an older build from loading a save and dropping what is fitted.
- **Commands** (`Items`): `Fit(Module, Machine)`, `Module` a `Place` (the sender's slot or a machine's), refused
  `items.noMachine`, `items.noCrate`, `items.noSlot`, `items.empty`, `items.notModule` (not a module, or not one this
  machine takes) and `items.fitted`; `Unfit(Machine, Module, Slot?)` into the sender's own slot, an empty one, or with
  none wherever it fits, refused `items.notFitted`, `items.noSlot` and `items.full`. A machine holding a module is not
  taken apart: `building.notEmpty` gains one clause.
- **One fold:** `IItemsQueries.Boost(machine)` sums what a machine's modules add. **Gains round down, costs up:**
  `Crafting`'s `StationOf`, now an instance method, makes a station's speed `Speed × (1000 + ΣSpeed) / 1000` rounded
  down and a fed one's heat its own plus theirs; `Power` finds the networks with a draw callback, each user drawing
  `Draws × (1000 + ΣDraw) / 1000` rounded up, and answers `Draw(machine)` for the panels. The arc doubles exact integers
  today (smelter 2000 → 4000 at 4 → 8; Former and assembler 1000 → 2000 at 3 → 6 and 2 → 4); the rounding is for the
  modules to come. Speed and share are floored separately: a smelter with the arc on a panel's 5 gets 625 thousandths,
  2500 a tick, still faster than 2000 without it.
- **Seen** (placeholders; the user's mockup rule stands for anything larger): the machine's panel shows a socket per
  module it takes, its tech's icon faint while empty, the module lit violet once fitted, glowing violet-white while a
  module that would go in is carried. A module is dragged or clicked onto it, shift-clicked from the bag, and dragged,
  clicked or shift-clicked out of it. **A shift flies it, as every shift-click that sends a stack does** (0075): into the
  socket, or out into the first empty slot, the hotbar first; the client decides each shift's flight in one place from
  the command it sends, and a test shift-clicks every item in every panel and every socket and fails on one that does
  not fly. A station with the arc fitted crackles while it works: crisp violet-white arcs along its outline, now and
  then a burst or one leaping down a side, the room lit violet-white, flickering (anime-line arcs of the effects
  toolkit's `Bolts`, now tinted); idle, nothing. The module's picture and the arc's icon (a small violet-white zigzag)
  are placeholders, the icon for the user to pick on a page.
- **Dev:** the testbed's supply crate holds an arc module for each station it fits; `-- --testbed --arc` scripts a fit
  into the smelter for recordings.

## Next

- **Slice 2, the arc on the drill:** the stack carries its module (`Stack.Module`, a `string?` written only when set,
  compared in `Alike`, carried by the floor, as charge is, 0053); `Fit`/`Unfit` gain a tool target; a run names its slot
  (`Run(Slot)`), its draw and damage boosted; the found `arcDrill` item and `Weapons.ArcDrill` go (dev-only today).
- **Slice 3, unlock modules:** the crucible as a `Heat` module at the smelter, once alloy has a named use; queued
  stations' unlock modules (extra rows) wait for a recipe that needs one.

## Rejected

- **Words for what fits where** (the first build: a "FITS" chip on the card naming the stations, an empty socket's "takes:
  Arc module / drop a module here to fit it"): with many modules and hosts a list says nothing at a glance; the tech's
  icon on both sides does.
- **Module slots after a machine's outputs** (the design's first version): every reader of a machine's slots (outputs,
  smelts, drones, a format step for every machine's new slot) would have had to skip them. A list beside the slots
  changes no reader.
- **Levels** (`Station.Level`, `Craft.Level`, `crafting.level`): the unlock modules fold in as heat or rows through the
  same `StationOf`; a level of their own had no user.
- **A module look per machine** (where its arcs sit on each picture): one generic crackle first; per-machine art waits
  for the user's picks.
- **Generic speed, productivity and capacity modules** (Factorio's): the arcs' tech is what a sortie brings back, not a
  catalogue of upgrades.

## Deliberately not handled

- **The arc doubles a network's demand:** a station fitted on a short network lowers every user's share, its own too.
  The player sees the warning bolt over the machines and adds power; nothing refuses the fit.
- **No module in generators, the analyzer, the charger, the drone bay, the pod or the furnace.**
- **A fit or a take is shown at once only by the peer that sent it** (its slots and the open machine's sockets, as a
  move is, 0014); a friend's shows a round trip later, as a friend's move does.
- **A machine gone some other way than taken apart** would keep its entry in `Fitted`; nothing a player does removes one
  but `Building`, which refuses while a module is fitted.
- **The crackle is silent:** a sound for it goes through the user's audition.
