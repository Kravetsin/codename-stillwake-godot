# 0078 — Two-handed items are held as a rifle

- Status: accepted
- Date: 2026-10-20
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
- **It is held across the chest and turned there** (`game/features/crew/TwoHand.cs`): the pivot sits 2 px ahead of the
  shoulder and 8 px under it, so the drill's tube runs across the middle of the chest and its bit ends a cell and a half
  from the shoulder. It turns so its edge's line runs through the pointer, at most 0.6 rad (about 34°) up and 0.6 down;
  past that it points as far as it turns. The pivot moves 3 px a radian forward aimed up and 3 px a radian up aimed down,
  which keeps both grips in reach.
- **Both arms reach for the grips** with the rig's own two-bone solve (`Rig.Reach`): the near fist on the grip, its
  elbow back as a trigger arm's; the far fist on the foregrip, drawn behind the body and the item. Over the whole clamp
  both fists close on their grips, no arm crosses the helmet and no elbow rises above the shoulder (`TwoHandTests`).
- **One hold for the hit test and the drawing:** `Aim.Touched` traces the bit of the same hold, so what sparks is what is
  hit. A friend's hero shows the hold derived from what it sends: aimed at the point it drills while it runs, else resting
  0.3 rad down.

## Why these numbers

The arm reaches 11.2 px to the fist's middle and the grips are 9 px apart, so the hold has little room: a pivot lower or
further out than this leaves a grip out of reach somewhere in the clamp. Up past 0.6 rad the drill's chuck crosses the
helmet and the near grip swings out of reach; down past it the near elbow rises above the shoulder, the far grip leaves
reach, and the bit of a 33 px drill held at the chest heads for the floor. They were found by a search over pivots and
clamps against those limits, then checked on a contact sheet (`-- --pose-sheet=<dir>`).

## Deliberately not handled

- **Aiming steeper than the clamp** points the drill only as far as it turns: digging straight down a shaft shows it
  aimed 34° down. The dig goes by the pointer's cell (0077), so nothing is lost but the look.
- **The near and far shoulders stay one point**, as the rig draws them; a bladed stance would need a second shoulder.
- **The hit test assumes the body upright**, as the axe's does; a hero leaning afloat draws the hold turned with it.
- **The far hand is mostly hidden** behind the item, as a rifle's support hand is from the near side; no picture of it
  wrapping the foregrip.
