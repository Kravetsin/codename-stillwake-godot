# Plan: the railgun

Branch `feat/railgun`, worktree `../stillwake-railgun`. ADR to write: `docs/decisions/2026-10-27-the-railgun.md`, amending
[shots pierce](../decisions/2026-10-27-shots-pierce.md) (its pierce kind "everything" and the memory of a body passed come here).

## The user's rules

- Crafted from an arc shard; held two-handed like the drill; its barrel drawn along the line to the pointer, straight up and
  down included (the shot's heading itself is one of 256, as every shot's).
- Fires nails now; a steel bolt later (data: its ammo list). A very fast nail, much less pulled by gravity.
- Pierces everything it hits, walls and ground too, losing damage at each (keep about 650‰); stops where its hit does
  nothing (bedrock, lava, a planet's fog, the ship aboard).
- A fixed charge of the battery a shot, like the drill's draw.
- Charge, all or nothing: hold about 0.8 s, a ready ping at full; release at full fires and spends; release earlier fires
  nothing, spends nothing. The charge time is the client's, as the pace is (0034).
- Art and sounds picked from a page (claude.ai/artifact/Mu11esXVX5tWcDafhpA3RC); until picked, the code runs on placeholders.

## Decisions

### Content (`sim/Content/Shots.cs`)

- `Gun` gains, with defaults that leave the nailer's line as it is: `Pierce Pierces = Pierce.Broken` (`enum Pierce { Broken,
  All }`: what its hit breaks or kills only, the nailer's; or everything it hits), `int Fall = 1000` (thousandths of the projectile's pull), `int Energy = 0` (power-ticks a shot), `int Charge = 0`
  (ticks held; 0 fires on the click).
- `Railgun = new("railgun", ["nail"], Damage: 22 (+ the nail's 2), BlockDamage: 4, Knock: 10, Speed: 15000, Reach: 384, Pace: 40,
  PierceKeep: 650, Pierces: Pierce.All, Fall: 100, Energy: 120, Charge: 16)`: 24 a hit, then 15, 9, 5, 3, 1; 4 off a block,
  then 2, 1; 300 cells a second, 25 ticks' flight; 29 a tick of gravity on the first planet; 15 shots a battery.
- Item `railgun` (1 a stack, gear, tool group); craft at the arc bench, 200 ticks: arcShard 1, copper 4, coil 2, tube 1,
  frame 1, circuit 2 `ContentTests.Tools`: Fall 0..1000, Energy and Charge ≥ 0, and PierceKeep < 1000 for `Pierce.All` (so a shot ends).

### Sim

- **Gravity:** `ShotFlight.Gravity(kind, projectile, gun)`: today's × `gun.Fall / 1000`, rounded down; no gun, the full pull.
  The sim's `Step` and the client's `StepOf` both pass the shot's gun.
- **`Shot.Passed`** (a `List<ShotPass>` of the existing `ShotPass`; saved through a `SavedPassed` shim, null while empty, as
  `SavedShots` is: no format bump): what a pierce-all shot hit and flew on through that still stands — a creature alive, a
  wreck whole, a cell scarred. `Pierced` (existing) counts every pierce, broken or not, and folds the damage; `Passed` holds
  only the survivors, skipped after. A body spans cells and a tick's last cell is asked again at the next; without it the
  shot would hit a survivor twice. At most six: the fold reaches 0 by then. `ShotTarget` gets explicit numbers, saved by them
  (never renumbered).
- **`IBuildingQueries.ShotTakes(int cell)`** (Building's first query interface): what `Hit` already guards by — the surface's
  tools break things (`Rules.ToolsBreak`: false aboard, so the ship stops a shot), the cell is inside the grid's `Zone` (a
  planet's fog is not), and it holds a breakable wall (not bedrock or lava, `Material.Unbreakable`) or a built piece. `Hit` reuses it. Creatures gains the dependency.
- **The walk** (`Meets`) for `Pierce.All`: targets as today, past `Passed` (a solid cell in it too); a target with nothing
  left ends the shot, listed (as today); a solid cell `ShotTakes` refuses ends it, listed with Damage 0 and Broke false; any other hit counts a
  pierce and the shot goes on, the target into `Passed` if it still stands. `Pierce.Broken` keeps today's path.
- **`StepOf`** judges the same: a pierce-all copy flies through creatures, wrecks and cells `ShotTakes` allows. A copy always
  starts with nothing passed, never seeded from the sim's `Passed`; its `passed` counts every pierce, as `Pierced` does.
- **Fuzz:** `FuzzFire` draws its gun from `Content.Shots.Guns`, now and then arming a railgun with a battery, charged or
  flat, so the generic suites fly pierce-all shots, write `Passed` and meet `items.flat`.
- **Energy:** `FireProblem` after ammo: `items.flat` when `Energy > 0` and the hero cannot pay it (`CanDrain`); `Apply`
  drains it (`Drain`; `CanDrain` and `Drain` exist on Items), nothing in creative.

### Client

- **Charge** (`GunHand.Step(elapsedMs, pressed, held, gun, …)`; `GunStep` gains `Charge` 0..1 and `Charged`, full; `Ready`
  keeps meaning the cooldown is out): a charge begins only on a fresh press that is not a grab (`HeroView.Hands`' grab owns
  the press otherwise), the cooldown out; it charges while held, `gun.Charge` ticks as real time; full stays full while held; released full fires (`Fire`, the copy,
  the cooldown); released early, a switch of item, a pose that does not hold, a pass-out or a change of surface (`Drop`) sends nothing and
  cuts the charge's sound. With nothing to fire or no energy (`FireProblem` `items.notEnough` / `items.flat`) a press clicks
  dry, no charge: `HeroView.Guns.Shoot` keeps the muzzle for `items.flat` as it does for `items.notEnough`. `Charge == 0` keeps fire-on-click.
- **Pose** (`TwoHand`; angles from the facing's forward, +90° straight up, −90° straight down): keys at −90° and +90°, `Up = Down = 90°`, the facing on the pointer's side: the barrel's line
  through the pointer at any heading. The drill shares it (one table; its picture points where its fan strikes). Tuned on
  `game/dev/PoseSheet.cs`; the drill checked against the helmet there.
- **Look:** `HeldLook.Railgun` (Aimed, Both, `Charged` picture): the charged picture over the plain one at the charge's
  alpha; at full a glint on the held stage. Its trail, by gun (`ShotLooks`), the user's pick.
- **Sound:** three cues, the user's picks: the charge (pitch rising over it, cut on an early release), the ready ping, the
  shot (by gun, in `HeroView.Guns` and `ShotsView`).
- **Card:** PIERCE "65%"; a tile for `Energy` when above 0, drawn as DRAW (`card.perShot`).

## Tasks

1. Content: the four Gun fields, the railgun, its item, craft and i18n; content tests.
2. Sim: gravity share, `Shot.Passed`, `IBuildingQueries.ShotTakes`, the pierce-all walk, `StepOf`, energy in `Fire`.
   READMEs (Creatures, Building), the ADR.
3. Client: `GunHand` charge, `HeroView.Guns` wiring, the pose, the look and sound on placeholders, the card.
4. Art and sounds once picked: export to `game/art`, cut the sounds, CREDITS rows.
5. A dev demo `--railgun`: a hero with the railgun on the test strip, sentries and soil ahead, firing level and straight up
   by script. Frames.
6. Full suite, format, ExportRelease; `/review-implementation`; PR.

## Tests

- **Scenarios** (`sim/Tests/RailgunTests.cs`): through a sentry (24 of its 30) once though the tick ends inside it, the loper
  behind for 15; three cells of stone take 4, 2, 1 and the creature behind 5; stopped at bedrock, the fog, a wall aboard;
  `Fire` drains 120, refused `items.flat` below it with nothing spent, nothing spent in creative; a save just after passing a
  creature loads and the creature is not hit again; `StepOf` matches the sim through a creature and a wall. The nailer's
  tests unchanged.
- **Unit:** the gravity share; the folds at 650.
- **Invariant** (`ShotsInvariantTests`): a pierce-all shot hits each thing at most once in its life; shown to bite with `[]`
  for `Passed` once.
- **Client** (`game.tests`): `GunHand` — early release sends nothing, full fires once, a long hold stays full, no energy
  clicks dry; `TwoHand` — at every degree −90..90 the barrel's line passes the pointer within 0.1 px and both fists reach.

## Deliberately not handled

- Friends do not see the coils fill or hear the charge: a friend's aim is never sent. They see the shot and its hits.
- No partial charge, overcharge, heat or module for the railgun.
- Energy is not reserved while charging: a battery moved off the belt mid-hold makes the release click dry.
- Two half-empty batteries do not pay one shot (Drain pays from one).
- Own walls and built pieces on a planet are scarred by a shot through them: aiming is the player's. Machines are not solid to
  a shot and are passed.
- Block damage folds to 0 after three cells (4, 2, 1): a creature behind four cells of stone is not reached.
- A pointer near the vertical flips the facing, so the pose mirrors there.
- A guest's copy and charge sound play though the host may refuse its `Fire` (a battery drained in the round trip); a hitch
  of a frame may finish a charge at once.
- Headings are 256 steps: at 300 cells a shot may land a few cells off; a finer heading is its own change if play asks.
