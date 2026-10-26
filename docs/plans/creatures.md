# Plan: Creatures composed, then the sentry

## Why

The game will have very many and very different creatures (`2026-10-26-creatures-are-composed`). Today a species is a
flat record and its behaviour a mode switch, so each new creature would touch the switch, the save and the client.
This plan turns the module into the composed one that ADR describes, without changing how any creature behaves, and
then adds the first hostile, the sentry robot on a derelict, as its first new species.

**Order and PRs.** Each task below is its own branch and PR, merged before the next starts. C-0 and C-1 change
nothing a player sees; C-2 to C-5 bring the sentry. C-0 and C-1 touch most of the module and its client: agree a
window with the other developer so nobody else edits `sim/Fauna` or `game/features/fauna` meanwhile.

**Words.** A *piece* is one piece of debris (Debris' `Piece`); a *derelict* is a far-field wreck kind (0056). *Lifetime*
is the record saying how long a creature lives; the *keep box* is `Keep`, the box round each hero that keeps creatures.
A `Hit` is the record the hit path takes; the amount a move takes is its `Damage`. Times are in ticks (20 a second),
lengths in cells, speeds in thousandths of a cell a tick (`Unit`), distances to heroes by box distance (the larger of
the two axes).

## Tasks

### C-0 — The module is called `Creatures` ✅

A rename and nothing else: robots and pets are not fauna.

- **Sim:** `sim/Fauna` → `sim/Creatures` (`Creatures.csproj`, namespace `Stillwake.Sim.Creatures`), `FaunaModule` →
  `CreaturesModule`, `FaunaState` → `CreaturesState`, module id `fauna` → `creatures`, so its commands become
  `creatures.strike`, `creatures.hitBy`, `creatures.release`; `Registry.Fauna` → `Registry.Creatures`, `Registry.csproj`
  and `Stillwake.sln`. `Content.Fauna` → `Content.Creatures` (its `Species`, `Habitat`, `Place` and numbers unchanged),
  `PlanetType.Fauna` → `PlanetType.Creatures`.
- **Refusal keys** `fauna.*` → `creatures.*` (`notHere`, `full`, `blocked`, `gone`, `notAttacking`) and their texts in
  `Fauna.En.cs` → `Creatures.En.cs`.
- **Client:** `game/features/fauna` → `game/features/creatures` (`FaunaView` → `CreaturesView`);
  `game/Content/logic/FaunaArt.cs` and `FaunaLooks.cs` → `CreatureArt.cs`, `CreatureLooks.cs`; the references in
  `Main.cs`, `render/Layers.cs`, `HitLooks.cs`, `Sound.cs`, `features/crew/HeroView*.cs`, `crew/logic/Aim.cs` and
  `Rams.cs`.
- **Dev:** `game/dev/FaunaDemo.cs` → `CreaturesDemo.cs`; the `--fauna` and `--fauna-trace` modes (`dev/logic/DevModes.cs`,
  `dev/Main.Dev.cs`) → `--creatures` and `--creatures-trace`; `dev/demos/FaunaHands.cs` → `CreatureHands.cs`,
  `dev/demos/PlanetScenarios.cs` and `dev/demos/README.md`.
- **Art:** `game/art/fauna` → `game/art/creatures`, re-imported once (`godot --headless --path game --import`), its
  `.import` files regenerated; the parts tool `tools/planet-art/fauna` → `tools/planet-art/creatures` and
  `tools/planet-art/README.md`.
- **Tests:** the `Fauna*` test files and classes in `sim/Tests` (`FaunaTests`, `FaunaRamTests`, `FaunaInvariantTests`,
  `FaunaTesting`) and `game.tests` (`FaunaAimTests`, `FaunaArtTests`, `FaunaLooksTests`) renamed; `DesyncTests`,
  `BudgetTests` and `RamsTests` updated.
- **Save:** format 33. Its step renames the `fauna` slice to `creatures` **in place**, on the ship and on every surface:
  it rebuilds each `slices` object key by key, writing `creatures` where `fauna` was, because `World.Restore` compares
  the slices to the modules in order and a removed-then-added key would land after `landing`. The fixture
  `format-33.json` written (`STILLWAKE_WRITE_FIXTURE=1`); `MigrationsTests` loads `format-31` and `format-32` through
  the step.
- **Docs:** the module's README (now `sim/Creatures/README.md`) and the client feature's, `sim/Crew/README.md`,
  `docs/map.md`, `docs/glossary.md` (creature, species, drive, act, faction, grudge, lifetime),
  `.claude/commands/new-module.md`. ADRs keep the old word: they are history.

**Done when**
- no file or folder name contains `fauna`, and `grep -ri fauna` finds it only in `docs/decisions`, `docs/plans`, the
  fixtures `format-31.json` and `format-32.json`, and the step that renames the slice;
- `MigrationsTests` loads every fixture and plays on;
- the full `dotnet test Stillwake.sln` passes;
- a planet with creatures let out by `--creatures` shows lopers grazing, fleeing and ramming as before (frames
  recorded with `--write-movie`).

### C-1 — The core, every creature behaving as before ✅

**Built as planned, but:** a drive keeps where it is within itself in a creature's `Step` (grazing: idling or wandering); the
lunge has no `Contact`, `Interruptible` or `GroundOnly` flags and acts no `HitPeers` yet, the ram being their one user; a hit
carries its peer, not a `Ref`; `Target`, `Mem` and the relations' reader come with the sentry (C-3). The trace matched on every
seed from the first run.

The composition of the ADR, built only as far as today's four species need it. Starts once C-0 is merged.

**First commit: the trace.** A test `CreaturesTraceTests` runs the four species over a set of fuzz seeds on a planet,
heroes striking, moving and being rammed, and folds into one number per seed, tick by tick: every creature's `Id`,
`Species`, `X`, `Y`, `VX`, `VY` and `Damage`, the world Rng's state, and the hashes of the Crew and Items slices (suits
and dropped yields). Its expected values are recorded on `main` after C-0. Every later commit of C-1 keeps them; C-1's
last commit deletes the test.

