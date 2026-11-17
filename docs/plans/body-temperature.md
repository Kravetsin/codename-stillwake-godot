# Design: body temperature, slowness and weight

Status: third draft, 2026-11-17, reviewed by `design-reviewer` with its cuts applied. It is built from the user's calls in conversation; the earlier stateless draft was
dropped once death and slowness by temperature were asked for. Phase 1 lands after `feat/mild-climate`, whose degrees it
reads. Three phases, each its own branch and its own play.

## Problem

1. **The thermometer stops at −40° in space while the frost keeps growing.** The field's edge drains air through a second
   rule, `EdgeCost`, that never reads as degrees.
2. **The HUD shows an environment number.** "−20° in space" reads oddly.
3. **Cold only costs air.** Nothing else limits where a hero can go, and gear has one axis, the tank.

## The model

- **Two vitals.** Air is for breathing. The body's temperature is the hero's own, and cold and heat act only on it: cold
  no longer costs air.
- **One limit per suit, the same everywhere.** A suit's price is its weight, not a second number.
- **One slowness.** The body and, from phase 2, the weight slow the hero through the same two factors: movement and
  work.

## Phase 1: the body

### As the player meets it

- **The HUD.** The thermometer shows the body, 37.0° when well, to a tenth.
- **The hover.** It shows the body, «Здесь −160°» for the place, and both limits, the crossed one red.
- **Space is really cold.** About −100° by the hull, falling to −200° at the field's line (128 cells out) and holding there.
- **The basic suit (−110°).** By the ship nothing is felt. Flying out after debris, a hero:
  - slows from about 20 cells past the zone;
  - is at half speed about 50 cells out;
  - passes out about 71 cells out.
- **The quilted suit (new, −150°)** is free to 64 cells and dies just short of the line, about 121 cells out. Later suits
  reach farther. How far a hero goes from the ship is the suit's to decide.
- **A bare hero in vacuum** (0° limit) is dead in about three seconds by the hull.
- **The first planet** (mild climate: +12° day, −8° night). A suit feels nothing there. A bare hero:
  - by day, nothing;
  - on a mild night, 35.4°: chilly, not slowed;
  - in a night dust storm (−18°), 33.4°: slowed to about 82%.
- **Rooms** (+20°, aired) warm the body back toward 37.0°, about ten seconds from 29°.
- **Heat** mirrors the cold above the suit's `High`: slowed from 39°, dead at 42°. On the deep hot band (+60°) a basic
  suit (+50°) settles at 39.0°, just short of slowing; a bare hero (+30°) dies.
- **The cape** lowers both limits by 5°, as today.

### The rule, each breath, in hero id order

1. **Felt degrees**, as today: `Felt` (below `Low`) or `Heat` (above `High`).
2. **Target:** `Normal − Sway × felt` in the cold, `Normal + Sway × heat` in the heat. With nothing felt, the target is
   `Normal`.
3. **Move:** the body closes `Pull`‰ of its gap to the target, rounded away from zero so it lands exactly. A wider gap
   moves it faster, so the deep cold freezes quickly.
4. **Death:** a body at or under `ColdDeath`, or at or over `HeatDeath`, passes out at once through today's `PassOut`, as
   a hero with no air does.
5. **Where the step runs:** every breath, after the creative hero's and the passed-out hero's `continue`, outside the thin
   air's `beat` gate. A creative hero never moves its body.
6. **What goes:**
   - the cold's and the heat's air cost: `Chills`, `Temperature.Bite`;
   - the edge's: `EdgeCost`, `Field.EdgeCost`, `Field.OffGrid`;
   - the regen suit's "never the cold".

   Grit and lava keep costing air.
7. **The last breath:**
   - it clears on `covered && burn == 0`;
   - it starts on `net < 0 || burn > 0`;
   - the felt degrees leave both.

### Slowness: two factors, in thousandths

- **`MoveOf(peer)`** scales run, walk, climb and jetpack thrust. It scales the jump's height, not its speed: the speed by
  √factor, or a 200‰ jump would rise 4%.
