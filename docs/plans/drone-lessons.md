# Design: The drone learns its forms

## Problem
The drone's lamp and scan come from the black market. The crew buys each one once, so nobody learns anything by playing.
Players also cannot tell the weapons apart: in playtests the damage looked equal, though the sledgehammer one-shots some
wreckage the axe does not.

## Decided (the user, 2026-11-21)
- The drone learns from many sources; players should want to try every corner and action to teach it. New sources are
  data, not code per form.
- The scan is learned by watching: about ten wreck pieces broken.
- The lamp is copied from a lamp a player built: passing near it teaches it. The lamp matters only on the planet.
- The wreck's emergency lamps teach the alarms: the drone's thought icons over its head (meteor and bolt, cold, heat;
  `docs/plans/drone-thoughts.md`). The dart toward danger stays innate.
- Lamp and scan leave the black market. Growth is each player's own.
- Not now: other alien cores, levels from use, feeding, modules in threads, lightning, the magnet.

## Player experience
1. The hero pairs with the core in the starting wreck. Passing its red emergency lamps, the drone lingers by one and
   sweeps it with a thread: it has learned to warn. From then on a meteor or bolt shows as a thought over it.
2. The hero breaks wreck pieces with whatever is in hand. At the tenth, the drone sweeps the last piece. The scan form's
   icon shows over the head, a glowing pixel flies to the «Изучения» key, and the F wheel's scan slot stops being dashed.
3. Later a player builds a lamp on the ship. Passing near it, each friend's drone flies to it, sweeps it, and learns the
   lamp form.
4. On the board (T), the «Спутник» district shows the known forms, and a question mark for each one not yet known.

Goes wrong: a hero who learned the scan dies, or a friend leaves and comes back. They keep every form and every count,
because learning belongs to the player, not the body.

## Out of scope
- Teaching from an item held in the inventory. `HasFound` already answers it, so it is one source kind to add once a
  lesson names an item (the magnet is the likely first).
- A lesson with several ways. A second way is a second lesson with the same form.
- Teaching a friend, forgetting, or losing a form.
- Refunds for saves that bought the lamp or scanner crew-wide (saves break at release).

## Scale
- World: 3 lessons; one hero's reported cell checked against that surface's teaching machines.
- Players: 4+, each learning separately.
- Frequency: one count per wreck broken (every few seconds while salvaging); a nearness check each tick.

## Approach
A lesson is content data in `sim/Content`: `Lesson(Form, Source)`, where `Source` is either `Deed(Id, Count)` or
`Near(Machine)`. Forms keep their ids `droneLamp` and `scanner`, so the wheel's existing `Knows(peer, id)` check keeps
working; the alarms get a new id. Lamp and scanner leave the market's rings and worths.
- Scan: `Deed("wreckBroken", 10)`.
- Lamp: `Near("lamp")`, within `Lessons.Reach` cells of the footprint.
- Alarms: `Near("emergencyLamp")`.

A near lesson is learned only by a hero with a drone: the drone does the scanning. Deeds always count.

**Deeds, in Knowledge** (each player's record): `Learner` gains `Deeds`, saved as counts sorted by id, and the op
`Did(peer, deed)`. It adds one to the count and, when the count reaches a deed lesson's threshold in the content table,
calls `Grant(peer, form)` itself. `Debris.Hit` already gets the hitter, from tool commands and shots alike, and already
holds `IKnowledgeOps`; it calls `Did` when a piece breaks. No watcher.

**Near, a new module `Lessons`**, rules only, depending on Crew, Items and Knowledge. Research runs before Crew and
cannot read where heroes stand; Crew is about bodies. Each tick in derivation it grants a near lesson to any paired hero
within `Lessons.Reach` (as far as the screen shows) of its machine. No commands, no events.

**Client:**
- The find beat (`FindScan`) plays with the form's thread icon. For a near lesson, the drone first flies to the closest
  such machine.
- The «Спутник» district goes into `BoardDistricts`.
- Thoughts show only once the alarms are known.

## Determinism and co-op
- Inputs: hero cells from the `Enter` reports the client already sends (client-authoritative: a player could fake a cell
  to learn the lamp, acceptable among friends); wreck hits from commands the host stamps with `From`.
- Counts are whole numbers with no division. Grants do the same thing in any order, and the `Granted` list is sorted.
- A joining friend gets the Knowledge slice, with counts, in the snapshot. A new peer id starts from zero.

## Trade-offs accepted
- A hero running past a lamp learns it. The drone's lingering is drawn afterwards.
- Arc and derelict lamps teach nothing; only built lamps teach the lamp, only emergency lamps the alarms.
- Breaking a delivery crate counts as wreckage.
- Old saves keep a lamp or scanner the crew bought, for everyone.

## Tests
- Scenarios: ten breaks by peer 1 teach peer 1 the scan; nine do not; peer 2 learns nothing. A shot that breaks a wreck
  counts for the shooter. A paired hero within reach of a built lamp learns it; beyond reach does not; an unpaired hero
  does not. An emergency lamp teaches the alarms, not the lamp. A friend who did not build the lamp learns it by passing.
  `BuyBlock("scanner")` is refused `research.locked`. Knowledge tests: counts are saved; a reborn hero keeps them.
- One invariant (FsCheck): after a random walk and random breaks, a peer knows a lesson's form exactly when its source
  was met.

## Cost
- Modules: new `Lessons`; changed Knowledge, Debris, Registry (one `SaveFormat` step for the new module).
- Content: the lessons table, `Lessons.Reach`, the deed id, the alarms id; lamp and scanner out of `Research.Tabs` and `Market`.
- About 750 lines (sim 250, client 300, tests 200).

## The «Спутник» district (the user, 2026-11-21)
Known forms show; forms not yet known show as question marks, with no hint of how to learn them and no progress counts.
If playtests show nobody finds the scan, a hint comes then. `DeedsOf` stays internal.
