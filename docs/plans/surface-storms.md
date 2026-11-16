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
- **Lightning: one bolt per storm**, a hazard and a spectacle, leaving no loot. A glow crawls over the ground it will
  strike first, so it is seen coming: the struck cell and the open ground `Spread` columns either side, 20 in rain (wet
  ground), 10 in dust (the user's, 2026-11-16). Then the bolt strikes: a small crater (Building's `Burst`) and a shock of
  100 (`ICrewOps.Shock`; `Burst` hurts no hero, sim/Building/Bursts.cs:73) to every hero under the open sky in those
  columns: a back wall or a cave shelters. The target is the highest open-sky cell, built or not, within 24
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
3. **Caught on a hill.** The glow crawls over the hilltop and the ground round it; in its 3 s the hero runs out of the
   zone (at half pace a run is about 13 cells a second: 20 cells in 1.5 s) or under a back wall, and the bolt craters the
   top. If they stay in the open in the zone, they lose 100: a basic or quilted suit's whole air and their last breath.
4. **A roof struck.** The shelter is the highest thing near: the glow crawls over its roof, the bolt cracks or breaks that
   cell, a hole the player rebuilds; the back walls below still shelter from the chill.
5. **Co-op.** Everyone on the planet sees one storm (one Sky slice); one hero is drawn per storm, the bolt falls near them,
   every hero in the zone under the open sky is shocked, in id order. A friend far away gets no bolt that storm.
6. **Save and load mid-storm or mid-glow:** the storm's `Active`, `Left`, `Ran` and the bolt's `BoltIn` or `Strike` are
   saved; the bolt strikes the same cell on the same tick. Earlier ground marks are lost.
7. **A guest joining mid-storm** gets both slices in the world sent, draws the storm from `SkyNow` with its warning share as
   far along as the others' (`StretchShown`), and the glow from Hazards' `Strike`. A strike between the snapshot and their
   first frame is missed, as with meteors.
8. **The ship and other planets are unaffected.** The ship's kind lists only its stretches. Icy and Scorched are
   `First with { … }`, so each sets `Stretches = []` until its own weather comes. A planet nobody is on keeps its storms
   running (0076); no bolt falls there, as no hero is there to draw.

## Slice 1 — storms: shipped

Built as planned, then tuned in play: two storms, dust and rain, one at a time; a storm halves a hero's pace in the open;
no wisps, no clear patch round the hero; streaks drawn by a shader. Its rules are
[surface-storms](../decisions/2026-11-16-surface-storms.md) and the `sim/Sky`, `sim/Crew` and `f/sky` READMEs.

## Slice 2 — lightning: shipped

Built as planned, then tuned in play: a shock of 100 as `Harm.Lightning`; St Elmo's fires over the zone and its helmets
in place of the glow's arcs; the channel's light and shade; the burn on a hero's view in the zone; the bolt starting above
the farthest view. Its rules are [storm-lightning](../decisions/2026-11-16-storm-lightning.md) and the `sim/Hazards` and
`f/hazards` READMEs.

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
- **A hero's cell reaches the host a round trip late**, even reported every tick: one who leaves the zone in the glow's
  last moment may still be shocked.
- **The zone is columns, not current through the ground:** a hero under the open sky on a ledge far above or below the
  struck ground is shocked too. **Ground marks** are lost on load.
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
