# 0078 — Two-handed items are held as a rifle

- Status: superseded by [2026-10-27-one-arm-holds](2026-10-27-one-arm-holds.md)
- Date: 2026-10-20
- Amended: 2026-10-20, the tuned hold (variant F of `exp/drill-poses`, without its crouch): a wider turn, a keyed pivot
- Supersedes: the drill held "Terraria's way" in [0077](0077-a-planets-ground-grows-from-its-seed-and-is-dug-by-hits.md)

## Context

The drill was held one-handed, as a pistol: the arm turned toward the pointer, rising at most 0.15 rad, and the drill
turned in the fist past that. The user found it odd, and a 33 px drill at the end of a straight arm reached some two and
a half cells from the shoulder, hiding the cells right before the hero that it digs. The drill's picture already has a
pistol grip; with a foregrip it reads as a rifle. Two-handed weapons and tools are coming (a harpoon gun, an electric
spear), and they need the same ready pose.

## Decision

- **An item may be held in both hands** (`HeldLook.Both`, a `TwoHands` record): its `Foregrip` for the far hand, the
  near hand on its `Grip`, and its `Mount`, the pixel between them that rests on the body's pivot. The drill and the arc
  drill are the first; their placeholder pictures gained a small foregrip under the front of the motor.
- **It is held across the chest and turned there** (`game/features/crew/TwoHand.cs`): aimed level the pivot sits 2.5 px
  ahead of the shoulder and 8 px under it, so the drill's tube runs across the middle of the chest and its bit ends a
  cell and a half from the shoulder. It turns so its edge's line runs through the pointer, at most 55° up and 85° down;
  past that it points as far as it turns. The pivot moves between four keys (`TwoHand.Keys`, from the shoulder):
  (5, 5.5) at 55° up, (2.5, 8) level, (3, 6) at 35° down, (5.5, 3.5) at 85° down. Aimed down it rises and comes out,
  so the drill stands upright before the legs, its point at the floor, like a jackhammer; the body does not change.
- **Both arms reach for the grips** with the rig's own two-bone solve (`Rig.Reach`): the near fist on the grip, its
  elbow back as a trigger arm's; the far fist on the foregrip, drawn behind the body and the item. Over the whole clamp
  both fists close on their grips, no arm crosses the helmet and no elbow rises above the shoulder (`TwoHandTests`).
- **One hold for the hit test and the drawing:** `Aim.Touched` traces the bit of the same hold, so what sparks is what is
  hit. A friend's hero shows the hold derived from what it sends: aimed at the point it drills while it runs, else resting
  0.3 rad down.

## Why these numbers

The arm reaches 11.2 px to the fist's middle and the grips are 9 px apart, so the hold has little room: a pivot lower or
further out leaves a grip out of reach somewhere in the clamp. The first hold, one pivot moving 3 px a radian and a
0.6 rad clamp each way, could not dig down: aimed at the cells under the feet it stopped at 34°. The user compared seven
holds in game (`exp/drill-poses`) and chose F, "rifle hold, tuned": the wider turn and the keyed pivot, the drill standing
before the legs aimed down. F also crouched and leaned the body aimed down; the user had it removed, so the legs and body
stay as they are at every turn. F's own keys, (3, 8.5) level and (4, 8.5) at 35°, left the far fist up to 2.3 px short of
the foregrip; a search over pivots half a pixel apart within 3 px of F's found the nearest keys that keep both fists on
their grips, the far grip within 11 px of the shoulder, no arm across the helmet and no elbow above the shoulder over the
whole clamp (`TwoHandTests`), then checked on a contact sheet (`-- --pose-sheet=<dir>`).

## Deliberately not handled

- **Aiming steeper than the clamp** points the drill only as far as it turns: straight up shows it 55° up, the cell
  right under the feet 85° down. The dig goes by the pointer's cell (0077), so nothing is lost but the look.
- **The body never crouches or leans** for the hold: aimed down the drill's point reaches the floor before the feet and
  may cross it by a pixel or two, as an item drawn over a wall does.
- **Aimed steeply up the drill passes before the helmet**, drawn over it; only the arms are kept off it.
- **The near and far shoulders stay one point**, as the rig draws them; a bladed stance would need a second shoulder.
- **The hit test assumes the body upright**, as the axe's does; a hero leaning afloat draws the hold turned with it.
- **The far hand is mostly hidden** behind the item, as a rifle's support hand is from the near side; no picture of it
  wrapping the foregrip.
