# 0058 — One place for every machine window; equipment at the right edge

- Status: accepted
- Date: 2026-10-18
- Supersedes in part: [0047](0047-equipment-the-jetpack-and-the-slower-swim.md) (the block right of the
  inventory's grid)
- Mockup: "Tab and Machine Windows", option A (published as an artifact; approved)

## Context

Machine windows opened in two places. The queued stations (the Fabricator, the Former) docked to the right
of the inventory; crates, the furnace, the smelter, the burner generator, the analyzer and the charging
station opened under it, each in a panel edged its own colour. The equipment sat between the grid and a
docked station. The screwdriver's display-only slot sat left of the hotbar, so the player read it as the
first slot and pressed 2 for the axe in slot 1. Slots were 36 px with 28 px icons, the 16 px art at ×1.75,
so its pixels came out uneven; the chosen hotbar slot's amber edge would vanish on an amber tool.

## Decision

- **Every machine opens under the inventory's grid,** level with its left edge, in one frame
  (`Shared.MachineFrame`): the same panel, a header with the machine's name in bold and, for one on the power
  grid, a bolt and its power — lit while it works, dark at 0 while it does not (a generator shows what it
  makes) — then the machine's own body, unchanged. The items feature builds its machines' frames; the
  crafting feature docks a queued station's frame in the same place, read from `Ui.InventoryFoot` (the grid's
  left edge and bottom), which replaces `Ui.InventoryEdge`.
- **A queued station's body is two columns:** its recipes on the left, the shared queue and the outputs on the
  right, so the whole Tab screen fits 1280×720 at the large slot size.
- **Equipment at the screen's right edge,** Terraria's place: head, back, belt, then a new row, **Hands**, with
  the screwdriver, display-only as before (0030), with its tooltip and no "RMB" label. Its top is level with
  the inventory's grid; it shows only while the inventory is open.
- **Hotbar digits:** each slot shows the key that chooses it, 1 to 9, small in its top-left corner, open or
  closed. The violet "?" of an item not analysed moves to the top-right corner, in every slot and in the hand.
- **Slots are tinted by what they hold** (`Slots.TintOf`, painted by `Shared.SlotLooks`, one definition with
  the Fabricator's tool recipes): a tool (`ItemCards.IsTool`) amber, a worn item (`ItemSpec.Equip`) the
  equipment's green, anything else plain grey. Under the pointer a tinted slot's edge goes full amber or green.
  The chosen hotbar slot is edged 2 px near-white. Equipment slots keep their own look.
- **One slot size,** `Ui.SlotScale`, read by every slot: Normal, 40 px with 32 px icons (×2), or Large, 56 px
  with 48 (×3). The suit's and the energy gauges and the inventory are laid out from it (`Slots.GaugeTop`,
  `Slots.InventoryTop`). Esc → Settings sets it; it applies at once and is kept in `user://settings.cfg`
  (`Platform.Settings`).

## Rejected

- **Machine windows right of the inventory** (option B): a wide station and the equipment would fight for the
  same space, and the eye would look in two places again.
- **An amber edge for the chosen slot kept:** it disappears on a tool.
- **44 px slots:** the icon would still be ×2 with wider margins; 40 is the tightest whole multiple.
- **The Fabricator's single column at the large size:** it overran 720 px.

## Consequences

- A queued station's bolt reads 0 while it rests, as the charging station's always did.
- The build menu's cards keep their own 48 px size: they show blueprints, not item slots.
- `--slots=large` shows the large size for a recording without keeping it.
