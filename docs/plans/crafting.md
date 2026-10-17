# Design: Crafting, blueprints opened by their parts, a burner generator, the lifepod

## Problem

Blueprints open from a hand-written table (`Research.Unlocks`) and from random schematics, so how fast
the ship grows is luck, and nothing tells the player why a blueprint is closed. Every build is paid in
raw materials: there are no parts to make, carry or later automate. The only power the player can
add needs nothing to run, so fuel matters to the furnace alone.

## Player experience

1. In the wreck the hero smelts scrap, opens the inventory (Tab) and clicks the wire in the crafts
   beside it: the wire joins the queue, its bar fills for a second, a wire lands in the inventory.
   Shift-click queues five; each unit takes its own second.
2. The wire lays the cable that powers the analyzer in room B from the wreck's solar panel.
3. Iron goes in the analyzer: stripes on its screen, then a green check; plate and gear appear among
   the crafts. Every item is learned this way, raw or made.
4. Nothing pops up over the hero. Seeing the check, the player walks to the analyzer and opens it:
   right under the item in its slot are the blueprints and crafts that item goes into, the opened ones
   edged green, the closed ones grey; over a closed one, the items it still waits for.
5. Plates, gears, wire and a rotor analysed, the burner generator opens. Built and fed planks, it
   burns, breathes its room's air like the furnace and powers what is cabled to it.

Goes wrong:

- Ten wires queued with scrap for six: refused, nothing is queued; the craft shows the scrap short.
- A queued craft is cancelled: what its units not yet made took is given back, the one in progress
  included; with no room for it in the inventory, the cancel is refused.
- The inventory is full when a unit finishes: it waits, its bar full, until a slot is free.
- The generator's room loses its air: the fire goes out, the fuel in it is lost, the lights go dark.

## Out of scope

- Electronics: circuits, broken electronics, mining them. Until then the solar panel cannot be built:
  the wreck's one panel is the only one.
- Workbenches and crafting speed; the auto-assembler; tools.
- Recipes that make more than one item, or need a station.
- A generator burning only on demand: it burns while it has fuel.
- Dropping a finished item at the hero's feet.
- Loading saves from before this change.

## Scale

