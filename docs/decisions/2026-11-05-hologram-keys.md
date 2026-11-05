# The edge's keys are holograms of what can be used, and the bulb bursts at the screen's top

- Status: accepted
- Date: 2026-11-05
- Supersedes [tablet-key](2026-10-31-tablet-key.md) and [bulb-signal](2026-11-02-bulb-signal.md); the looks of [key-lens](2026-11-02-key-lens.md),
  [edge-lenses](2026-11-03-edge-lenses.md) and [market-key](2026-11-03-market-key.md); the saddle and credit display of market-key; where
  [tablet-edge](2026-11-02-tablet-edge.md)'s keys sit

## Context

The edge's six frosted lenses sat in a saddle whether they could be used or not: a market with nothing to sell for, a suit key on a body with no
suit. They looked like part of the tablet's metal, so the eye passed over them. The bulb over the hero was small and was lost in a busy room,
and nothing tied it to the research key, where the find actually waits. The user's idea (item 6 of the 2026-11-04 UI batch), weighed and accepted:
the keys become holograms, only those that can be used, and a big bulb at the top of the screen bursts with the find flying down into its key.

## Decision

- **Only usable keys show**, in the order air, build, research, market, suit, journal, the row centred over the middle of the bezel's top and closed
  up with no gaps (`EdgeRow`). Air, build and journal always; research from the board's first note; market from the first credit or good
  bought; suit while its mode can be switched or is on. A key that comes or goes flickers in or out over 0.35 s, the rest sliding over 0.25 s.
  A hidden key's hotkey still does what it did.
- **Holograms**, as picked (`crt`): small green phosphor screens with grain, a flicker and a rolling line, drawn additively through
  `shared/holo.gdshader` with a scanline every other row. A key that is on turns its glass bright with the motif dark on it. Amber: research
  while a note is unread, market while an order met is unseen. Motifs are `holo_<key>` masks exported by `tools/anime-art/subjects/edgekeys.js`.
- **Dim, then bright:** at 40 % unless the pointer is within 64 px above the row's top or over a tablet that is up; it brightens in 0.12 s and
  dims in 0.5 s (`KeyGlow`, now in `shared/logic`, its times passed in). Hover and press as before, with the name and hotkey under the pointer.
- **Numbers** on a plate under the key, counting and flaring (`CountFlare`): research the finds waiting less those still flying, hidden at 0;
  market the credits, in gold. The saddle and the bezel's credit display go; the tablet's own charge shows again.
- **The bulb** (`hang`): the bulb picture, 40 by 48, hung cap up on a short cord at the top centre of the screen (`ScreenBulb`), seen by this
  player only. `BulbSignal` keeps its timing and rules (one at a time, a find while it burns makes it burn on, opening the tablet puts it out,
  nothing for what waited when a world was opened). It now counts the finds it carries.
- **The flight** (`beam`): at the burst the find that lit it hatches, its own picture at 40 px. The research key throws a beam up to it in 0.14 s,
  and the find slides down it, shrinking to 18 px and fading into the hologram's green, landing 0.9 s after the burst (`FindFlight`). The key's
  position is read every frame, as the row rides a tablet that is up. On landing the number takes what was carried. A find after the burst
  goes onto the number at once.
- The picks were made on https://claude.ai/artifact/Uxp2obkdchmTwGA5RCmay1, from four hologram treatments, four bulbs and four flights, each
  animated through every state over a frame of the HUD.

## Deliberately not handled

- Sound, rebinding, an order of the player's own, numbers on other keys, light from the holograms or the bulb on the world.
- A key appearing while the pointer is on the row slides the row under it, which can steal a hover in the 0.25 s of a rare change.
- Several finds at once: one picture flies; the number tells how many.
- A suit switch never worn: its key never shows, and V still answers.

## Consequences

- `HeroBulb`, its world light and the `-- --bulb=walk` and `=space` demos are gone; `-- --bulb` and `--bulb=two` play the new one.
- The `key_*` and `key_lens_*` pictures and `keylens.js` are removed.
