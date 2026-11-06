# Leaks tripled, the pod with them

- Status: accepted; supersedes the leaks and the pod's rate of [air-bubbles](2026-11-05-air-bubbles.md)
- Date: 2026-11-06

## Context

Players of the 2026-11-06 playtest felt no shortage of air on the ship. Air is plentiful: a vaporizer held 400 scrap cells,
and canisters lie in the wrecks just above the ship, so many that players sold more of them than they used.
The user chose to raise the leaks, not cut the vaporizer, so canisters weaken along with it.

## Decision

- **Every material's leak and `Air.DoorLeak` are tripled:** scrap 90, iron 72, glass 18, soil 360, stone 270, bedrock 108,
  doors 60.
- **The pod has its own rate, `Air.PodRate` = 36000 a period**, three times a vaporizer's `Air.GeneratorRate`. With it the
  starting ship holds as before: room A, the airlock and the sealed hold (the-scoop). A source that takes no liquid in is
  the pod and makes at its rate; the survey's `MadeOn` now holds each network's making itself, not its satisfaction.
- So a vaporizer holds a third of what it did. That is about 133 scrap cells, or some 85 of an ordinary room with its hull.
  A canister keeps a room for a third as long.

## Consequences

- Every room built past the starting ship costs three times the air: more vaporizers, more canisters, glass back walls.
- Test maps sized for one vaporizer were cut down to fit: the row's first room, the door bubble's left room, the glass room.
  The budget hall got glass back walls.
- The dev worlds, the testbed and the hills, put glass behind their rooms as they are made (`GlassBacks`): their few
  vaporizers could not keep up with scrap.
- The tests' `RunLeaking` charges a tick the leak that tick left. The sim always charged the cells aired that same tick,
  and with the leak growing three times as fast the old reading drifted off.

## Not done

- No new source or electrolyser: the playtest's ice → canister went away separately, and making oxygen is its own design.
- Breathing stays a unit per 20 ticks: beside the leaks it is still small, and the shortage is meant to come from building.
