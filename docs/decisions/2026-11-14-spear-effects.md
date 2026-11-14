# The spear's effects

- Status: accepted; amends [blade-trail](2026-11-10-blade-trail.md) for the spear
- Date: 2026-11-14

## Context

The spear, drawn raw ([salvage-tools-raw](2026-11-14-salvage-tools-raw.md)), had nothing on its thrust, and its arc module
([0098](0098-the-tool-bench-modules-in-tools.md)) showed nothing at all: the arc crackles only on a spinning drill or a
charging gun, and blade-trail leaves aimed tools, the spear among them, without a trail. The user picked the effects on pages
drawn at the game's scale and asked for two in all: "один для удара, а второй будет просто постоянным эффектом".

## Decision

- **Plain spear, on the thrust: «Линза и вихри».** A lens of squeezed air swells before the point as the elbow snaps, holds at
  the hit and tears off as two eddies rolling back over and under the blade, spinning opposite ways. The world under it is
  moved by a smooth field per pixel (a screen shader at the shimmers' layer, under the hero, so the spear itself never bends),
  a breath paler. The first page's haze was cut in column slices and read square; the picked one is smooth everywhere.
- **Arc spear, on the thrust: «Бег по кабелю».** The coil glows as the elbow cocks and flicks an arc across its tube; on the
  snap an arc runs down the cable to the point, lighting it behind; at the hit the point spits three short arcs.
- **Arc spear, all the while it is held: lightning, not a trail.** First the picks «След за клинком» with «Ползущие дуги»
  (the user: "А можем совместить ползущие дуги и след за клинком?"); in the game the ghost read as a solid bar, and the user
  asked for more, as in Warframe: "постоянно по оружию прокатываются заряды молнии и такие же молнии когда игрок машет или
  бежит". So charges roll down the cable from the coil to the point all the while, three at once, quicker on a run, each
  an arc over its stretch with a glow at its head, spitting a spark of an arc as it leaves the point; and the moving blade
  throws branching lightning back to where three of its points were 55 and 110 ms ago, brighter the faster and the more
  level it moves, motes shed off it. A jump of the blade (a turn, a hard landing) lets the wake go, so it never spans one.
- The arc spear has no haze: its thrust is the arc's.
- All of it is the client's: light on the held stage and one shader node, keyed to the thrust's drawn ms and the spear's
  picture, as `SpearFx` and `SpearWake` say.

## Deliberately not done

- The other struck tools keep blade-trail's haze; nothing here generalises the lens to them, since none of them thrusts.
- The cable is a line of the arc picture's pixels; a new arc picture redraws it by hand.
- The charges and sparks roll from the client's clock, not the sim: friends see their own.
