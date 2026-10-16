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
- **What it hits comes from the picture.** The item marks its working edge, two points of its picture
  (`HeldLook.EdgeFrom`, `EdgeTo`; for the axe the bright steel's outer line). The figure and the contact
  hold it the same way (`AxeSwing.EdgeAt`: the shoulder, the hand by the rig, the grip, the forearm's
  angle and the hold, the edge). A swing turns the arm past the line to its point by the angle that
  puts the edge's middle on that line as it strikes (`AxeSwing.Lead`), so the blade meets what it is
  aimed at, not the hand. Contact is the path the edge sweeps over the downstroke, from crossing the line
  to `AxeSwing.Through` past it, sampled every 2° and tested as segments against each wreck's aim box
  (`Aiming.Struck`). No reach of its own: a new grip, angle or picture moves the reach with it.
- **Art data in the client:** `game/Content/HeldLook.cs`, one record per held item (the fire axe the only
  one, principle 2); the sim keeps `Axe.Damage`, `Axe.SwingTicks` and `Hit(Piece)` as they were.

## Consequences

- The axe reaches 1.18 cells from the shoulder straight ahead (about 1183 thousandths), down from 2:
  the arm's 0.63 to the fist, the haft square to it, the blade's toe; the edge's near end is 0.95 out.
  Chopping means floating close to the wreck, nearly touching it.
- The arm in a swing points about 52° below the line to its point while the blade crosses it: the
  blade, not the fist, is aimed.
- Held square, the blade's edge runs along the haft and so mostly along its own path: the band it
  sweeps is narrow, an arc from 0.95 to 1.18 cells from the shoulder. Nothing nearer is touched; a wreck
  is three cells long, so a hero that near is inside its aim box, which the band crosses.
- The lean and a wreck's rocking are still ignored (0039): the contact is the upright body against the
  still picture.
- Others see the same grip and swing; nothing on the wire changed.
- The screwdriver and the planner's sheet keep their own holds; they swing at nothing.
