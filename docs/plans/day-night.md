# Design: Day and night, then storms, on a planet's surface

Status: design, reviewed by `design-reviewer`, waiting for the user's go. Once built, its ADR supersedes the line in 0076
slice 4, "no gradient, sun, clouds or day and night".

The sim runs at 20 ticks a second. Outside on the first planet a basic helmet (45 units) lasts about 225 s today
(thin air, `ThinAir` 5: one unit every fifth 1 s breath), a heavy helmet (100) about 500 s.

## Where the mechanic is heading

One sim module owns a surface's **time of day and its weather**, and one rule its users share: **the chill under the open
sky**. Everything else is looks. Its users, in order:

1. **Night** (slice 1): dark, and it costs air outside.
2. **Storms** (slice 2): the same chill, harder, at random, with a warning.
3. **Rain** (once liquids exist): a storm that rains.
4. **Hostile creatures** (the `hostile-creatures` design running now): may read `Night`.
5. **Further planet types:** their own day and storms, as data.

Slice 1 is built and played before slice 2 is planned: night alone already sends a hero underground at dusk, and the
playtest says whether a random storm adds anything night does not.

## Who owns it

**A new module `Sky` (`sim/Sky`, id `sky`)**, no module dependencies, registered before Crew and Creatures. Crew must
read the chill, and Hazards, where flares are scheduled, runs after Crew (`Registry.cs`); moving it earlier reorders the
meteors' construction step against Building. Time read from the world's tick is rejected (modules have no clock; a planet
opens at a different tick in every world), and client-only weather is rejected (it costs air, so every peer agrees on it).

## Slice 1: night

### Rules as the player meets them

1. **The day.** A planet's kind names its `Cycle`. `Time` counts 0 to `Length − 1` in Sky's slice, advanced as
   `(Time + 1) % Length`. Time 0 is the start of daylight ("day"; there is no noon in the sim), and a planet opens at 0,
   so the first landing always has a whole day ahead and never starts in the dark.
