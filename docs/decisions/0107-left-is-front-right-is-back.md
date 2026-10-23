# 0107 — Left is the front, right is the back; the menu plans, the hands build

- Status: accepted; its cards, removal card and the weapons on a ship's back walls superseded by [0108](0108-left-plans-right-removes.md); amends [0030](0030-the-build-tab-is-the-mode.md) (the right button works plans, the middle removes) and
  [0077](0077-a-planets-ground-grows-from-its-seed-and-is-dug-by-hits.md) (only natural walls are dug)
- Date: 2026-10-23
- Design: agreed by both developers in chat; no design file

## Context

Each button meant something else in each mode: the right one built plans with the menu open and breathed a canister, the middle one
removed, the axe and the drill knew only the front layer, and a back wall had cards of its own beside its wall's. With 8 px cells
(0104) players work back walls often (planets' ground, a ship's rooms), and the controls had to be relearned per tool.

## Decision

Two rules, everywhere.

**1. The build menu plans; the hands do.**

- The open menu only places plans and marks removals. It never builds.
- The screwdriver is a real item (`screwdriver`, `Content.Screwdrivers`), a gear tool like the axe: every hero comes aboard with
  one in its first hotbar slot (`Inventory.Kit`, given by crew the first time a peer has a hero, `IItemsOps.Kit`). Held, the left
  button on a plan builds it and on a mark takes the thing apart, at the hero's work as before: `Screwdriver.Work` ticks of work a
  tick held (1 for the basic one), the client's pace as a weapon's is (0034), so a better screwdriver later is a row of data. It is
  crafted at the lifepod of scrap, should one be lost; dying keeps it (gear).
- The inventory's Hands slot goes. Saves stay format 28: it is unreleased, and a save made before has heroes without the screwdriver,
  which the lifepod makes.

**2. The left button is the front layer, the right the back** (`Layering.Back`: the right alone; both held, the front).

- A material's card (`PlaceTool.Back`) plans its wall on the left and the back wall of the same material on the right; the separate
  back-wall cards are gone, their blueprints unchanged. Doors, ladders, floors, pipes and machines are front only: the right does
  nothing with them.
- The removal card, first on every tab, replaces the middle button: the left marks or cancels in the tab's front layers
  (`Tools.FrontLayersFor`), the first thing under the drag's start choosing the kind as before; the right marks or cancels back walls
  only, those behind machines too. On the power tab its left press near a cable cuts it.
- The axe and the drill strike back walls on the right with the same brush and damage: `Dig(Cell, Tool, Back)`. A back wall of any
  material breaks at its material's strength (ship hull 2 and 5, soil 4, stone 12), keeps a crack of its own beside its wall's
  (`Crack.Back`), gives its material's cost to the digger and fells what hangs on it for them. Bedrock and lava never break
  (`building.bedrock`); no back wall is `building.nothingToDig`. A ship's back wall broken opens its room to space: intended.
- A canister's right click still breathes it; nothing else held used the right button.

**Seen without words.** While the right button is held the walls, edges and doors within three cells of the pointer fade to 35%
(`Lit.SeeThrough`), so the back wall worked shows. The aim's outline is solid for the front and dashed a pixel inside for the back.
A material's card shows a dimmed square of its back wall behind its wall's.

**The menu** shows 10 by 2 cards a step of the art larger than a slot (48 px at the normal slot, 64 at the large), in the frame
three rows took; its tabs stay (structure, air, power, production, storage) and count known cards but the removal.

## Rejected

- **A separate `DigBack` command:** the same validation, apply and fuzz with one flag between them.
- **The sim checking the screwdriver is held** for `Work`: weapons are not checked either (0034); a client without one sends nothing.
- **Keeping the middle button as a shortcut:** two ways to remove, and the middle button is the dev camera's pan.

## Accepted limitations

- A friend's swing is drawn hitting the front layer: which button began it is not sent (`Seen` carries no layer).
- A hull back wall breaks silently, its chips grey: no metal break sound is picked for it yet.
- A save made before this has no screwdriver for its heroes until the lifepod makes one.
