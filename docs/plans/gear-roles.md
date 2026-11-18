# Design: what helmets and suits each give

## Problem
Only the suit sets temperature limits today. Every helmet stands to −110° like the basic one, so a helmet only decides air: the heavy helmet beats the quilted one everywhere, and nothing in the head slot trades air against warmth. The quilted helmet also can't be made before the planet, because its cloth only comes from planet ruins, and derelicts only after the engine.

## Decided with the user
- Helmets and suits stay separate slots.
- Temperature limits add up: a worn piece adds its own degrees to both limits.
- Air stays a helmet capacity (the HUD bar's pieces), not a rate.
- Heavy helmet = air, heated helmet = warmth: an either-or in the head slot.
- The heat suit gets its own heat helmet.
- The warm helmet is makeable in space, of space parts: no cloth in space.
- Space zones are a separate later design.

## Player experience
1. A new hero wears the basic helmet and suit. The dial's hover reads «Выдерживает до −110°», «Жар до +50°», as it does today.
2. They make coils and wire at the workbench, as for other gear.
3. At the workbench the heated helmet's card reads «Запас воздуха 50», «Холод −90°», «Жар 0°». The heavy helmet's reads 80, «Холод −50°», «Жар +10°»: what each piece adds, not a limit.
4. With the heated helmet on, the hover reads −150°. The hero flies farther before the frost starts, but the air bar is shorter than in the heavy helmet.
5. The ice belt arrives (−140° by the hull). In the heated helmet the hero works by the hull without feeling anything. A crewmate in the heavy helmet freezes at once.
6. On the planet, the heat helmet and heat suit together stand −80° / +80°; the pair is colder in space than the basic one.

**When it goes wrong:** a player wears the heavy helmet out into the ice belt. Frost covers the screen as soon as they leave the hull and the body settles near 31°, so the axe and the drill slow down; about 19 cells past the zone it heads for death. The hover shows the total, −110°, in red: the card's −50° was the helmet's part of it.

## Out of scope
- The quilted suit (−150° in the body temperature plan): without weight it beats the basic suit in space outright, so it ships with weight, phase 2 of `docs/plans/body-temperature.md`.
- Weight, and clothes (phase 3 of the same plan).
- The space zones: cold bands up, the hot belt below, a bigger space, small craft.
- Anything on the thermometer ring: the ring is the body's scale (28°–42°), the place's limits have no point on it; the totals stay in the hover.
- Set bonuses, coolant consumables.
- The cape's warmth stays `Cape.Warmth` and `Caped`; moving it into the item rows waits for clothes.

## Approach

| Item | Slot | Tank | Cold | Heat | With the basic other piece | Source, stage | Trade |
|---|---|---|---|---|---|---|---|
| none | — | — | 0 (`BareLow`) | +30 (`BareHigh`) | — | — | — |
| basic helmet | head | 25 | −50 | +10 | −110 / +50 | lifepod, space | the free one |
| heavy helmet | head | 80 | −50 | +10 | −110 / +50 | black market, space | more air, ordinary warmth |
| **heated helmet** (new, replaces the quilted) | head | 50 | −90 | 0 | −150 / +40 | workbench: basic helmet, 2 coils, 2 wire, 2 plates; space | warmth for air and heat |
| **heat helmet** (new) | head | 25 | −30 | +30 | −90 / +70 | workbench: basic helmet, 2 mica, 2 alloy scrap, 1 glass; planet | heat for cold |
| basic suit | suit | — | −60 | +10 | −110 / +50 | lifepod, space | the free one |
| regen suit | suit | — | −60 | +10 | −110 / +50 | black market | gives air back, paid in charge |
| heat suit | suit | — | −50 | +20 | −100 / +60 | workbench, mica; planet | heat for cold |

Every number is a placeholder in `sim/Content`.

**The sum.** `CrewModule.LowOf` and `HighOf` read the suit slot alone today, then the cape. They become the bare limit plus the `Low` or `High` of every worn piece, then the cape as now. `ItemSpec.Low` and `High` change meaning, from a suit's absolute limit to any worn piece's signed part. `Felt`, `Heat`, `BodyHeat` and the dial keep reading `LowOf` and `HighOf`. The world's edge stays a wall: the coldest sum, −155° with the cape, against `Deep`'s −2000°.

**The heated helmet replaces the quilted one** (the user, 2026-11-18): cloth stays off space, where it would lie unused, so space gets its own warm helmet of space parts, warmed by heating coils. The quilted helmet is removed, its art and craft with it; its cloth recipe leaves no gap, the planet getting the heat helmet. A coil heating off the belt's charge is a later idea, not built.

**The ice belt** stays gated, because it thins the debris stream four times. In `Stretches`, its `Unlock` changes from the quilted or heavy helmet to the heated helmet alone: the heavy helmet no longer helps against cold. Nothing checks what a hero wears; the cold alone decides who works there. Saves break at release, so a save whose belt the heavy helmet opened is not handled.

**What this leaves ready for the space zones:** the cold answered by −150°, the heat by the heat pair's +80°; a zone's opening can name a piece in its `Unlock`, as the ice belt does.

**Client.**
- Card rows become `card.cold` «Холод −60°» and `card.heat` «Жар +10°», replacing `card.withstands` and `card.withstandsHeat`: «Выдерживает до …» on a helmet would be false.
- The dial's hover keys stay and now show the totals.
- New key: `item.heatHelmet` «Жаростойкий шлем». Its art is a pick page, last; a placeholder until then.

## Determinism and co-op
- Gear changes enter through the existing item commands.
- The limits are whole-degree sums, no division.
- Limits are derived from the inventory; nothing new is saved or sent. The body stays client-authoritative (0002).

## Trade-offs accepted
- **The heavy helmet beats the basic one outright**: the basic is the free one. Weight becomes its cost later.
- **The heat suit with a basic helmet now feels cold right past the zone**, −100° where today −110°. The trade is the point; the PR says so.
- **A helmet worn without a suit stands −50°**, today 0°. Nobody goes into vacuum without a suit on purpose.
- **The ice belt opens with the heated helmet**: once coils and wire are made, not from the first debris.

## Tests
- A table-driven content test: every helmet (or none) with every suit (or none), with and without the cape, gives the table's sums, and `Low < High`, `Low ≤ Room ≤ High`.
- Scenarios: bare 0° / +30°; the heated helmet made at the workbench; the ice belt opens for the heated helmet and not for the heavy one.

## Cost
- Crew: `LowOf` and `HighOf`. No new module; no `World.SaveFormat` change (limits are content and derived).
- Content: `Items` (the meaning of `Low` and `High`, the helmets' parts, the heat helmet row), `Crafts`, `Stations`, `Stretches` (the `Unlock` only), the debris loot.
- Client: the two card keys, the heat helmet's name and placeholder.
- About 200 lines, art excluded.

## Art
The heated and heat helmets' looks come from GPT references on one sheet each → a pick page → traced (`tools/anime-art/trace.js`), as the quilted helmet's was. The user asked for a batch of other helmets on the same round, added to the game as items waiting for their use.
