# Plan: the starting planet's surface — storms, lightning, plants that yield

Planet 1's surface holds nothing to stay up top for: the player lands, digs down and stays down. This plan gives it
weather, a lightning bolt to watch for, and two surface plants with yields: the first step of "finish the starting planet
before other types". Rules and decisions confirmed with the user on 2026-11-16.

Three slices, each played by the user before the next, each opening with its pick page (CLAUDE.md, art and sound picks):
**slice 1** storms (`feat/surface-storms`), **slice 2** lightning, **slice 3** surface plants. Time is in ticks, 20 to a second.

## Rules (the user's, 2026-11-16)

- **Open sky**, the one definition used throughout: a cell of the surface's top band (`Terrain.BandOf == 0`) with no back
  wall (`BackAt == null`), as Crew's chill already reads it (sim/Crew/Crew.cs:567–570). A back wall or a cave is shelter.
- **Weather is a shared mechanic** scheduled by Sky's stretches ([stretches-of-space](../decisions/2026-11-15-stretches-of-space.md)):
  a storm is a stretch on a planet's kind, numbers in `sim/Content`. Planet 1's storm is the first user, built now; the icy
  planet's blizzard and the scorched one's ash storm come next as new data (a stretch and its look), not new code.
- **Storms on planet 1** (`PlanetTypes.First`): about 3 in a 20-minute day (`Content.Sky.First`, 24 000 ticks), each 1–1.5
  min, warned about 40 s ahead while the wind rises and the horizon dulls.
- **The cold.** A storm makes the open sky colder by its `Chill`, through the existing path (`SkyNow.Chill` →
  `CrewModule.TemperatureAt`). The numbers, once: the surface band is −30° (`PlanetTypes.First.Temperatures[0]`), a basic
  suit withstands −40° (`Items.cs:269`), night's chill is 20 (`Content.Sky.First.Chill`), the storm's 20
  (`Stretches.Storm.Chill`). In the open: −50° in a storm by day, −50° at night, −70° in a storm at night (they stack, no
  extra rule). Daylight dims, grit and rain streaks blow: client look only, rain is **not** sim water.
- **Lightning: one bolt per storm**, a hazard and a spectacle, leaving no loot. A glow crawls over the target first, so it
  is seen coming; then the bolt strikes: a small crater (Building's `Burst`) and a shock (`ICrewOps.Shock`; `Burst` hurts
  no hero, sim/Building/Bursts.cs:73) to heroes in reach. The target is the highest open-sky cell, built or not, within 24
  columns of one hero drawn per storm: in co-op, one strike per storm is an accepted limitation.
- **The one damage rule** applies to the crater: a player's hit on their own ship takes a tenth, everything else full. The
  bolt is no player's, so it is full. A struck roof cell cracks or breaks as the crater's `Burst` pays for it, leaving a
  hole in the shelter; the glow on that cell is the warning. (No room on a planet holds air: no machines are built there.)
- **Surface plants with yields:** a fibre reed gives fibre, cloth at the workbench (today cloth comes only from ruins and
  derelict Quarters, sim/Content/Vessels.cs:410; the cape and the quilted helmet need it); a wild food plant gives an
  existing fruit, no new dish. `Flora.All`'s chances sum to 1000, so the new plants take chance from others.
- **Every new look and sound is picked by the user on a pick page before it is built.** **Determinism.** Storm and strike state live in their slices; every draw is from a fork of the world's Rng (salts per
  module, below); ticks and degrees are integers; the target's tie-break is fixed.

## Player sequences

1. **A storm in the open.** Grass sways harder, the wind rises, the horizon dulls: 40 s of warning. Then streaks and dim
   light. Air is lost to the cold at `Temperature.Bite` (20 thousandths a degree past the limit, a breath a second): a
   basic suit with no cape, 10° past by day, loses 0.2 a second, **12–18 units over a storm** of its 45 (lungs 20, tank
   25); 30° past at night, 0.6 a second, **36–54 units**: a whole storm in the open at night empties a basic helmet.
