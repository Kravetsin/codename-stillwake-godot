# Plan: heat

Status: designed (`design-author`, cut by `design-reviewer`, numbers and unlock picked by the user on 2026-11-14, reviewed by
`/review-plan`); step T-3 of [temperature](temperature.md). H-1 built ([heat](../decisions/2026-11-14-heat-limits.md)).

## Why

The ember band reads +60° and costs nothing: the deep feels like the garden, nothing asks for a heat suit, and no card says
what a body does against heat. The cold already runs on degrees past a limit (temperature-in-degrees); heat is its other side.

Words: **cold felt** is `Felt` (degrees under `LowOf`), **heat felt** is `Heat` (degrees over `HighOf`), **felt** their sum
(at most one is above 0). The heat suit is `Suit.Heat`; the screen's heat effect is **the haze**.

## Player sequences

The first planet's degrees run smoothly with depth ([temperature-gradients](../decisions/2026-11-14-temperature-gradients.md)):
+15° at the amber deep's middle, +43° at its bottom, +60° from the ember band's middle down.

1. A suited hero digs down. The amber deep is calm all through.
2. Past +50°, about a fifth into the ember band, the basic suit feels heat: the thermometer takes its heat colour, a haze rises
   at the screen's edges, the helmet drains faster. From the ember's middle down (+60°, 10° felt) a basic helmet's 45 units
   last 112 s, not 225; the haze there is moderate (10 of the 30 that draw it full), a hero with no body gets the full haze.
3. With the cape on, heat starts at +45°, just inside the ember band, and the deep costs 15°: 90 s. The hero takes the cape off
   going deep, or keeps it for its guard against blows.
4. Once the crew has had every part of the heat suit (mica from the cave walls and alloy scrap from the ruins being the late
   ones) and a workbench stands, the suit is known. Its card: «Выдерживает до −40°», «Выдерживает до +60°». In it the ember band
   costs only the breath.
5. The regen suit in the ember: it gives back the thin air's share, never heat felt, so it feels the heat as the basic suit does
   and lasts as the heat suit does (225 s) while its belt has power. The heat suit needs no power; the two never go together.

## The rule

Crew owns it, as it owns the cold; nothing new is saved.

- **A hero's heat limit** (`HighOf(peer)`): its body's `Item.High`, or `Temperature.BareHigh` (+30°) with none. The cape lowers
  both limits by `Cape.Warmth` (5°): it helps against cold and hurts against heat.
- **Heat felt** (`Heat(peer, cell)`): `max(0, TemperatureAt(cell) − HighOf(peer))`, 0 with no hero. `Felt` stays the cold side.
- **The breath** computes one `felt = Felt + Heat` and uses it in both places `felt` is used today: `Chills(felt)` at the
  same `Bite`, and the check that a covered breath ends the last breath only with nothing felt (`Crew.cs`, the `felt == 0` line).
  So heat past empty air starts the last breath, and the regen suit gives none of it back, as with cold.
- `HighOf` and `Heat` join `ICrewQueries` beside `LowOf` and `Felt`, and Crew's README `## API` line.
- **The frost** (`Report.Cold`) reads `Felt` alone, unchanged. **The haze** (`Report.Heat`) is `Heat` over `FeltFull` (30°),
  eased `s·(2−s)` as the frost is. The field's edge and lava stay apart, as today.

### The limits, from what each thing is made for

| Body | Low | High | Why |
| --- | --- | --- | --- |
| none | 0° | +30° | a body's comfort |
| `suit`, `regenSuit` | −40° | +50° | made for space and the surface: space's coldest, the ember band's top |
| `heatSuit` (new) | −40° | +60° | made for the ember band: its hottest |

An aired room (+20°) is inside every pair, the cape's too. Hotter planets later ask for more than the heat suit.

### Walk-through (a basic helmet's 45 units; a breath a second; thin air 0.2 units a second; each degree felt 0.02 more, `Bite` 20‰ a breath)

