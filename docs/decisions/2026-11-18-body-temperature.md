# The hero's body temperature

- Status: accepted; supersedes [temperature-in-degrees](2026-11-14-temperature-in-degrees.md)'s air cost of the felt degrees
  (`Temperature.Bite`) and its field's edge, [heat-limits](2026-11-14-heat-limits.md)'s air cost of the heat,
  [0050](0050-the-edge-is-made-of-oxygen.md)'s cold past the field (`EdgeCost`, `Field.EdgeCost`, `Field.OffGrid`) and its frost,
  [temperature-gradients](2026-11-14-temperature-gradients.md)'s degrees afloat, and
  [the-thermometer](2026-11-14-the-thermometer.md)'s reading of the place
- Date: 2026-11-18

## Context

The thermometer stopped at −40° in space while the frost kept growing: the field's edge drained air through a second rule,
`EdgeCost`, that never read as degrees. The HUD showed the place's number, and "−20° in space" read oddly. Cold only cost
air, so nothing but the tank limited where a hero went. The user asked for a body that freezes and overheats, dies of it and
slows before it does. Designed in `docs/plans/body-temperature.md` (weight and clothes under the suit stay open there), then
changed twice in play.

## Decision

- **Two vitals.** Air is for breathing. The body's temperature is the hero's own; cold and heat act only on it and cost no
  air. `Chills`, `Bite`, `EdgeCost`, `Field.EdgeCost` and `Field.OffGrid` are gone; the last breath on air clears on a covered
  breath with no grit or lava.
- **Degrees** (`TemperatureAt`): afloat `Temperature.Hull` (−100°) in the zone, falling to `Space` (−200°) at the field's line
  (`Field.Margin`, 128 cells), then straight on to `Deep` (−2000°) at the world's edge by the deeper axis; off the grid the
  deep's. An aired cell is `Room` (+20°). Planets as before, the first's surface made mild ([mild-surface](2026-11-18-mild-surface.md)).
- **Limits:** a suit's `Low` and `High`, the same everywhere: basic, regen and heat −110°, highs +50°, +50°, +60°; bare 0° and
  +30°; the cape 5° lower both. `Felt` and `Heat` are the degrees past them.
- **The body lives on the client** (`BodyHeat`, f/crew). Each frame it closes `Temperature.Pull` (250‰) a second of its gap to
  a target `Normal` (37.00°) ∓ `Sway` (0.20°) a felt degree. Away from normal it moves at most `Drift` (0.003°) a second a
  degree felt, so a basic suit on the field's line (90 felt) cools 0.27° a second, 33 s to death; back toward normal it is
  unbounded. Never saved or sent; well again on waking, a rejoin and in creative. The first build kept it in the sim and
  killed a hero flying fast past the field in three seconds, its loot out of reach.
- **Death:** at `ColdDeath` (28.00°) or `HeatDeath` (42.00°) the body lives a last breath of `Crew.LastBreath` (10 s), as a
  hero out of air does; back inside in time ends it. At its end the client sends `Succumb`, which passes the hero out as any
  death does (refused `crew.noHero` with no hero, out, or creative). `PassOut` empties the air too. The body lies as it died,
  frosted over or scorched.
- **The edge is out of reach:** the deep's cold grows the cooling, so at the jetpack's top speed straight out a basic suit dies
  some 24 cells short of the edge. How far a hero goes is the suit's to decide, not the tank's.
- **Slowness** (`BodyHeat.SpeedOf`): 1000‰ at normal, falling straight to `Least` (200‰) at either death, each hundredth its
  share, rounded down: about 0.9% a tenth of a degree cold, 1.6% hot. It scales the walk, run, climb, a jump's height (its
  speed by the root), the swim in water (`HeroInput.Move`), every melee stroke (`AxeSwing`) and a run tool's pace
  (`DrillPace`). Afloat nothing weighs: the space swim and the jetpack keep their speed. Guns, building and crafting never slow.
- **Seen before death:** the screen's frost and haze show the place at once (`Report.Cold`/`Heat`, whole at 30 felt); the suit
  frosts over or scorches with the body (`SuitFx`, the picks «Выстуживание» F2 and «Подпалины и искры» H3 on
  [Костюм не справляется](https://claude.ai/artifact/Gy3NwTdcgmpyWyqPPAb5Wq)); the temperature dial shows the body to a tenth
  and where the place draws it ([hud-dials](2026-11-18-hud-dials.md)).
- **Cards** state the fact: «Выдерживает до −110°», «Жар до +50°».

## Consequences

- By the ship a basic suit feels nothing; out after debris it feels the cold, and starts to slow, some 13 cells past the zone.
  The heavy helmet's tank no longer buys reach.
- A bare hero on the first planet's surface is chilled at night (35.4°, some 14% slower) and more in a night dust storm
  (33.4°, a third slower); bare by the hull in vacuum it dies in about 40 s, its last breath included.
- Cold death is a last breath, then a pass-out: the frost, the dial and the slowness are the warning, no alarm.

## Deliberately not handled

- The sim never checks the body or the slowness: a client could ignore them. Co-op among friends trusts it, as with movement
  (0002); positions and hits still enter as commands, so nothing desyncs.
- Others' bodies are not known: their strokes and drills replay at full pace; a rejoin starts the body well.
- No food or heat source warms the body; a room does, in seconds.
- A treadmill or wheel turns at full effect under a frozen hero.
- No suit past the basic's −110° yet: the quilted suit (−150°), weight and clothes under the suit are the plan's, still open.
