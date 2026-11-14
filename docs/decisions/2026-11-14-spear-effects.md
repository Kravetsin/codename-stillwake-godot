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
- **Arc spear, all the while it is held: «Срыв разряда».** Tried first and dropped: a solid trail behind the blade (read as
  a bar), arcs thrown back from it (ropes waving in the wind; the user: "там это отрывающиеся кусочки дуги"), charges rolling
  down the cable and pieces of equal length ("выглядит теперь как конфетти"), and a quick flicker at rest. The user then
  asked for no idle animation of its own, sprouts of lightning on the blade that fly off as it moves, built on "одна область
  произвольной формы, которая генерирует эффект" rather than a few points, so that paid effects later share it. So:
  - **One area** (`EffectArea`): the drawn pixels of the held picture, modules laid over, inside the module's crackle box;
    its inside and its outline with the way out. An effect draws its places from it, never from fixed points.
  - **At rest** the area only fizzes: tiny sparks, under a world pixel or so, here and there over it, calm.
  - **Moving**, it builds a charge by how far its middle goes; past a threshold drawn anew each time the charge lets go in a
    burst of a few bolts over some frames, torn off its trailing edge and flung back, left in the world where they tore. Their
    lengths follow a power law, 1 to 12 px: most short, now and then a long one, forked. A still blade's charge leaks away; a
    jump of the blade (a turn, a hard landing) is no motion.
  - **A torn bolt is steady** (`Bolt.Steady`): its shape rolled once and held, never re-rolled every two frames as other
    arcs are; it dies by being eaten from both ends, its forks from their tips, bright until its last 30%.
- The arc spear has no haze: its thrust is the arc's.
- All of it is the client's: light on the held stage and one shader node, keyed to the thrust's drawn ms and the spear's
  picture, as `SpearFx` and `ArcBurst` say.

## Deliberately not done

- The other struck tools keep blade-trail's haze; nothing here generalises the lens to them, since none of them thrusts.
- The cable is a line of the arc picture's pixels; a new arc picture redraws it by hand.
- The sparks and bolts roll from the client's clock, not the sim: friends see their own.
- One area an effect: a tool with two crackle boxes uses the first; none has two yet.
- The burst is the arc's on the spear only; the drill and the guns keep their crackle until a paid effect needs the area.
