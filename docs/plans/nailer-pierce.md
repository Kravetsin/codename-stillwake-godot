# Plan: a gun's shot pierces, the drawn nail follows the sim's, weapon cards show both damages

Branch `feat/nailer-pierce`, worktree `../stillwake-pierce`. ADR to write: `docs/decisions/2026-10-27-shots-pierce.md`,
amending [shots are the sim's](../decisions/2026-10-26-shots-are-the-sims.md).

## The user's rules

- After a hit that pierces, a shot flies on with both damages (bodies, blocks) multiplied by its gun's keep, thousandths,
  rounded down. The nailer keeps 500: a nail does 6, then 3, then 1, then 0. The railgun (the next task) keeps 650–700:
  one logic, other numbers.
- The nailer pierces only what its hit breaks or kills: a wreck broken, a creature killed, a cell broken. A hit that does
  not, ends the shot as today. The railgun will pierce everything; its task adds that kind.
- The bug: in single-player a nail that breaks a wreck is drawn flying on through it, doing nothing. The sim's shot did
  stop and hit; the shooter's own copy (`GunHand`) steps after the world's tick, when the wreck is already gone.
- Item cards: every weapon and gun shows its damage to bodies (creatures, wrecks) and to blocks; the nail shows what it
  adds. Built from `sim/Content`, so the railgun's card comes free.

## Decisions

### Content

- `Gun` gains `PierceKeep`, thousandths, 0..1000 (a content test holds it there); the nailer 500. 0 would make every
  pierce leave nothing, so the shot ends at the next body.
- `Content.Shots.DamageOf(gun, projectile, pierced = 0)` and `BlockDamageOf(…, pierced = 0)`: the sum as today, then
  `× PierceKeep / 1000` `pierced` times, each rounded down (damages are never negative: integer division floors). No gun
  (a creature's, the sentry's arc): no fold.
- Expected: 6/1 at 500 → 6/1, 3/0, 1/0, 0/0; 6 at 700 → 6, 4, 2, 1.

### Sim (Creatures, Debris, Building)

- **Hits say whether they broke.** `IDebrisOps.Hit` and `IBuildingOps.Hit` return `bool`, true when the wreck, or what
  stood in the cell, broke. `CreaturesModule.Hurt` returns true when the creature died. Callers that do not care
  (`Strike`, `HewWrecks`, `Hew`) ignore it. `IDebrisQueries.Breaks(int piece, int damage)` and a private
  `CreaturesModule.Breaks(Creature, int damage)` are the checks those ops decide by, so a prediction and the sim agree on
  the same world.
- **`Shot.Pierced`**, int, `[JsonInclude]` with the other fields, 0 = none yet: no `SaveFormat` bump.
- **The walk in `Fly`.** `ShotFlight.Step` is unchanged but for its doc ("the caller may change cells as it is asked").
  `Fly`'s own callback, for a cell, repeats: take the first target as today (solid, then the lowest-id live creature,
  then the first wreck still out); none, the cell does not stop the shot. The damage for its kind (block damage for
  solid, else damage) at `Pierced`:
  - 0: the shot ends `Met` there, no op called (no `Struck`, no knock, no draw from the Rng);
  - else the op is called and a `ShotHit` recorded; broke or killed: `Pierced += 1` and look again in the same cell,
    the broken thing gone; else the shot ends `Met` there.
  So every target in a cell is met in order; a pierce goes on walking in the same tick, its point and arc as if nothing
  had been there. A knock is the gun's whole `Knock` on every hit.
- **`StepOf` for the client stays a query.** The public `StepOf(f, p, gun, pierced, passed)` uses a second callback that
  never calls an op: it predicts by `Breaks` (wrecks and creatures; a solid cell stops the copy), skips the ids in
  `passed` (creatures and wreck pieces it has predicted broken), and returns the hits it predicted. A test steps it on a
  world and checks the world's hash unchanged.
- **Derived `Hits`**: `ShotHit(int Shot, Ref By, string Projectile, string? Gun, int Cell, (int X, int Y) From,
  ShotTarget What, bool Broke)`, `ShotTarget { Solid, Creature, Wreck }`; `From` the shot's point as the tick began, where
  today's end effects are drawn from. Every hit of the tick in order, cleared as `Fly` starts, `[JsonIgnore]`. `Ended`
  stays as it is.

### Client

