# 0102 — Creatures live in the sim

- Status: accepted; slice 1 (the sim) built, slice 2 (the client) with it as a stacked change
- Date: 2026-10-22
- Design: `designs/creatures.md` (the session's scratchpad), its review and the user's decisions, which override its top

## Context

The first planet's ground, caves and ruins are there to explore, and nothing on it moves. Creatures are the next
mechanic: neutral ones on the first planet (the user's word), hostile ones on planets to come, a pen aboard later to keep
them. The user's focus for the first slice is how a creature takes hits and reacts to the player; what it gives is a
placeholder. A creature walks, falls, is struck by the axe and the drill of any hero and drops what it gives: every peer
must agree on where it is and when it dies, so its body, its hit points and its yields are the world's.

## Decision

- **A module of every surface, `Fauna`** (`sim/Fauna`, `dependsOn` Grid, Crew, Items), as debris is (0006): creatures are
  spawned, moved and killed by the same integer rules on every peer, saved and hashed with the world. Inputs are only
  commands — `Strike`, the dev `Release` — and the heroes' reported cells. It runs last, after crew, in the simulation phase.
- **State:** each creature's `Id, Species, X, Y, VX, VY, Damage, Mode, ModeLeft, SeenAt`, the module's `NextId` and
  `Clock`. Derived, never stored (the review's cut): its facing, the sign of `VX`; whom it flees, found as its flinch
  ends; the beat, `Clock % Beat`. `SeenAt` stays, as the far field's (0052): despawning by linger needs it.
- **Habitats are data on the planet type** (`PlanetType.Fauna`, 0095): each a species, a place — the open surface (no back
  wall) or a cave floor in one depth band (0080, `Terrain.BandOf`) — and a weight. The first planet has lopers on its
  surface; its bands are ready for the glimmite and the driftbell (the design's slices 3 and 5). The ship is no planet
  type: it spawns nothing, draws nothing and its fauna slice never changes, so a ship's run is as before but for the
  slice's bytes.
- **Its own ring.** Round each hero in id order, on a beat of 2 s, a point 62 to 76 columns out (past the farthest view's
  60, inside the keep box's 80) on either side, then down the column inside the keep box to the first place the habitat's
  species stands; never in any hero's view (`Fauna.View`, the debris' own). At most 10 in a hero's keep box from spawning
  and 40 on a surface. Gone 30 s out of every keep box, at once out of every hard box, and with the last hero. The ring is
  drawn alongside the far field's (`DebrisModule.SpawnAround`), some fifteen lines alike: a shared ring waits for a third
  caller and a hash-equality test that proves debris draws as before.
- **Walking** (`Live.cs`): integer, per axis, under the planet's gravity (`Kind.Fall`, as stacks fall); solid is what
  stops a stack. A walker steps up one cell, turns back at a wall it cannot step and at a drop of three rows or more, and
  walks off a shallower one. Modes: idle, wander, flee, hurt; only an idle or a wander begun draws from the Rng.
- **The loper,** 2 × 2, 12 hit points: six axe hits, twelve of the drill, six of the fitted drill. Wanders at 2 cells a
  second, flees at 9 for 3 s (a walking hero loses it, a running one at 13 catches it).
- **A hit** (`Strike(Creature, Tool)`) is any weapon's, its damage the tool's `ToolStats` for its sender (0098), refused
  `fauna.gone`, `items.flat`, `items.notRunning` as wrecks' hits are (0054, 0055). It knocks the creature away from the
  striker's reported cell (`Knock`, a hop up), a flinch of 6 ticks steering nothing, then flight away from the nearest
  hero. The last hit drops its yields by nobody from its top row, so they fall where it stood: placeholders, two or three
  biomass and a seed one time in four. The roll is debris' loot roll, moved to `Core` (`Loots.Roll`) for both.
- **Dev:** `Release(Species, Cell)`, a command as `world.open` is, refused `fauna.notHere`, `fauna.full`, `fauna.blocked`;
  the client's `--fauna` sends it.
- **Save format 26:** an empty fauna slice after every other, aboard and on every surface.

## Measured

On this machine, Release build, the first planet with a hero and its cap of 40 lopers walking, struck every half second:
**0.055 ms** a tick mean against 0.024 idle (`BudgetTests.KeepsAPlanetsTickWithFortyCreaturesUnderBudget`).

## Rejected

- **Creatures in the client or on the host only,** sent as snapshots: a second state path, interpolation of others' worlds,
  and hits judged against positions that differ by peer. In the sim they cost a few integer adds each.
- **Sharing the far field's ring now** (the design's first version): changing debris' spawning in the same change, and every
  pinned draw with it; the review cut it to a later, separate step.
- **Herds and fleeing from a running hero** (the design's first version): the review kept single lopers that flee when hit.

## Deliberately not handled

- **Creatures collide with nothing alive:** not with each other nor with heroes.
- **A slope is stood on at its top;** the client draws a creature down onto the diagonal.
- **They sense through rock;** flight is from the nearest hero by box distance.
- **Despawning forgets damage,** and hunting gives biomass without end but the cap and the beat.
- **Nothing aboard:** no creature lives on the ship, nor may be let out there; a pen aboard (the user's later idea) is a
  decision of its own and nothing here blocks it.
- **Creatures in the ruins:** they spawn on the open surface only; one that wanders into a ruin is welcome there.
