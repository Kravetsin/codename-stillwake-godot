# A form learned is shown in a vision

- Status: accepted; supersedes in part [find-scan](2026-11-21-find-scan.md) (a lesson showing its form by the head) and
  [drone-lessons](2026-11-21-drone-lessons.md) (a form working from the moment it is granted)
- Date: 2026-11-22

## Context

In a playtest the scanner was learned unnoticed: it worked from its lesson's first frame, and the beat by the head was lost
in play. The user's idea: pull the player out of reality as the meeting does and show the form one on one, the hero
invulnerable so it is fair. A first vision of 5 s read as a slideshow; it is onboarding seen once, so it takes its time.

## Decision

- **The lesson** keeps its 0.6 s going and 3 s studying in the world. Then comes the vision, then the form's icon flies to
  «Изучения», and only as it lands does the form open on this client (`FormsOpen`). The sim's grant stays instant. A form
  known as the world is first seen, or in creative, is open at once; one whose lesson is never heard of opens after 2 s.
- **The scene**: the world fades to black, the HUD goes, the hero floats held on the vision's own zoomed layer, a little
  lifted out of the dark, the drone small before him. One thing at a time: it arrives, holds to be read (1.2 s or more),
  the drone takes it, the result holds (1 s or more), and nothing else moves meanwhile. No shake.
  - scanner, 13.8 s: a whole piece's row full, then a cracked one's half lit;
  - lamp, 12.7 s: the drone copies a lamp's dish, its beam finds a crate in the dark;
  - alarms, 13.5 s: the cold and its snowflake, then a far meteor and the red dart (the user chose these two).
- **The picks** (`tools/pick-kit/pages/form-vision.js`): `picks/formVision` is `trace`, «Проступает схема» (a spark thread
  runs over what is taken, its picture darkens, the diagram stays), over the plain pictures, ember and glow;
  `picks/formVisionFocus` is `frame`, «Кадр следует» (the view glides in to about 1.3 on each thing), over a spot and a
  thread.
- **No data engine**: a scene method per form on one shared frame (`FormVision`). A test fails for a lesson's form with none.
- **The spare**: the client sends `Envision` once as the vision begins, and `Hero.Meets` becomes `Crew.VisionTicks` (the
  longest vision and a second for the trip to the host) unless more is left. It is a command of its own because `PairDrone`
  also pairs; the same field with the same meaning, so no format bump. A spared hero's client sends no ram or arc either,
  which mends the meeting too.
- **Held** where the vision found the hero, until the sim puts it elsewhere (a pod arrives, it wakes from passing out):
  then held there.
- **Co-op**: only the learner sees the scene; friends see a held hero.
- `-- --form-vision` plays the three.

## Deliberately not handled

- Skipping, sound, replaying a vision.
- Forms learned before pairing (the user: the axe needs the workbench, which comes later).
- Shorter visions leave the hero spared up to 2 s past their end: one window for every form.
- A world reloaded mid-vision keeps the spare and loses the scene.
- A hit before `Envision` reaches the host counts. A hero passed out as the study ends skips the scene.
- Two forms learned at once play one after the other.
- Pictures blur a little while the view is in at 1.3.
