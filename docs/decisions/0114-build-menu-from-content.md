# 0114 — The build menu is laid out from content

- Status: accepted; supersedes [0113](0113-machine-traits.md)'s limitation that the build menu lists its cards by kind
- Date: 2026-10-24

## Context

0113 made a new machine whose behaviours exist a row of `Machines.All`, its art and its name, "plus its build-menu card":
`Tools.Tabs` in `game/features/building/Tools.cs` listed every machine card by kind, tab by tab, in code. A new machine
still touched the client to appear, and the order of the menu lived apart from the rows it lists.

## Decision

- **A card is a row with a menu slot.** `MenuSlot(Tab, Order)` (`sim/Content/Menu.cs`) sits on the row of what a card
  places: `MachineSpec.Menu`, `Build.Menu` (the build rows beside the structure specs), and a material's `Menu` for its
  wall and `BackMenu` for its back wall. A row with no slot has no card: the lifepod, bedrock, lava, plaster.
- **Tabs are content too.** `MenuTabs.All` is the tabs' order; a slot names one of them.
- **Order is a number.** On a tab, cards go by `Order`, lower first, ten apart so one fits between; ties keep walls, then
  back walls, builds and machines, each in its rows' order.
- **The client only lays it out.** `Tools.TabsOf(materials, builds, machines)` builds the tabs; `Tools.Tabs` is it over
  the catalogues. Ids, labels and placements are as before (`wallScrap`, `tool.<kind>`).

A new machine whose behaviours exist is now a row of `Machines.All` with its `Menu` slot, its art and its name
(`tool.<kind>`, `tool.<kind>.about`).

## Rejected

- **One slot per material, the back wall card derived from it:** a hidden offset in code; two slots say it.
- **The order as the rows' order:** the catalogues are ordered for other readers (codes, the materials weakest first); the
  menu's order would follow them by accident.
- **Tab per trait:** a tab is a grouping for the player, not a behaviour; the generator makes air and is on the air tab
  while drawing power.

## Accepted limitations

- The hand tools' cards (the stripper's cable, the wrench's pipe and valve) stay in `Tools.CardsOf`: they are not built
  by the plan and have no row.
- A slot's tab is a string checked by a test (`BuildMenuDataTests`), not by the compiler.
- A tab's overlay (`Tab.Overlay`) stays in code; no tab has one now.
