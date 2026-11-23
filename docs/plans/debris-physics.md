# Design: Debris on host physics

Rules confirmed with the user, 2026-11-23, after playing a throwaway prototype (`../stillwake-physics-proto`): pieces are
rigid bodies on the host only; they collide with each other, never with the ship or heroes (as in Raft); the collision box
is about 80% of the picture, mass by area, no gravity, a wreck may turn; the harpoon's line is a rope anchored at the hero's
body pulling at the hook point, paying out under load to 10 cells, E reels to 3, right click lets go; only the hero pulls,
never slowed; hits land where the piece is drawn.

## Problem
A harpooned piece today sticks to a point in front of the muzzle. Pieces pass through each other, and a wreck is hit by an
upright box, so a turned wreck can only be hit by stepping into the box's corner. The prototype the user played had real
bodies and a rope, and it felt right. The game cannot do any of that yet.

## Player experience
1. The hero harpoons a hull section 20 cells away. The line sags while the section drifts on. When the line goes taut, the
   section starts turning about the hook point.
2. The hero jets away. The section lags behind, swings wide and pays the line out, up to 10 cells. A crate it sweeps into
   goes spinning off.
3. Holding E reels the section in to 3 cells. The hero chops it where its turned picture is.
4. A right click lets go. The section keeps its swing, then settles back into the stream.
5. A friend sees the section turn the same way and shoots it where it is drawn.

How it goes wrong:
- The hero flies into the airlock while towing. The line drops at the door, and the section flies on outside, through the
  hull as all debris does.
- A guest on a 100 ms ping jets off at full speed. For a moment its line looks stretched a few cells, then the piece follows.

## Out of scope
- **Collisions with the ship or heroes.** The user chose this: debris passes the ship, as in Raft.
- **Bursts pushing pieces, and drums bursting on impact.** Next users. The fuel-drums ADR's "no push" stands for now.
- **Ship nets that stop pieces and tear, the two-ended scoop net, the drone hauling by thread.** Next users; no field or
  hook goes in for them now.
- **Two lines on one piece.** The last hook wins, as in 0044.
- **A rope that wraps around pieces.** The line passes through everything.
- **Guests predicting their own held piece.** That would go beyond 0002.
- **Splitting bodies into near and far, or sleeping ones.** Every piece is a body: within budget, and a lone drifting piece
  costs nothing on the wire.
- **Debris on planets.** Planets have no debris zones (0076).

## Scale
- **World:** up to about 800 pieces: the stream (Max 60), the far field (640), the asteroid band (3), a stretch's stream.
  Each piece is one body on the host. Pieces rarely touch in normal play.
- **Players:** 4 or more, one line each.
- **Frequency:** at most one feed command per tick (20 Hz), none while nothing strays. `Hook`, `Wind`, `LetGo` on key presses.

## Approach
**Sim: Debris keeps everything.** Each `Piece` gains saved motion fields:
- `Vx`, `Vy`: thousandths of a cell per tick.
- `Angle`, `Spin`: in 65536ths of a turn, the spin per tick.

Every tick, on every peer, each piece moves by its motion. `Speed` keeps its role as the piece's own drift: the speed the
physics eases it back to. A new piece starts with motion `(−Speed, 0)`. Its starting angle and spin come from its id, so
spawning draws nothing new from the Rng: one rule by size, small pieces tumble up to `TumbleMax`, wrecks start still, as
drawn today (the made-up tumble in `DebrisView` moves into the sim, as hits read it).

`Reel` and `Reeling` are removed. A saved list, `Lines`, replaces them: `{ By, Piece, HookX, HookY, Length, Winds }`, the
hook point in the piece's own frame. At most one line per hero and one per piece.

**Commands:**
- `Hook(Piece, HookX, HookY)`: refused `debris.gone`, `debris.notOutside`. A new hook replaces the sender's own line; the
  last hook on a piece wins.
