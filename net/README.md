# Net

How peers share one world ([0002](../docs/decisions/0002-coop-model.md)): a host orders every
peer's commands tick by tick, every peer runs the same simulation on them, and what lives outside the
simulation — heroes — is passed along beside it. Ported from `packages/net` and the session half of
`apps/game/src/session`; their rules hold here unchanged.

- **Assembly:** `Stillwake.Net` (`net/Net`), plain C#, no Godot: it references the sim core only.
  Its tests are `net/Tests`. The ENet transport lives in the game (`game/net/EnetLink.cs`).
- **Files:** `Channel.cs` (the `IChannel` contract, the loopback, the inbox every channel keeps),
  `Protocol.cs` (every message), `Host.cs`, `Client.cs` (and `Client.Join`), `LocalSession.cs` (a host
  and its player's client), `Diff.cs` (desync reports), `Session.cs` (a session and the time that
  drives it), `Playout.cs` (the measured reserve, 0015), `Clock.cs`, `RoundTrip.cs`, `BadNetwork.cs`.

## The protocol

Messages are records, written as JSON (`Protocol.Json`) with a `kind`; the loopback copies through
the same JSON, so in-process tests carry what the wire carries.

| To the host                 | To a peer                                         |
| --------------------------- | ------------------------------------------------- |
| `command`: an encoded command | `tick`: the tick and its commands, each `Stamped` with its sender |
| `hero`: this peer's hero state | `hero`: another peer's hero state, with its id   |
| `hash`: a tick and its hash | `welcome`: the peer's id, the host's build, a save |
|                             | `resync`: the host player's world, saved          |

Commands travel encoded by the sender's world (`World.Encode`), and are decoded by each peer's
(`World.Decode`) with `From` set from the stamp. The host runs no world and never decodes them.

## The session

- **The host** runs no world. It stamps each command with its sender's id and releases it in the
  next tick to every peer, in the order received. Peer ids count up from 0 and are never reused.
  Peer 0 is the host player's own client: the world joiners start from and hashes are checked against.
- **A client** runs only the ticks the host released, in order; a gap is a bug and throws. It sends
  commands; it never changes its world any other way. `Queued` counts the ticks waiting, and
  `Update(limit)` runs at most that many.
- **A `Session`** drives one: on the host, a `Clock` turns frame time into released ticks (at most
  250 ms of them after a stall); on a joined peer, `Playout` runs each tick at the earliest it has
  been seen to arrive plus a reserve, the 95th percentile of lateness over five seconds plus 10 ms,
  within 10–300 ms, eased at a tenth of the time passed, at most five ticks a frame (0015).
- **Commands in flight:** `Client.InFlight` lists this peer's commands sent and not yet run. Each of
  its own in a released tick is the oldest in flight; one that is not throws. The game shows them at
  once on top of the world (0014); `RoundTrip` times them, sent to run.
- **A channel** (`IChannel`) delivers in order, and keeps what arrives before a receiver is set until
  one is (`Inbox`). Two carry it: the loopback, and ENet in the game.

## Joining mid-session

- `Host.Admit(channel)` sends `welcome`: the peer's id, the host's build, and peer 0's world saved on
  the tick the host releases next (`IWorldSource`, which first runs peer 0 up to that tick). Then the
  peer gets every tick like the others; commands the host holds but has not released ride in the next.
- `Client.Join(load, channel, build, joined, refused)` refuses another build, loads the world and
  hands back a client that catches up on the ticks that came since. The game's build is its
  assembly's informational version.
- `Host.Disconnect(id, farewell)` drops a peer that left: it is sent nothing more and checked no more,
  and `farewell`, an encoded command, goes out in the next tick stamped with its id.

## Heroes

A client sends `hero` with a state net never reads (a string); the host passes it to every other
peer stamped with the sender, and keeps nothing.

## Hash checks

- Every client sends `hash` after each `Protocol.HashEvery` (20) ticks, once its update is done: the
  host may check it at once and save peer 0's world, which must not be halfway through its ticks. The
  host checks the others against peer 0's, holding a report that comes first, and keeps peer 0's
  last 30.
- A peer that differs gets `resync`: peer 0's world on the host's next tick. The client takes it in
  order with the ticks, records a `DesyncReport` (each module that differs, up to ten paths in it,
  and whether the rng drifted), and plays on from the host's world. Its older reports are not checked
  again. `Session.Desyncs` keeps the reports; the game prints them.

## Testing

- `Desync.Fuzz` (`net/Tests/Desync.cs`) runs a host and three clients in one process on every
  module's `Fuzz`, clients updating out of step and a few ticks at a time, hashes compared every 20
  ticks; `joinAt` adds a client joining mid-run. A resync counts as a desync. A module with commands
  and no generator fails it. It runs on the registry's wreck and on the test modules `board` and `drift`.
- `BadNetwork` delays a channel both ways, `Lag` plus up to `Jitter` ms, in order; tests drive it on
  their own `TestTiming`, the game on real timers with `--lag` and `--jitter`.
- Two processes: `godot --headless --path game -- --host --auto --for=16` and
  `godot --headless --path game -- --join --auto --for=12`; add `--cheat` to a joiner to have the host
  resync it, `--lag=100 --jitter=60` for a link across the internet. Each prints its hash every 100
  ticks, its desyncs and its round trip.

## Deliberately not handled

- **The folds of 0014.** What a door, a stack or a grab looks like with own commands in flight needs
  `grid.afterToggles`, `items.afterMoves` and `debris`, which are not ported yet; `InFlight` is.
- **The relay of 0012.** ENet connects peers directly on a LAN or a forwarded port; playtests across
  networks wait for GodotSteam (G-09), whose relays cross NAT. Its routing and tests are not ported.
- **Browser time:** the worker's beat (`time.ts`) and the build check (`update.ts`) served a page;
  Godot keeps processing in a background window.
- Others hear of a peer leaving only through the farewell the game passes; the game passes none until
  `crew.leave` is ported.
- A host that loads a save leaves its joined peers behind.
- No host migration, no anti-cheat: friends trust each other (0002).
- Large saves go in one reliable ENet packet; fine for the wreck, measured again with the reference
  world.
