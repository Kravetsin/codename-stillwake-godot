# 0098 — The tool bench: modules in tools

- Status: accepted; slice 1 built (a drill carries its arc module, its card and picture put together from what is fitted);
  the bench, slice 2, built as the arc bench ([0099](0099-the-arc-bench.md))
- Date: 2026-10-21
- Design: `designs/tool-bench.md` (the session's scratchpad) and its review, which overrides its top
- Amends: [0086](0086-modules-one-socket-for-machines-and-tools.md)'s slice 2 (no `Run(Slot)`: a tool works with its
  stack, found by its item); [0075](0075-effects-are-light-not-shapes.md)'s and [0085](0085-ruins-on-the-first-planet.md)'s
  found arc drill, gone; [0096](0096-the-sky-shard.md)'s arc drill shock

## Context

The arc module (0086) doubles an electric station; its `Damage` was there for the drill, and the drill had a stand-in: the
arc drill, an item of its own, found nowhere but a dev world (`Weapons.Found`). Modules will be many, and so will the tools
that take them, the cutters and laser guns after the drill (0053). A module as a second item per tool doubles every tool
for every module; a module carried by the tool itself is one value on its stack, as its charge is (0053), which 0053 named
as the shape the next per-item value would take.

## Decision

- **A tool carries its module on its stack:** `Stack.Module`, the module's item id, written to a save only when set, on a
  stack of one of an item among the module's `Hosts`. The arc's `Hosts` gain `"drill"`: a host is a station's machine kind
  or an electric tool's item. It goes wherever the stack goes, every copy made with `with`, as charge does: moves, swaps, crates,
  drones' cargo, the floor (`FloorStack.Module`, `AsStack`, `LayDown`), surfaces, death (the drill is gear, kept). Stacks
  merge only when alike, and `Alike` compares it (now `Stack.Alike`, read by drones and the client's shift-click too), so a
  fitted drill and a plain one swap. A sort lays the plain stack first, then by module, ordinal, so its order stays total.
- **A tool works with its stack:** `IItemsQueries.ToolStack(peer, item)` is the first stack of that item the peer carries,
  a fitted one first: with two drills the fitted one is used, whichever is in hand. What a stack works with is one pure
  fold, `ToolStats.Of(stack)` (`sim/Items`): its damage a hit, pace and draw, from its item and its modules
  (`ToolStats.ModulesOf`), the modules' additions summed first (`Boost.Plus`, the step a machine's `Boost` folds by too),
  then applied once, damage a gain rounded down, draw a cost rounded up; a tool's pace is its weapon's (`Speed` is a
  station's). The sim and the card both call it, so they cannot disagree. `Damage(peer, tool)` is that stack's
  (`Boost.Gain(1, +1000)` = 2), the weapon's own while the peer carries none: a hit's sender was never checked for holding its weapon (0034), and the fuzz sends hits from empty hands.
  `Debris` takes it off a wreck, `Building` adds it to a crack. A run draws the tool's `Draw` a cost by the module, rounded
  up (`Boost.Cost(3, +1000)` = 6, the same fold), and `items.flat` is judged on that. `Run(Item)` stays: the sim is told which tool runs,
  never which slot.
- **The fitted drill:** 2 a hit every 2 ticks, 20 a second, for 6 a tick, so a battery runs it 15 s; the ruins' alloy
  breaks in 3 s (15 of the axe, 6 of the plain drill). The cradle's shock stays paced (0096): 1 a drill hit, module or none.
- **The arc drill goes:** its item, `Weapons.ArcDrill`, `Weapons.Found`, its icon and its four held pictures. Every weapon is
  crafted again. A dev save holding one no longer loads (accepted, as before).
- **A tool's picture is put together from its parts:** a held look declares its mounts (`HeldLook.Mounts`, a `ToolMount`
  each: a kind, the pixel an overlay's top-left goes on, the box its live effect plays over; the drill has one, `coil`, at
  (4, 4)). A module declares an overlay per kind of mount it shows on (`ModuleLooks`, `ModuleOverlay`: a picture and a
  `MountFx`, none or the arc's crackle). `HeldParts.Of(look, modules)` lays them out: mounts in the tool's order, on each
  the modules' overlays in the order they are fitted, each over the one before; an overlay for a mount the tool lacks shows
  nowhere; every crackling mount's box crackles (`DrillLight`, one state a box). `HeldArt` blends the layers over the still
  picture and each spin frame once and keeps the texture, and the hand (`Figure`) and the card (`ToolPicture`) both draw
  it. The arc's overlay is `art/module_arc_coil.png`, the old arc drill's blue coil turns cut out of it, so the fitted
  drill looks as it did. A mount of one kind is one shape on every tool that has it, so one overlay fits them all.
- **Seen as the sim decides:** the figure is told the module of the stack its hero works with (`Figure.Fitted`, from
  `ToolStack`), so a friend's fitted drill crackles too; how a hit sounds and how much of its look it throws
  (`GroundSounds.HitAt`, `Impacts.ShareAt`) take the sim's damage for its striker. A fitted tool's slot wears its module's
  tech icon, the 0097 hexagon, as a badge in its bottom-left corner.
- **The card puts itself together from the stack:** `ItemCards.Fitted(card, modules)` builds a tool's tiles and draw from
  `ToolStats` for the stack and for the plain tool; a number that differs is drawn in the tech colour (its icon's rim) of
  the first module that changes it, with a small arrow up or down and "×2" beside it (`CardBoost`); the HIT tile, which no
  module changes, stays plain. The draw tile reads the fold too (the fitted drill's 6, no longer the item's 3). Each fitted
  module is named on its tech's strip at the bottom, "Arc module fitted". No card code names a module: two modules, or
  another tool, compose the same way.
- **Save format 25, an identity step:** no save before held a module on a stack; the bump keeps an older build from
  loading a fitted drill and dropping its module.
- **Dev only, for now:** `--arc-drill` (with `--asteroid`, `--planet` or `--ground=…`) and the testbed's supply crate give a
  drill with the arc fitted. Nothing in play fits one.

## Next

- **Slice 2, the bench:** `toolBench`, 2×1, unpowered, a frame, two plates and a gear, opened by analysis; one slot taking
  only an item a module fits (`items.noTool`, a machine flag `Tunes`); `Fit`/`Unfit` gain a bench branch writing the module
  onto the slot's stack; the bench is not taken apart while it holds a tool; its panel reuses the machine panel and the
  module socket; a placeholder look; the shift-flight test covers it. Drones never target it: no recipe takes a tool in.
- **Slice 3, later:** the bench's own mark ("tunes"), the tool shown clamped on the bench, the user's mockups of badge and
  bench.
- **Several modules on one stack:** `Stack.Module` holds one; `ToolStats`, the card and the pictures already take a list
  (`ToolStats.ModulesOf`), so only the stack and its save change when a tool takes a second.

## Rejected

- **`Run(Slot)`** (the design's first version): a run naming its slot, with its draw and damage read from there. The sim
  would follow the hand, which is the client's word; `ToolStack` reads the same stack without a new command shape, and
  dropping saved runs from a format step.
- **A module as an item beside the tool** (`arcDrill`, one item per tool per module): many modules and tools multiply.
- **A card or picture per module and tool:** every combination drawn by hand; the user asked for the card and the picture
  to assemble themselves from what is fitted.
- **Feel tuned per module** (hit effects scaled by damage, a heavier whir, an extra crackle at the hit): each module would
  need its own tuning. The user kept the fitted drill's look as it is; the difference is the time a wall takes.

## Numbers (why the difference is easy to miss)

A hit every 2 ticks (0.1 s) either way; the plain drill takes 1 a hit, the fitted 2. Hits to break, and those hits times the pace:

| Struck            | Strength | Plain           | Fitted         | Axe (2 a swing, 0.5 s) |
| ----------------- | -------- | --------------- | -------------- | ---------------------- |
| soil              | 4        | 4 hits, 0.4 s   | 2 hits, 0.2 s  | 2 swings, 1 s          |
| stone             | 12       | 12 hits, 1.2 s  | 6 hits, 0.6 s  | 6 swings, 3 s          |
| alloy (ruins)     | 60       | 60 hits, 6 s    | 30 hits, 3 s   | 30 swings, 15 s        |
| hull section      | 15       | 15 hits, 1.5 s  | 8 hits, 0.8 s  | 8 swings, 4 s          |
| asteroid          | 120      | 12 s            | 6 s            | 30 s                   |

A battery, 1800 power-ticks: 600 ticks (30 s) of the plain drill's 3, 300 ticks (15 s) of the fitted 6. Soil, most of a
planet's ground, breaks in under half a second with either, about as fast as the bit moves from one cell to the next, so
the halving shows only on stone, alloy and wrecks. `DrillArcTests` checks the hits for soil, stone and alloy and the
battery ticks.

## Deliberately not handled

- **A friend's idle fitted drill looks plain** where its peer's inventory is not yet the sim's: the figure reads the sim's
  slots, so it shows once the move that brought it has applied, as every friend's move does.
- **Two drills carried:** the fitted one drills, draws and is drawn in hand, whichever is chosen.
- **The arc halves a battery's time** on the drill: twice the draw, as it doubles a station's. The player feels it.
- **No sound for the crackle**, as 0086: a sound goes through the user's audition.
- **Drones carrying a fitted drill:** they fetch what an input keeps, and none keeps a tool; their cargo would keep it.
