# 0108 — In the plan menu the left button plans and the right removes; the stripper lays cables

- Status: accepted; supersedes the card and removal parts of [0107](0107-left-is-front-right-is-back.md) (its rule 2 for
  the menu, the removal card, the axe and drill on a ship's back walls); 0107's screwdriver stands
- Date: 2026-10-23
- Design: agreed by both developers in chat; no design file

## Context

0107 gave a material's card its wall on the left and its back wall on the right, and a removal card on every tab. Players
asked for one rule in the menu instead: the left builds, the right takes away. The axe and the drill breaking a ship's back
walls (0107) let a swing meant for the ground open a room to space; the ship should come apart only on purpose. And the
power tab mixed cards with laying, carrying and cutting cables, with an overlay of its own.

## Decision

**1. The plan menu: the left button plans, the right marks for removal** (`Tools.ActOf`).

- Back walls have their own cards again, one per material (`backWallScrap`, …, "Back wall: scrap"); a material's card plans
  its wall only. The removal card is gone.
- The right button, by click or drag, marks or cancels in the selected card's layer: with a back wall's card, back walls only,
  those behind machines too; with any other card, the front layers (`Tools.FrontLayersFor`: under the pipes overlay pipes,
  valves and machines, else machines and structure), the kind of the first thing under the drag's start deciding what is
  taken, as before.
- While a back wall's card is in hand and aims at the world the front layer fades round the pointer (`Lit.SeeThrough`), as it
  does while the right button digs back walls with no card.

**2. The screwdriver** builds and takes apart only what the menu planned or marked (0107, unchanged).

**3. Weapons never break a ship's walls** (`building.builtWall`). A `Dig` on a wall or a back wall of a material that is not
natural (`Material.Natural`: the hull's scrap and iron) is refused, front or back. Soil, stone and the ruins' alloy, grown
or laid, are still dug, and bedrock is still `building.bedrock`. A ship is taken apart only by a mark and the screwdriver.

**4. The stripper** (`stripper`, `Content.HandTools.Stripper`), a gear tool every hero comes aboard with beside the
screwdriver (`Inventory.Kit`), crafted at the lifepod of a scrap and a wire.

- Held, the cables' overlay shows (`Tools.OverlayShown`); put away, it hides. The plan menu's power tab keeps its machines only,
  with no overlay and no cable card.
- The left button lays a cable from the node pressed to the one released on, paid from the inventory (`Power.CableCost`), led
  on through supports past its reach, as the cable card did. With Ctrl held at the press, a peg or a plug is carried instead,
  as an empty hand did on the power tab.
- The right button cuts the cable nearest the pointer (within `PowerHands.CutReach`), refunding it (`Cut`).

## Rejected

- **Keeping a removal card beside the right button:** two ways to do one thing.
- **Laying cables as plans built by the screwdriver:** a cable is one piece laid in one stroke; a plan would only add a step.
- **The sim checking the stripper is held** for `Lay` and `Cut`: weapons and the screwdriver are not checked either (0034, 0107).
- **A press on a peg carrying it, elsewhere laying:** a second cable from a peg could not be laid; Ctrl chooses.

## Accepted limitations

- A hero who already came aboard before this has no stripper until the lifepod makes one (saves stay format 28, unreleased).
- The stripper's icon is a placeholder, drawn by `tools/ship-art/hand-tools`; the hero holds no picture of it yet.
- A soil, stone or alloy wall laid aboard is still dug by the axe: it is ground, whatever it was laid on.