- **What draws which shot** goes into `logic/ShotDraws.cs` (testable): the host and single-player (`Session.Host != null`)
  draw every shot, their own too, from the sim, and fly no copies; a guest draws others' from the sim and its own as
  copies, a round trip ahead. The host's `Fire` applies on the next tick, when the copy's first step was: nothing lost.
- **`ShotsView`** draws the shots `ShotDraws` gives it, and shows every `ShotHit` of the tick but a guest's own (its copy
  shows those), so a shot born and ended in one tick still shows its hits. It plays no fire sound for the own hero's
  shots: `HeroView` played it at the click.
- **The guest's copy** (`GunHand`, `OwnShot`): carries `Pierced` and `Passed` (ids), steps by the public `StepOf`, and
  `Tick` returns the hits it predicted, shown as they happen; `TakeEnded` as today.
- **`ShotEnds.Show(ShotHit)`** draws by `What` and `From`, not guessing from the grid. A hit that broke its target only
  sounds: the break's own effects (Building's, Debris's) show the rest, as the material is gone.
- **Cards.** `ItemCards.IsTool` counts guns (their slots tinted as tools, as they are tools); ammo (a projectile in some
  `Gun.Ammo`) gets its own branch of `Card`, not a tool tint.
  - Weapon: DPS, DMG, BLOCK (`card.block`, boosted by modules through `ToolStats.BlockDamageOf`), HIT; the drill's draw.
  - Gun: DMG and BLOCK with its first ammo at `pierced` 0; RELOAD (`Pace`, the harpoon's label); PIERCE, the keep as
    "50%" (`card.pierce`).
  - Ammo: DMG `+2`, BLOCK `+0`, the projectile's own.
  - `ItemCardView` wraps tiles in rows of three when a card has more than three.

## Tasks

1. **The failing test first** (`game.tests`): a world with a 6 hp wreck before a muzzle, one `Fire`; each tick, the
   drawn nail as the frame draws it (today: `GunHand` stepped after the tick). Assert it ends where the sim's `ShotEnd`
   is. See it fail. After the fix the same test reads the drawn shots through `ShotDraws` as the host.
2. **Content:** `PierceKeep`, the folds, unit tests (the expected values above; no gun, no fold), the 0..1000 test.
3. **Ops return bools** and `IDebrisQueries.Breaks`; Debris and Building READMEs, a line each.
4. **Creatures:** `Shot.Pierced`, the walk, `Hits`, `Breaks`, the public `StepOf`. README: Rules, Guarantees (a shot never
   passes a solid cell that still stands after its tick), Deliberately not handled. `ShotsInvariantTests` to match.
5. **Client:** `ShotDraws`, `ShotsView`, `HeroView.Guns`, `GunHand`, `ShotEnds`.
6. **Cards:** `ItemCards`, `ItemCardView`, i18n, tests for the nailer, the nail, the axe, the drill.
7. **ADR, READMEs, map lines**; `tools/adr-index.ps1`; delete this plan.
8. **Frames:** `--nailer`: a nail through a broken wreck into the next; the cards. Full suite, format, `ExportRelease`. PR.

## Tests (scenario, `sim/Tests/ShotsTests.cs`, unless named)

- A nail breaks a wreck and hits a creature behind it for 3, in one flight.
- A wreck the nail does not break ends it, `Met` there.
- The chain: three wrecks with 6, 3 and 1 hp left all break; a fourth body is not hit (0 left), the shot ends `Met` there
  and that body's `Struck` is untouched.
- A creature killed and a wreck in its cell: both hit, in that order, in one tick.
- A nail breaks a cell of strength 1, flies on at block 0, and the next wall ends it uncracked.
- A shot that pierced ends the tick at the point an unobstructed one would.
- Save just after a pierce, load: plays on alike (`Pierced` saved). Two worlds kept alike through a chain whose breaks
  roll loot.
- `StepOf` predicts a chain as `Fly` then plays it, and leaves the world's hash unchanged; a wreck of several cells is
  predicted broken once (`passed`).
- Mutation once: drop the rounding in the fold and watch the chain fail.

## Deliberately not handled

- **A guest's copy at a block it would break** stops there; the sim's shot flies on, the guest seeing its hits only
  through what breaks.
- **A guest's prediction is wrong** when another peer's blow lands first: cosmetic, as before.
- **A frame that runs two ticks** shows only the last tick's hits, as `Ended` today; a joiner sees no hits of shots
  fired before it came.
- **Knock is not kept**: every hit knocks with the gun's whole `Knock`.
- **Pierce kind `All`** and the memory of a living body passed come with the railgun.
