# Catcher missile

- Status: accepted
- Date: 2026-11-10

## Context

In space the catcher keeps its range round its hero, fires its laser every 2.5 s and a barrage every 25 s
([catcher-motion](2026-11-10-catcher-motion.md)). Step 4 of `docs/plans/catchers-space.md` gives it a homing missile
beside them. The user's rules: a regular beat; it turns slowly after a hero and bursts on the first thing it meets; a hero
escapes it by dodging or by leading it into a wreck; it cannot be shot down; its burst breaks hull at full strength and the
room vents (one damage rule). Not on planets, and nothing else fires one.

## Decision

- **The beat (`HuntDrive`, space only).** Every `Hunt.Missile.Every` ticks (300, 15 s), its first a whole wait after the hunt's
  first look, a `Tell` (20 ticks, 1 s) starts: its quarry must be in sight within `Range.Far`, not shut in a room, with no barrage
  running. The tell ends in a launch from wherever the catcher is, at its quarry's middle, even if the quarry is out of sight
  by then: a tell is a promise. A barrage waits for a tell, and a barrage's end restarts the missile's wait. `Mem` keeps
  `missileAt` and `launchAt`, 0 for none. `FightOf` shows `LaunchLeft`.
- **The flight (`Shots.cs`).** A projectile that `Homes` (`Homing(Speed, Turn)`; the missile 1000 thousandths of a cell a tick,
  20 cells a second, and 24 fine headings a tick, some 42° a second) keeps an integer `Shot.Heading`, so its speed never shrinks
  from rotating its velocity. Each tick, before its common step, it takes the nearest awake hero by squared distance, in crew
  order, strictly nearer only: in co-op a friend draws it off. Within its burst's `Reach` (4 cells) of that hero, in sight, it
  bursts where it is. Otherwise it turns toward the hero by at most `Turn`. Then it flies as every shot does: it bursts on a
  cell, a creature or a wreck, or at its `Flight`'s end (160 ticks). Its catcher is in `Passed`, so it never meets it.
- **The burst** is `Blasting.Missile` (Power 450: a scrap hull's cell breaks, iron cracks; wrecks in reach take 60) and the shot's
  `Damage` 6 for heroes in reach and in sight.
- **Its look, the user's picks on two pick pages.** The missile «Самоделка» (`shot_missile`, `tools/anime-art/subjects/missile.js`):
  a scrap pipe with a welded cone, nose along its flight, flipped flying left. Behind it a torn orange flame with a pale heart and a
  warm light at the nozzle, and grey smoke puffs that swell and fade over 1.5 s, so its whole arc shows (`ShotLooks.Missile`,
  `ShotRocket`, drawn by `ShotStreaks`). The tell «Захват» (`CatcherFx.LockOf`): four amber corners round the quarry, 40 px closing to
  22, untwisting, blinking over the last quarter, snapping shut at the launch.
- **Its sound, the user's picks.** A fighter's lock-on warble as the tell begins (`CatcherLock`), heard as far as the salvo. A
  rocket's hiss looped where it flies (`MissileFlight`), cut off by its burst. It leaves as the salvo and bursts as the charge.
  Space mutes the World bus, so a catcher's launches, its bursts afloat, the lock and the hiss play on the Suit bus, as the sentry's
  arc does.

## Deliberate omissions

- Shooting the missile down: shots never meet shots. Loose pieces (not wrecks) do not stop it, as they stop no shot.
- Missiles on planets; homing for anything else or for heroes; leading its target. It turns toward where a hero is, so a hero who
  waits and darts across its way at the last moment escapes it.
- A laser shot during the tell, and a barrage while a missile flies: allowed, a harder moment.
- A missile launched beside a wreck or a wall bursts at once. The catcher keeps 20–35 cells off, past the burst's reach, so only
  the wreck takes it.
- No off-screen warning: the catcher keeps within its range, so its missiles start on screen.

## Consequences

- No new input and no format bump: heroes' reported cells only; `Heading` is written only when set; the `Mem` keys are 0 for none.
- A guest's dodge is judged about a round trip late, as against the shell.
- Every number is a placeholder for a play pass.
