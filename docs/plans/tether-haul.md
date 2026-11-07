# Plan: hauling in along the tether

## Why

A hero out in space without a jetpack gets back only by swimming, 3.6 cells/s: up to 7 s from the tether's 24-cell end.
The tether holds the hero out but never helps it home. The jetpack already covers crossing space; this is the way back
for one who has none.

## Decided with the user (2026-11-07)

- **Shift, held**, afloat on a tether with no jetpack worn: the hero hauls itself straight toward the tether's anchor
  (`Hero.Anchor`), keys aside. Letting go of Shift stops it, as letting go of the keys stops a swim.
- **12 cells/s**: slower than a tool's pull (the harpoon's 20, the grapple's 30), a pull by hand. From the tether's end to the
  door in 2 s.
- **It comes in at speed**, as off the harpoon's line: the tether comes off as the body re-enters the hull, and the feet brake
  it in about a cell. Nothing added.
- **Its look is open**: three candidates (step 2), small deltas on today's afloat pose drawn by the game's own figure, picked
  on a published page.
- **No cost**: hauling spends no air and nothing else; the suit's air stays the clock outside.

## The rule

- `Content.Hero.Haul = 200` thousandths a step (12 cells/s at 60 steps/s); `Hero.cs` reads it as `Spec.Haul`
  (`Spec` = `Stillwake.Content.Hero`).
- The haul's target is `Hero.Anchor`: a point, the body's middle where it was last in the hull before it left.
- **A latch against running out with Shift held.** `Hero.HaulArmed`: on a tether, Shift has been up at least once since the
  tether was tied. One line at the end of `Step`, after `Tethered` (or the jetpack's untying), so whatever built the step
  (`Pulled`, `Swim`, `ShutOut`, `Glide`) cannot lose it: `armed = result.Anchor != null && (this.HaulArmed || !input.Sprint)`.
  No anchor, no latch; `Hero.At` and a replicated hero start unarmed; it is never sent or saved. A hero that runs out of a door
  with Shift held is not hauled straight back in: Shift does nothing till it is let go and pressed again, accepted.
- **The haul**, in `Move`, after the grapple's `Pull` and before `Swim`: afloat, armed, an anchor, `input.Sprint`, and
  `input.Jetpack` false → `Pulled(ship, hero, anchor, facing, false, Spec.Haul)`. `input.Jetpack` is the view's "a jetpack is
  worn", every step, Shift or not. It faces the anchor's side, or while swinging the axe the swing's. `Pulled` slides along
  walls, holds the body at the point and zeroes the kick (`Kx`, `Ky`): the hands on the line take a gun's recoil.
- **Round the hull** (found in tests: out of a door and up over it, a straight haul presses the hero onto the roof's lip):
  the tether bends where the body went round the hull, `Hero.Wraps`, the last bend first. Each step on a tether, a bend
  the body now passes straight from the one before it (or the anchor) is let go, and where it cannot pass straight to the
  last bend, one is made where its middle was a step before; a haul makes none. The haul goes to the last bend, then on.
  At most 32 bends; wound further, a haul may stop flush at the hull, and a swim frees it.
- **What wins**: the grapple's pull (checked first); a jetpack worn (Shift thrusts, no tether); rammed, passed out or
  seated (the view sends no Sprint). The tool in hand still works while hauling.
- **Nothing on the wire, nothing in the sim** (0002): others see the hero move as it is sent. The pose reads a haul from what
  every peer has, `Poses.Hauling(hero)`: an anchor, a step's move (`Vx`, `Vy`) longer than `Spec.Swim`, and pointing at the
  anchor (its dot with the way to the anchor positive).
- Shift's label: "Sprint, jetpack, haul" / «Бег, джетпак, подтягивание».

## Accepted limitations

- **A door shut between the hero and its anchor**, or the hull built across the way back after the tether bent: the haul
  presses it flush to the wall and it stops there. Letting go and swimming always frees it.
- **The anchor's cell cut away into space**: the haul holds the hero at the anchor, still tethered; it swims on.
- **A kick or a harpoon's pull toward the anchor** reads as a haul to `Poses.Hauling` while it lasts.
- The tether is drawn as today, reeled in with its lag: a little slack while hauling.
- Only the label tells a player that Shift hauls: Shift is the space key already, and the controls page lists it.

## Steps

1. **The mechanic.** `Content.Hero.Haul`; `HaulArmed` and the haul in `Hero.cs`; label En and Ru. Tests in
   `game.tests/HeroTests.cs`: a haul moves `Haul` a step toward the anchor, the keys aside, over many steps with Shift held
   (the latch survives `Pulled`); it arrives aboard with no anchor; a hero run out with Shift held is not hauled until
   Shift is let go and pressed; the grapple's pull wins; a jetpack thrusts instead; round a corner it stops at the wall
   and a swim moves it on; `InSpaceNeverLetsTheBodyPastTheTether` with Sprint drawn, and the distance to the anchor never
   grows while hauling.
2. **The candidates.** `Poses.Hauling`, and three looks switched by a dev modifier `--haul-look=a|b|c`, each a delta on
   `Paddling`: (a) hand over hand, the arms alternately reaching along the line toward the anchor and drawing back to the
   chest; (b) both hands reaching together and pulling, a rowing stroke, the knees drawing up on the pull; (c) the body
   tilted toward the anchor, head first, short quick grips by the visor, the legs trailing. A dev sheet
   (`--haul-sheet=<dir>`, as `PoseSheet`) draws the figure alone through a haul cycle for each look and four directions
   to the anchor, a line drawn toward it, and saves the strips.
3. **The pick page**: a published Artifact playing each candidate's strip at the game's scale, one click picks it, the pick
   kept in the page's `db`, a copy button besides. The pick stays in `Pose.cs`; the other two, the modifier and the sheet go.
4. **The dev room** `--haul`: the testbed, no jetpack, creative (no air to run out), the hero by the open airlock; with
   `--autopilot` it swims out to the tether's end (Shift up) and hauls back, for a recording.
5. **Docs**: an ADR `2026-11-07-tether-haul.md`, the crew README's line, the demos README's `--haul` row,
   `docs/glossary.md` if it lists the tether.
