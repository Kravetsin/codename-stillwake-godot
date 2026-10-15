# 0028 — The lifepod, and crafting by machine only

- Status: accepted; supersedes the hand crafting of
  [0027](0027-what-is-analysed-opens-what-is-made-of-it.md) ("Parts are crafted by hand", "a queue per
  hero"); its opening rule stands
- Date: 2026-10-15
- Design: `docs/plans/crafting.md`, task 3 (deleted once built)

## Context

The crafts window by hand, docked beside the inventory on Tab, proved either bulky or hard to search
as crafts grew: tabs, pages and pins to find four parts. And a ship taken apart or opened to space
could leave a player stuck: waking in vacuum, passing out, waking again; or with no power and nothing
to make parts with.

## Decision

- **One object answers both: the lifepod,** as Subnautica's Lifepod 5. A 3×3 machine built into the
  ship (`Fixed`: never planned, never taken apart, no blueprint; meteors hit no machine), drawn on the
  map (`O`). The wreck's room A holds it where `S` was. A map with no pod keeps `S`.
- **Heroes are made at its hatch:** at the start and after passing out, with a full suit.
- **Its air is for the suit:** a conscious hero within `Crew.PodReach` cells of it takes up to
  `Crew.PodRefill` units a breath of the pod's own air first, from no store, before the room's or the
  suit's own; it never gives the room any. A hero in a ship open to space can still breathe and work
  at the pod, slowly; aired rooms stay the way to live.
- **Parts are made by machines only,** as in Subnautica and RimWorld. The pod holds the first, the
  Fabricator: no power, half speed (`Crafts.PodSpeed`), three output slots. E opens its panel beside
  the inventory, as the furnace and the analyzer open: recipes in fixed places, a queue, the outputs.
- **Queues belong to machines, not to peers:** `Craft(Machine, Recipe, Count)` and
  `Cancel(Machine, Entry)`. The crew shares a pod's queue: anyone may add to it, take from its outputs
  or cancel an entry, its cost given to whoever cancels. Work is counted in thousandths of a tick, as
  research counts it; a unit done waits in the machine, full, while its outputs have no room.
- **Every craft is the pod's while it is the only station;** a craft names its station once a second
  one comes (principle 2). A Fabricator built by the player, drawing power at full speed, is a later
  task.

## Rejected

- **Keeping crafting by hand beside the pod:** two ways to make one thing, and the window that proved
  bulky stays.
- **A spawn point and a separate workbench:** two objects for what one answers; neither helps a player
  stuck in vacuum on its own.
- **Air from the pod into the room:** endless air would make tanks and generators pointless; the pod
  keeps a hero alive, it does not make a ship livable.
- **Refunding whoever paid:** it needs a record of who paid every unit; among friends the canceller
  is near enough.
- **A queue per hero at a shared machine:** whose queue runs first would need rules nobody sees.

## Consequences

- Saves from before this change are refused (save format 3).
- The pod cannot be moved: the ship grows round it; moving it is a later question.
- Walking to the pod for every part: the queue runs while the hero is away; ordering from the planner
  tablet is a later idea.
- `Crafting.QueueMost` is the panel's four slots, shared by the whole crew.
