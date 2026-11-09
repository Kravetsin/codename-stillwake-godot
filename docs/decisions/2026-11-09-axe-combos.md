# Axe combos

- Status: accepted; supersedes in part [swing-motions](2026-11-07-swing-motions.md): a combo's steps begin and end away from
  rest, each spliced from where the last left the arm; supersedes in part [one-arm-holds](2026-10-27-one-arm-holds.md): in a
  combo the far hand grips the fire axe's haft; supersedes in part [tool-reach](2026-11-05-tool-reach.md): a swing's
  dust is shed off the blade
- Date: 2026-11-09

## Context

Held, the fire axe struck the same stroke every 500 ms: the arm alone turned at the shoulder, the body stood still, so a
held chop read as a lever, not a strike. The user picked, on a page of candidates drawn at the game's scale, card l3
«На максимум»: a three-step combo with the body in each strike, the far hand taking the haft on the heavy step, and a pale
trail on each cut.

## Decision

- **A combo is data** (`ComboStep`, `BodyMotion` in `game/Content/logic/Combos.cs`): each step its arm keys, its length, its
  hit share, the shares its blade cuts through and its body's keys on the real clock (lean, dip, push, far arm, grip, each
  foot's steps). `Stroke.Combo` names a weapon's; it plays in the Straight and Down sectors, the others keep their motion. The
  fire axe's three steps (`Combos.FireAxe`) are the pick one for one: 450, 450 and 600 ms, hits at 204.75, 630 and 1212 ms of
  a held cycle. A new weapon's combo is rows of data and a pick page, never code.
- **A play is self-contained** (`MotionPlay`: `Ms`, `HitShare`, `Step`, `Body`, `BodyFrom`). `MotionPlay.Of(stroke, aim,
  step, from)` splices: its first key is the pose it replaces, the shoulder moved by the old line less the new, so the arm
  never jumps; the body channels begin where they are and the feet where they stand. Single strokes splice only after a
  combo's step, so every other weapon plays as before.
- **The clock** (`AxeSwing`): held, a step chains at its own end, the next of the combo while the new aim's sector has the same
  one, else step 0 or that sector's single stroke. Let go after a step that leaves the body away from rest, **the back**
  (`MotionPlay.Back`, 250 ms, no hit) eases the arm and the body home, the far foot first. `Ready` is unchanged: a press
  once the 500 ms pace allows begins step 0 from wherever the back has the arm. `Stop` resets the chain.
- **The arm stops 50 ms only on a bite** (0075), as before; the body runs on its own clock and never stops.
- **The body layer** (`ComboBody`): standing, the hip at the feet's middle plus the push, lowered by the dip and where a leg
  cannot reach, each leg reaching its ankle knee forward, boots flat; nothing further ahead than the floor is free
  (`Hero.RoomAhead`, looked at only while a step plays). Striding, only the lean and the far arm; in the air, afloat,
  climbing or lying, nothing. `BodyBlend` eases it over 100 ms as it starts, stops or the hero turns round, in the world's
  axes, by the same smoothstep as the arm's hand-overs (`SwingArm.Left`); it snaps where the body drops. Laid over the
  pose's own legs, a toe or heel under the floor turns its boot flatter, then bends its knee (`Poses.OnFloor`, the gait's rule):
  the finisher's lunge put a toe 2.3 px into it.
- **The figure**: the upper body hangs from a waist node turned by the lean about the hip; at no lean it is where it was,
  so every held item, beam, light and cape is untouched. The legs stay below it. A twin waist holds what is drawn behind
  the legs. The far arm keeps its turn in the body's frame; gripping, it closes on the haft (`HaftGrip`: 3.5 px below the
  near fist, slid within −4 to 5.5 px to where it reaches, 0.2 px short of straight). The head takes back 0.6 of the lean
  and follows the blade by 0.08 of its turn.
- **The air's cut, no trail:** a swing's dust is shed off the blade's edge (`AirCuts.Shed`), a mote every 14 ms while the
  edge moves within the fan in the cut, each flying straight out from the shoulder and stopping at the range: it reads as off
  the blade and still shows how far it reaches. It was a sweep laid out from the cut's timing, which began at the wind-up and
  ran ahead of the blade. The picked pale trail was dropped once played: it fought the dust. A wave sent out by the swing,
  doing the damage, was played and rejected as out of the setting (PR 519, closed).
- **Friends:** `Busy.Step` carries the step begun; a friend's figure plays it spliced from its own pose. No new swing number
  by a step's end plays the back, as a hero let go does.

## Consequences

- Holding strikes on average every 500 ms, the pace unchanged; clicking strikes no faster than before.
- Hits are cast from the physics shoulder over the weapon's fan, not the advanced one: the fan's reach covers the 1.5 cells a
  step carries the blade. Nothing reaches the sim but today's commands.
- Golden samples from the pick page's own code (`ComboTests`) hold the port to 0.01 in every channel.
- **Not handled:**
  - A missed hit: the arm follows through while the body holds over the hit. Unseen at this speed.
  - Feet may be drawn over a hole or a one-cell step's edge: `RoomAhead` looks at the body's rows only. Only drawn.
  - A press during the back sets a foot lifted mid-step down at once, at most 1.4 px.
  - The lean can push the head about 3 px into a wall; the blade already does.
  - Turning round between steps keeps the chain; the stance swaps sides through the blend.
  - No combo for the sledgehammer, the pickaxe or the spear, nor in the Up and Under sectors: each needs its own pick.
