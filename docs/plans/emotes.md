# Design: gestures

Reviewed (design-author, cut by design-reviewer); the wheel against two keys is the user's call (open questions).

## Problem
There is no voice or text chat, so a player cannot tell the friend beside them "here", "come", "look there", "yes" or "no".
Today they can only jump or walk at each other.

## Player experience
1. Put the mouse on the broken pipe and tap F. The hero points at it with the near arm, index finger out and head turned to it,
   for about a second.
2. Hold F. Four hand shapes appear around the pointer (the hand's own frames, enlarged): yes above, no below, wave on the
   left, come on the right.
3. Move toward one and let go. The hero plays it while still walking, jumping or floating: a wave overhead, a beckon at the
   chest, or a thumb pumped up or down.
4. A friend who has the hero on screen sees the same gesture one send later.
5. Tapping F on a ladder, while drilling or with the plan out does nothing: the hands are busy. A held item stays in the hand
   and moves with the arm; using it cuts the gesture off.

## Out of scope
- A sim command, saves or replays: a gesture changes nothing in the world.
- Sound: a gesture is something you see, and most play happens in vacuum.
- Friends off screen: no pulse on the edge markers. Gestures are for whoever is in view.
- "Wait/stop": a palm held out looks the same as pointing at this scale, and "no" covers it.
- Nodding or shaking the head, whole-body emotes, leg movement.
- **Turning the wrist** per key: a 4 px hand's tilt is not seen; shoulder and elbow only. Added if the sheet shows it missing.
- **Hiding the held item** or letting go of a two-handed haft: the arm moves and the item goes with it.
- A friend joining mid-gesture misses it: it lasts a second.
- Gestures on a ladder, sitting, strapped in, passed out, with busy hands or with the plan out.
- A key per gesture, rebinding wheel slots, a queue of gestures, pointing that follows a moving thing; the far arm.

## Approach
Client only, in `crew`. A gesture changes nothing the world holds, so a command would add a tick of delay, a new module and a
save-format bump for state nothing reads.

**The gestures are data**, a table in the game's Content next to `SwingMotions`: id, wheel slot, hand frame, length, arm keys as
`MotionKey` (shoulder and elbow by share). Every gesture starts and ends at rest; a held pose is two keys alike:

| Gesture | Arm | Length | Hand |
|---|---|---|---|
| Point | straight along the line to the target, held | 1.2 s | point |
| Wave | upper arm overhead, elbow rocking three times | 1.6 s | open |
| Come | forward at chest height, elbow folding in twice, wrist curling | 1.4 s | open |
| Yes | forearm up in front of the chest, one pump upward | 1.0 s | thumb |
| No | the same arm, pumped downward | 1.0 s | thumb, flipped |

**On the rig.** A gesture is the front arm's played motion, blended in and out over `SwingArm.BlendMs` (check `SwingArm` takes
an arm with no weapon; else a small player beside it). Legs and body untouched. Point aims along the line to its target; the
others straight ahead.

**Hand frames** (hand.md's strip): 0 grip (today's), 1 open, 2 point, 3 thumb; "no" is frame 3 flipped. Three new frames per
hand kind, drawn once; the arm's pump carries yes and no, the thumb only helps. The wrist node does not turn.

**Input.** One new bind, `Gesture`, on F. A tap (released within `TapMs` ≈ 200 ms inside the dead zone) points at where the
pointer was at the press. A hold opens the wheel: four pictures round the press point, the slot by the pointer's angle past the
dead zone; letting go picks it, in the centre cancels. While the wheel is open, clicks do not use the item. The
wheel's pictures are the hand frames enlarged: no art of its own.

**Start and end.** Plays while the hero is in Stand, Walk, Run, Air or Float with nothing else in its hands' work and the plan
away, checked every frame: anything else ends it, as do its time running out and another gesture. Moving, jumping, landing and
floating do not.

## Co-op
Nothing enters the sim. A gesture is hands' work like a chop: `Handwork` gains a value per gesture and the sent `Busy` carries
it, `At` the point's target (as the drill's aim), `Swings` counting gestures so the same one twice is news, as a chop's swings
are (`Remote`). A receiver plays it where the count is new, on its own clock, its length from Content. The local start rule must
not read its own gesture as busy hands.

## Trade-offs accepted
- A gesture with a long tool in hand may look odd; checked on the dev sheet.
- At 8–12 screen px the thumb is only a hint; the pump up or down says yes or no.
- Only what is under the pointer can be pointed at.
- On a friend's screen a gesture can run up to one send longer or shorter.

## Tests
- game.tests: a tap points, a hold opens the wheel, the slot by angle and dead zone, the centre cancels; start rules by pose
  and the plan; walking keeps it, using the item ends it, a second replaces it; every gesture's keys at rest at shares 0 and 1;
  a sent gesture plays once on the other side per new count.
- Visual: a dev sheet of the five gestures standing, walking and floating, with and without the axe.

## Cost
No sim module. `crew`: `Figure`, `Seen`/`Remote`, the sender, the wheel; one `Bind` with its En/Ru name; the gesture table.
Art: 6 hand frames (3 per hand kind). About 400 lines with tests (about 200 without the wheel). Lands after feat/hand.

## Open questions
- **The wheel or two keys.** The reviewer would ship point (F) and wave (a second key) only, and add yes, no and come after a
  playtest asks: half the code, no wheel. The wheel gives all five now, and room for more later.
- Is F the right default key?
