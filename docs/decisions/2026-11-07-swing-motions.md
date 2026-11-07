# Swing motions

- Status: accepted; supersedes in part [0039](0039-melee-by-contact.md): a stroke's aim is kept against the hero's place, not the world
- Date: 2026-11-07

## Context

Every melee stroke was one straight arm turning at the shoulder (`AxeSwing.Lift`), the same for every weapon and every aim;
aimed up or under the feet, its shape was only cut short. A stroke aimed at a fixed point of the world (0039), so a hero who
drifted or was knocked mid-swing had its arm re-aim at that point every frame and twist. More weapons are coming, and one that
moves differently meant new code each time.

## Decision

- **A motion is apart from a weapon** (`SwingMotion`, data in `game/Content/logic/SwingMotions.cs`; played as `MotionPlay`,
  with the shaped `Overhand` and the `Jab`, in `game/features/crew/logic/MotionPlay.cs`): a weapon's row (`Stroke.Motions`)
  names one motion for each **aim sector**, as Terraria's items name a use style. A new weapon that moves like an old one is a
  row of data. A motion is keys of `(Share, Shoulder, Elbow, Ease)`, the shoulder relative to the aim line, every motion at
  rest at shares 0 and 1, so a held stroke runs into the next whatever its sector. A motion swings or thrusts (`MotionKind`):
  the stroke's kind is the kind of the motion its press chose.
- **Four sectors** (`Sectors`), by the aim angle from the front shoulder in the arms' frame, without the item's `Lead`: up to
  −45°, to 20°, to 60°, to 90°; a bound goes to the smaller angle; an angle behind is mirrored to the front. A fixed-key motion is
  turned to the exact aim within half its sector.
- **Chosen once at the press**, sector and motion, and kept with the stroke (`AxeSwing.Play`): moving the pointer or the body
  mid-stroke never switches motions. The angle comes from the figure, faced the way the stroke makes it face
  (`Figure.AimAngleOf`), since only it knows the arms' frame.
- **Today's two strokes ported exactly:** the overhand's keys are built at the press from `Overhand.ShapeFor` and reproduce the
  old `Lift`; the jab keeps the spear's elbow curve (`Strokes.ElbowAt`) and its shaft aimed through the point. Pace, lengths,
  `HitShare`, the 50 ms arm stop on contact (0075), the sounds and the sim are unchanged.
- **The aim travels with the hero:** `AxeSwing.Aim` is the offset from the hero's place at the press, re-taken at every stroke
  of a held run, in the world's axes. The arm aims at the hero's place now plus it; the fan is cast from the hero at the strike
  toward the same offset. A zero offset aims straight ahead the way the hero faces.
- **The wire is unchanged:** `Busy.At` is still the world point at the press. A friend's body is drawn 150 ms behind while its
  swings play as heard, so its offset is taken from the hero sent in the first packet that brought the swing's number
  (`Busy.Hear`), and while it swings its figure faces the offset's side.

## Consequences

- A hero pushed off mid-swing keeps striking the same point relative to itself; the old twist is gone.
- New motions (step 2 of the plan) are rows of keys, one pick each; only the overhand is shaped by its aim.
- A friend's offset is exact up to the 50 ms between its packets: a running friend's stroke may be a fraction of a cell off.
  It is only drawn; their hits are their own client's.
- A friend's place comes from the first packet with the swing number, up to about 50 ms after the press, and its angle from
  this screen's figure of it: near a sector bound another screen may draw another motion. We accept it: it is only drawn.
- A body turning under the stroke (afloat) does not turn it: the offset keeps the world's axes.
- No combos, no wrist, no torso lean: one motion per sector, each added with the first motion that needs it.