2. **Sheltering.** Under a back wall (a built shelter, a ruin's room) or in a cave the storm's chill stops at once, as the
   existing chill path rules; the band's own −30° stays.
3. **Caught on a hill.** The glow crawls over the hilltop under the hero; they step out of reach in the 2 s glow and the
   bolt craters the top. If they stay, they lose a third of a basic helmet's air.
4. **A roof struck.** The shelter is the highest thing near: the glow crawls over its roof, the bolt cracks or breaks that
   cell, a hole the player rebuilds; the back walls below still shelter from the chill.
5. **Co-op.** Everyone on the planet sees one storm (one Sky slice); one hero is drawn per storm, the bolt falls near them,
   every hero in reach is shocked, in id order. A friend far away gets no bolt that storm.
6. **Save and load mid-storm or mid-glow:** the storm's `Active`, `Left`, `Ran` and the bolt's `BoltIn` or `Strike` are
   saved; the bolt strikes the same cell on the same tick. Earlier ground marks are lost.
7. **A guest joining mid-storm** gets both slices in the world sent, draws the storm from `SkyNow` with its warning share as
   far along as the others' (`StretchShown`), and the glow from Hazards' `Strike`. A strike between the snapshot and their
   first frame is missed, as with meteors.
8. **The ship and other planets are unaffected.** The ship's kind lists only its stretches. Icy and Scorched are
   `First with { … }`, so each sets `Stretches = []` until its own weather comes. A planet nobody is on keeps its storms
   running (0076); no bolt falls there, as no hero is there to draw.

## Slice 1 — storms (`feat/surface-storms`)

**Pick pages first.** Look: the storm (grit against rain streaks, how far daylight dims, the dull horizon in the
backdrop's and fog's tint), each candidate moving through warning, storm and easing back beside the hero. Sound: the wind,
a loop, CC0 from Kenney or Freesound as `CREDITS.md` asks; it fades out with the storm and never stays on.

**Sim.** Every touched call site, each verified:

| Where | Change |
| --- | --- |
| `sim/Content/Stretches.cs:20` `Stretch` | gains `int Warning`; `Zone` becomes `DebrisZone? Zone = null`. |
| `sim/Content/Stretches.cs:26` `const Warning`, `:43` `All`, `:49` `Get` | the const goes (`IceBelt`, `ContrabandDrift` take `Warning: 3 * Minute`); `All` becomes `Ship`, beside `FirstPlanet = [Storm]`; `Get` goes for `Kind.StretchOf`. |
| `sim/Content/Surfaces.cs:46` `Kind`, `:95` Ship | `Kind` gains `Stretch? StretchOf(string id)` over its own list; the ship takes `Stretches = Content.Stretches.Ship`. |
| `sim/Content/Surfaces.cs:~107` TestStrip | `Stretches = PlanetTypes.First.Stretches`: it copies planet 1's fields one by one. |
| `sim/Content/PlanetTypes.cs:172` First | `Stretches = Content.Stretches.FirstPlanet`. |
| `sim/Content/PlanetTypes.cs:227`, `:247` Icy, Scorched | `Stretches = []`: `First with` would carry the storm. Small and SmallSpire keep it, so the fuzz meets storms. |
| `sim/Content/Debris.cs:142` `Zones` | `IceBelt.Zone!`, `ContrabandDrift.Zone!`. |
| `sim/Sky/Stretches.cs:9` `Coming`, `:76` `Bring` | the stretch's own `Warning`. |
| `sim/Sky/Stretches.cs:35` `Unlocked` | an empty `Unlock` returns true: open from the start. |
| `sim/Sky/Stretches.cs:57` the choice, `:87` `Draw` | from Sky's fork (below). |
| `sim/Sky/Stretches.cs:90` `StretchOf`, `:102` `IndexOf`; `Sky.cs:78` `StretchNow`, `:86` `Now`; `Fuzz.cs:14` | read `Rules.Stretches` and `Rules.StretchOf`. |
| `sim/Debris/Drift.cs:239` | reads `Rules.StretchOf(_running)!.Thins`. |

- `Stretches.Storm`, first numbers: `new("storm", [], First: new(6000, 9600), Gap: new(5700, 7300), Duration: new(1200,
  1800), Chill: 20, Warning: 800)`. `Stretch.Gap` runs from a storm's end to the next start, warning included; gap plus
  duration averages 8000 ticks, 3 storms a day. The schedule ignores the hour.
- **Sky's fork.** Each draw (`Draw`, the choice) is one fork, `Random.Fork(SkySalt ^ Here·k₁ ^ Drawn·k₂)`, and `Drawn`
  goes up by one per fork. `Drawn` is saved in `SkyState` ("none yet" initializer); `Here` is the surface's index, so two
  planets drawing on one tick differ; `SkySalt`, `k₁`, `k₂` are odd constants chosen at implementation. Nothing is drawn
  from the world's Rng, so no planet or strip test pinned to later draws moves. The ship's stretches use the fork too (one
  rule): run `StretchesTests`, `DebrisZonesTests`, `ReputationTests`; re-pin what moves, never loosen.
- No format bump: `Drawn` is new with a "none yet" initializer. An old planet save opens its storm on its next tick.

**Client**

| Where | Change |
| --- | --- |
| `game/Content/logic/StretchLooks.cs` | `Look.Far` and `Look.Band` become optional; a new `Weather` record holds the picked streaks, the dim, the haze colour and the sway gain; the `storm` look has no far picture and no radar band. `StretchShown.See` (:164–165) takes the stretch's `Warning` instead of the global const. |
| `game/Main.cs:718` `ShowStretch`, `game/features/computer/ComputerPanel.cs:191`, `game/features/sky/FrostView.cs:142` | pass the warning; the radar band shows only a look with a `Band`. |
| `game/Main.cs:~692` `ShowDaylight` | dims the sky light and turns the backdrop and fog tint toward the haze, by the storm's share. |
| `game/render/Stars.cs:126`, `game/render/StretchBackdrop.cs:43` | a look with no `Far` draws nothing. |
| `game/SurfaceViews.cs:50` | FrostView gated on a stretch whose look has `Frost`; a new line adds `StormView`, gated on one whose look has `Weather`. |
| `game/features/sky/StormView.cs`, `logic/Streaks.cs` (new) | streaks over the open sky above the surface line, read once at load from `GroundLine`, never walked per frame; the logic unit-tested in `game.tests`. |
| `game/features/flora/logic/Gusts.cs` | the sway's strength grows with the storm's share. |
| `gc/Sound.cs`, `f/sound/SoundView.cs` | the wind cue, its volume following the share. |
| `game/features/dev-panel/DevPanel.cs:101` | lists the current surface's `Rules.Stretches`, not the ship's. |

**Tests** (`sim/Tests/StormsTests.cs`, scenarios on the strip unless noted)
- The storm opens on the first tick; the first comes within `Stretch.First`, later ones within `Stretch.Gap` of the last end.
- It is `Coming` for its own 800 ticks, not the ship's 3 minutes; `StretchesTests` reads `Belt.Warning`.
- Temperature at an open-sky cell: −50° in a storm by day, −50° at night, −70° in both; under a back wall and below the
  top band, unchanged.
- A basic-suited hero, no cape, standing in the open through a storm loses 12–18 units to the cold by day and 36–54 at
  night (counted from `Chills`, breathing's own cost excluded).
- `StretchNow("storm")` runs a storm on the strip; refused `sky.noStretch` for `"storm"` aboard and `"iceBelt"` on a
  planet, `sky.stretchRunning` while one runs. Icy and Scorched see no stretch over a long run; the ship's list is exactly
  `[IceBelt, ContrabandDrift]`. Save and load mid-storm: the same `Now` and end.
- The world's `RngState` is untouched across a tick that chooses or starts a storm (a calm strip, nothing else drawing).
- Invariant: `StretchesTests`' FsCheck property runs on planet kinds too — at most one chosen, coming or running; `Left`
  within `Duration`; `NextIn` within the bigger of `Stretch.First` and `Stretch.Gap`. Prove it bites by giving `Coming` the
  global 3 minutes back.
- Suites that may now meet a storm: the files naming `TestStrip` (`grep -l TestStrip sim/Tests net/Tests`, 25 today) and
  those growing planet 1 (`PlanetTesting`). Run them once here; any that runs past 6000 ticks reading temperature or air
  states the storm in the test, never hides it.

## Slice 2 — lightning (after slice 1 is played)

**Pick pages first.** Look: the glow, the bolt, the crater's dust and its ground mark. Sound: thunder, a one-shot with no
Loops flag.

**Sim**
- `sim/Content/Stretches.cs`: `Stretch` gains `BoltRule? Bolt = null` (a blizzard or an ash storm leaves it null):
  `record BoltRule(Span At, int Glow, int Columns, int Power, int Reach, int Shock)`. The storm's: `At` 200–1000 ticks into
  the storm; `Glow` 40 (2 s, past the 20 ticks a hero's cell may be reported late, `Crew.ReportEvery`); `Columns` 24;
  `Power` enough to break the struck soil cell and crack its neighbours; `Reach` 2 cells; `Shock` 15 units, a third of a
  basic helmet's 45. A content test keeps `At.Most + Glow < Duration.Least`.
- `sim/Hazards/Bolts.cs` (new); the constructor gains `ISkyQueries`, `ICrewQueries`, `ICrewOps`. `Registry.cs:103`
  changes its arguments only: Sky and Crew are made, and run, before Hazards already.
- The slice gains `BoltIn` (ticks till the glow, 0 for none), `Strike` and `Drawn`. `Strike(int Cell, int Left)` is a bolt
  glowing over `Cell`, `Left` ticks from striking, saved `[JsonInclude, JsonIgnore(Condition = WhenWritingNull)]`, the
  others `WhenWritingDefault`: no format bump. **Hazards' fork:** `Random.Fork(BoltSalt ^ Here·k₁ ^ Drawn·k₂)` per draw,
  `Drawn` up by one each, `BoltSalt` its own, distinct from Sky's.
- **The bolt, in construction after the shower and the flare**, on a surface not `Calm`:
  1. On a storm's start tick (`Now.Stretch` has a `Bolt` and `Now.Ran == 0`), draw `BoltIn` in `At` from Hazards' fork.
  2. When `BoltIn` reaches 0, draw one hero from Hazards' fork among `Heroes` on this surface, alive and not `Out`, in id
     order. None: no bolt this storm, and nothing more is drawn.
  3. The target: of the columns within `Columns` of that hero's, inside the grid's zone (never the fog), each column's
     topmost wall cell, built or natural, whose cells above are all open sky; the highest, ties to the column nearest the
     hero, then the lower index. None: no bolt. Else `Strike = (cell, Glow)`.
  4. When `Left` reaches 0: `Burst(FloorStack.Nobody, cell, Power, Reach, Spoils.Lost)`, full; then `Shock(peer, Shock,
     Harm.Shock)` in id order to every hero on this surface whose `Hero.Cell` lies within `Reach` of it by
     `Math.Max(|dx|, |dy|)`, as `Crew.PodReach` is measured; `Shock` itself skips a hero `Out` or creative. `Strike = null`.
  5. A storm that ends with `BoltIn` pending drops it; a `Strike` glowing always strikes (40 ticks at most). `SetCalm`
     drops both. A plant in the crater's reach is felled by nobody, its yields dropped at its cell as every burst's are
     (0085): nothing only lightning gives.
- **`BoltNow` (dev and fuzz):** the sender's hero's columns get a `Strike` now, by the kind's first `BoltRule`, storm or
  not. Refused `hazards.noBolt` where no stretch of the kind has one, `hazards.boltGlowing` while one glows,
  `hazards.noHero` with no hero of the sender here. `Fuzz` sends it one turn in 64, so ModulesTests' 100–400-step suites
  meet a glow and its strike in save, load and two worlds kept alike. The dev panel gets a button.
- Hazards' queries gain `Strike` (the glow's cell and ticks left) for the client.

**Client.** `game/features/hazards/BoltView.cs` (new) reads `Strike` and draws the glow over the target; when it has gone
after a glow the view saw, the strike there: a flash, thunder, the crater's dust. It keeps a ground mark per cell struck
while this surface is shown, not saved. The shock is shown as every `Harm.Shock` is.

**Tests** (`sim/Tests/LightningTests.cs`, strip maps with hills, `Meteors: true` so the strip is not calm)
- One storm with a hero, exactly one strike, seen glowing `Glow` ticks before; its cell holds when the hero walks away.
- The target: a hilltop within 24 columns beats lower ground nearer the hero; a built roof higher than the ground is the
  target and its wall breaks, at full damage; a column with a back wall above its top wall is skipped; ties go to the
  nearest column, then the left; nothing in the zone, no bolt.
- The shock: a hero within 2 cells loses 15 air, its last hurt `Shock`; one 3 cells away loses nothing; a creative hero,
  and one `Out`, lose nothing; a hero on another surface is never counted.
- No hero on the surface when `BoltIn` reaches 0: `BoltIn` was drawn, no hero is drawn, no strike. A `Calm` world: no bolt.
- Co-op: two heroes, the bolt near the drawn one, both shocked in reach. Save and load in the glow: the same cell and tick.
  A plant in reach drops its yields. `BoltNow`'s three refusals by reason. Nothing is drawn from the world's Rng.
- Invariant (FsCheck over seeds on `Small`, storms brought by `StretchNow`): at most one natural strike per storm. Prove it
  bites by not clearing `BoltIn` at a strike.

## Slice 3 — surface plants (after slice 2 is played)

**Pick pages first.** The fibre reed, the wild food plant and the fibre item icon: one page each.

**Sim**
- `sim/Content/Flora.cs` `All`, two kinds appended last: `reed` Tall 4, Level 0, Room 0, Clear 4, Chance 60, Apart 6,
  `Yields [("fibre", 3)]`, Strength 0; the food plant Tall 2, Level 1, Room 1, Clear 2, Chance 50, Apart 14, Strength 0,
  `Yields [(Foods.Fruit, 2)]` (eaten raw, half of porridge and hash at the galley; today only from the farm). The 110
  chance taken: acacia 370 → 320, tuft 150 → 110, bellTeal 100 → 90, bellViolet 80 → 70; the sum stays 1000. Icy and
  Scorched keep their own lists.
- `sim/Content/Items.cs`: `new("fibre", 200, BurnTicks: 100)`. `Market.cs`: fibre 8; cloth keeps its explicit 30 (check
  Market prefers it now that cloth has a recipe).
- `sim/Content/Crafts.cs`: `new("cloth", [("fibre", 3)], Ticks: 60)`; `Stations.cs` workbench row `["coil", "rotor",
  "battery", "cloth"]`. Cloth is understood once fibre is obtained, by the unlock rule as it stands.
- **Old saves:** a planet's plants grow again from its seed on every load; only `Felled` cells are saved
  (`BuildingModule.Rebuild`, sim/Building/Flora.cs:113). So an old save's planet regrows by the new chances: reeds and
  food plants appear on it, other plants move, and a felled cell stays bare whatever now grows there. Accepted: saves
  break at release.

**Client.** The two plants into the flora atlas as picked, with their sway (`logic/Sway.cs`) and fall (`logic/Topple.cs`);
the fibre icon; i18n keys in `En` and `Ru`.

**Tests**
- Felling a reed gives 3 fibre, the food plant 2 fruit. The chances add to 1000 in order; Icy and Scorched grow neither.
- Cloth is 3 fibre at the workbench, understood once fibre is obtained and not before. A grown planet 1
  (`PlanetTesting.First`) has both plants.
- Seeded growth tests pinning plants or counts move with the chances (`PlanetRulesTests`, `PlanetYieldsTests`, anything
  reading `PlanetTesting.First`'s flora): re-pin them and say so in the commit.

## Deliberately not handled

- **Lightning leaves no loot** (the user's call): content gated by weather is easy to miss. **A lightning rod** as
  counterplay is an idea for later, not designed; nothing draws the bolt, and a tree's crown is never the target.
- **Rain as water** (a look only; day-night.md's rain waits for its plan). **Wind** pushes, breaks and fells nothing.
- **More than one bolt per storm, or one per hero:** a friend far from the drawn hero gets none, accepted for co-op.
- **A hero's cell is up to 20 ticks late** (`Crew.ReportEvery`): one who left the reach in the glow's last second may still
  be shocked. The 40-tick glow leaves 20 ticks to step out as the sim sees it.
- **A hero in reach is shocked wherever they stand,** indoors under a struck roof too. **Ground marks** are lost on load.
- **Felled plants do not grow back** (0079): reeds and food plants are finite on a planet 4096 columns wide.
- **No text forecast:** wind, sway and horizon warn; the radar shows no storm band.
- **The blizzard and the ash storm** are named next users, not built: each a `Stretch` with its look, a `BoltRule` only if
  it has one.

## Open for the user

None: settled 2026-11-16. Every number here is a first number for the playtest.

## Consequences / supersedes

- **Supersedes the storm part of `docs/plans/day-night.md`** (every 12–25 min, 1.5–3 min, `Chill` 2, no lightning, its own
  `Sky.Storm` record): storms are a stretch on a planet's kind, with the numbers above. Its "Slice 2: storms" section is
  deleted when slice 1 ships; its rain and its omissions stay until rain is planned.
- **Supersedes in part** [stretches-of-space](../decisions/2026-11-15-stretches-of-space.md): slice 1's ADR
  (`YYYY-MM-DD-surface-storms`) records the warning per stretch, an empty `Unlock` open from the start, `Zone` optional,
  stretches per surface kind and draws from a fork. Slice 2's records the bolt, its target rule and that it leaves no loot.
- **READMEs:** `sim/Sky` (its "Storms and rain … no weather yet" and "a stretch on a planet" omissions go; Rules gain
  storms), `sim/Hazards` (dependencies, the bolt, `BoltNow`), `sim/Building` (the plants), `docs/map.md`'s Day and night
  domain, `docs/glossary.md` (storm/гроза, lightning/молния, fibre reed).
