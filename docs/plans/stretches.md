# Stretches of space

Spells of space the ship drifts through, unlocked by progress, so a crew that has landed still meets new content. One
shared mechanic; each stretch is a row of data. Designed by `design-author`, cut by `design-reviewer`, open questions
answered by the user on 2026-11-15, revised after `/review-plan`.

## Decisions

**What a stretch is.** A record in `sim/Content/Stretches.cs`: `Id`; `Unlock`, items, any one obtained opens it; `First`,
`Gap` and `Duration`, tick ranges, both ends included; `Chill`, degrees colder outside while it runs (0 for none); and
its own debris zone. One `Stretches.Warning` constant, 3 min, for all until one needs another. Times are stored in ticks
(20 a second, 1200 a minute).

**Where it lives.** In `Sky`: it already owns what the sky does, and it is registered before `Crew` and `Debris`, which
read it. `Sky` gains a `Knowledge` dependency (registered before it). Only the ship has stretches: `Kind.Stretches`, empty
on planets, which draw nothing.

**Unlocks are per world**, read from `Knowledge.ObtainedOf`: crafted, bought, looted or caught, shared by every surface.
Creative `Spawn` never calls `Items.Obtained`, so spawned items unlock nothing. Unlocks never lock again: `Obtained` only
grows.

**The schedule**, in `Sky`'s construction system, each tick, ship only:
1. Every stretch whose `Unlock` has been obtained and is not in `Opened` is added to it, in content order.
2. With nothing running and nothing chosen (`Next` none) and `Opened` not empty, the next is chosen: the first in
   `Opened` not in `Came`, if any, else one drawn uniformly from `Opened` by the world's Rng. `NextIn` is its `First`
   range drawn if it never came, else its `Gap` range drawn. A single opened stretch still costs a draw for `Gap`.
3. `NextIn` counts down. Its last `Warning` ticks are the warning: `Coming` is set. A `First` or `Gap` shorter than the
   warning makes the whole wait a warning.
4. At 0 it starts: `Active` = `Next`, `Left` = its `Duration` drawn now, the id added to `Came`, `Next` cleared.
5. `Left` counts down; at 0 `Active` clears, and step 2 chooses the next, the gap measured from the end.

A choice once made stands: a stretch opened while another is chosen, coming or running waits its turn. So at most one is
chosen, coming or running. Draws are made in the order: which stretch, then its wait; its duration when it starts.

**Loading an older save**, with a railgun obtained long ago: `Opened`, `Came`, `Next` and `Active` load empty, so the
first tick opens what was obtained and chooses the next at its `First`. A veteran world gets its first stretch 5–10 min
after the load, never on its first tick.

**`SkyNow`** gains `Stretch` (the id running, or none), `Coming` (the id in its warning, or none) and `ComingIn` (ticks
left of the warning). It no longer returns `default` without a `Cycle`: night stays off, a stretch still shows. Every
`new SkyNow(...)` site follows, and the README's invariant "`Now` all 0 without a day" is rewritten.

**Cold.** `Now.Chill` = night's + the running stretch's. `Crew` already takes `Chill` off every open cell under the sky
afloat (never aired ones), so `Crew`'s code does not change; a test proves the belt's cold reaches an outside hero. The
cold turns on and off at once, on the tick the stretch starts and ends.

**Debris.** Each stretch's stuff is an ordinary zone with a `Stretch` id. A zone whose stretch is not running is skipped
before its beat is looked up, so its first piece comes a beat after the start; the stream's box (`InStream`) leaves such
zones out. The base stream runs unchanged; the zone's own `Max` and beat thicken the stream. Pieces already out keep
drifting after it ends, as any piece does.

**Contraband and catchers.** The wanted meter is unchanged. `contraband` is worth 1200 (`Market.WorthOf`), one number for
the meter, the credits and the reputation, so 3–7 crates fill a meter and a catcher follows nearly every full meter while
the drift lasts. Only what a hero breaks or loots fills it: leaving the crates alone brings no catchers. Contraband counts
at full worth toward the mafia's reputation, always one of the things it wants, whatever the level. Sold through a trade
station like anything else. The catcher itself is not changed (40 HP): played first, decided later.

