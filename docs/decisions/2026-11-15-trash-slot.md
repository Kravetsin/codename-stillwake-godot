# A trash slot in creative mode

- Status: accepted
- Date: 2026-11-15

## Context

The user wanted to get rid of items, as Terraria does: one trash slot, a stack dragged in by mouse lying there until the
next replaces it. Played, it was kept to creative mode, where items are made from nothing and so cost nothing to lose.

## Decision

- **One more inventory slot, after the equipment** (`Inventory.Trash`, `Total` one more): saved and carried with the
  inventory, no new state or command. Save format 65, its step giving every inventory an empty trash.
- **A `Move` onto it replaces what it held** (`Stacks.Trashed`): the count moved goes in, the old stack is gone, never merged
  nor swapped. Out of it, a move as from any slot.
- **Open in creative mode only** (`SlotOpen`): elsewhere a move into it is refused `items.noSlot` and it is not shown. What it
  holds as a peer leaves creative waits there, unseen, till it is back.
- **Nothing but a move naming it reaches it**: costs, gifts, pick-ups, sorts, stashes and death use the own slots only.
- **By mouse only**: a stack carried and put there with the left button (a drag or a click). The right button puts nothing in,
  shift does nothing there; its stack is picked back as any slot's, never breathed nor eaten.
- **Its own frame under the bag**, flush with its right edge (the hotbar's `Cuff` metal), the bin glyph's ghost while empty
  (`MarkLooks.Trash`, the glyph page's pick A, steel), lit while a stack is carried.
- **Only a destroyed stack bursts and sounds** (`Clicks.Destroys`): a stack into a trash holding one flashes its ring and
  throws steel sparks for 0.5 s (`TrashSparks`, `TrashBurst`, the effect page's pick A), and plays a soft fwump
  (`ui/trash-1.wav`, the sound page's pick C). Into an empty trash it only clicks. The trash's, alike for every item.

## Deliberately not handled

- No keyboard way in, no confirmation and no undo past the one slot: the slot is the undo for the last stack.
- Kept (starred) items are not protected; the star is the stash's.
- No trash in crates or machines, none for drones.
- Taking a stack out of a hidden trash outside creative is not refused by the sim: the client never shows it to click.
