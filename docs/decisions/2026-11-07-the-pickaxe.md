# The pickaxe

- Status: accepted; its fan amended by [pick-reach](2026-11-07-pick-reach.md)
- Date: 2026-11-07

## Context

On the 2026-11-07 playtest the user went down to the planet, dug with the drill till its battery was flat, and found nothing
for stone but the axe: 8 a second on blocks, three swings a cell of stone, 1.5 s. An axe on stone makes no sense either. The user
asked for a pickaxe: about half the drill on blocks, a slightly larger reach than the drill's, needing no charge, clearly better
than the axe on stone.

## Decision

- **A weapon swung as the axe is** (`Weapons.Pickaxe`): 6 on blocks a blow every 6 ticks, 20 a second, half the drill's 40 and
  two and a half times the axe's 8. Stone and ore (strength 12) break at the second blow, 0.6 s a cell, the drill's 0.3 s doubled;
  alloy in ten blows, 3 s, the drill's 1.5 s doubled. 1 a blow on wrecks, 3.3 a second, under the axe's 4: it is for the ground.
- **Never a cell at a blow.** A blow of 12 every 12 ticks gives the same 20 a second and a slower, heavier swing, but breaks
  stone with no crack seen first, against the rule `NaturalGroundBreaksInMoreThanOneHitOfAnyWeapon` keeps. So the swing is quick,
  a blow every 0.3 s, as a pick pecks at a face.
- **Its fan** first the drill's, a cell longer (9 cells); now wider and as short as the axe's ([pick-reach](2026-11-07-pick-reach.md)).
- **Made at the workbench**, a row of its own, of two plates and a tube, `Ticks: 90`: understood as its parts are
  obtained (the board's rule), all of them from the first wrecks, so it is there before the first landing, the drill long after.
  Not sold, as no tool is (`Market.Unsold`).
- **In the hand** (`Strokes.Pick`): the axe's swing (`AxeSwing`), played over its own pace of 300 ms, the blow at `HitAt`. Its
  whoosh and its hits on wrecks and ground are the axe's: no new sound.
- **Its look, as picked** on an Artifact page of five, «Шип из шпангоута» (`ribSpike`): a length of a ship's rib in old primer, its
  lightening hole kept, torch-cut into a spike and welded to a pipe haft gripped in rubber, the point down (`HeldLook.Pickaxe`,
  `zfittings.js`); its icon the same drawing. The recipe follows it: the rib's plates, the haft's tube.

## Consequences

- Underground the drill stays the fast tool while charged; flat, the pickaxe is half its speed, not a fifth.
- The sledgehammer keeps its 6 on blocks, under the pickaxe's 20 a second by its pace of 60.

### Deliberately not handled

- No modules fit it: the arc's entries name the tools they boost, and none is asked for the pickaxe.
- No sound of its own: the axe's whoosh at twice the rate. A pick's own clink waits for a sound pick if the user wants one.
- The blow rate is the client's pace, unchecked by the sim, as every weapon's is (0034).
