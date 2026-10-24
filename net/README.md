# Net

## Files

| File | Purpose |
| --- | --- |
| `Net/BadNetwork.cs` | How bad to make a link, in ms: every message waits `Lag` plus up to `Jitter`. |
| `Net/Channel.cs` | One end of a two-way link: sends `TOut`, receives `TIn`, in order. |
| `Net/Client.cs` | A peer's view of the session: it sends commands and runs only the ticks the host released. |
| `Net/Clock.cs` | Turns frame time into whole steps of `stepMs`: the host's ticks. |
| `Net/Diff.cs` | Desync reports from two saves. |
| `Net/Host.cs` | Orders the session: collects every peer's commands and releases them tick by tick. |
| `Net/LocalSession.cs` | A host and its player's client in one process, joined by a loopback. |
| `Net/Playout.cs` | When a joined peer runs the ticks its host released: the measured reserve (0015). |
| `Net/Protocol.cs` | How messages are written on the wire: JSON. |
| `Net/RoundTrip.cs` | Times this peer's commands from sent to run. |
| `Net/Session.cs` | A session this machine plays in, and the time that drives it. |
| `Tests/BadNetworkTests.cs` | A link made bad on purpose. |
| `Tests/Desync.cs` | The desync fuzz: a host and its clients in one process on every module's fuzz generator. |
| `Tests/DesyncTests.cs` | Runs the desync fuzz. |
| `Tests/Fixtures.cs` | A shared log: the order of its entries shows the order commands applied in. |
| `Tests/HashesTests.cs` | Hash checks and resyncs. |
| `Tests/JoinTests.cs` | Joining mid-session. |
| `Tests/PaceTests.cs` | How a session's time runs: the clock and a joined game's playout. |
| `Tests/SessionTests.cs` | The host and clients ordering commands. |
| `Tests/SurfaceStampTests.cs` | A command goes out stamped with the surface its sender is on (0076); sessions carry every surface. |

How peers share one world (0002): a host orders every peer's commands tick by tick, every peer runs the same
simulation on them, and what lives outside the simulation (heroes) is passed along beside it.

- **Assembly:** `Stillwake.Net` (`net/Net`), plain C#, no Godot: it references the sim core only. Its tests are
  `net/Tests`. The transports live in the game (`game/net`: `EnetLink`, `RelayLink`).

## The protocol

Messages are records written as JSON (`Protocol.Json`) with a `kind`; the loopback copies through the same JSON, so
in-process tests carry what the wire carries (no ADR).

| To the host | To a peer |
| --- | --- |
| `command`: an encoded command | `tick`: the tick and its commands, each `Stamped` with its sender |
| `hero`: this peer's hero state | `hero`: another peer's hero state, with its id |
| `hash`: a tick and its hash | `welcome`: the peer's id, the host's build, a save |
| | `resync`: the host player's world, saved |

Commands travel encoded by the sender's world (`World.Encode`) and are decoded by each peer's (`World.Decode`), `From`
set from the stamp. The host runs no world and never decodes them. `Client.Send` stamps each with its peer's surface
(`Surface`, 0076), so one whose hero has moved on by its tick is refused.

## The session

- **The host** (0002) runs no world. It stamps each command with its sender's id and releases it in the next tick to
  every peer, in the order received. Peer ids count up from 0, never reused. Peer 0 is the host player's own client:
  the world joiners start from and hashes are checked against.
- **A client** runs only the ticks the host released, in order; a gap is a bug and throws. It changes its world only
  by sending commands. `Queued` counts the ticks waiting; `Update(limit)` runs at most that many.
- **A `Session`** drives one: on the host a `Clock` turns frame time into released ticks, at most 250 ms of them after
  a stall (no ADR); on a joined peer `Playout` runs each tick at the earliest it has been seen to arrive plus a reserve,
  eased toward its target, at most five ticks a frame (0015).
- **Commands in flight** (0014): `Client.InFlight` lists this peer's commands sent and not yet run; each of its own in a
  released tick is the oldest in flight, or it throws. The game shows them at once on top of the world; `RoundTrip`
  times them. Those sent from a surface the peer has left (0076) are left out: they will be refused `world.elsewhere`.
- **A channel** (`IChannel`) delivers in order and keeps what arrives before a receiver is set (`Inbox`) (no ADR).

## Joining mid-session

- `Host.Admit(channel)` sends `welcome`: the peer's id, the host's build, and peer 0's world saved on the tick the
  host releases next (`IWorldSource` first runs peer 0 up to it). Commands held but not released ride in the next tick.
- `Client.Join(load, channel, build, joined, refused)` refuses another build (the assembly's informational version),
  loads the world and catches up on the ticks since.
- `Host.Disconnect(id, farewell)` drops a peer: it is sent and checked no more, and `farewell`, an encoded command,
  goes out in the next tick stamped with its id. The game passes `crew`'s `Leave` (`game/Main.Sessions.cs`).

## Heroes

A client sends `hero` with a state net never reads (a string); the host passes it to every other peer stamped with the
sender and keeps nothing (0002). The game draws the others (`game/features/crew/logic/Remote.cs`).

## Hash checks

- Every client sends `hash` every `Protocol.HashEvery` ticks once its update is done, so the host may check it and
  save peer 0's world at once. The host checks the others against peer 0's, holding a report that comes first, and
  keeps peer 0's last 30 (no ADR).
- A host player alone hashes nothing (`Client.Hashing`, from `Host.Peers`); once a guest is admitted it hashes again
  from the ticks the guest will report (no ADR).
- A peer that differs gets `resync`: peer 0's world on the host's next tick. The client takes it in order, records a
  `DesyncReport` (each module that differs, up to ten paths, whether the rng drifted; another surface's modules as
  `1:grid`; `surfaces` or `where` when surfaces or who is on them differ), and plays on from the host's world. Older
  reports are not checked again; `Session.Desyncs` keeps them.

## Testing

- `Desync.Fuzz` runs a host and three clients on every module's `Fuzz`, updating out of step, hashes compared;
  `joinAt` adds a mid-run joiner. Commands are drawn from the sender's surface's modules, one in `worldEvery` the
  world's own (0076). A resync counts as a desync; a module with commands and no generator fails. It runs on the
  registry's wreck and the test modules `board` and `drift`.
- `BadNetwork` delays a channel both ways; tests drive it on `TestTiming`, the game on real timers (`--lag`,
  `--jitter`). Two headless processes (`--host` / `--join`, `--auto`, `--cheat` to force a resync) print hashes,
  desyncs and round trips.

## Deliberately not handled

- The folds of 0014 live in the game: features fold own commands in flight into what they show (`features/items`,
  `game/shared/logic/Predicted.cs`).
- Others hear of a peer leaving only through the farewell the game passes.
- A host that loads a save leaves its joined peers behind.
- No host migration, no anti-cheat: friends trust each other (0002).
- A save goes in one message: fine for the wreck, measured again with the reference world.
