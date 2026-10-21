# 0098 — The tool bench: modules in tools

- Status: accepted; slice 1 built (a drill carries its arc module); the bench, slice 2, next
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
  a fitted one first: with two drills the fitted one is used, whichever is in hand. `Damage(peer, tool)` is the weapon's
  damage, a gain by that stack's module, rounded down (`Boost.Gain(1, +1000)` = 2), the weapon's own while the peer
  carries none: a hit's sender was never checked for holding its weapon (0034), and the fuzz sends hits from empty hands.
  `Debris` takes it off a wreck, `Building` adds it to a crack. A run draws the tool's `Draw` a cost by the module, rounded
  up (`Boost.Cost(3, +1000)` = 6), and `items.flat` is judged on that. `Run(Item)` stays: the sim is told which tool runs,
  never which slot.
- **The fitted drill:** 2 a hit every 2 ticks, 20 a second, for 6 a tick, so a battery runs it 15 s; the ruins' alloy
  breaks in 3 s (15 of the axe, 6 of the plain drill). The cradle's shock stays paced (0096): 1 a drill hit, module or none.
- **The arc drill goes:** its item, `Weapons.ArcDrill`, `Weapons.Found` and its icon. Every weapon is crafted again. Its held
  pictures stay, as the fitted drill's look (`HeldLook.ArcDrill`, its coil and crackle, `HeldLook.Of(item, module)`). A dev
  save holding one no longer loads (accepted, as before).
- **Seen as the sim decides:** the figure is told the module of the stack its hero works with (`Figure.Fitted`, from
  `ToolStack`), so a friend's fitted drill crackles too; how a hit sounds and how much of its look it throws
  (`GroundSounds.HitAt`, `Impacts.ShareAt`) take the sim's damage for its striker. A fitted tool's slot wears its module's
  tech icon, the 0097 hexagon, as a badge in its bottom-left corner; its card draws it fitted and ends in the module's strip
  with one line, "Arc module fitted". Its tiles stay the plain drill's.
- **Save format 25, an identity step:** no save before held a module on a stack; the bump keeps an older build from
  loading a fitted drill and dropping its module.
- **Dev only, for now:** `--arc-drill` (with `--asteroid`, `--planet` or `--ground=…`) and the testbed's supply crate give a
  drill with the arc fitted. Nothing in play fits one.

## Next

- **Slice 2, the bench:** `toolBench`, 2×1, unpowered, a frame, two plates and a gear, opened by analysis; one slot taking
  only an item a module fits (`items.noTool`, a machine flag `Tunes`); `Fit`/`Unfit` gain a bench branch writing the module
  onto the slot's stack; the bench is not taken apart while it holds a tool; its panel reuses the machine panel and the
  module socket; a placeholder look; the shift-flight test covers it. Drones never target it: no recipe takes a tool in.
- **Slice 3, later:** the bench's own mark ("tunes"), DPS on the card by the module, the tool shown clamped on the bench,
  the user's mockups of badge and bench.

## Rejected

- **`Run(Slot)`** (the design's first version): a run naming its slot, with its draw and damage read from there. The sim
  would follow the hand, which is the client's word; `ToolStack` reads the same stack without a new command shape, and
  dropping saved runs from a format step.
- **A module as an item beside the tool** (`arcDrill`, one item per tool per module): many modules and tools multiply.
- **The card's DPS by the module** now: a line naming the module is enough till the bench makes fitting a choice.

## Deliberately not handled

- **A friend's idle fitted drill looks plain** where its peer's inventory is not yet the sim's: the figure reads the sim's
  slots, so it shows once the move that brought it has applied, as every friend's move does.
- **Two drills carried:** the fitted one drills, draws and is drawn in hand, whichever is chosen.
- **The arc halves a battery's time** on the drill: twice the draw, as it doubles a station's. The player feels it.
- **No sound for the crackle**, as 0086: a sound goes through the user's audition.
- **Drones carrying a fitted drill:** they fetch what an input keeps, and none keeps a tool; their cargo would keep it.
