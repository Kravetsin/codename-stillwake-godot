# A stroke hits with its swept blade

- Status: accepted; supersedes, for the axe, sledgehammer and spear, the fan of [the fan of rays](2026-10-25-fan-of-rays.md), and
  the wreck outline and tool prompt of [0039](0039-melee-by-contact.md) and [tool-edge](2026-11-05-tool-edge.md)
- Date: 2026-11-05

## Context

After tool-edge the drill hit what its drawn bit touched, but the axe, hammer and spear still hit by a fan of rays from the
hand. Struck at the ship's hull they threw no sparks: the fan strikes nothing aboard, where tools break nothing. And the
outline named one wreck while a hit struck two, which the user found unintuitive; they asked to drop the selection.

## Decision

- **A stroke hits what its blade sweeps** (`Aiming.StrokeEdges`, `ToolEdges.Swept`): a swing's edge as drawn at every 2° of the
  downstroke's last stretch, a thrust's point at full stretch, `Bite` either side, each tested as a running tool's edge is. It
  stops at the first sample that touches the ground; wrecks and creatures met before are all hit. It sends `HewCells`,
  `HewPieces` and `Strike`s as a running tool does, each only if the sim would take it.
- **The ground it bites first rings and sparks**, taken or not: the hull aboard, bedrock, a wall of no damage. A built wall
  rings as a wreck's metal under that tool (`GroundSounds.HitAt`).
- **No aim for what hits or shoots**: with a tool, a weapon or a gun in hand no wreck, wall or plant is outlined, glows or gets a
  prompt; a small piece is still grabbed. Outlines stay for the hands: grabs, E, plans.
- **Sparks off every wreck touched**: a running tool streams sparks at each wreck its edge touches, not only the first.

## Consequences

- Melee reaches as far as drawn, about three cells from the shoulder; `Range` now matters only for the cutter.
- The client sends no `Hew` or `HewWrecks`; the sim still takes them, for a later clean-up.
- Deliberately not handled: the blade only tests the downstroke's last stretch, so what the drawn wind-up passes through is not
  hit; a friend's stroke rings where its blade bites as drawn here, not as its peer saw it.
