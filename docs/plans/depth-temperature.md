# Design: temperature by a planet's depth band

Status: designed (`design-author`, cut by `design-reviewer`), awaiting the user's answers to the open questions; nothing
built. Spore gas in the garden, which the user named with it, is a later slice (see the end).

## Problem

Going down a planet changes nothing about how a hero survives, so a descent takes no preparation and gear gives no reason
to craft. Night's chill, the bare-body cold and the cape's warmth are three separate rules, and the cape only spares a
fifth of the cold, so it can never remove one, as the user wants it to.

## Player experience

1. The hero lands. On the surface by day a faint frost shows at the screen's edge, and the plain suit's air drains a
   little faster than before.
2. The hero puts on the cape and the frost is gone. At dusk under the open sky it comes back, lighter than without the
   cape.
3. Digging into the teal garden, the frost fades even without the cape.
4. In the amber deep a faint heat haze shows at the screen's edge. In the ember band it is strong, and the air drops fast.
5. The hero goes back up, makes a heat suit at the workbench, and goes down for magnetite in the heat cracks.

Going wrong: a hero chases magnetite into the ember band in the plain suit. The haze is strong, the air lasts about
37 s, and the hero passes out with the body left below. The next trip starts with the heat suit on.

## One mechanic

`Crew` owns it; the breath is its only consumer. One rule replaces the bare cold, night's chill and the cape's warmth:

- **A cell's temperature**, in degrees from comfort (below 0 cold, above 0 heat), from one private `TemperatureAt(cell)`:
  - an aired cell: `Temperature.Room` (0);
  - any other: the kind's `Temperatures[band]`, the band from `Terrain.BandOf` when `Rules` is a `PlanetType` (as
    `Creatures/Spawn.cs` does), else 0; in band 0 only, minus `SkyNow.Chill` where the cell has no back wall. Deeper
    bands never feel the night.
- **Gear:** two new item fields, `Warmth` and `Cooling`, summed over what is worn in the body and cape slots.
- **Felt** = max(0, −t − Warmth) + max(0, t − Cooling). Each felt degree costs `Temperature.PerDegree` (100‰ of a unit)
  a breath, paid by the suit's air alone, as the cold is today.

Where it is heading: real temperatures replace only the body of `TemperatureAt`; gear, felt degrees and their cost stay.
Storms add to `SkyNow.Chill`, as [day-night](day-night.md) plans. A cold suit, if one is ever wanted, is a row with more
`Warmth`.

Two costs stay apart:
- **The field edge** (0050) is a leash on the ship's build field, and gear must not lengthen it. The cape stops sparing it.
- **Lava** is a contact hurt that cuts the last breath. No suit makes lava walkable; the heat suit does not spare it.

Query: `Felt(peer, cell)`, signed degrees (negative cold, positive heat), replaces `Cold` and `Warmth`; its caller is the
client's `Report`. No commands, no events.

## Numbers (all in `sim/Content`)

- `Temperatures`: the ship `[-10]`; the first planet `[-11, -8, 1, 10]` (cold, garden, amber deep, ember). A kind
  without the field reads `[0]`.
- `Sky.Cycle.Chill`: 2 degrees. `Cycle.Every`, `Crew.BareCold` and `Cape.Warmth` go.
- Warmth: the basic body 10, the regen suit 10, the cape 1. Cooling 0 on all three.
- **The heat suit** (`heatSuit`), a body: Warmth 10, Cooling 10. Made at the workbench, beside the cape and the regen
  suit, from 4 mica, 4 alloyScrap, 2 plate and 2 tube, 240 ticks. Wearing it means not wearing the regen suit.

A basic helmet (45 units) outside on thin air, where breathing alone costs 0.2 units a second:

| Case | Felt | Seconds (today) |
| --- | --- | --- |
| Plain suit, band 0, day | 1 | 150 (225) |
| Plain suit, night, open sky | 3 | 90 (112) |
| Plain suit, night, under a back wall | 1 | 150 (225) |
| Cape, band 0, day | 0 | 225 |
| Cape, night, open sky | 2 | 112 |
| Garden (−8), plain or cape | 0 | 225 |
| Amber deep (+1), plain or cape | 1 | 150 |
| Amber deep, heat suit | 0 | 225 |
| Ember (+10), plain suit | 10 | 37 |
| Ember, heat suit | 0 | 225 (heavy helmet 500) |
| No body: band 0 day / night open / garden / ember | 11 / 13 / 8 / 10 | 34 / 30 / 45 / 37 (112 / 56 / 112 / 225) |
| Ship vacuum, plain suit / no body | 0 / 10 | 45 / 22 (unchanged) |

