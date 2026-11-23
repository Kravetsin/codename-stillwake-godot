# Lessons

## Files

| File | Purpose |
| --- | --- |
| `Lessons.cs` | The light drone learning by looking: a hero with a drone near a teaching machine learns its form; `Teacher`, the machine it learns from. |

The light drone learns its forms from what its player does ([drone-lessons](../../docs/decisions/2026-11-21-drone-lessons.md)).
The lessons are data, `Content.Lessons.All`: `Lesson(Form, Source)`, the source a `Deed(Id, Count)` or a `Near(Machine)`. This
module holds the near lessons' rule; deed lessons are counted where the deed is done (`Knowledge.Did`, called by `Debris.Hit`).
What is learned is Knowledge's, each player's own. A surface module, registered, run and saved last on every surface.

- **Depends on:** Grid (the machines of a kind and their footprints), Crew (the heroes and their reported cells), Players
  (`HasDrone`), Knowledge (`Knows`, and `Grant` through its ops).
- **State** (`LessonsState`): empty. No commands, nothing saved but the empty slice (format 71), nothing derived.
- **Files:** `Lessons.cs` — the module, its empty state and its query interface.

## Rules

- Each tick, in derivation, every hero in hero order whose peer has a drone learns each near form it does not know yet whose
  machine of that kind stands within `Lessons.Reach` (24 cells) of its reported cell: `Knowledge.Grant(peer, form)`, for good.
- The distance is to the machine's footprint, a box: the gaps along each axis, squared and summed, compared with `Reach`
  squared. No floats, no division.
- Only built machines teach: a plan of a lamp is no machine. An unpaired hero learns nothing by passing, and learns once it
  has its drone and passes again. Each peer learns on its own: a friend who did not build the lamp learns it by passing.
- The lessons today: the lamp (`droneLamp`) near a `lamp`; the alarms (`droneAlarms`) near an `emergencyLamp`. The scan is a deed.

## API

- **Queries** (`ILessonsQueries`): `Teacher(cell, kind, reach = Reach)` — the machine of `kind` nearest `cell` within `reach` of its footprint,
  the first in `MachinesOf` order on a tie, null for none: what a near lesson is learned from, and where the client's drone flies
  to look as the lesson plays (research's `FindScan`); at `Lessons.Wonder` (40) what it is curious about (drone-thoughts): only
  `Reach` teaches.
- **Operations:** none.

## Invariants

- A peer knows a lesson's form exactly when its source was met: its hero, with a drone, came within reach of a teaching machine,
  or its deed count reached the lesson's (`LessonsTests`, an FsCheck property proved to bite).
- Nothing here is ever undone: a form learned stays through death, leaving and coming back.

## Deliberately not handled

- Teaching from an item held: no lesson names one yet; `HasFound` answers it when one does.
- A hero running past a teaching machine learns it at once; the client draws the drone lingering afterwards.
