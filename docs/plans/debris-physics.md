# Design: Debris on host physics

Rules confirmed with the user, 2026-11-23, after playing a throwaway prototype (`../stillwake-physics-proto`): pieces are
rigid bodies on the host only; they collide with each other, never with the ship or heroes (as in Raft); the collision box
is about 80% of the picture, mass by area, no gravity, a wreck may turn; the harpoon's line is a rope anchored at the hero's
body pulling at the hook point, paying out under load up to 10 cells, right click lets go; only the hero pulls, never slowed;
hits land on the piece's turned shape. Answers of the same day: holding the left button (the gun's, as today) winds the line
in to 3 cells, E stays the grab; every piece tumbles, wrecks too; bursts push pieces in a try right after; the physics always
runs, wherever the host player stands.

**Words:** the *host player* is the peer running the session (peer 0); its client runs the bodies. *mc* is a thousandth of a
cell. A *turn unit* is a 65536th of a turn.

## Problem
A harpooned piece today sticks to a point in front of the muzzle. Pieces pass through each other, and a wreck is hit by an
upright box, so a turned wreck can only be hit by stepping into the box's corner. The prototype the user played had real
bodies and a rope, and it felt right. The game cannot do any of that yet.

## Player experience
1. The hero harpoons a hull section 20 cells away. The line sags while the section drifts on. When the line goes taut, the
   section starts turning about the hook point.
2. The hero jets away. The section lags behind, swings wide and pays the line out, up to 10 cells more. A crate it sweeps
   into goes spinning off.
3. Holding the left button reels the section in to 3 cells. The hero chops it where its turned picture is.
4. A right click lets go. The section keeps its swing, then settles back into the stream.
5. A friend sees the section turn the same way and shoots it where it is drawn.

How it goes wrong:
- The hero flies into the airlock while towing. The line drops at the door, the rod snaps back, and the section flies on
  outside, through the hull as all debris does.
- A guest on a 100 ms ping jets off at full speed. For a moment its line looks stretched a few cells, then the piece follows.
- Another hero hooks the same piece: the first hero's rod snaps back, as a lost line's always does.

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
- **A cap on the party size.** The user may add one if crowds break the physics budget; not now.

## Scale
- **World:** about 700 pieces: the ship stream (Max 60), the far field (640), the asteroid band (3); a stretch of space
  (stretches-of-space) adds its own stream for its length. Each piece is one body. Pieces rarely touch in normal play.
- **Players:** 4 or more, one line each.
- **Frequency:** at most `FeedMost` bodies a tick in the feed (20 Hz), none while nothing strays. `Hook`, `Wind`, `LetGo` on
  presses.

## Approach
**Sim: Debris keeps everything.** Each `Piece` gains saved motion fields: `Vx`, `Vy` in mc a tick; `Angle` in turn units,
`Spin` in turn units a tick. Each tick, on every peer, in the simulation phase in id order: `X += Vx`, `Y += Vy`,
`Angle = (Angle + Spin) & 0xFFFF`. The sim eases nothing: drift, drag and collisions are the host player's physics, reaching the
sim only through the feed. A piece then off the grid on any side is gone (before the catch). `Speed` stays the piece's own
drift, the speed the physics eases it back to. A new piece starts with motion `(−Speed, 0)` and an angle and spin from a fixed
integer mix of its id (a splitmix step, never `GetHashCode`), spin up to `TumbleMax`: every piece tumbles, wrecks too. The
made-up tumble in `DebrisView` moves into the sim, as hits read it.

**Lines** replace `Reel` and `Reeling` (in step 2): a saved list `{ By, Piece, HookX, HookY, Length, Winds }`, the hook point
in mc in the piece's own frame, `Length` in mc, `Winds` whether the left button is held. At most one line per hero and one per
piece. **Derived:** the line of each hero and of each piece, rebuilt in `Rebuild()`.

**Commands:**
- `Hook(Piece, HookX, HookY)`: the hook point clamped into the piece's box. `Length` starts at the anchor-to-hook span, at
  most `Harpoon.Range`. Replaces the sender's own line; the last hook on a piece wins, the other line dropped. Refused
  `debris.gone`, `debris.notOutside`.
- `Wind(On)` and `LetGo`: refused `debris.noLine`. The client sends them only while it sees its line, and `Wind(false)` when
  the harpoon leaves the hand.
- `Moved(ForTick, Bodies, Lines)`: the host player's feed. Bodies: `Id, X, Y, Vx, Vy, Angle, Spin`, the pose at the start of
  `ForTick`, before its drift. Lines: keyed by `(By, Piece)`, each `Length`. On apply in the commands phase, in id order, each
  body is set and moved on `Tick − ForTick` ticks of its own motion; a gone piece or a line no longer matching is skipped.
  Refused `debris.notHost` from anyone but the host player. At most `FeedMost` bodies, the farthest strayed first, the rest
  the next tick.

