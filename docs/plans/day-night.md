# Design: storms, then rain, on a planet's surface

Status: designed, not planned. Night, slice 1, is built: its rules are
[night-chills](../decisions/2026-11-07-night-chills.md) and `sim/Sky/README.md`. Storms are planned once night has been
played, if the playtest says a random storm adds what night does not.

## Slice 2: storms

- `Sky.Storm(EveryMin, EveryMax, Warning, LastMin, LastMax, Chill)` on the kind as `Storms?`, scheduled as flares are
  (`Hazards/Flares.cs`): a countdown drawn from the world's `Rng`, first on the tick the planet opens, so the first storm
  is at least `EveryMin` away; its last `Warning` ticks warn; then it blows for `LastMin`..`LastMax`.
- While it blows, the chill under the open sky is `Storm.Chill` more, on top of night's.
- It does nothing else: pushes no one, breaks nothing.
- Looks: over the warning the horizon dulls and the grass and flora sway, the wind rises in the sound. Then grit
  streaks, haze and dimmer daylight, easing off over a short tail. The wind's direction comes from the storm's start tick.
- First numbers: every 12–25 min, a 30 s warning, 1.5–3 min long, `Chill` 2, so a basic helmet lasts about 75 s
  in a storm by day and 56 s at night: a storm sends a hero to rock, it is not outlasted in the open.
- What it adds: `SkyState.StormIn`, `StormLeft`; `SkyNow.StormComing`, `StormLeft`; two draws (re-pinning tests that pin
  draws after a landing); a dev path to start one.
- Picks: what a storm carries (grit, ash, snow), its haze and warning on the horizon, the sway, the wind's sound.

## Rain, later

A storm that rains, a field on `Sky.Storm` added with its user. Today's `Water` module (cisterns, electrolysers) holds
water in machines; flowing water is the module `liquids-design` is designing. Rain needs **no** interface from it: that
module reads `ISkyQueries.Now` and pours water into open-sky columns itself, so Sky stays dependency-free and early in the
order. Nothing more about water is designed here.

## Deliberately not handled

- **Wind pushing anything:** see the ADR.
- **Fog banks**, **a separate cold snap**, **lightning, storm damage, felled plants:** no player need names them.
