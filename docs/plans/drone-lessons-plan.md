# Plan: the drone learns its forms

Builds [drone-lessons.md](drone-lessons.md). Try stage: filtered tests, local commits, a dev flag; docs and ADR at ship.

## Order

1. **Content** — `sim/Content/Lessons.cs`: `Lesson(Form, Source)`, `Deed(Id, Count)`, `Near(Machine)`, `Lessons.All`,
   `Lessons.Reach`, `Lessons.WreckBroken`. `LightDrone.AlarmsMode = "droneAlarms"`. Lamp and scanner out of
   `Research.Tabs` (rings 0 and 1) and `Market` (`Raw`, `Unsold`, the two `Worth` calls).
2. **Knowledge** — `Learner.Deeds` (`DeedCount(Deed, Count)`, sorted ordinally, null-written while empty: no bump of
   its own); `IKnowledgeOps.Did(peer, deed)` counts and grants each deed lesson whose count is reached; `DeedsOf`
   internal.
3. **Debris** — `Hit` calls `Did(peer, WreckBroken)` as a piece breaks, a delivery crate too; a burst by no hero
   (`FloorStack.Nobody`) counts for no one.
4. **Lessons module** — `sim/Lessons/` (Crew, Knowledge; Grid and Players through them): a derivation system grants a
   near lesson to each paired hero within `Reach` of a machine of its kind, by the box distance to its footprint,
   squared, no floats. No commands, no saved state: an empty slice. Registered last on every surface, run and saved last.
5. **Save** — fixture for 70 first, `SaveFormat` 71, step 70 → 71 adds an empty `lessons` slice aboard and on every
   open surface.
6. **Client** — forms on the board: a `companion` district («Спутник») of the three forms, a frame each, the picture
   the form's thread icon rasterised (`FormPictures`); unknown is the board's plain «?», no pencil, no hint. A form
   learned is a lesson, a longer beat ahead of its batch's items (`FindScan`, client only): the drone goes to the thing
   (0.6 s; the closest teaching machine, or the wreck last seen broken, `DroneSenses.Visit`), fixes its eye on it and
   fills the scanner's 26 dots over it (3 s), shows the form by the head (1.5 s: the lamp lit, the scan out, the
   alarms pulsing red), then its icon flies to the key as a find's, the key flaring 1.2 s. Finds wait behind it; the
   tablet opening puts out finds, not lessons; with no thing found the dots fill over the head. Friends' beams read their own
   `Knows`. Mode texts say learned, not bought. The alarms gate: only the id and its lesson; thoughts are not built.
7. **Dev flag** — `--lessons`: the wreck's pod room, a hero paired beside an emergency lamp, ten engine blocks drifting
   by to break, nine broken already.

## Tests (sim/Tests/LessonsTests.cs unless said)

| Rule | Test |
| --- | --- |
| Ten breaks by a peer teach that peer the scan; nine do not; a friend learns nothing | `TenWrecksBrokenTeachTheScan…` |
| A shot that breaks a wreck counts for the shooter | `AShotThatBreaksAWreckCountsForTheShooter` |
| A burst by no hero counts for no one | `AWreckBrokenByNoHeroCountsForNoOne` |
| A paired hero within reach of a built lamp learns it; beyond reach does not; unpaired does not | three facts |
| An emergency lamp teaches the alarms, not the lamp | `AnEmergencyLampTeachesTheAlarmsNotTheLamp` |
| A friend who did not build the lamp learns it by passing | `AFriendWhoDidNotBuildTheLampLearnsItByPassing` |
| A plan of a lamp, not built, teaches nothing | `ALampStillAPlanTeachesNothing` |
| `BuyBlock` of the lamp or the scanner is refused `research.locked` | `ResearchTests` |
| Counts are saved and loaded; a reborn hero keeps them and its forms | `KnowledgeTests`-style facts here |
| Invariant: after random walks and breaks, a peer knows a form exactly when its source was met | `[Property]`, proved to bite |
| Migration 70 → 71 | `MigrationsTests` (the fixture) |
| Board: the companion district holds the forms, unknown shows «?» | `game.tests` `BoardLayoutTests`, `BoardViewTests` |
| Lesson beat: stages and times, before its batch's items, finds wait behind, the tablet keeps it, long flare | `game.tests` `FindScanTests` |
| The form shown off: lamp, scan, alarms' red pulse | `game.tests` `DroneModesTests` |

## Deliberately not done

- Lessons takes no Items: no item lesson is named yet (the design's own out of scope).
- Thoughts and their gate: the alarms id is learned and shown on the board, used once thoughts are built.
