# The crew shuttle looks as «Десант — Салазки»

- Status: accepted; built
- Date: 2026-11-16
- Supersedes: the look line of [crew-shuttle](2026-11-15-crew-shuttle.md) («Ледокол»)

## Context

«Ледокол» read as a sea ship, the user's word. Four more rounds of pick pages followed: nine new crafts, nine after a
reference picture of a sleek plated shuttle, nine thick ones, then nine sets of legs for the one the user chose, «Десант»
(G of the thick round), drawn 10 % lower with its canopy moved up and a fuel gauge at the stern. The user picked G4, its skids.

## Decision

- **Its look is «Десант — Салазки»**: a light-grey dropship, its tinted canopy along the upper bow, a wide sliding door edged
  in yellow, a far engine pod high and a near one low, a sight glass of biofuel on the stern quarter, two long skids on
  swinging links. Drawn by `tools/anime-art/subjects/shuttle.js`.
- **The footprint stays 24 × 10**, so every save loads as it was (the user's condition: as long, as tall or lower). The craft is
  72 px tall, 9 cells, standing on its skids on the platform row; the footprint's top row is empty of paint.
- **Its canopy never changes, whoever sits behind it** (the user's word, 2026-11-17, after playing it lit): no seat shows on
  the craft, which overrides crew-shuttle's "slits will light warm". Only the course lamp on its mast lights while a course is
  set (`crewShuttle_course`, drawn by `ShuttleLights`).
- **The fuel port's fitting alone meets the picture's edge** at the port (`PipeArtTests`): the near pod's bell stands back from it.

## Deliberately not handled

- **Folded skids**: their three frames (down, half, up) are in `shuttle.js` but not exported; the shuttle has no lift-off in the
  world yet (slice 3 of [shuttle-flight](../plans/shuttle-flight.md)).
- **A 24 × 9 footprint**: it would move every saved shuttle's top row and needs a save step; the empty row costs nothing a player does.