- **`WorkOf(peer)`**, in phase 1, scales the tools used out in the cold: the swing's cadence and the drill's and
  grinder's rate. Building and crafting happen aired at +20°, where the body warms within breaths, so they are left at
  full speed.
- **From the body:** both are 1000 within `Easy` (2.0°) of 37.0°. They fall linearly to `Least` (200) at 28.0° and at
  42.0°, so at the edge of death an axe swings about once in five seconds.
- **Where it applies:** the client applies `MoveOf` to its own hero's controller, since movement is client-authoritative
  (0002). It reads its own hero's sim body a few lockstep ticks late, and positions replicate as before. `WorkOf` applies
  where each of its three sites is timed; the plan names them.

### Degrees

- **`Afloat`** is today's ramp with new ends: `Temperature.Hull` (−100°) in the zone, falling to `Temperature.Space`
  (−200°) at the field's line, holding past it.
- **Off the grid:** `Space`, as today.
- **Planets are unchanged.**
- **No ramp to −273° at the world's edge.** No suit lives past −200° yet: the quilted dies at −195°. The ramp comes with
  the first suit that needs it.

### Gear

- **Suits:**

  | Suit | `Low` | `High` | Note |
  | --- | --- | --- | --- |
  | basic | −110° | +50° | |
  | regen | −110° | +50° | the regen is its trait |
  | heat | −110° | +60° | the heat is its trait |
  | quilted (new) | −150° | +50° | |

- **The quilted suit and the clothes** are made in another session by the user (2026-11-17). Phase 1 builds the
  mechanic with today's three suits; once the new items land, their `Low`/`High` are set to this table.
- **Bare:** `BareLow` 0°, `BareHigh` +30°, as today.
- **Cards** state the fact: «Выдерживает до −110°», «Жар до +50°».

### HUD and screen

- **The number:** the sim's body, glided between breaths by today's `Thermometers.Glide`, with `GlideS` about 1 s so the
  once-a-breath steps slide.
  - frost-blue below 37.0°, deeper toward 28°;
  - ember above, deeper toward 42°.
- **Frost and haze:** drawn from the same glided body, so the number and the screen agree. `FeltFull` and the edge's share
  go.

### Numbers (placeholders, in `sim/Content`)

| Name | Value |
| --- | --- |
| `Temperature.Normal` | 3700 (hundredths) |
| `Sway` | 20 hundredths per felt degree |
| `Pull` | 250‰ |
| `Easy` | 200 |
| `ColdDeath` | 2800 |
| `HeatDeath` | 4200 |
| `Least` | 200‰ |
| `Hull` | −100 |
| `Space` | −200 |

| Felt | Settles at | Speed | Case |
| --- | --- | --- | --- |
| 8 | 35.4° | 100% | bare, mild night |
| 18 | 33.4° | ~82% | bare, night dust storm |
| 30 | 31.0° | ~54% | basic suit, about 50 cells past the zone |
| 40 | 29.0° | ~31% | |
| 45 | 28.0° | 20%, then dead | |
| 100 | dead in 3 breaths | | bare by the hull: 37 → 32 → 28.3 → 25.4 |

### Saved and derived

- **Saved:** `Hero.Body`, in hundredths off `Normal`, where 0 means well. It is an added field whose initializer means
  "none yet", so there is no format bump (added-save-fields-need-no-bump). `PassOut` and the pod's print reset it.
- **Derived:** `MoveOf` and `WorkOf` come from `Hero.Body` alone (and, in phase 2, gear and gravity). The target stays
  private to the breath step.
- **Determinism:** integers only; `long` where products may grow.

## Phase 2: weight and gravity

- **Weight.** An item may have a `Weight`: thousandths of movement it takes at the ship's gravity.
  - the basic suit 100 (−10%), the quilted 150;
  - the jetpack and the helmets small;
  - the card: «Тяжесть: −10% к движению».
- **Move.** `MoveOf` multiplies in `1000 − Σweight × gravity / 1000`, with gravity in thousandths of the ship's:
  - aboard: 1000;
  - the first planet: 700 (−7% in the basic suit);
  - open space: 0, so weight is free afloat.
