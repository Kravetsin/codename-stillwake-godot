# A guest draws through late ticks

- Status: accepted; extends [0014](0014-own-actions-show-at-once.md) to hits on wrecks, and changes what
  [0015](0015-ticks-play-out-after-a-measured-reserve.md) drew while a tick is late
- Date: 2026-11-04

## Context

A guest playing over the relay felt the game stutter where a host alone never does: debris stopped dead now and
then and jumped on, and a hit on a wreck landed a beat late. Both follow from the session, not from a slow frame:

- A guest runs a tick only once the host has released it (0002), and everything reaches it over one ordered stream:
  the relay's WebSocket, TCP. One lost packet holds every message behind it until it is sent again. The reserve (0015)
  is sized to the 95th percentile of lateness, so a loss now and then runs past it. Under a model of the relay (40 ms
  each way, 10 ms of jitter, 1% of packets held 200 ms), `Playout` ran out of ticks 24 times in two minutes, for up
  to 233 ms each, then ran three or more ticks in one frame 21 times.
- While it waited, everything was drawn at the last tick's end (progress held at 1), so drifting debris stood still,
  then jumped four ticks on.
- A hit's jolt, sparks, flare, chop and nudge waited for its tick to raise the wreck's `Damage`: for a guest the way to
  the host and back plus the reserve, about 150 ms on a clean link and up to 400 ms on a lossy one; for a host alone
  under 50 ms.

## Decision

- **Debris drifts on through a late tick.** `Session.Drift` is `Progress` that runs past 1 while the next tick is
  late, up to `Playout.AheadTicks` (6, 300 ms, the reserve's ceiling) more; `PieceDrawn` draws a drifting piece at
  `X − Speed × Drift` and a reeled one never past its reel's point. Drift is even, a fixed speed a tick, so the ticks
  that catch up put each piece where it is already drawn: no stop, no jump. Every drawer of debris (the debris view,
  the harpoon's rod and aim, a catch's loot) reads `Drift`, so they agree.
- **An own hit is felt as it is sent** (0014): the frame a `HewWrecks` of this peer's is first in flight, each wreck its
  fan strikes jolts, flares, throws its sparks, and the chop and the nudge play. Its tick brings only the cracks, and
  the break with its own sound. A drill's hits keep being felt on their tick: the crew already plays its strikes as
  they are sent, and its sparks are drawn at the running bit.

## Deliberately not done

- **Running ahead with a rollback:** a guest that steps its world past the host's ticks and steps back on a
  difference needs a save a tick and a resimulation, for every module; drawing ahead gives the same feel for what
  moves most, debris.
- **Drawing ahead anything else:** creatures, shots, heroes held by the sim and machines still wait on a late tick;
  they are seldom what is watched while it is late, and their motion is not even, so a guess would be corrected.
- **A piece caught or broken while drawn ahead** is put back where the sim says: at most six ticks of its drift,
  under a cell.
- **A hit felt and then refused** (the wreck broken by another player's hit in the same tick) shows its jolt and chop
  for nothing: the cost of feeling it at once.
- **Losses on the relay:** it stays TCP; Steam's networking in release, or a datagram relay, would take the stalls
  away at their source, and is a bigger change.

## Consequences

- A guest's debris and own hits feel as a host's do; what the sim decides about them still arrives a round trip later.
- Breaking a wreck now sounds its last hit and then the break, where the break alone sounded before.