- World: one queue per hero of at most `Crafting.QueueMost` (9, the window's row) entries; a few analyzers and
  generators; about 25 blueprints and crafts.
- Players: each crafts alone, all in parallel.
- Frequency: a craft or cancel command every few seconds at most.

## Approach

### What opens what (Grounded)

A blueprint or a craft is known when it is granted, or when every item of its cost is analysed. One
rule, no table: `Research.Unlocks` and the schematics go. Granted at the start of the wreck: furnace,
analyzer, crate, scrap wall and back wall, and the wire craft. The cable is always known.
`Research.Locked` lists what analysis cannot open yet: the solar panel, until electronics exist.

| Made by hand (each unit) | Cost                 | Opens with      |
| ------------------------ | -------------------- | --------------- |
| wire (1 s)               | 2 scrap              | granted         |
| plate (2 s)              | 2 iron               | iron            |
| gear (2 s)               | 1 iron               | iron            |
| rotor (3 s)              | 1 gear, 2 wire       | gear, wire      |

| Built                           | Cost                             | Opens with              |
| ------------------------------- | -------------------------------- | ----------------------- |
| cable                           | 1 wire                           | always                  |
| scrap wall / back wall          | 2 scrap                          | granted                 |
| iron wall / back wall           | 1 plate                          | plate                   |
| floor, ladder, pipe             | 1 scrap                          | scrap                   |
| lamp                            | 2 wire, 1 scrap                  | wire, scrap             |
| door                            | 2 plate, 1 gear                  | plate, gear             |
| valve                           | 1 scrap, 1 gear                  | scrap, gear             |
| vent                            | 1 plate, 1 gear                  | plate, gear             |
| tank                            | 4 plate, 1 gear                  | plate, gear             |
| oxygen generator                | 4 plate, 1 rotor, 2 wire         | plate, rotor, wire      |
| burner generator (new)          | 4 plate, 2 gear, 1 rotor, 4 wire | plate, gear, rotor, wire|
| solar panel                     | unchanged                        | locked                  |
| furnace, analyzer, crate        | unchanged                        | granted                 |

Schematics leave the loot of supply pods and cargo crates; nothing takes their place.

### research

Stores `Analysed` (items, sorted) and `Granted` (`Layout.Known`, now naming crafts too; a map that
sets none knows all). `Known` is no longer stored: queries work it out from costs, 25 entries read on
validation and on panel timers. Queries: `Knows(Placement)` stays for `Building`, with `Knows(name)`
beside it for crafts; `Recipes()` — each blueprint and craft with its cost, whether known, and which
of its items are not analysed yet (replacing `Blueprints()`); `At(analyzer)` as today, its
`research.nothingNew` read as "done". "One item short" and "what an item opened" are the client's
filters of `Recipes()`.

An analyzer works only while its item is not yet analysed; empty or done, it rests and draws nothing
(as today). Items stay in its slot.

### crafting (new module)

`Items` cannot own it: it needs `Research` to know what is known, and `Research` depends on `Items`.

- State: a queue per peer, entries `{ id, recipe, count }`, the first with its `progress` in ticks;
  ids count up per peer, so an entry keeps its id while those before it finish.
- `craft { recipe, count }`: the whole cost of `count` units is taken at once (as Rust does). Refused
  with `crafting.unknown`, `crafting.full` (queue at `QueueMost`) or `items.notEnough`.
- `cancel { entry }` by id: the entry goes and the cost of its `count` units left comes back, the
  unit in progress included. Refused with `crafting.noEntry`, or `items.full` when the refund does
  not fit, as `Cut` is. The refund is the cost as content gives it now, as building's is.
- In the simulation phase, in peer order, each queue's first entry counts a tick; at its ticks one
  unit goes into the inventory and the next starts. With no room it waits, full, and counts no more.
- Needs no power; moving does not stop it. Depends on `Items` and `Research` only.

Content: `Crafts`, keyed by the item made, `{ cost, ticks }`; `Crafting.QueueMost`. `Power.CableCost`
becomes 1 wire.

### burner generator

The furnace's fire, a second time: a fuel slot, fuel burnt tick by tick, the room's air breathed
(`Breath` every `BreathEvery` ticks), out when either runs short. The `Furnace` module keeps every fire
(`Fires` by machine id) and lights a burner generator by the same rules but smelts nothing; that is
its second caller, so the fire's rules become shared there rather than copied. Which slot holds the
fuel moves from the constant `Furnace.FuelSlot` to the machine's spec: the furnace's second, the
generator's only one. While burning, a generator makes `Makes` (6, the panel's 5 and a little more)
to its cable network; out, `Furnace` rests it through the existing `IPowerOps.Rest`, and `Power`
counts no resting maker. `Power` runs first, so it sees this a tick late. No new dependency of
`Power`: `Atmosphere` already depends on it and `Furnace` on `Atmosphere`.

### Client

- The crafts window, docked beside the inventory (built: `game/features/crafting/README.md`): a
  pinned row of up to 9 (right-click pins, kept in `user://crafts.cfg`), tabs by an item's icon (the
  wheel over them switches), then the tab's grid of 9 columns by 5 rows, a family per row. Every
  craft has a fixed place in content (`Crafts.Tabs`; today one tab, Parts: plate, gear, rotor; then
  wire), so nothing moves when one opens: one not known is a dark silhouette in its place, one short
  of items dimmed. Click queues 1, shift-click 5 (refused whole when unpaid); a click on a short one
  goes to the first crafted item it lacks, its slot pulsing amber; a raw one's tip says where it
  comes from. Cost, need and have, time, all in the one tooltip. The queue under the grid, 9 slots:
  the running unit's bar, a click on an entry cancels it.
- The analyzer's screen: running lines while working, a green check when done, dark unpowered. Its
  panel: under the slot, every blueprint and craft the item in it goes into once analysed (as many as
  there are, in rows), opened or greyed out. `Learned`, the list over the hero's head, goes: with many
  blueprints opening at once it would cover the screen.
- The burner generator: art knocked together from scrap like the furnace, its fire seen through a
  grate; the fire, its light and embers drawn by the furnace's code, moved to a shared place since
  features do not import each other. The fuel slot opened with E.
- Art for wire, plate, gear and rotor, each visibly made of its source.

## Determinism and co-op

Only `craft` and `cancel` enter the sim, stamped with `ctx.from`. Progress is whole ticks; queues run
in peer order, fires in machine id order. `Power` reading `Burning` a tick late is the same on every
peer. A peer that joins gets queues, `Analysed` and fires in the snapshot. Nothing new is
client-authoritative. Fuzz draws `craft` (known, unknown, unpaid, full) and `cancel` (real and missing
entries, and ids of entries that finished).

## Trade-offs accepted

- A peer leaves with a queue → it finishes into that peer's inventory → items already belong to the
  peer id.