- `Wind(On)` and `LetGo`: refused `debris.noLine`. The client sends them only while it sees its line (E also grabs).
- `Moved(Bodies, Lines)`: the host player's feed: each straying piece's `X, Y, Vx, Vy, Angle, Spin` and each line's
  `Length`. A piece gone by the tick it applies on is skipped.

**A line drops** when its holder leaves, passes out, or is reported outside space; when the holder's client enters a room
(it sends `LetGo` at once); when its piece is grabbed, broken or caught; when a sentry cuts it (`Unreel`).

**Every reader of a wreck's place uses one turned shape:** `Shape(piece).Gap(x, y)`, the distance to the turned box, 0
inside (covering a cell is a gap of 0), replacing `WreckBox`.
- Debris: the `HewWrecks` fan and `HewPieces`.
- Creatures: shots (`WreckIn`) and bursts; `WreckAt` in `Clinger.cs` loses its copy of the box. Clingers and sentries keep
  their perch upright (a trade-off below).
- Unchanged, judged by the middle cell: nets, grabs, salvage drones, the far field's despawn.
- New rules: the far field keeps a piece on a line; a piece pushed off the grid on any side is gone.

**The host player's client** (`features/debris`):
- One rigid body per piece, in its own physics space through PhysicsServer2D, running wherever the host player stands.
- Bodies made and freed as pieces appear and go in the sim.
- Each body's box is `ShapeShare` of its picture, mass from that box's area, no gravity, eased back toward its own drift and
  tumble.
- Bodies collide only with each other; a spawn on another piece is left to the solver to separate.
- The rope is `rope.gd` ported into a `logic/` class, anchored at the holder's body: the host's own hero, or a guest's latest
  sample extrapolated to now.
- After each tick the host sends `Moved` for every body whose worst corner strays from the sim's extrapolation by more than
  `FeedSlack` (an eighth of a cell): one threshold for place and turn.
- `Moved` is addressed to the debris surface and accepted from the host player wherever its hero stands: a small change in
  Core and net, as today every command goes to its sender's surface.

**Guests and the client:**
- `PieceDrawn` returns a full pose (position and angle), extrapolated between ticks; a correction snaps (at most about a
  pixel); easing only if the co-op try shows a jump.
- Aim, `ToolEdge`, `ToolRays`, the scan, the rod's hit test, `WreckPixels`, the break and net views test the turned box.
- `DebrisView`'s made-up tumble and rocking go.
- A line is drawn from its holder to the hook point, taut once the span reaches `Length`.

**Saves:** the pieces' motion and the lines are saved and hashed. `World.SaveFormat` is bumped; its migration gives each old
piece `(−Speed, 0)` motion and drops old reels (saves break at release anyway). After a load the host rebuilds its bodies
from the save; contacts are not kept.

## Determinism and co-op
- **Inputs:** the physics runs only in the host player's client and enters the sim only through `Moved`.
- **Units:** positions and speeds in thousandths of a cell, angles in 65536ths of a turn; the host rounds to nearest.
- **Turning a shape:** Core's sine table (`Rays.FineDirection`); products in `long`, truncated toward zero; a point exactly
  on an edge is inside.
- **Order:** `Moved` applies in the commands phase, in host order, its bodies in id order; the drift then moves every piece
  one tick. The host sends each body as it will be at the start of the tick its command applies on.
- **Joining mid-session:** the save holds every pose, motion and line.
- **Trust:** the host's word on bodies and the holder's own `LetGo`, as 0002 allows among friends.
- **Performance:** in the prototype 700 bodies cost about 0.5 ms; a pile-up of 2549 bodies with 6502 contacts 7.5 ms. The
  host's stray check is one pass a tick; the sim's per-piece work is today's drift. The wire carries nearly nothing in normal
  play, 33.6 KB/s per guest in that pile-up.

## Trade-offs accepted
- **A guest's towed piece trails by the round trip:** at full jet speed the line looks a few cells stretched; reads as
  elasticity; prediction is excluded by 0002.