**Winding and paying out** are the host player's rope: it pays out under load up to `PayOut` beyond the hooked `Length`,
winds in at `ReelSpeed` while `Winds`, paid-out rope first, to `Stop`; the feed carries the new `Length`. At `Stop` winding
does nothing.

**A line drops** (`Winds` cleared with it) when its holder leaves, passes out or is reported outside space; when the holder's
client enters a room (it sends `LetGo` at once); when its piece goes for any reason: grabbed, broken, caught, despawned, off
the grid; when a sentry cuts it (the `Unreel` op); when another hook takes the piece. The holder's rod snaps back.

**Every reader of a wreck's place uses one turned shape:** `Shape(piece)` with `Gap(x, y)`, the distance in mc to the turned
box, 0 inside (covering a cell is a gap of 0), and `At(local)`, a point of the piece turned into the world; replacing
`WreckBox`. A hit changes no shape: a wreck takes damage whole and, broken, is gone with its body.
- Debris: the `HewWrecks` fan and `HewPieces`.
- Creatures: shots (`WreckIn`) and bursts by `Gap`; `WreckAt` in `Clinger.cs` loses its copy of the box. Clingers, sentries
  and nests stand on their wreck, feet on its surface: their perch point turned with it (`At`), their body drawn turned by the
  wreck's angle, and their box turned the same way for every hit (the same `Shape`).
- Unchanged, judged by the middle cell: nets, grabs, salvage drones, the far field's despawn.
- The far field keeps a piece on a line.

**The host player's client** (`features/debris`):
- One rigid body per piece in its own physics space through PhysicsServer2D, stepped at the physics rate wherever the host
  player stands, on a planet too.
- Bodies made and freed as pieces appear and go in the sim; set from the sim's pose when the sim moved it otherwise (a load).
- Each body's box is `ShapeShare` of its picture, mass its area times `Density`, no gravity, eased back toward its own drift and
  tumble by `Drag`.
- Bodies collide only with each other; a spawn on another piece is left to the solver to separate.
- The rope is `rope.gd` ported into a `logic/` class with its brake (`Brake`, `BrakeRamp`), anchored at the holder's body:
  the host player's hero, or a guest's latest sample extrapolated at most `AnchorAhead` (0.5 s), then held.
- After each tick it sends `Moved` for every body whose worst corner (any corner of its box) strays from the sim's pose by
  more than `FeedSlack` (an eighth of a cell): one threshold for place and turn.
- **Core and net:** a command type marked surface-addressed carries its surface and is applied there whatever the sender's;
  `Client.InFlight` keeps it when the sender changes surface. Only `Moved` is so marked.

**Guests and the client:**
- `PieceDrawn` returns a full pose (position and angle), extrapolated between ticks; a correction snaps (about a pixel);
  easing only if the co-op try shows a jump.
- Aim, `ToolEdge`, `ToolRays`, the scan, the rod's hit test, `WreckPixels`, the break and net views call the sim's `Shape`.
- A line is drawn from its holder to the hook point, taut once the span reaches `Length`.
- Refusals have their keys in En and Ru.

**Saves:** motion and lines saved and hashed. Step 1 bumps `World.SaveFormat` (old pieces get `(−Speed, 0)`, angle and spin
by the id rule); step 2 bumps it again (old reels dropped). Saves break at release anyway. After a load the host player
rebuilds its bodies from the save; contacts are not kept.

## Determinism and co-op
- **Inputs:** the physics runs only in the host player's client and enters the sim only through `Moved`.
- **Units:** mc and turn units, the host player rounding to nearest.
- **Turning a shape:** Core's sine table, `Rays.FineDirection(Angle >> 4)` (4096 headings); products in `long`, truncated
  toward zero; a point exactly on an edge is inside. The client tests hits with the same code.
- **Order:** `Moved` in the commands phase, host order, bodies in id order, carried on from `ForTick`; then the drift.
- **Joining mid-session:** the save holds every pose, motion and line.
- **Trust:** the host player's word on bodies and the holder's own `LetGo`, as 0002 allows among friends.
- **Performance:** in the prototype 700 bodies cost about 0.5 ms; a pile-up of 2549 bodies with 6502 contacts 7.5 ms. The
  stray check is one pass a tick; the sim's per-piece work is today's drift. The wire carries nearly nothing in normal play,
  33.6 KB/s per guest in that pile-up.

## Trade-offs accepted
- **A guest's towed piece trails by the round trip:** at full jet speed the line looks a few cells stretched; reads as
  elasticity; prediction is excluded by 0002.