- The inventory is full at a finish → the unit waits in the queue → no drop, so no dependency on
  `crew`.
- The whole cost is taken at queueing → a player cannot spend the same scrap twice, and a refund needs
  no record of what was paid.
- A generator burns with nothing cabled → the player takes the fuel out to stop it → no demand logic.
- Planks, canisters, chunks and hull plates open nothing yet → they are still analysed and checked →
  one rule, no exceptions.
- The power reaches `Power` a tick late → 50 ms nobody sees → no new module order or dependency.
- The same recipe queued twice is two entries → each cancelled alone → as Rust shows it.
- Taking apart the wreck's machines refunds parts and opens blueprints early → the cost is the room's
  power or air → everything is made of something.
- Two analyzers finish the same item on one tick → it is learned once.
- Saves from before this change are refused → the format bump.

## Tests

Scenarios:

- A wire is made in its ticks; five queued come one by one.
- Each refusal: unknown, full, notEnough, noEntry.
- Cancel gives back the whole entry's cost, the running unit's included.
- A full inventory holds the finished unit until a slot is free.
- Creative mode crafts for free and knows everything.
- Analysed iron opens plate and gear but not the door; the door opens after plate and gear.
- A locked blueprint stays closed with every item of its cost analysed.
- An analyzer rests empty and when done; it stalls unpowered.
- A burner generator with planks and air powers a lamp; without air its fire goes out and the lamp
  goes dark; without fuel it makes nothing.
- The wreck: furnace → wire → cable → iron analysed → plate crafted.

Invariants:

- `Analysed` only grows; real items, each once, sorted.
- A queue never passes `QueueMost`; its first entry's progress stays below its ticks; ids within a
  queue only grow.
- Items are conserved: what a craft took equals what its units gave plus what cancelling returned.

Existing tests: `Unlocks`, schematics and pod loot scenarios become analysed-item ones; cable costs
change to wire; the furnace's become fire scenarios where they hold for both.

## Cost

- Modules: new `Crafting` (sim and client). Changed: `Research`, `Furnace` (fires of both kinds),
  `Power`, `Building` costs, `Debris` loot, `Items` content, the analyzer's screen and panel.
- Content: `Crafts`, `Crafting.QueueMost`, `Research.Locked`; wire, plate, gear, rotor; the burner
  generator; new costs.
- A new ADR supersedes the parts of 0024 about unlocks and schematics; the concept is updated.
- Estimated diff: 1400–1800 lines with tests, in two tasks, each playable and tested alone (the new
  costs need parts, and parts need crafting, so the rule and the crafts cannot be split):
  1. **Parts and their blueprints** (built): the analysed rule, `Locked`, schematics gone, the four parts, the
     `Crafting` module and its panel, the new costs, the analyzer's screen and panel.
  2. **The burner generator** (below, built): shared fires, the fuel slot by spec, resting makers, its art.
  3. **The lifepod** (below, built): where the hero is made, air for the suit, the Fabricator in it;
     crafting by machine only, the hand queue and its window gone.

## Task 2: the burner generator (built)

Built on `feat/burner-generator` ([0045](../decisions/0045-a-burner-generator-shares-the-furnaces-fire.md)),
as the burner generator section above says, with these differences:

- `Furnace` rests a cold generator from its **construction** phase, not after its fire: power's
  derivation the same tick sees it, so one just built never makes a tick, and the lag stays one tick.
- **A fire fed in time relights at once,** the furnace's too: its last tick of fuel burnt, the next
  fuel is lit on the same tick. Otherwise every plank burnt out left a dark tick: a lamp blinking and
  the crackle starting over every fifteen seconds.
- Format 8: a fire's `furnace` is now `machine`, renamed by its step.
- The client: the furnace feature draws both fires, since it mirrors the module that owns them; nothing
  moved to a shared place. The generator's flywheel spins while it burns. Its panel: the fire over
  its fuel slot and a bolt with what it makes. No hum yet: a new sound waits for the user's audition.
- Map character `B`; on the build menu's Power tab after the solar panel; the testbed lights one in
  room two, powering the lamp over it and the airlock's.

## Task 3: the lifepod, and no crafting by hand (built)