- **Guests see collisions as small snaps,** up to about an eighth of a cell (a pixel).
- **A spawn on another piece** may pop apart once.
- **A sentry rides a turned wreck's perch drawn upright.** Wrecks turn only when pushed or roped: rare.
- **A delivery knocked off the grid is lost,** as one past its end is today.
- **The sim's reported-cell check for dropping a line can be up to a second late;** the holder's own `LetGo` on entering a
  room covers what players hit.
- **The sim hits wrecks where they are at the tick,** the screen draws between ticks: up to one tick's motion apart, as today.

## Tests
- Scenarios: `Moved` sets a pose and the drift carries it on, a gone piece skipped; on a wreck turned 45° a fan through the
  old box's corner misses and one through the turned tip hits; a burst's `Gap` to the turned box; a clinger on its turned
  perch; one per refusal of `Hook`, `Wind`, `LetGo`; one per drop reason (pass-out, room, grab, break, a sentry's cut);
  the last hook wins, one line a hero; a held piece kept by the far field, never netted; a piece leaving the grid gone; the
  save migration.
- Invariants: every piece on the grid; at most one line per hero and per piece, each on a live piece; a turned box's gap to
  its middle cell 0; a box turned half a turn the same gaps.
- game.tests: the rope pays out to `PayOut` and winds to `Stop`, its brake; a heavy piece lags a light one; extrapolation;
  aiming at turned boxes; the feed sends only straying bodies; the pile-up budget (2500 bodies) measured. Debris's `Fuzz` draws `Moved` (gone pieces too), `Hook`,
  `Wind`, `LetGo`.

## Cost
- Modules: Debris, Creatures (wreck readers), Core and net (the surface-addressed command); game features debris, crew (rope,
  line), shared.
- Content: Debris `ShapeShare` 800‰, `Density`, `Drag`, `FeedSlack`, `TumbleMax`; Harpoon `PayOut` 10 cells, `Brake`,
  `BrakeRamp`.
- ADRs: a new `debris-host-physics` supersedes 0006 and 0044's reel-to-point (gun, shot, cooldown, grapple stay) and 0034's
  upright box; 0052 gets one line.
- About 2,200 lines, 700 of them tests.

## Open questions
1. **E both reels and grabs.** (a) Holding E winds, a press grabs the held piece once within hand reach; (b) E only winds,
   grabbing gets another key. Recommend (a).
2. **Guest towing lag.** (a) Accept it, measure in step 2 with `--lag 60/120`; (b) predict a guest's own held piece (a new
   ADR beyond 0002). Recommend (a).
3. **Which pieces tumble.** (a) Small ones, as today; wrecks start still and turn only when pushed or roped; (b) everything
   spins, as in the prototype. Recommend (a), so sentries and nests stay upright.
4. **When bursts push pieces.** (a) Its own small try right after step 2; (b) later. Recommend (a).
5. **The host player on a planet while a guest tows in space.** (a) The feed is addressed to the debris surface, a small
   change in Core and net, so physics runs wherever the host stands; (b) physics pauses while the host is off the debris
   surface, pieces drift on by stored motion, and a guest's `Hook` is refused (`debris.hostAway`). (b) is less work;
   (a) if the party often splits.

## Users this stage and next
- This change builds three shared parts: the pose feed, the host's bodies, lines. Its own users: the harpoon (a line at a
  hero) and collisions between pieces.
- Next, on the same parts: the drone's thread hauling a piece (a line at the drone); bursts pushing pieces (the host reads the
  tick's bursts); drums bursting on impact and ship nets that stop pieces and tear (contacts as a second list in the feed,
  nets as static bodies); the two-ended scoop net (a line between two anchors).

## Steps
1. **Single-player.** Motion and angle in the sim, the bodies and the feed, the rope (`Hook`, `Wind`, `LetGo` replace
   `Reel`), the turned shape for every reader, one save bump. Turned pieces alone are not playable (nothing turns a wreck
   without bodies or a rope), so they come together. Play: the prototype's feel, in the game.
2. **Co-op.** Others' lines from `Lines`, the host anchors guests' ropes, `FeedSlack` tuned under `--lag` across two
   processes. Play: with a friend.