**Content (`sim/Content/Creatures.cs`), data only:**
- `Species(Id, Look, Shape Body, Life Life, string Faction, Lifetime Lifetime, IReadOnlyList<Drive> Brain,
  IReadOnlyList<Move> Moves, IReadOnlyList<LootEntry> Yields)`.
- `Walker(Width, Height, Wander, Flee, Jump, JumpAcross)`: a `Shape` with `Gait.Walk`.
- `Life(Hp, Knock, Hop)`.
- Drives: `Graze`, `Flee(Ticks, CorneredChance, CorneredReach)`, `Retaliate(Chance, Reach, FleeLeast, FleeMost)`,
  `Escape`.
- `Lunge(WindUp, Speed, Ticks, Past, Damage, Contact, Interruptible, GroundOnly)`: a `Move`, the ram.
- `Factions`: `crew` and `grazer`, grazers `Ignore` crew. `Lifetime.Wild`. Habitats as today, still on the planet
  type.
- The four species rewritten in these terms with today's numbers: `Ram`, `RamChance`, `RamReach`, `RamBackReach`,
  `RamFleeLeast`, `RamFleeMost`, `WindUp`, `Charge`, `ChargeTicks` and `RamPast` move into their records. `Stun` goes:
  no species has one, so there is no `Flinch` drive until a stunned species comes. A content test checks every species
  has a body, life, faction and at least one drive.

**Today's modes, as drives and acts:**

| Mode | Becomes |
| --- | --- |
| `Idle`, `Wander` | `Graze` in charge (idling or wandering, its own lengths as today) |
| `Flee` | `Flee` in charge |
| `Retreat` | `Retaliate` in charge, fleeing before it turns |
| `WindUp`, `Charge` | the `Lunge` act in its wind-up or active phase, begun by `Retaliate` or a cornered `Flee` |
| `Escape` | `Escape` in charge, after the lunge ends or a `HitBy` |
| `Hurt` | gone: only a species with `Stun` entered it, and there is none |

**The module:**
- `Creature` loses `Mode` and `ModeLeft` and gains `Drive` (its index in the brain), `DriveLeft` and `Act` (`Move`,
  `Phase`, `Left`, `AimX`, `AimY`, `HitPeers`). Nothing else yet: `Target`, `Mem` and the rest come with the sentry.
- **Rules,** one class a record, stateless as the ADR says: `WalkerBody` (today's `Move`, `Across`, `StepOnto`, `Jump.cs`
  and the rest), `GrazeDrive`, `FleeDrive`, `RetaliateDrive`, `EscapeDrive`, `LungeAct`, `HabitatSpawner`. Discovery maps
  record types to rules once; `CreatureContractTests` fails on a record with no rule or two.