Decided with the team on 2026-10-15; built on `feat/lifepod`
([0028](../decisions/0028-the-lifepod-and-crafting-by-machine-only.md)). As built: `PodRefill` 2 and
`PodReach` 1 (standing by it); `QueueMost` 4, the panel's four slots; the pod in the wreck's room A
at columns 51–53 against the airlock, its hatch at (52, 22), the furnace's place at (49, 21) kept. The crafts window by hand proved either bulky or hard to
search, so parts are made by machines only, as in Subnautica and RimWorld. And a ship taken apart
or opened to space must never leave a player stuck: waking in vacuum, passing out, waking again; or
with no power and nothing to make parts with. One object answers both: the **lifepod**, as
Subnautica's Lifepod 5.

### Player experience

1. The hero is made in the lifepod: at the start, and after passing out, its hatch opens and the hero
   climbs out with a full suit.
2. Standing by it, the suit fills with the pod's own air, slowly; it gives none to the room. A hero in a
   ship open to space can still breathe and work at the pod.
3. E at the pod opens its **Fabricator** beside the inventory, as the furnace and the analyzer open:
   its recipes in fixed places, its queue, its output (the mockup, "Fabricator panel").
4. A click on the wire queues one, a shift-click five; the scrap leaves the inventory at once. The
   pod's screen runs and the queued slot fills from the bottom. It needs no power and is slow, half
   speed: the pod is the floor that is always there, not the factory.
5. Finished wire lands in its output slots; a click takes it into the inventory. The queue runs while
   the hero is away.
6. Co-op: one pod for the crew, its queue seen by all; anyone may add to it, take from it, or cancel
   an entry, its cost given back to whoever cancels. Every hero is made there.

Goes wrong: the output is full and the unit at work waits there, full; a hero dismantling the pod is
refused (`building.fixed`), as for everything built into it.

### Approach

- **Content.** `lifepod`: about 3×3 cells, `Fixed` (never taken apart, never hit), 3 output slots and
  no input slots (`Inputs = 0`), no power drawn; crafts at `Crafts.PodSpeed` (half). Its air for the
  suit: `Crew.PodRefill` units a breath within `Crew.PodReach` cells, from no store. Every craft is the
  pod's while it is the only station; a craft names its station once a second one comes (principle 2).
  A Fabricator built by the player, drawing power and working at full speed, is a later task.
- **Map.** The wreck's room A holds the pod where `S` is now; a map's pod replaces `S` as where heroes
  are made. A map with no pod keeps `S`, as the tests' maps do.
- **`Crew`.** The spawn is the pod's hatch cell. A conscious hero within `PodReach` of it draws the
  pod's air into the suit each breath, before the room's or the suit's own.
- **`Building`.** Refuses taking apart anything `Fixed`, with `building.fixed`.
- **`Crafting`: queues by machine, not by peer.** State: each crafting machine's `{ machine, next,
  progress, entries }`. `Craft(Machine, Recipe, Count)` and `Cancel(Machine, Entry)` add
  `crafting.noStation` (no crafting machine by that id) to today's refusals; the cost leaves the sender,
  the refund goes to the sender. Each tick, in machine order, a machine with an entry adds its speed
  (thousandths, as research counts); a unit done goes to its output (the machine's slots in `Items`),
  or waits full. Depends on `Items`, `Research` and `Grid` (which machines craft).
- **Client.** The pod's art: a capsule with a hatch, a porthole lit from inside and the Fabricator's
  screen (the sketch, "Lifepod"); the hatch opening as a hero is made. Its panel beside the inventory as
  the mockup draws it, with the suit's air filling while near. The Tab crafts window, its tabs, pages
  and pins go.

### Trade-offs

- The pod's air is endless → only for the suit, only by the pod, slowly → aired rooms, tanks and
  generators stay the way to live; the pod is the way not to die.
- The pod cannot be moved or taken apart → the ship grows round it; moving it is a later question.
- A queue paid by one and cancelled by another refunds the canceller → friends share a ship → no
  record of who paid.
- Walking to the pod for every part → the queue runs while the hero is away; ordering from the
  planner tablet is a later idea.
- Old saves (hand queues, no pod) are refused → the format bump.

### Tests

- A hero is made at the pod: first entering, and waking from passing out, with a full suit.
- By the pod the suit fills even in vacuum; past `PodReach` it drains as before.
- The pod refuses being taken apart, with `building.fixed`.
- A wire queued is made in twice its ticks at the pod's half speed, with no power anywhere.
- The output fills; a unit waits full until a slot is taken from.
- Refusals: noStation, unknown, full, notEnough, noEntry.
- Two peers queue at the pod; one cancels the other's entry and gets its cost.
- The wreck: the pod in room A, heroes made there.

## Decided with the team

- The burner generator makes 6; a plank burns in it as in the furnace, 300 ticks (15 s).
