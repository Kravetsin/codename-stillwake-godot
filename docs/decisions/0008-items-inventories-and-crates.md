# 0008 — Items, inventories and crates replace the shared stock

- Status: accepted; sorting the bag or a crate added 2026-10-18 (below)
- Date: 2026-10-11

## Context

P1's `stock` module is a set of counters for the whole ship: no place, no capacity, paid from
anywhere. With debris to salvage, things must be carried, stored and fetched, which is also the road
to automation moving items between storage and machines.

## Decision

- **One `items` module owns every stack:** each hero's inventory, by peer id, and each crate's
  contents, by machine id; creative mode lives there too. One module, because moving a stack between
  an inventory and a crate must be one atomic change of one slice.
- **A catalog of items** in content, keyed by a saved string id that is never renamed, each with a
  stack limit; salvage items also say what they dismantle into and how long it takes.
- **Building and loading canisters pay from the sender's inventory.** Refunds go to the sender, and
  are refused when they do not fit: nothing is ever lost.
- **Crates are machines with a fixed number of slots.** Items move between an inventory and a crate
  by command, Terraria's way: a whole stack, half of one, a swap, or into the first stacks and slots
  that take it.
- **The hotbar is the inventory's first row**, chosen with keys 1–9. Build tools are not an item: the
  builder is at hand whenever the selected slot is not a salvage piece.
- **`validate` sees the sender,** as `apply` does: whether a command can be paid or fits depends on
  who sent it.

## Rejected

- **Separate inventory and crate modules:** a move between them would cross two slices.
- **Keeping the shared stock beside inventories:** a magic store with no place leaves nothing to
  carry or automate.
- **A hammer item for building:** the sim would not check it, and a hammer left in a crate would hide
  the builder with no explanation.
- **Lasting player ids now:** they come with Steam.

## Consequences

- A hero's inventory belongs to its peer id: after a load it goes to whoever joins in that slot, and
  a peer that rejoins mid-session starts empty under a new id.
- Every refusal about items has an i18n key; the UI shows counts and icons, names on hover only.

## Amendment, 2026-10-18: sorting the bag or a crate

Taking from different stacks leaves half the inventory in half-empty stacks. Terraria's answer is a sort button,
and it is ours.

- **One command, `Sort(Crate?)`,** for the sender's bag or a crate. Stacks alike (one item, one charge) merge up to
  their limit; then they are laid from the first slot by a stable key: the item's group, then its id, then charge,
  fullest first, then count. Every item and every charge is kept: batteries holding other charge never merge.
- **Groups are data** (`ItemSpec.Group`): tools, wearables, materials and parts (the default), salvage, the rest
  (what is never carried but in creative mode, and the drone). A content test ties tools and wearables to `Gear`
  and `Equip`, so a new tool sorts with the tools.
- **The hotbar and the equipment are never sorted,** as in Terraria: the hotbar is laid out by hand for keys 1–9,
  and what is worn is in use. Only the bag rows are.
- **Only a crate sorts.** A machine's slots have their places, inputs, fuel and outputs, and a sort would break
  them; `items.notCrate` refuses one, `items.noCrate` a machine that is not there.
- **No reach in the sim,** as for a move: the client shows the button only on a crate it opened within reach.
- **Co-op:** two sorts of one crate in one tick apply in the host's order; the second finds it sorted. The client
  shows its own sort at once (0014), lets go of a stack it picked as it sorts, and lets go of one whose slot comes
  to hold another item, by a friend's sort or swap at a shared crate.
- **The button** is a small square drawn in code, three bars and an arrow, its name in the tooltip: in the top-right
  corner of the bag, and in a crate's header beside the drone badge. No key sorts.

Rejected: sorting the hotbar too (it is the hands' layout); a sort that only merges and does not reorder (half the
point is finding things); per-player sort settings (no one has asked).
