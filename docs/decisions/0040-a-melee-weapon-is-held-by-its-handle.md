# 0040 — A melee weapon is held by its handle

- Status: accepted
- Supersedes: of [0039](0039-melee-by-contact.md), the reach as content (`Axe.Reach`, 2000 thousandths
  of a cell, the arm and the axe end to end) and the blade's width as a number (`AxeSwing.Edge`); the
  sector they bounded
- Date: 2026-10-16

## Context

The hero held the fire axe by the very end of its haft, the haft carrying on from the forearm in a
straight line: an axe held by the fingertips. What it hit was a number tuned by eye beside the
picture, two cells from the shoulder, and the blade drawn never came near some wrecks it cracked. A
second melee item would have needed a second guess.

## Decision

These are the rules for every item swung in the front hand; the fire axe is the first.

- **Gripped on the handle, not at its end.** The held picture has a grip point where the fist closes:
  for the axe a third up the haft, two pixels of it showing below the fist (`HeldLook.Grip`).
- **Held at an angle to the forearm.** The handle leaves the fist turned back from the forearm by one
  angle per item (`HeldLook.Hold`): square, a quarter turn, for an axe, a hammer or a crowbar, held as a
  hammer is, the head standing up from the fist and its blade facing along the forearm, ahead. Hanging,
  the haft points ahead and the blade down; swung, the head leads.
- **The fist over the handle.** The grip sits on the middle of the glove (`Rig.FistBack`, a pixel back
  from the forearm's end), and the forearm is drawn over the item, so the hand wraps it.
- **Swung with a straight arm.** Through a swing the whole arm turns about the shoulder as one
  piece, the elbow never bent (`AxeSwing.ArmAt`): the fist at the arm's end, the item at the arm's
  angle and the hold. A bent elbow aimed by reaching for a point folded the forearm into angles no arm
  makes, raised and striking; at this scale a straight arm reads as well and cannot.
- **Eased in and out, never a jump.** The swing is still at either end (`AxeSwing.Lift`: wound up and
  eased back on halves of a cosine). Where a swing begins from the arm's resting pose, ends into it, or
  is followed by the next toward another point, the arm drawn the frame before is kept as an offset
  from the arm wanted, and that offset eases out (smoothstep) over the wind-up's length, 175 ms
  (`SwingArm`, the same for every figure, others' too). So the downstroke is always the swing's own
  arm, and the blade drawn striking is the one the contact tests. Hanging, the arm keeps its pose's
  natural bend.
- **What it hits comes from the picture.** The item marks its working edge, two points of its picture
  (`HeldLook.EdgeFrom`, `EdgeTo`; for the axe the bright steel's outer line). The figure and the contact
  hold it the same way (`AxeSwing.EdgeAt`: the shoulder, the straight arm's angle, the fist at its
  end, the grip, the hold, the edge). A swing turns the arm past the line to its point by the angle
  that puts the edge's middle on that line as it strikes (`AxeSwing.Lead`), so the blade meets what it is
  aimed at, not the hand. Contact is the path the edge sweeps over the downstroke, from crossing the line
  to `AxeSwing.Through` past it, sampled every 2° and tested as segments against each wreck's aim box
  (`Aiming.Struck`). No reach of its own: a new grip, angle or picture moves the reach with it.
- **Art data in the client:** `game/Content/HeldLook.cs`, one record per held item (the fire axe the only
  one, principle 2); the sim keeps `Axe.Damage`, `Axe.SwingTicks` and `Hit(Piece)` as they were.

## Consequences

- The axe reaches 1.49 cells from the shoulder straight ahead (about 1488 thousandths), down from 2:
  the straight arm's 0.70 to the fist, the haft square to it, the blade's toe; the edge's near end is
  1.17 out. (Bent by reaching for a point, the arm gave 1.18 and 0.95.)
  Chopping means floating close to the wreck, nearly touching it.
- The arm in a swing points about 35° below the line to its point while the blade crosses it (52°
  with the bent arm): the blade, not the fist, is aimed.
- Held square, the blade's edge runs along the haft and so mostly along its own path: the band it
  sweeps is narrow, an arc from 1.17 to 1.49 cells from the shoulder. Nothing nearer is touched; a wreck
  is three cells long, so a hero that near is inside its aim box, which the band crosses.
- The lean and a wreck's rocking are still ignored (0039): the contact is the upright body against the
  still picture.
- Others see the same grip and swing, eased the same way; nothing on the wire changed.
- A click at a wreck a cell and a half off, out of reach of a hand that bent, now bites.
- An arm turned to the swing from a bend that changes quickly meanwhile (running) may, while the offset
  eases out, bend at the elbow less than the pose would; never back past straight.
- The screwdriver and the planner's sheet keep their own holds; they swing at nothing.
