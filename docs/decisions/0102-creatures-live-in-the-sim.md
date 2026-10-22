# 0102 — Creatures live in the sim

- Status: accepted; slices 1 (the sim) and 2 (the client) built
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
  stops a stack. A walker steps up one cell and down one, turns back at a wall it cannot step and at a drop of three rows or
  more — at the brink, judged by where its edge gets this tick, so a fleeing one stops there rather than overhanging — and
  walks off a shallower one. A tick moves under a cell across (the fastest flight 750) and down (`MaxFall` 600), so it never
  passes through a wall a cell thick. Modes: idle, wander, flee, hurt; only an idle or a wander begun draws from the Rng.
- **It jumps** (the user's word after a playtest: pinned to a wall a loper could do nothing). A species' `Jump` (speed up) and
  `JumpAcross` (speed across in the air) are data; its highest and longest follow under the planet's gravity (`Leaps`). Where it
  would turn it first flies a leap through the grid, tick by tick as it would move, and takes it if it lands at least a cell on
  and no more than three rows down; else it turns as before. Fleeing, the lowest of three jumps that lands (sized for a ledge
  of two, of three, and its whole jump), over walls and across gaps; wandering, only up a ledge of two at its feet, so it does
  not bound about. In the air it presses flush on a wall and is carried over once clear. The loper: 440 up and 250 across,
  4.4 cells high and 10.5 long landing level on the first planet; it clears a wall of four and a gap of eight or so, and
  turns at a wall of five and at a pit too wide or deep. Still cornerable: under a low ceiling, or by a cliff of four rows
  or more, or a pit past its leap. No draw: it is a pure function of the grid.
- **The loper,** 2 × 2, 12 hit points: six axe hits, twelve of the drill, six of the fitted drill. Wanders at 2 cells a
  second, flees at 15 for 3 s, then calms to idling and wandering: faster than a hero's sprint of 13.2, so a blow is the end of
  the chase for now — the hunter follows and finds it calm again, corners it at a cliff or a wall, or will stun it (the arc's
  stun module to come). Tuned in play twice: at 9, then 6.4 (under a walk, so a hunter kept up and the drill held it), the
  user wanted it to outrun a sprint so a stun has a point.
- **A hit** (`Strike(Creature, Tool)`) is any weapon's, its damage the tool's `ToolStats` for its sender (0098), refused
  `fauna.gone`, `items.flat`, `items.notRunning` as wrecks' hits are (0054, 0055). It knocks the creature away from the
  striker's reported cell (`Knock` 200 a tick, a hop of under a cell). **Hit-stun is the species' own** (`Species.Stun`, ticks
  steering nothing before it flees, then flight from the nearest hero): neutral creatures have none and bolt the knock's way at
  the first blow; a hostile may hold still under one. The loper's is 0 (the user's word after the first recording: with an
  8-tick flinch on every hit the drill held it in place and ground it down; a neutral creature must get away). A faster tool
  knocks its pace's share (`Fauna.KnockPace`, the axe's 10 ticks): each drill hit a fifth, so ten hits a second do not bat it
  across the screen; and a blow never slows one already running the knock's way, so the drill's run of small knocks cannot hold
  it under the bit either. No hop renewed while it rises. The last hit drops its yields by nobody from its top
  row, so they fall where it stood: placeholders, two or three biomass and a seed one time in four. The roll is debris' loot
  roll, moved to `Core` (`Loots.Roll`) for both.
- **Dev:** `Release(Species, Cell)`, a command as `world.open` is, refused `fauna.notHere`, `fauna.full`, `fauna.blocked`;
  the client's `--fauna` sends it.
- **Save format 27:** an empty fauna slice after every other, aboard and on every surface.

## Slice 2: the client (`features/fauna`)

- **A blow is felt the frame it is struck.** This player's own `Strike` makes its creature react the frame it is sent, a round
  trip before the sim's knock arrives; a friend's as the sim's damage rises. The reaction (`CreatureMotion`): a white flash, a
  squash that springs back past round, a flinch thrown the way it is knocked, a wide eye and pinned ears, a 70 ms shake, the
  blade stopping on it with the figure's own bite; then the sim's knock, hop and flight, drawn between ticks. All of it is drawn
  only: the drawing is never held in place (the first version froze it 70 ms, which with the sim's stun read as pinning). A
  drill's hit is its share of the axe's, as for the ground (0094).
- **Seen and heard by what it is** (0093, 0094): an `Impact` per species (`Flesh`: white slash streaks, fur tufts, a warm puff,
  a glint and shock line, no blood) and a hit, thump and death per species, placeholders from the files here till the user
  picks. Death: a burst, puffs, a ring, the body swelling white and gone; its yields fall as the sim drops them.
- **The body is a placeholder** (the review's slice-1 art): a round body with ears, legs and one eye that watches a hero within
  9 cells, walking by the ground covered, leaning as it flees, tilted onto slopes under its feet.
- **Contact** is the wreck's (0039, 0054): the axe's swept edge and the drill's bit against each creature's body and a margin,
  but wherever the hero stands, not only out in space.
- `-- --planet --fauna` lets six lopers out by the hero and, without `--manual`, hunts them by script for recordings.

## Measured

On this machine, Release build, the first planet with a hero and its cap of 40 lopers walking, struck every half second:
**0.055 ms** a tick mean against 0.024 idle (`BudgetTests.KeepsAPlanetsTickWithFortyCreaturesUnderBudget`).

## Next

- **Stun from a module:** the arc's stun module will add hit-stun through a hit effect (`HitEffect`, 0054) on the tool, added to
  the species' own as the strike applies; nothing in the module reads it yet.

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
