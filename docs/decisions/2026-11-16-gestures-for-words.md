# Gestures, in place of words

- Status: accepted
- Date: 2026-11-16

## Context

There is no voice or text chat, so a player cannot tell the friend beside them "here", "come", "look there", "yes" or "no".
They could only jump or walk at each other.

## Decision

- **One key, a wheel.** The bind `Gesture` sits on F. A tap (released within `GestureWheel.TapMs`, 200 ms, inside the
  middle) points at the pointer. A hold opens a wheel round the press point (`GestureWheelView`), its turn shared out
  evenly: yes above, no below right, wave below left. Each slot shows the hand's own frame, enlarged. Letting go picks the
  slot by the pointer's direction; letting go in the middle cancels. A quick flick picks without the wheel showing. While
  the wheel is open, clicks do not use the item.
- **Four gestures, as keyframe data** (`game/Content/logic/GestureMotions.cs`). Each has a length, a hand frame, and keys
  of shoulder, elbow, wrist and head nod by share. Some keys are sampled from a curve (`Sampled`). The user picked each one
  from drawn candidates (2026-11-16):
  - point: the arm straight along the line to the target, two short jabs, held;
  - wave: overhead, the forearm rocking three times;
  - yes: a thumb up with two nods;
  - no: a thumb down, the palm's side shown (the far hand's picture, mirrored), pumped.

  Each is blended in and out over `Gestures.BlendMs` (180 ms), on the near arm only. Legs and body are untouched.
- **The held item is stowed.** While a gesture plays, the item is not drawn and the hand shows its frame. Using the item,
  busy hands, the plan out, a ladder, a seat or passing out ends it (`Gestures.CanStart`). Walking, jumping and floating
  keep it.
- **Client only, sent as hands' work.** `Handwork` gains a value per gesture. The sent `Busy` carries it: `At` is the
  point's target, and `Swings` counts gestures, so the same one twice is news. A friend's view (`GesturePlay.Hear`) plays
  it once per new count, on its own clock, and other work cuts it. Nothing enters the sim: a command would add a tick's
  delay and a module for state nothing reads.

## Considered

- **A key per gesture**: four binds for four signs, with no room for more. The wheel is one key.
- **Keeping the item in hand**: the pointing finger read only with empty hands, and an axe overhead hid the wave.
- **Shoulder and elbow only, no wrist**: "no" read as a twisted arm until it showed the palm's side and turned the wrist.

## Not handled

- Sound: a gesture is seen, and most play is in vacuum.
- Friends off screen get no pulse on the edge markers. A friend joining mid-gesture misses it: it lasts about a second.
- A "wait/stop" palm reads like pointing at this scale; "no" covers it.
- "Come here": its scoop stood out from the rest as the worst made and was dropped after play; a wave, or a point at
  the spot, covers it.
- Whole-body emotes, legs, the far arm. Rebinding wheel slots, a queue of gestures, pointing that follows a moving thing.
- On a friend's screen a gesture can run up to one send longer or shorter.