- **The brain** as the ADR's contract: a think tick every tick for now, `Wants` top down and never drawing, the drive
  in charge running, `Claim` only by a drive ranked above it. Each drive draws from the Rng exactly where today's mode
  change drew: the trace proves it.
- **The hit path** `Hurt(creature, Hit)`, used by `Strike` only for now: damage, knock at the pace's share, an
  interruptible act cut short, drives told in brain order, death into yields.
- **Death marks, as the ADR says.** A marked creature is gone to `StrikeProblem`, `HitByProblem`, `CreatureById`,
  `Creatures`, the caps of `Release` and `Spawn` (`Max`, `PerKeep`), `Kept`, the loop and the fuzz; its yields roll at
  the mark; the sweep runs at the loop's end, before `Spawn`; the mark is not saved. A scenario: two heroes strike a
  loper with one hit point left in the same tick; one kill, one yield roll, the second strike refused
  `creatures.gone`.
- **`HitBy`** is refused `creatures.notAttacking` unless the creature's act is active and its peer is not yet in
  `HitPeers`. The hit adds the peer, ends the act, and the drive in charge's `Ended` hands the creature to `Escape`, as
  today.
- **Targets** through `Nearest(c, stance, reach)` over the relations table: for today's species the nearest hero, as
  now.
- **`Posture`,** a derived query for the client and the tests during this move: `Idle`, `Walk`, `Flee`, `WindUp`,
  `Charge`, from the drive and act in charge. It is a bridge, not the client's future: new looks (the sentry's) read the
  drive, act and phase themselves.
- **Fuzz** draws the same commands as now.

**The client:** `CreaturesView`, `Rams`, `HeroView`, `HeroView.Others` and `CreaturesDemo` read `Posture` instead of
`Mode`; `HeroView` and `Aim` read the lunge's `Damage` and the body's size from the new records instead of `Ram`,
`Width` and `Height`. Looks stay derived from the seed and id (`CreatureVariety`). `CreatureSpeciesGuardTests` (as
`MachineKindGuardTests`) fails on a species id in client logic, allowing it only in the looks (`CreatureLooks`,
`CreatureArt`), `Sound.cs` and `HitLooks.cs`, which are keyed by species today.

**Save:** format 34. Its step drops every surface's creatures and keeps `NextId` and `Clock` (planets are reached only
from the dev panel; the ship has none). The fixtures hold no creatures, so a test feeds the step a planet surface's
JSON with creatures written by hand and checks that `NextId` and `Clock` survive and the world loads and plays on.
`format-34.json` written.

**Docs:** the module README rewritten around the composition (what it owns, its rules, the mark and sweep, how to add
a species and a rule); 0102's and 0103's status lines say their state and the ram as a mode are superseded by
`2026-10-26-creatures-are-composed`; that ADR's status says C-1 is built.

**Done when**
- the trace equals its recorded values on every seed;
- the existing scenario tests pass with their assertions meaning the same (through `Posture` where they read `Mode`),
  and the two-strikes scenario passes;
- `BudgetTests` holds: 40 lopers a tick within 0.08 ms (0.055 today);
- the full `dotnet test Stillwake.sln` passes;
- frames of the dev planet look as before.

### C-2 — Heroes report as in Terraria near hostiles

- The module gives `Threatens(peer)`: a creature within that peer's hero's keep box holds a grudge on it, is in an
  act aimed at it, or belongs to a faction that hunts `crew`. Lopers count from the moment they turn to ram.
- The client's report rule (`crew/logic/Report.cs`) sends `Enter` on every tick its cell changes while `Threatens`
  holds, else as now, every `Crew.ReportEvery` ticks (0050). At most one `Enter` a tick by construction.
- **Done when**
  - unit tests in `game.tests` prove the rule: every changed tick while threatened, once in 20 otherwise;
  - in a two-peer session over the dev relay with `lag=100`, a loper's ram aimed at a hero sprinting across lands
    within 6 cells of where that hero is on its own screen (26 cells a second times about 0.2 s);
  - the bytes a second the host sends with four heroes in a fight are measured and written in the PR; above 4 KB a
    second the threatened rule drops to every second tick.

### C-3 — The sentry, playable with a placeholder

**Its numbers** (content, `Creatures.Sentry`; a play pass may change them):