| Where (first planet) | Degrees | Basic suit | Basic + cape | Heat suit | Heat suit + cape | No body (felt) |
| --- | --- | --- | --- | --- | --- | --- |
| Amber deep, middle | +15° | 225 s | 225 s | 225 s | 225 s | 0° |
| Amber deep, bottom | +43° | 225 s | 225 s | 225 s | 225 s | 13° |
| Ember, a fifth in | +50° | 225 s | 150 s | 225 s | 225 s | 20° |
| Ember, middle and below | +60° | 112 s | 90 s | 225 s | 150 s | 30° |

## The heat suit

- `Suit.Heat` = `heatSuit`, a body: `Equip: Suit.Slot, Gear: true, Group: Wearable, Low: -40, High: 60`.
- Made at the workbench: 4 mica, 4 alloyScrap, 2 plate, 2 tube, 240 ticks.
- **Unlocked by the board's rule** (the user's pick; Research's README, "The rule"): a craft is understood once one of each
  part was ever obtained (`Knowledge.ObtainedOf` above 0) and a machine that makes it stands. Never sold: not in
  `Research.GearSold`.
- Wearing it means not wearing the regen suit: one body slot.
- Its look (the item's icon and the hero wearing it) is picked on the page and designed whole, not a basic suit with parts
  bolted on.

## On screen

- **Thermometer:** `Thermometers.Read` carries `High` and `Heat` too. Past `HighOf` the number and the star take the heat
  colour, deeper the further past, on the frost's scale (30° felt is full). The hover shows the limit being crossed, cold's or
  heat's, in `Tone.Bad` while felt. Inside both limits it is calm, as today.
- **Haze:** a screen layer like the frost, from the screen's edges, by `Report.Heat`.
- **Cards:** a body gets a second row «Выдерживает жар до +50°» with a heat sign (`CardSign.Heat`). The cape keeps «Тепло +5°»:
  warmth, which is what costs in heat.
- **Pick page** (CLAUDE.md, art picks), one subject per section: the heat suit (icon and worn, moving), the haze, the
  thermometer's heat colour, the card's heat sign.

## Saves, co-op, determinism

No new input: the cell comes by `Enter`, gear by `Items`. Whole degrees, paid as the cold is. `Item.High` is `int? High = null`,
`BareHigh` and the new item are content: no save field, no format bump. A world saved before the heat suit grants it on a later
tick by the rule. The crafting fuzz draws recipes from content, so it reaches the new craft unchanged.

## Steps, one PR each

1. **H-1, the rule:** `Item.High` on `suit` and `regenSuit`, `BareHigh`, `HighOf`, `Heat`, the breath's one `felt`; the cape
   on both limits; the cards' heat row (a placeholder sign until the pick); the thermometer's `Read` and hover by side.
   Tests: a scenario per basic-suit, cape and no-body cell of the walk-through; an aired ember cell free; heat past empty air
   starting the last breath, and a covered breath the schedule skips keeping it; the regen suit giving none of heat back.
   FsCheck: at most one of `Felt`, `Heat` above 0, for every body, bare, cape or not. `ContentTests`: for every body and bare,
   `(Low ?? BareLow) < (High ?? BareHigh)`.
2. **H-2, the pick page:** the four subjects above.
3. **H-3, the picks built:** the heat suit (row, craft, unlock test, icon, worn look) and its walk-through cells; the haze; the
   thermometer's heat colour; the card's sign. A dev room: the ember band, the suits and the cape to swap. Then this plan and
   [temperature](temperature.md) are deleted, what stays true moved into an ADR, Crew's README and the concept.

## Accepted trade-offs

- The heat suit only ties the regen suit in the ember band (225 s), the regen suit paying power for it; the heat suit is the
  one met first, needs no power and costs no credits.
- A suited hero's haze in the deepest ember is moderate, never full: it costs twice the breath there, not more.

## Deliberately not handled

- Heat from lava, furnaces, machines or the galley; rooms heated or cooled.
- Temperature for machines, plants, creatures or liquids.
- A suit with both regen and heat; coolant consumables.
- Lava's burn: still `Lava.Burn`, spared by no suit.
- Hotter planets: they need only their own `Temperatures` row.
