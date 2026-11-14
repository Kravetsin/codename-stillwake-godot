# Sky

## Files

| File | Purpose |
| --- | --- |
| `Fuzz.cs` | The module's `Fuzz` override: the commands the desync suite draws. |
| `Sky.cs` | The sky: a planet's hour, its night, `SetTime`, `ISkyQueries`. |

A planet's day and night ([night-chills](../../docs/decisions/2026-11-07-night-chills.md)): its hour counted a tick a tick,
its night the end of each day. What night costs is Crew's (its chill in degrees,
[temperature-in-degrees](../../docs/decisions/2026-11-14-temperature-in-degrees.md)); what it looks like is the client's (`Daylight`). The
numbers are `Content/Sky.cs`, on the surface's kind as `Surfaces.Kind.Cycle`.

- **Depends on:** nothing. Made right after `Grid`, listed `[Grid, Sky, Rooms, …]`, so Crew, made later, reads it.
- **State** (`SkyState`, saved): `Time` — ticks since the start of the day; 0 where the kind has no `Cycle`.

## Rules

- **The hour.** A surface whose kind has a `Cycle` opens at 0, the start of daylight, and in the simulation phase sets
  `Time = (Time + 1) % Length` each tick. With no `Cycle` (the ship) nothing moves.
- **Night** is `Time % Length ≥ Length − Night`; `Now.Chill` is then `Cycle.Chill`, the degrees colder under the open sky,
  else 0. Dusk and dawn are the client's.
- **A planet nobody is on** keeps running (0076): its hour passes.
- **`SetTime(Time)`**, from the dev panel: `Time` modulo `Length`, a negative one too, applied before the tick advances it.
  Refused `sky.noDay` with no `Cycle`, nothing changed.
- **Saves:** format 54 added the slice; its step puts `{"time": 0}` right after `grid` in every slices object, as loading
  reads them in the modules' order. A `Time` at or past a later, shorter `Length` is read modulo and wraps on its next tick.

## API

- **Queries** (`ISkyQueries`): `Now` — `SkyNow(Time, Night, Chill)`, all 0 with no day. Read by Crew (`Chill`) and the client.
- **Commands:** `SetTime(Time)` — `sky.noDay`.
- **Fuzz:** one turn in sixteen, an hour from −10 000 to 50 000; refused on the ship.

## Invariants

- `Now.Time` is in `[0, Length)` on a surface with a day; `Now` is all 0 on one without (`SkyTests`).
- `Now.Chill` is non-zero only at night.

## Deliberately not handled

- **Storms and rain** are slice 2 and later (docs/plans/day-night.md): no weather yet.
- **No hour shown aboard:** the sky is the clock; a later visit may land in the dark.