**Seen coming**, without words: over the warning the backdrop tints toward the stretch's colour, the radar draws its band
sliding toward the ship when a computer is built, and before the ice belt frost spreads over the ship's outer walls; all
derived from `Sky`'s saved state, so a peer joining mid-warning sees the same. The ice block, the contraband crate, the
contraband item and the frost are new looks: the user picks each on a published page before it is drawn in the game.

**Dev.** `StretchNow(Id)` opens the stretch (adds it to `Opened`), makes it `Next` in place of any chosen and sets
`NextIn` to the warning. Refused `sky.noStretch` (unknown, or the surface has none) and `sky.stretchRunning` (one is
running). `Sky`'s `Fuzz` draws it, so the save, load and desync suites hash a running stretch.

**Saved:** `Opened`, `Came` (ids in content order), `Next`, `NextIn`, `Active`, `Left`, each with a "none" initializer:
no format bump.

| | Ice belt | Contraband drift |
|---|---|---|
| Unlock | `quiltedHelmet` or `heavyHelmet` | `railgun` |
| First / Gap | 5–10 min / 20–40 min | 5–10 min / 25–45 min |
| Duration | 4–6 min | 5–8 min |
| Chill | 20° | 0 |
| Its zone | `iceBlock` pieces, 6–10 `ice` each (ice melts to water) | `contrabandCrate` pieces, `contraband` worth 1200 |
| Tint | blue-grey | amber-red |

All numbers are placeholders for a play pass, and live in `sim/Content` in ticks.

## Steps

1. **Content:** `Stretches.cs`, `Kind.Stretches`, the two zones with a `Stretch` id, `iceBlock` and `contrabandCrate`
   pieces, the `contraband` item and its worth; En and Ru names and refusal keys.
2. **Sky:** the schedule and its state, `SkyNow` reshaped, `StretchNow` and its `Fuzz`. Scenario tests: nothing obtained,
   nothing drawn; the quilted or the heavy helmet alone opens the belt, which comes at `First`, warns, runs, ends; the
   railgun obtained during the belt, the drift comes next and never before the belt ends; an older save's first stretch
   comes at `First`; a spawned helmet opens nothing; each refusal. Invariants: at most one chosen, coming or running;
   `Left` within `Duration`; `Opened` and `Came` only grow, each id once, `Came` within `Opened`; `Chill` = night's +
   the stretch's.
3. **Crew and Debris:** a test that an outside hero is colder during the belt and an aired one is not (no code in
   `Crew`); the stream skipping a zone whose stretch is not running, and `InStream` leaving it out; ice blocks only
   during the belt.
4. **Trade:** contraband counted at full worth toward reputation at every level, with its test.
5. **Picks:** the ice block, the contraband crate and item, the frost on outer walls, on a published page.
6. **Client:** the backdrop tint and the radar band from `SkyNow`, frost on outer walls (no walk per cell per frame), the
   picked pieces drawn.
7. **Try:** filtered tests, local commits, the lit testbed with `StretchNow` from the dev panel. Ship after the user's yes:
   the full suite, the ADR, READMEs (Sky, Debris, Crew, Trade, Knowledge as touched), the concept's Places line.

## Deliberately not handled

- Meteor and flare overrides: no stretch sets them; added with the first that does.
- Two stretches at once; steering round a stretch or jumping away from it.
- Unlocks per hero; a stretch reaching a hero on a planet (each surface has its own `Sky`).
- A stretch with nobody aboard passes unseen: the ship's surface keeps running.
- Losing or selling the helmet does not lock the belt again.
- Contraband caught after its stretch ended is worth as much as any: it is an item like others.
- The cold has no easing: it changes on the tick the stretch starts and ends.
- A crew without a computer sees the tint and the frost only.
- Planet storms (`day-night.md`) could use this mechanic but stay their own design.
