# Sky

## Files

| File | Purpose |
| --- | --- |
| `Fuzz.cs` | The module's `Fuzz` override: the commands the desync suite draws. |
| `Sky.cs` | The sky: a planet's hour, its night, `SetTime`, `ISkyQueries`. |
| `Stretches.cs` | Stretches, the ship's and a planet's storms: their schedule, the sky's fork and `StretchNow`. |

A planet's day and night ([night-chills](../../docs/decisions/2026-11-07-night-chills.md)), and stretches: the ship's spells of space
([stretches-of-space](../../docs/decisions/2026-11-15-stretches-of-space.md)) and a planet's storms ([surface-storms](../../docs/decisions/2026-11-16-surface-storms.md)). What the cold costs is Crew's (its chill in degrees,
[temperature-in-degrees](../../docs/decisions/2026-11-14-temperature-in-degrees.md)); what it looks like is the client's
(`Daylight`, `StretchLooks`). The numbers are `Content/Sky.cs` (`Surfaces.Kind.Cycle`) and `Content/Stretches.cs` (`Kind.Stretches`).

- **Depends on:** `Knowledge` (what has been obtained). Listed `[Grid, …, Knowledge, Sky, …]`, before Crew and Debris, which read it.
- **State** (`SkyState`, saved): `Time` — ticks since the start of the day, 0 with no `Cycle`; the stretches' `Opened` and
  `Came` (ids in content order), `Next` and `NextIn` (the one chosen, ticks till it starts), `Active`, `Left` and `Ran` (the one
  running, ticks left and run), `Drawn` (forks drawn). Each added with a "none" initializer: no format bump.

## Rules

- **The hour.** A surface whose kind has a `Cycle` opens at 0, the start of daylight, and in the simulation phase sets
  `Time = (Time + 1) % Length` each tick. With no `Cycle` (the ship) nothing moves.
- **Night** is `Time % Length ≥ Length − Night`; its `Cycle.Chill` is then added to `Now.Chill`. Dusk and dawn are the client's.
- **A planet nobody is on** keeps running (0076): its hour passes.
- **`SetTime(Time)`**, from the dev panel: `Time` modulo `Length`, a negative one too, applied before the tick advances it.
  Refused `sky.noDay` with no `Cycle`, nothing changed.
- **Stretches**, on a kind with them (the ship, planet 1), in the construction phase each tick: every stretch with an `Unlock` obtained
  (`Knowledge.ObtainedOf`), or none, joins `Opened`; with none chosen or running, the next is chosen — the first opened never come, else
  one drawn from `Opened` — its wait its `First` if it never came, else its `Gap`, drawn. Its last `Warning` ticks it is
  `Coming`; at 0 it runs for its `Duration`, drawn then, and joins `Came`; at its end the next is chosen, the gap from the end.
  A choice made stands: at most one is chosen, coming or running. Draws: which, then its wait; its duration as it starts;
  each from a fork of the world's Rng salted by the surface and `Drawn`, which then goes up: the world's own Rng is untouched.
- **`StretchNow(Id)`**, from the dev panel: opens it and makes it next in place of any, its warning whole. Refused
  `sky.noStretch` (unknown, or the surface has none) and `sky.stretchRunning`.
- **Saves:** format 54 added the slice; its step puts `{"time": 0}` right after `grid` in every slices object, as loading
  reads them in the modules' order. A `Time` at or past a later, shorter `Length` is read modulo and wraps on its next tick.
  An older save's stretches load empty: what was obtained opens on the first tick, the first coming at its `First`.

## API

- **Queries** (`ISkyQueries`): `Now` — `SkyNow(Time, Night, Chill, Stretch, Coming, ComingIn, Ran, Pace)`: `Chill` night's plus the
  running stretch's, `Pace` its `Pace` (1000 with none). Read by Crew (`Chill`, `Pace`), Debris (`Stretch`) and the client.
- **Commands:** `SetTime(Time)` — `sky.noDay`; `StretchNow(Id)` — `sky.noStretch`, `sky.stretchRunning`.
- **Fuzz:** one turn in sixteen, an hour from −10 000 to 50 000, refused on the ship; `StretchNow` of any stretch.

## Invariants

- `Now.Time` is in `[0, Length)` on a surface with a day, 0 without (`SkyTests`).
- At most one stretch chosen, coming or running; `Left` within its `Duration`; `Opened` and `Came` only grow, each id once,
  `Came` within `Opened` (`StretchesTests`).
- `Now.Chill` is night's plus the running stretch's.

## Deliberately not handled

- **Rain is a look**, not water; wind pushes nothing. Lightning is planned (docs/plans/surface-storms.md).
- **No hour shown aboard:** the sky is the clock; a later visit may land in the dark.
- **Two stretches at once**, unlocks per hero: each surface has its own `Sky`.