- **Guests see collisions as small snaps,** up to about an eighth of a cell (a pixel).
- **A spawn on another piece** may pop apart once.
- **A creature's own moves on a turning wreck** (a sentry's pounce, an arc) still start along the world's axes from its perch;
  only its stance, drawing and box turn with the wreck.
- **A delivery knocked off the grid is lost,** as one past its end is today; it takes a wreck swung on the trade station's row.
- **The reported-cell check for dropping a line can be up to a second late;** the holder's own `LetGo` on entering a room
  covers what players hit.
- **Hits meet the piece where it is at the tick;** the screen draws between ticks, up to one tick's motion apart, as today.
- **A guest's stalled anchor** holds the rope still for up to half a second, then the line is where the guest was.

## Tests
- Scenarios: `Moved` sets a pose and carries it on from `ForTick`, a gone piece and a stale line skipped, a guest's refused,
  one from the host player on a planet applied; on a wreck turned 45° a fan through the old box's corner misses and one through
  the turned tip hits; a burst's `Gap` to the turned box; a clinger on its turned perch, a shot meeting its turned box; step 1: an old reel on a tumbling
  wreck, hit on its shape; one per refusal of `Hook`, `Wind`, `LetGo`; the hook point clamped into the box, the starting
  `Length`; one per drop reason (pass-out, room, grab, break, despawn, off the grid, a sentry's cut, another hook); the far
  field keeps a held piece, never netted; a piece leaving the grid gone; each save migration.
- Invariants: every piece on the grid; angles in range; at most one line per hero and per piece, each on a live piece; a
  turned box's gap to its middle cell 0; a box turned half a turn the same gaps.
- game.tests: the rope pays out to `PayOut` and winds to `Stop`, its brake; a heavy piece lags a light one; extrapolation;
  aiming at turned boxes; the feed sends only straying bodies, at most `FeedMost`; the pile-up budget at 2500 bodies, three
  times the world's count, measured. Debris's `Fuzz` draws `Moved` (full size, gone pieces, from a guest), `Hook`, `Wind`,
  `LetGo`; the desync suite draws `Moved` from the host player on another surface.

## Cost
- Modules: Debris, Creatures (wreck readers), Core and net (the surface-addressed command); game features debris, crew (rope,
  line), shared.
- Content, the prototype's numbers where it had them: Debris `ShapeShare` 800‰, `Density`, `Drag`, `TumbleMax`,
  `FeedSlack` (125 mc), `FeedMost`, `AnchorAhead`; Harpoon `PayOut` 10 cells, `Stop` 3 cells (as today), `ReelSpeed`,
  `Brake`, `BrakeRamp` 8.
- ADRs: a new `debris-host-physics` supersedes 0006 and 0044's reel-to-point (gun, shot, cooldown, grapple stay) and 0034's
  upright box; 0052 gets one line. The Debris README's "left of its `Start` unless reeled" becomes "unless moved by the feed".
- About 2,200 lines, 700 of them tests.

## Settled with the user
- **Winding:** the left button held, as the gun's press today; E stays the grab (E came from the prototype only).
- **Tumbling:** everything, wrecks too; so creatures stand on their wreck feet on its surface, turned with it.
- **Bursts push pieces:** a small try right after co-op.
- **Host away:** the physics always runs; the feed is addressed to the debris surface.
- **Guest towing lag:** accepted, measured in the co-op step with `--lag 60/120` (prediction would go beyond 0002).

## Users this stage and next
- This change builds three shared parts: the pose feed, the bodies, lines. Its own users: the harpoon (a line at a hero) and
  collisions between pieces.
- Next, on the same parts: the drone's thread hauling a piece (a line at the drone); bursts pushing pieces (the host player
  reads the tick's bursts); drums bursting on impact and ship nets that stop pieces and tear (contacts as a second list in the
  feed, nets as static bodies); the two-ended scoop net (a line between two anchors).

## Steps
1. **Tumbling pieces.** Angle and spin in the sim, every piece tumbling, the turned shape for every reader, creatures on
   their turned perch, the client drawing the sim's pose; the old reel stays; its save bump. `Vx`, `Vy` wait for step 2, their
   first user (the feed), and its bump gives old pieces `(−Speed, 0)`. Play: wrecks turn, hits land on the picture.
2. **Bodies and rope, single-player.** The bodies and the feed, the rope (`Hook`, `Wind`, `LetGo`; `Reel` removed); its save
   bump. Play: the prototype's feel, in the game.
3. **Co-op.** Others' lines from `Lines`, the host player anchors guests' ropes, the surface-addressed feed, `FeedSlack`
   tuned under `--lag` across two processes. Play: with a friend.
4. **Bursts push pieces** (a next user, its own small design).
