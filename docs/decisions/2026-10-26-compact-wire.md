# A compact wire

- Status: accepted; built
- Date: 2026-10-26

## Context

Everything on the wire was JSON, nested: a command a JSON string inside a JSON message, a hero's state the same, and on
the playtest relay the whole message a JSON string again inside the relay's JSON frame. In a heavy fight (every hero
sprinting and threatened, reporting its cell every tick, 2026-10-26-creatures-are-composed) the host sent 81 KB/s with
four players and 410 with eight, 121 and 598 through the relay: an `Enter` cost 108 bytes for a few of meaning, a hero
273. Heroes go to every other peer, so the cost grows as N×(N−1), and more is to come (creatures, bosses, raids). This
is the core of co-op, cheapest to change while the game is small.

## Decision

- **One packer, `Packing` (net),** writes a record by its declared types with no names: its public constructor's
  parameters in order, then its other settable properties by name (a command's `From` and `Surface`). Ints as zigzag
  varints, bools as a byte, doubles and the hash as 8 bytes, strings and byte arrays by length, enums checked against
  their values. A reference value carries whether it is there; one sent alone does not. An abstract type goes as a
  tag, its place in a list: `Protocol`'s for messages, the world's command types in name order for commands. Reflection
  once a type, cached. It lives in net, not the sim core: the sim bans doubles, and only the wire needs it.
- **Three callers:** commands (`Wire.Encode`/`Decode` on a world, replacing `World.Encode`), the hero's state
  (`SentHero.Encode`), every message (`Protocol.Write`/`Read`, the loopback included). Hashes and saves stay JSON.
- **The relay batches:** the relay server carries text frames and is redeployed only when asked, so the game's
  messages to one end ride in one frame a game frame, packed together as base64 (`RelayFrames.Batch`). They leave on
  the next poll: one frame, about 17 ms, later than before.
- **Bytes no peer of this build writes** are dropped with a warning (`Links.Read`): an older build's JSON cannot be
  read far enough for the welcome to say "another build".

The same fight now costs the host 11 KB/s with four players and 57 with eight, 19 and 89 through the relay
(`NetTrafficTests`, which holds those as budgets); an `Enter` stamped is 10 bytes, a hero relayed about 45.

## Deliberately not handled

- **Deltas and interest:** every hero goes whole to every other peer. Sending what changed, or only to peers on its
  surface or near it, is the next lever if eight players need it.
- **Hero doubles** (`Look`, `At`) stay 8 bytes each; floats or ticks would save about 10 bytes a hero.
- **Binary relay frames:** they would drop the base64 and the JSON frame, about a third of the relay's bytes, but need
  the relay server changed and redeployed.
- **Saves** go as JSON text in `welcome` and `resync`: a join is rare and off the N×(N−1) growth.
- **Peers of different builds** stall instead of being told; the welcome's build check stays for builds that pack alike.
