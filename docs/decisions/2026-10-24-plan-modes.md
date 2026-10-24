# The plan has modes: building, wiring, piping

- Status: accepted; supersedes the stripper of [0108](0108-left-plans-right-removes.md), the wrench of
  [0109](0109-pipes-are-spans.md) and the hand tools' menus of [0110](0110-the-plan-has-a-focused-layer.md)
- Date: 2026-10-24

## Context

Cables were laid with the stripper (0108) and pipes with the wrench (0109): items in every hero's kit, each opening a
menu of its cards on the build tablet when held with the plan away (0110). So the player had three ways into one
tablet, two of them through the hotbar, and two hotbar slots spent on tools that did nothing but switch it. The sim
never asked for either item: `LayProblem` checks only that the hero is aboard and carries the cost.

## Decision

- **The stripper and the wrench are gone**, as items, crafts, research and kit. `Inventory.Kit` is the screwdriver alone.
- **The open plan has three modes** (`PlanMode`, `PlanHand.Mode`): building, wiring and piping, picked by Z, X and C or by
  three keys on the tablet's left edge, mirroring the layers' keys on its right (`PlanKey`), the mode's lit.
- Wiring and piping are what the stripper and the wrench were, unchanged: their cards alone on one tab (cable; pipe and
  valve), their overlay, the left laying and the right cutting, paid from the inventory as laid. Building is the plan as
  it was; only it has tabs, search, layers, the middle button and Q.
- Leaving building puts the card in hand aside, so wiring draws no ghost; it is back in hand on return. The mode is kept
  while the plan is away, as the tab is. Away from the ship the plan only builds and the modes' keys hide.
- Format 30: no save before it loads (`Migrations.Oldest`), every hero having carried both tools and nothing released.

## Deliberately not handled

- The modes' keys have no tooltip; the hint line names Z X C.
- No hotbar slot opens the plan in a mode: B opens it in the last one.