## Determinism and co-op

- No new input: the hero's cell arrives through `Enter`, gear through `Items`' commands.
- Integers only. A breath pays ⌊(n+1)·r/1000⌋ − ⌊n·r/1000⌋, n = `Clock / BreathEvery`, r = felt × `PerDegree`: rounded
  down, the rest paid on a later breath, nothing carried in the save. A breath skipped (creative, out) loses at most a
  unit. Every hero pays on the same breaths: at felt 1 a unit every 10 s, so the first may come up to 9 s after entering
  the cold. The last breath starts or ends on a paying breath only, as it does on thin air's beat today.
- Nothing new is saved. `Hero.Spared` and `Soften` stay for blows alone; an old remainder under 1000 is valid for them.
  No `SaveFormat` bump.
- A peer joining receives nothing new: temperature comes from content, cell and sky.

## Player-visible changes and trade-offs accepted

- **The surface by day costs a plain suit 150 s instead of 225 s:** the user's "slightly cold"; the cape gives it back.
- **A body is now needed on a planet:** without one a hero lasts about 34 s. The pod makes a body for nothing.
- **The cape spares none of the field edge's cost:** its frost there gets heavier. The edge is a leash.
- **An aired room deep down is comfortable,** in the ember band too: a sealed base is a shelter. Real temperatures fix it.
- **A felt change between breaths can shift a unit** from one breath to the next. Too small to matter.
- **Night no longer reaches below band 0,** where today a cave cell without a back wall felt it at any depth.

## Deliberately not handled

- Per-cell simulated temperatures; heat from lava, machines or fires; temperature for machines, plants, creatures or
  liquids.
- A cold suit, coolant consumables, a second tier of heat gear.
- Degrees shown as numbers anywhere.
- Any change to bands or ores.
- Creatures (another session's).

## Plan

One PR, `feat/depth-temperature`, after the user's answers.

1. **Content.** `Temperature` (`Room`, `PerDegree`); `Kind.Temperatures` defaulting to `[0]`, the ship's and the first
   planet's rows; item fields `Warmth`, `Cooling` on the body rows and the cape; `Chill` 2; `Cycle.Every`,
   `Crew.BareCold`, `Cape.Warmth` gone; the `heatSuit` row, recipe and workbench entry, and its unlock (open question 2).
   `ContentTests.Hero.cs` and `CapeTests.cs` (cold softening) rewritten with it.
2. **Crew.** `TemperatureAt`, `Felt`, the paying schedule in the breath, replacing `ColdAt`'s bare and chill parts;
   `EdgeCost` kept, no longer softened. `Cold`/`Warmth` give way to `Felt`. A failing scenario per table row first, on
   maps whose rows fall in each band; plus: an aired cell in the ember band costs nothing, lava burns a heat suit as
   hard as a plain one, the cape spares none of the edge. Invariants (FsCheck): more Warmth or Cooling never costs more;
   over 10 breaths at a steady felt f exactly f units are paid — broken once to see it bite.
3. **Client.** `Report` reads `Felt` plus the edge cost; `Frost` takes a tint and is made twice, cold and heat. The
   haze's look is a user pick on a published page (CLAUDE.md, art picks), then exported.
4. **Docs.** An ADR `YYYY-MM-DD-depth-temperature` superseding the cold parts of night-chills, suits-are-worn and
   the-cape-is-worn; Crew's and Sky's READMEs; the concept's cape line ("spares a fifth … of the cold"); this plan
   deleted.

Estimated diff: about 400 lines with tests.

## Later: spore gas in the garden

The user's third depth idea. It is not temperature: it is a hurt laid in an area, which the capback's gas already is
(`PatchKind` `Gas` in `Content.Creatures`, laid by `Vent`, read in `Creatures/Patches.cs`). The slice lays the same kind of
patch from a terrain feature in band 1 (a fissure that puffs on a timer), and a filter on the helmet mount spares it. It
waits for the creatures work now in `Creatures` to land, so two sessions do not edit that module at once.

## Open questions

1. The amber deep at +1 (the heat starts there, a plain suit pays a little) or 0 (only the ember band asks for gear)?
2. How the heat suit is unlocked: a board note, research, or the black market like the regen suit?
3. The "cold band chills the suit as night does": here band 0 is the surface's band, where the cape covers the day and
   night is what chills. If the cold band's rock below the surface should bite through the cape too, the band needs a
   colder row of its own under the surface (a fifth band, or a depth inside band 0), which this design does not have.
4. Spore gas as the next slice once the creatures work lands, as above?
