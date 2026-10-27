# Every item is held in one hand

- Status: accepted; built
- Date: 2026-10-27
- Supersedes: [0078](0078-two-handed-items-are-held-as-a-rifle.md)

## Context

The drill and the railgun were held as rifles (0078): both hands on their grips, the item turned at a keyed pivot before
the chest, both arms solved to reach it. The user found the hold too elaborate for what it gives, and with the new run
(an item carried trailing behind on a spring, `HeroMotion.Carry`) and the cape over the arms it was the one hold left
that moved unlike the rest.

## Decision

- **Every item is held in the front hand by its `Grip`.** An aimed one (the harpoon, the nailer, the drill, the railgun)
  turns with the arm at the shoulder so its edge's line runs through the pointer (`HarpoonShot.ArmFor`), the elbow bent
  by the item's `HeldLook.Elbow`: 0.4 radians for the drill and the railgun, straight for the rest. The arm and the item
  turn as one piece, so the aim and the edge come from the same arm (`AxeSwing.ArmAt`, `EdgeAt`).
- **The far arm is free:** it swings and pumps with the pose.
- **One hold for the hit test and the drawing**, as before: the drill's bit is traced from the same arm
  (`Aim.BitOf`), so what sparks is what is drawn.
- `TwoHand`, `TwoHands` and `HeldLook.Both` are gone.

## Trade-offs

- **The drill reaches about a cell further:** held at arm's length rather than before the chest, its bit ends some
  a cell past where the two-handed hold's did, and it hides more of the cells right before the hero that it digs, the
  problem 0078 set out to solve. Accepted for the simpler hold; the bend of the elbow is the number to turn if it bites.
- **No crouch or brace for a heavy gun:** the railgun is held as the harpoon is.
