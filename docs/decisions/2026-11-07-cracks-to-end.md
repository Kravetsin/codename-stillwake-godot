# A wreck's cracks run on until they cover it at the break

- Status: accepted; supersedes the wreck's cracks ("penDent") of [damage-looks](2026-11-05-damage-looks.md)
- Date: 2026-11-07

## Context

Players hitting a big asteroid (200 hit points, about 50 s of the axe) gave up after about ten seconds, thinking no damage got
through. Each blow's six pen lines grew over the next 0.45 of the hit points, and a wreck showed at most 8 dents, so on a big
wreck the look barely changed. Five ways to show progress were drawn on a pick page; the user liked none much and took
«Трещины до конца» (`cracksToEnd`) as the one that looks right, with a scanner for exact numbers to follow.

## Decision

- **A dent where each blow lands**, deepening with each blow there (one within 3 px), no cap on how many: its radius
  `DentLeast` plus `DentPerHits` a doubling of the blows there, plus `DentRadius` times the share lost.
- **One crack network out of the first blow** (`WreckCracks.Network`), laid out once by the wreck's id over its picture's own
  pixels: five trunks fanned toward its middle, wandering, forking up to three deep, each ending a little past the edge.
- It **runs on with the share lost** to the power 0.45, whole by 0.92 lost, so a twentieth lost (ten axe swings on the big
  asteroid) already shows cracks well past the dent; its lines widen by fork depth and as they run.
- Ink and lit line as before: a dark pen over a thin lit one 0.4 px below, clipped to the picture, redrawn on a change of damage.

## Consequences

- The six lines out of each blow and the 8-dent cap are gone; `DentGrow`, `DentLines` and `MostBlows` with them.
- A wreck seen first already damaged roots its network at its middle (`WreckCracks.Unseen`), as before.

### Deliberately not handled

- The network roots at the first blow seen; later blows elsewhere only dent, the network does not branch from them.
- A wreck's picture flipped or turned carries its cracks with it as a child; nothing reads its pixels again after the first blow.
