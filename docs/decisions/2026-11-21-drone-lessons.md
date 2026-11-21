# The drone learns its forms from what its player does

- Status: accepted; supersedes in part [light-drone](2026-11-21-light-drone.md) (the lamp and scanner bought on the market for
  the crew)
- Date: 2026-11-21

## Context

The drone's lamp and scan came from the black market, bought once for the whole crew, so nobody learned anything by playing.
The user wanted the drone to learn from many sources, each player's own, so that players try every corner and action to teach it.

## Decision

- **A lesson is data** (`Content.Lessons.All`): `Lesson(Form, Source)`, where the source is `Deed(Id, Count)` or `Near(Machine)`.
  A new source is a row, not code per form. Forms keep their ids, so the wheel's `Knows(peer, id)` stays:
  - the scan (`scanner`): `Deed("wreckBroken", 10)`, ten wreck pieces broken in space; a delivery's crate counts, the hold's
    piles do not (the user's call);
  - the lamp (`droneLamp`): `Near("lamp")`, a lamp a player built; a plan of one teaches nothing;
  - the alarms (`droneAlarms`, new): `Near("emergencyLamp")`, the wreck's red emergency lamps.
- **Deeds are counted in Knowledge**, each learner's `Deeds` (saved as `deeds`, sorted by id, written only once one is done).
  `IKnowledgeOps.Did(peer, deed)` counts one and grants each deed lesson whose count it reaches. `Debris.Hit` calls it as a piece
  breaks, for whoever hit it, by tool or shot; a burst by no hero counts for no one.
- **Near lessons are the new module `Lessons`** (rules only, an empty slice, no commands): each tick in derivation a hero with a
  drone (`Players.HasDrone`) within `Lessons.Reach` (24 cells, about the screen round the hero) of a teaching machine's
  footprint learns its form. The distance is the box distance squared, no floats. `ILessonsQueries.Teacher(cell, kind)` is the
  nearest such machine, where the drone flies to look. Registered last on every surface; save format 71 adds its slice.
- **Learning is the player's**, not the body's: a hero reborn, or a friend who leaves and comes back, keeps every form and count.
- **Lamp and scanner leave the market**: out of `Research.Tabs` and `Market`'s worths; `BuyBlock` of either is refused
  `research.locked`.
- **The «Спутник» board district** (`companion`) holds the forms in table order: a known one its thread icon, an unknown one the
  board's plain «?», with no hint of how to learn it and no count (the user's call; a hint comes only if playtests need one).
- **The beat** of a form learned is the find scan's lesson ([find-scan](2026-11-21-find-scan.md)); the grant itself is instant.
- **The alarms gate the drone's thoughts** (`docs/plans/drone-thoughts.md`, not built). The dart toward danger stays innate.
- `-- --lessons` plays it: a hero paired by an emergency lamp and a wreck, nine broken, learns the alarms, then the scan.

## Deliberately not handled

- Teaching from an item in the inventory: `HasFound` answers it once a lesson names an item (the magnet is the likely first).
- A lesson with several ways: a second way is a second lesson with the same form.
- Teaching a friend, forgetting or losing a form; levels from use; feeding; other alien cores.
- A hero running past a lamp learns it; the drone's lingering is drawn afterwards. A faked reported cell could teach the lamp:
  acceptable among friends.
- Arc and derelict lamps teach nothing.
- The alarms' icon is a hand-drawn warning triangle (`FormIcons.Alarms`), a stand-in until the thoughts' icons are drawn.
- Old saves keep a lamp or scanner the crew bought, for everyone (saves break at release).