| What | Value |
| --- | --- |
| Body | `Clinger(Width 3, Height 2)`, mounted at a point of its piece's box outline (`along`, in `Mem`), riding the piece's drift; flies straight, through walls |
| Life | `Hp 40`, `Knock 0`, `Hop 0`: never knocked |
| Faction | `machines`, which hunts `crew` |
| Lifetime | `Bound` to its piece; when the piece is gone, it guards the last point its mount had until the keep box lets it go |
| `Guard` | notice 12 cells from its mount (box distance), alert 20 ticks (1 s), then its lunge at the target |
| `Lunge` | wind-up 16 ticks, speed 1200 (24 cells a second) on the longer axis toward the aim, 17 ticks (about 20 cells), `Damage 6` suit units, contact, not interruptible, no `Past`: it runs its whole length unless it hits |
| `Return` | after the lunge: flies back to its mount at 600 (12 cells a second) and clamps on once the mount is within one tick's flight; back home, `Guard` takes over |
| `CutTether` | outranks the rest: on any tick a reel holds its piece, it unreels it (`IDebrisOps.Unreel`) |
| Yields | 3–5 `scrapChunk` |

- **Debris:** `IDebrisQueries` (a piece by id; its clock), `IDebrisOps.Unreel`, and on each piece a `Struck` record,
  `{ By, At }`: the last peer whose `HewWrecks` hit it, at Debris' clock; at two in a tick, the last in the host's order.
  Added with a null initializer, so no save bump. The sentry acts on a `Struck.At` newer than the one it last handled
  (in `Mem`), taking `By` as its grudge (`Target`). Debris' README loses "no module reads it". The module now depends on
  Debris, which runs before it, so a reeled piece has moved one step before its cable is cut; the tests expect that.
- **`Creature`** gains `Target` (a `Ref`) and `Mem` (the ADR's ordinal list of pairs). A test saves, loads and hashes a
  world whose creature holds keys differing only in case (`alerted`, `alertTicks`, `Along`).
- **A surface runs creatures** where creatures or spawners are, so the sentry lives in space. `Release` asks the
  body's `Fits` instead of refusing without gravity; `--sentry` lets one out on the piece under the cursor, refused
  `creatures.noPiece` (with its text) where there is none. The README's "nothing aboard" rule becomes this one.
- **The client:** a `Box` look, a squat box with one eye lamp casting light in its colour: teal while guarding,
  yellow when alerted, red and brighter from the wind-up on, read from the drive, act and phase; the cable snapping at
  a cut.
- **Accepted:** a derelict escorted onto the hull brings its sentry over the ship, and its lunge passes walls (nothing
  collides, 0006).
- **Done when** scenario tests cover: riding the drift; alert, then the hero leaving; alert held into a lunge; a struck
  piece or robot turning it on the striker at once; `HitBy` paying 6 once per peer; the return and the clamp; a reel cut
  a step after it starts; death into scrap; the piece gone; `creatures.noPiece`. Frames show the eye's three colours and
  a lunge. The module and Debris READMEs say what changed.

### C-4 — Sentries out on the derelicts, with their look

- `OnDebris(kind → 500‰)` on the ship's kind: every derelict whose id is past a saved watermark draws once, in id
  order (`IDebrisQueries` gives the pieces past it). The derelicts already out when this lands all draw on the first
  tick: accepted. The README's guarantee that a surface with no creatures draws nothing from the Rng becomes "with no
  creatures and no spawners". A density check over an hour of drift.
- The robot's art from a pick page (the user chooses), crawling along the outline, the wind-up's flicker and whine,
  the unclamp's clack, the thruster trail and puffs, sparks and clang on hits, the snap of a cut cable; sounds from an
  audition page (the user chooses).
- **Done when** a play pass at the far field meets a sentry on about every other derelict, and the user has picked
  its look and sounds.

### C-5 — The sentry's part

Waits for the user to choose what it teaches: a robot core toward drones, a battery cell toward the Arc branch, a
magnetic foot toward hull boots, or a servo toward the tool bench. Then the item, its yield line and its analyzer
lesson.

## Not in this plan

Each shared piece the ADR names comes with its first creature, in its own plan: shots, owned creatures (the pet),
wards, hunting, afflictions, paths and climbing aboard, wearing walls (with the ship's breakable hull), raids, `Use`
on a creature, bodies sharing a life, lairs and bosses, and a stunned species' flinch.

## Deliberately not handled

- **Old wild creatures** do not survive C-1's save step: planets are reached only from the dev panel.
- **The sentry's numbers** wait for a play pass.
- **Renaming in ADRs:** they keep the word fauna as history.
