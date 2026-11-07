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

1. **The day.** A planet's kind names its `Cycle`. `Time` counts 0 to `Length − 1` in Sky's slice and wraps. Time 0 is
   morning and a planet opens at 0, so the first landing always has a whole day ahead and never starts in the dark.
2. **Night** is `Time ≥ Length − Night`: the only line the sim draws. Dusk and dawn are looks.
3. **The chill.** At night a hero whose reported cell has no back wall pays `Cycle.Chill` more air on the thin-air beat,
   as `BareCold` is paid. It goes through the suit like the edge's cold; the cape spares `Cape.Warmth` of it.
   - Shelter is a back wall: caves, halls and shafts (the terrain gives them back walls), ground under an overhang (the
     same), a dug tunnel (digging leaves the back wall), or a lean-to of back walls, already on the planet's build list.
   - A pit or cave mouth open to the sky near the surface has no back wall (`Terrain.cs`) and chills, as it shows the sky.
   - `Cold(peer)` includes the chill, so the frost already drawn appears as a hero steps out and clears as they step in.
     Nothing new tells the player.
4. **What the player does differently.** By day they work the surface: grazers, ruins, flora. At dusk they go underground,
   where the ore is anyway, put up back walls, or go aboard while the planet's night passes. The cape, of hunted hide,
   makes night outside cheaper. In the dark the helmet lamp and the plants that already glow are what they see by.

| Outside, basic helmet | Cost | A full suit lasts |
| --- | --- | --- |
| Day | 1 unit per 5 s | about 225 s |
| Night | 2 units per 5 s | about 112 s |

### Looks (client)

- `game/Content/logic/Daylight.cs` turns the sky's `Time` into the moment's daylight from keyframes per planet type in
  `PlanetLooks`: day (today's `Light.SkyOf`), dusk, night; dawn is dusk in reverse. Dusk fades over the minute before
  the night line, so the light warns before the chill bites; dawn over the minute before it lifts.
- Night is a dim blue-grey, about a tenth of noon: the helmet lamp matters, the surface is never pitch black. Caves do
  not change.
- `Starlight` is its own node in `LightView`, its sky baked when built; the plan checks it can be scaled by one colour
  (its `Modulate`) without being recomputed, before claiming it is within budget.
- Night's ambience replaces the day's.
- **For the parallax backgrounds:** `Daylight.Backdrop(PlanetLooks, int time) → (R, G, B)`, one colour to multiply every
  backdrop layer by. `planet-backgrounds` consumes that and nothing else; a moving sun or gas giant is theirs to draw from
  the same `time`.

### Numbers (`sim/Content/Sky.cs`; fades in `game/Content`)

`Sky.Cycle(Length, Night, Chill)` sits on `Surfaces.Kind` as `Cycle?`, beside `Meteors` and `Flares`, null for the ship;
`TestStrip` copies the first planet's.

| Number | First planet | Real time |
| --- | --- | --- |
| `Cycle.Length` | 24 000 ticks | 20 min |
| `Cycle.Night` | 7 200 ticks | 6 min |
| `Cycle.Chill` | 1 unit a beat | |
| client dusk, dawn | 1 200 ticks each | 1 min |

Placeholders until a playtest.

### The shared query

`ISkyQueries.Now → SkyNow(int Time, bool Night, int Chill)`, all zeros on the ship. Crew reads `Chill`; the client reads
`Time` and `Night`; hostile creatures may read `Night` once their design names a rule. Nothing goes on `Habitat` until then.

### Dev rooms

No command. A dev room opens its planet at a given `Time` (Sky's `Init` reads it, as Hazards seeds its slice), so the
user lands a minute before dusk and watches it fall. Scrubbing the hour in a live room is not offered.

### Saves, co-op, determinism

- **Saved:** `SkyState.Time`. A module is added, so `World.SaveFormat` takes the next number with an empty step: a
  planet in an older save wakes at morning.
- **Derived:** nothing; `Night` and `Chill` are computed from `Time` and the kind.
- Integers and ticks; one comparison, no division, no `Rng`, no command. A planet nobody is on keeps running (0076):
  its hour passes, nobody is chilled. A joining peer gets the slice with the world and sees the same hour.
- The chill reads the hero's last reported cell, at most a second old, against a 5 s beat.

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

A storm that rains, a field on `Sky.Storm` added with its user. It needs **no** interface from the liquids module: the
liquids module reads `ISkyQueries.Now` and pours water into open-sky columns itself, so Sky stays dependency-free and
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

## Looks and sounds for the user to pick (Artifact pages)

- Slice 1: the first planet's day, dusk and night colours for the daylight and `Daylight.Backdrop`, with how dark night
  gets beside the helmet lamp; night's ambience.
- Slice 2: what a storm carries (grit, ash, snow), its haze and warning on the horizon, the sway, the wind's sound.

## Open questions

1. **Does the night's chill go through the suit, softened only by the cape?** Recommended: yes, like the edge's cold;
   paid only by bare heroes, night would cost almost nobody anything.
2. **A 20-minute day with a 6-minute night?** Recommended: yes for now, about one landing in three meets night; retune
   after the playtest.
3. **Night first, storms after it is played?** Recommended: yes.
4. **The planet's hour seen from aboard?** Recommended: not yet; landing into night is a choice to dig or wait.