2. **Night** is `Time % Length ≥ Length − Night`: the only line the sim draws. Dusk and dawn are looks.
3. **The chill.** At night a hero whose cell (`Hero.Cell`, the cell under the body's middle, as the hero reports it) has
   no back wall pays `Cycle.Chill` more air once every `Cycle.Every` breaths. Nothing else gates it: suit or no suit,
   thin air or not. It is the same unit as the thin-air breath's; the cape spares `Cape.Warmth` of it, as of all cold.
   - Its beat is its own (`Clock / BreathEvery % Every == 0`), not `ThinAir`'s, so a planet type with no thin air
     still pays it at the table's rate.
   - Shelter is a back wall: caves, halls and shafts (the terrain gives them back walls), ground under an overhang (the
     same), a dug tunnel (digging leaves the back wall), or a lean-to of back walls, already on the planet's build list.
   - A pit or cave mouth open to the sky near the surface has no back wall for its top few rows (`Terrain.cs`, the fade
     under the surface's line) and chills, as it shows the sky.
   - Walls and a roof without back walls are no shelter; nor do they hold air, since a room needs back walls. The frost
     stays on inside, and the dark sky still shows behind: that is how the player learns a back wall is the answer.
   - `Cold(peer)` reports the cost of a beat, the chill included, so the frost already drawn appears as a hero steps
     out and clears as they step in. Nothing new tells the player.
4. **What the player does differently.** By day they work the surface: grazers, ruins, flora. At dusk they go underground,
   where the ore is anyway, put up back walls, or go aboard while the planet's night passes. The cape, of hunted hide,
   makes night outside cheaper. In the dark the helmet lamp and the plants that already glow are what they see by.

| Outside, basic helmet, no cape | Cost | A full suit lasts |
| --- | --- | --- |
| Day | 1 unit per 5 s | about 225 s |
| Night | 2 units per 5 s | about 112 s |

### Looks (client)

- `game/Content/logic/Daylight.cs` turns the sky's `Time` into the moment's daylight from keyframes per planet type in
  `PlanetLooks.Look`: day (today's `Light.SkyOf`), dusk, night. Dusk fades day → dusk → night over the `Daylight.Dusk`
  ticks before the night line, so the light warns before the chill bites. Dawn fades night → dusk → day over the
  `Daylight.Dawn` ticks before night ends, inside night: the sky brightens while the chill still bites, and lifts with it.
- Night is a dim blue-grey, about a tenth of day: the helmet lamp matters, the surface is never pitch black. Caves do
  not change.
- `Lit`'s shader adds the stars' term (`star`) and the lamps' separately; a `sky` uniform multiplies `star` alone, so the
  baked starlight is never recomputed (checked by `plan-reviewer-completeness`).
- The fog at a planet's sides (`FogLooks`) darkens with the daylight, so the far land does not glow at night.
- **For the parallax backgrounds:** `Daylight.Backdrop(PlanetLooks.Look, int time, Sky.Cycle cycle) → (R, G, B)`, one
  colour to multiply every backdrop layer by. `planet-backgrounds` consumes that and nothing else; a moving sun or gas giant is theirs to draw from
  the same `time`.

### Numbers (`sim/Content/Sky.cs`; fades in `game/Content`)

`Sky.Cycle(Length, Night, Chill, Every)` sits on `Surfaces.Kind` as `Cycle?`, beside `Meteors` and `Flares`, null for
the ship; `TestStrip` copies the first planet's.

| Number | First planet | Real time |
| --- | --- | --- |
| `Cycle.Length` | 24 000 ticks | 20 min |
| `Cycle.Night` | 7 200 ticks | 6 min |
| `Cycle.Chill` | 1 unit a beat | |
| `Cycle.Every` | 5 breaths | a beat every 5 s |
| client `Daylight.Dusk`, `Dawn` | 1 200 ticks each | 1 min |

Placeholders until a playtest.

### The shared query

`ISkyQueries.Now → SkyNow(int Time, bool Night, int Chill)`, all zeros on the ship; `Chill` is the cost of a beat, at
night only. Crew reads `Night` and `Chill`; the client reads `Time` and `Night`; hostile creatures may read `Night` once their design names a rule. Nothing goes on `Habitat` until then.

### Dev rooms

A dev command `SetTime(Time)` sets the hour (taken modulo `Length`), refused `sky.noDay` where the kind has no `Cycle`
(the ship); only the dev panel sends it, as it sends Hazards' `FlareNow` and `SetCalm`, ungated as they are, and `Fuzz`
draws it as Hazards' draws theirs. A layout field was weighed and dropped:
a planet is grown from its seed by `OpenSurface`, so no dev layout reaches its `Init`.

### Saves, co-op, determinism

- **Saved:** `SkyState.Time`. A module is added, so `World.SaveFormat` takes the next number; its step puts
  `"sky": {"time": 0}` in each slices object at Sky's own index (`World.Load` refuses slices out of the modules' order,
  `Sim.cs` `Restore`): a planet in an older save wakes at the start of day.
- **Order:** Sky is listed right after `Grid` (`[Grid, Sky, Rooms, …]`), in `Phase.Simulation` like Crew, so Crew reads
  this tick's hour. Slices are written and hashed in list order, the same on every peer.
- **Derived:** nothing; `Night` and `Chill` are computed from `Time` and the kind.
- Integers and ticks; one comparison, no division, no `Rng`; one dev command. A planet nobody is on keeps running (0076):
  its hour passes, nobody is chilled. A joining peer gets the slice with the world and sees the same hour.
- The chill reads the hero's last reported cell, at most a second and a round trip old, against a 5 s beat.
- A `Time` loaded at or past a later, smaller `Length` is read modulo `Length` and wraps on its next tick.

## Slice 2: storms (designed now, planned after slice 1 is played)

- `Sky.Storm(EveryMin, EveryMax, Warning, LastMin, LastMax, Chill)` on the kind as `Storms?`, scheduled as flares are
  (`Hazards/Flares.cs`): a countdown drawn from the world's `Rng`, first on the tick the planet opens, so the first storm
  is at least `EveryMin` away; its last `Warning` ticks warn; then it blows for `LastMin`..`LastMax`.
- While it blows, the chill under the open sky is `Storm.Chill` more, on top of night's.
- It does nothing else: pushes no one, breaks nothing.
- Looks: over the warning the horizon dulls and the grass and flora sway, the wind rises in the sound. Then grit
  streaks, haze and dimmer daylight, easing off over a short tail. The wind's direction comes from the storm's start tick.
- First numbers: every 12–25 min, a 30 s warning, 1.5–3 min long, `Chill` 2, so a basic helmet lasts about 75 s
  in a storm by day and 56 s at night: a storm sends a hero to rock, it is not outlasted in the open.
- What it adds then: `SkyState.StormIn`, `StormLeft`; `SkyNow.StormComing`, `StormLeft`; two draws (re-pinning tests
  that pin draws after a landing); a dev path to start one.

## Rain, later

A storm that rains, a field on `Sky.Storm` added with its user. Today's `Water` module (cisterns, electrolysers) holds
water in machines; flowing water is the module `liquids-design` is designing now. Rain needs **no** interface from
it: that module reads `ISkyQueries.Now` and pours water into open-sky columns itself, so Sky stays dependency-free and
early in the order. Nothing more about water is designed here.

## Deliberately not handled

- **Wind pushing anything.** Hero movement is client-authoritative and could read it, but stacks, bodies, creatures and
  nails are sim: pushing only heroes reads wrong, pushing all is four rule sets for a look, and gusts annoy more than
  they add. The wind is seen and heard.
- **Fog banks:** fog at a planet's sides is the world's edge; banks would read as one.
- **A separate cold snap:** a storm is a cold snap with grit; one weather that matters beats two alike.
- **Lightning, storm damage, felled plants:** no player need names them.
- **A body that keeps the chill out:** the cape is the answer now; a warmer body is a later suit row.
- **A clock anywhere,** the planet's hour seen from aboard included: the sky is the clock.
- **Seasons, moons, days differing by place, flora or grazers reacting to night,** creatures' senses dimmed by dark.
- **The ship's sky:** space has no day; solar panels are unaffected.
- **A back wall taken away** (the clear layer) changes the shelter at once; the frost shows it.
- **Beaming down at night:** a later visit may land in the dark with nothing said aboard; the dark and the frost are
  the warning, and the pod's air is a beam away.
- **Night's sound:** the planet has no ambience to change yet (`Sound.Ambient` waits for its first); a night bed comes
  with a planet ambience, not before.

## Looks and sounds for the user to pick (Artifact pages)

- Slice 1: the first planet's day, dusk and night colours for the daylight and `Daylight.Backdrop`, with how dark night
  gets beside the helmet lamp.
- Slice 2: what a storm carries (grit, ash, snow), its haze and warning on the horizon, the sway, the wind's sound.

## Decided with the user

The design's four open questions, answered by the recommendations; the user said go on the plan for night
(2026-11-07) without changing them, so they stand until the user says otherwise:
1. The chill goes through the suit, softened only by the cape.
2. A 20-minute day with a 6-minute night, retuned after the playtest.
3. Night first; storms planned after night is played.
4. The planet's hour is not shown aboard.

## Plan: slice 1, night

Each task is one commit on `claude/day-night-96d09c`; one PR into `main` at the end. Before any Godot run or
`tools/snap.ps1`, check no Godot process is running (`Get-Process Godot*`). Reviewed by `/review-plan` (three lenses).

### N-1 — The `Sky` module

- `sim/Content/Sky.cs`: `Sky.Cycle(Length, Night, Chill, Every)` and the first planet's numbers; `Surfaces.Kind.Cycle?`
  beside `Meteors`; `PlanetTypes.First` sets it, `TestStrip` copies it; the types made from `First` by `with` inherit it.
- `sim/Sky/` per `/new-module`: `SkyModule : Module<SkyState>`, id `sky`, no dependencies. `SkyState.Time` (saved).
  A system in `Phase.Simulation` sets `Time = (Time + 1) % Length`; nothing where the kind has no `Cycle`. `Init` starts
  at 0. `ISkyQueries.Now → SkyNow(Time % Length, Night, Chill)`, zeros on the ship.
- `SetTime(int Time)`: sets `Time` to it modulo `Length`, a negative one too; refused `sky.noDay` without a `Cycle`.
  `Fuzz` draws it, on the ship as well (refused there).
- `Registry.cs`: `public SkyModule Sky { get; }`, made right after `Grid`, listed `[Grid, Sky, Rooms, …]` in `surface`
  (so aboard after `Players, Knowledge, Grid`). Crew's constructor takes it.
- Save: write format 53's fixture first; bump `World.SaveFormat` to 54; step 53 → 54 rebuilds each slices object, aboard
  and on every surface (`root["surfaces"]?.AsArray() ?? []`: a ship-only save has none), with `"sky": {"time": 0}`
  inserted right after `"grid"`. `MigrationsTests` upgrades every fixture; a case checks the upgraded slices' names are
  `m.All`'s ids in order, for a ship-only save and one with a planet.
- Tests (`sim/Tests/SkyTests.cs`, scenarios): an opened planet starts at 0 and counts a tick a tick; it wraps at
  `Length`; `Night` is false at `Length − Night − 1`, true at `Length − Night` and at `Length − 1`, false at 0;
  `SetTime` sets, and wraps a value past `Length` and a negative one; refused `sky.noDay` on the ship, state untouched;
  the ship's `Now` is all zeros; a slice loaded with `Time ≥ Length` reads modulo and wraps.
  `ModulesTests` covers save, load and fuzz.
- Existing planet scenarios (`TestStrip`, `Small`) that run past tick 16 800 with a hero outside now meet night:
  search them (`Run(` over 16 000) and pin `SetTime(0)` where night is not what they test.
- `sky.noDay` in the refusal strings, English and Russian.

### N-2 — The chill in Crew

- `ColdAt(peer, cell, bare)` keeps its `bare` gate for `BareCold` and takes a second, `chill`: when true it adds
  `Sky.Now.Chill` where `Grid.BackAt(cell) == null`. The breath passes `chill` on the chill's own beat
  (`State.Clock / BreathEvery % Cycle.Every == 0`, false without a `Cycle`); the cape softens the sum (`Soften`).
  `Cold(peer)` passes both true: the cost of a beat, which the frost draws.
- `Report.Cold`'s `bare` flag (`Cold > EdgeCost`) then shows `BareFrost` out at night with no new code; rename it
  `beyondEdge` and say in its doc comment that the chill counts.
- Tests: on the strip at night a suited hero in the open loses `Chill` more over a chill beat than by day; under a back
  wall (the shelter at `S`, the cave) nothing more; a hero in a walled, roofed hut with no back walls is chilled; with
  the cape, over ten beats, the chill softened by `Cape.Warmth` as `Soften` carries it; by day nothing more; aboard
  nothing more; on a kind with `Cycle` and no `ThinAir`, the chill still once every `Every` breaths. Proven to bite:
  drop the back-wall check once and watch the shelter test fail.

### N-3 — The pick: day, dusk and night

The page of "Looks and sounds for the user to pick", slice 1: a published Artifact (CLAUDE.md "Art and sound picks"), a
subagent writing its files: the strip's ground and hills at 8 px with the hero, under four or five really different dusk
and night palettes, each animated through a whole cycle (day → dusk → night → dawn) in a few seconds, the helmet lamp's
pool lit at night, the backdrop's colour on a stand-in of today's `PlanetSky` layers; one click picks, kept in the
page's `db`. Runs alongside N-1 and N-2.

### N-4 — Daylight in the client

- `game/Content/logic/Daylight.cs` (Godot-free): `Dusk`, `Dawn` (ticks); `Sky(PlanetLooks.Look, int time, Sky.Cycle)`
  and `Backdrop(PlanetLooks.Look, int time, Sky.Cycle)`, each `(R, G, B)`, from the picked keyframes on `Look`
  (`DuskSky`, `NightSky`, `DayBackdrop`, `DuskBackdrop`, `NightBackdrop`; day's sky is today's `Light.SkyOf`).
  Tests in `game.tests`: day's colour from 0 to the start of dusk; night's from the line to the start of dawn; dusk ends
  on night's colour at the line; dawn ends on day's colour at `Length`, which is 0; no step between neighbouring ticks
  larger than a fade's share of a tick.
- `Lit`: a `sky` uniform (vec3, 1 by default) multiplying `star` only, set like `ambient` on every material.
  `Main` sets it each frame on a planet from `Sky.Now`; the ship keeps 1.
- `PlanetSky` multiplies its layers by `Daylight.Backdrop`; `FogView` its fog colour by the same. If
  `planet-backgrounds` has landed new layers by then, apply it to theirs; read the shared notes folder first.
- Frames: the strip by day, in dusk, at night with the helmet lamp, a cave at night (unchanged).

### N-5 — Dev

- Dev panel: "dusk" sends `SetTime(Length − Night − Daylight.Dusk)`, "day" sends `SetTime(0)`; their strings in its
  `.En.cs` / `.Ru.cs`.
- A dev room for the user: `--planet` in creative mode, the hero beamed down and "dusk" sent, so night falls a minute in.

### N-6 — Docs

- ADR `docs/decisions/2026-11-07-night-chills.md` (superseding 0076 slice 4's "no day and night" line), `tools/adr-index.ps1`.
- `sim/Sky/README.md` (Files first), `docs/map.md` line, `docs/glossary.md` (день, ночь, сутки → `Sky`), concept's
  planet paragraph, Crew README's cold section, render README for the `sky` uniform.
- This plan cut down to slice 2 and rain once slice 1 is merged; what stays true moves into the ADR and READMEs.

### Done when

`dotnet test Stillwake.sln` green; `dotnet format --verify-no-changes` clean; DocsGuard green; the user has played the
dev room through a dusk.