- **Weight never slows work.** Gravity touches neither tools nor hands (the user's call, "for now").
- **Is −10% noticed?** Run drops from 440 to 396 px/s. Taking the suit off in a room or on the planet feels lighter, yet
  the start is not sluggish.

## Phase 3: clothes and the loadout swap

- **Leather clothes.** The first worn clothing for the first planet: about −15° to +35°, little weight; the look picked on
  a page.
- **The loadout button.** It swaps the body (and the back, if one exists) with a second set; the helmet stays.
- **No back slot today.** The air tank lives in the helmet, so a back slot is phase 3's own question.
- Phase 3 gets its own short design once 1 and 2 are played.

## Users of the mechanic

- **Phase 1:**
  - the space field and its edge, now the reach of a suit;
  - planet nights, storms and the hot band;
  - rooms, the cape and the four suits.
- **Phase 2:** every worn item, and every surface's gravity, which already exists (`Surfaces.Kind.Gravity`).
- **Next:** hotter and heavier planets, and suits past −150°.

## Trade-offs accepted

- **Suits make planet cold trivial.** On purpose: the price is weight (phase 2), and the bare hero's limits are what
  planets test.
- **The tank no longer buys reach.** The heavy helmet's selling point narrows to breath; reach is the suit's
  (supersedes 0050 and the heavy helmet's off-grid guarantee).
- **Cold death is sudden.** It is a pass-out at 28°, not a gasp; the slowness and frost before it are the warning.
- **No time-left anywhere.** Only facts on cards; the HUD shows the body, not a countdown.

## Out of scope

- **Gravity on work speed:** the drill is mechanical.
- **Food or heat sources warming the body.**
- **A hand-held thermometer:** the user's idea, deferred; the hover reads the place.
- **Wounds or loads slowing a hero:** no user yet.
- **Other heroes' bodies on your screen.**

## Tests (phase 1)

- **Scenarios:**
  - a bare hero by the hull dies in three breaths;
  - a basic suit holds 37.0 in the zone;
  - a suit settles at the table's values;
  - the body moves exactly `Pull`‰;
  - a room warms it back;
  - `ColdDeath` and `HeatDeath` pass out;
  - `PassOut` resets the body;
  - the cape shifts the target by 5 × `Sway`;
  - cold costs no air;
  - grit and lava still do;
  - the degrees run −100 / −200 and hold past the line;
  - `MoveOf` and `WorkOf` read 1000 / 200 at the ends;
  - a creative hero's body never moves;
  - the regen suit and the last breath behave without the cold.
- **Port** `FieldTests`' edge cases onto the degrees.
- **Callers that stop naming `EdgeCost` and `OffGrid`:**
  - `RegenSuitTests`, `FogBandsTests`, `PlanetRulesTests`;
  - `TemperatureTests`;
  - `ContentTests` and `ContentTests.Hero`.
- **Invariants (FsCheck):**
  - the body never passes its target;
  - the factors stay within [`Least`, 1000];
  - the degrees never warm farther from the zone.
- **Client:**
  - the shown body glides and never overshoots;
  - the hero controller scales by `MoveOf`;
  - the hover's text.

## Cost (phase 1)

- **Changed:**
  - Crew: body, factors, degrees; air's cold costs out;
  - Content: temperature numbers, suits, the quilted suit and its craft;
  - client: thermometer, frost, hero controller, the work sites.
- **Docs:**
  - the Crew README;
  - an ADR superseding `temperature-in-degrees`' air cost, 0050 and the heavy helmet's off-grid guarantee.
- **Size:** about 500 lines.

## Plan (phase 1)

Built on `feat/mild-climate`. Each step builds and its tests pass before the next.

### 1. Content

- `Temperature`:
  - `Hull` −100, `Space` −200;
  - `Bite` out;
  - in: `Normal` 3700, `Sway` 20, `Pull` 250, `Easy` 200, `ColdDeath` 2800, `HeatDeath` 4200, `Least` 200, each with
    its `///` saying what it is for.
- `Field.EdgeCost` and `Field.OffGrid` out; `Field.Margin` stays (the ramp's length).
- `Items`: `Suit.Basic`, `Suit.Regen` and `Suit.Heat` at `Low: -110`; `High` unchanged.

### 2. Crew (sim)

- **`Hero.Body`:** hundredths off `Normal`, `[JsonInclude, JsonIgnore(WhenWritingDefault)]`, 0 well.
- **The breath step:** right after `if (!breath) continue;`, a private `Warm(hero)`:
  - felt = `Felt` or `Heat` at `hero.Cell`;
  - target = `−Sway × felt` or `+Sway × heat`;
  - step = `(target − Body) × Pull / 1000`, rounded away from zero, never past the target;
  - if `Body ≤ ColdDeath − Normal` or `≥ HeatDeath − Normal`: `PassOut(hero); continue;`.
- **Resets:**
  - `PassOut` sets `Body = 0`;
  - the creative branch sets `Body = 0` before its `continue`.
- **The air section:**
  - `cold` is `GritAt` alone; `Chills`, `EdgeCost` and the `felt` term go;
  - the last breath clears on `covered && cold == 0 && burn == 0` and starts on `net < 0 || cold > 0 || burn > 0`, so
    grit still counts.
- **Queries** (`ICrewQueries`):
  - `BodyOf(peer)`: hundredths, `Normal + Body`, `Normal` with no hero;
  - `MoveOf(peer)` and `WorkOf(peer)`: 1000 within `Easy` of `Normal`, else linear to `Least` at the death lines,
    computed in `long`, rounded down;
  - `EdgeCost` out;
  - docs on `Felt`, `Heat` and `TemperatureAt` updated.
- `Afloat` is unchanged in code: it already holds at `Space` past `Field.Margin`. Off the grid stays `Space`.

### 3. Client

- **Movement:** `HeroInput` gets `Move` (thousandths), filled in `HeroView.cs` beside `Slowed` from
  `m.Crew.MoveOf(client.Id)`. `Hero.cs` scales:
  - `Paced`'s top (times `Slowed`'s);
  - climb up and down, swim, water swim;
  - the jetpack's `Accel` and `Top`;
  - the jump speed (and the ladder's and the water's) by √(Move/1000), so the height scales by `Move`.

  Floats are fine here.
- **Work:**
  - `AxeSwing`: the stroke's `PaceMs` × 1000 / `WorkOf`;
  - `DrillPace.Due`: pace × 1000 / `WorkOf`, rounded up, own drill and others' replay alike (`HeroView.Others.cs`).
- **Thermometer:**
  - `Thermometers.Read` carries `Body` (from `BodyOf`) beside the place's degrees;
  - the number is the body to a tenth, glided (`GlideS` 1.0, snap 0.05°);
  - depth is `Eased` of the cold share `(Normal − body)/(Normal − ColdDeath)` or the heat share
    `(body − Normal)/(HeatDeath − Normal)`;
  - the hover: «Здесь {n}°» and both limits, the crossed one red;
  - keys in `logic/*.En.cs` and `*.Ru.cs`.
- **Frost and haze:** `Report.Cold` and `Report.Heat` from the same shares of `BodyOf`; `FeltFull` and the edge share
  go.

### 4. Tests

- **New:** `BodyTests` (sim) for every scenario in the design's Tests section, and FsCheck for the body never passing
  its target and the factors staying within [`Least`, 1000].
- **Updated** wherever cold cost air:
  - sim: `SkyTests`, `TemperatureTests`, `PlanetRulesTests`, `RegenSuitTests`, `FogBandsTests`, `LavaTests`,
    `SuitsTests`, `ContentTests`, `ContentTests.Hero`, `FieldTests` (edge cases become the degrees' ramp), and the storm
    and lightning tests that topped up air against the cold;
  - client: `ReportTests`, `GustsTests`, `SoundReachTests`.

  Each rewritten assertion asserts the body or the factors, not the air.
- **Client unit tests:**
  - `MoveOf` scaling in `Hero.Step`;
  - the drill and swing pace at 500‰;
  - the thermometer's shares and text.

### 5. Try

`tools/test.ps1` on `sim/Tests` filters `Body|Temperature|Sky|Field|Suit|Regen|Planet|Lava|Storm|Lightning`, then
`game.tests`; `dotnet build game`; commit; launch the plain game for the user.
