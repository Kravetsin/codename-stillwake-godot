# Storm lightning

- Status: accepted
- Date: 2026-11-16

## Context

A storm on planet 1 ([surface-storms](2026-11-16-surface-storms.md)) chilled and slowed, but held nothing to watch for. Slice 2 of
`docs/plans/surface-storms.md` gives each storm one bolt: a hazard seen coming, and a spectacle. Played by the user and tuned on
what they saw.

## Decision

- **One bolt per storm**, `Stretch.Bolt` (`BoltRule`, `sim/Content/Stretches.cs`): drawn 200–1000 ticks into the storm, near one
  hero drawn awake on the grid, on the highest wall cell under nothing but open sky within 24 columns of them (ties to the nearest
  column, then the left). It glows 60 ticks (`Strike`, saved), then strikes: Building's `Burst` of 60 within 2 cells, nobody's,
  so in full (one damage rule), and a shock of 100 to every hero under the open sky within `Spread` columns (20 in rain, 10 in
  dust), in id order. A back wall or a cave shelters; the zone is columns, not current through the ground.
- **The shock is 100** (the user's): air pays first, then the last breath, so a basic or quilted helmet's hero is knocked out in
  the zone; a heavy helmet or a stim survives it. It is `Harm.Lightning`, its own hurt, so the client tells it apart.
- **Draws come from Hazards' own fork** of the world's Rng (salted by the surface and a saved count), never the world's stream. No
  format bump: every field added has a "none" initializer. A hero reports every tick while a bolt glows.
- **`BoltNow`** (dev panel, fuzz) glows a bolt near the sender's hero now, storm or not: refused `hazards.noBolt`,
  `hazards.boltGlowing`, `hazards.noHero`.
- **Seen** (the user's picks over three rounds, 2026-11-16): the glow is St Elmo's fires over the zone's ground and on the
  helmets of the heroes in it, more lighting as it grows, no arcs; the bolt «Ступенчатый лидер», a faint leader stepping down,
  the return stroke climbing, two more strokes, forks, starting above the farthest view so it is never short; «Свет от канала»,
  a hard light at the channel and a blue shade over the rest; sparks and dark grains, arcs running over the zone, soot streaked
  downwind (kept while the surface is shown, not saved). A hero in the zone sees «Ожог на сетчатке»: the channel burnt into
  the screen, held about 5 s and gone by 6, and a white edge flash with arcs. No shake, no freeze, no rings; no blue hurt wash
  for `Harm.Lightning`.
- **Heard:** thunder, a short close crack (Freesound 840628, loganzsound, CC0), a one-shot.
- **The storm's wash** blurs the screen alike at every zoom: one softness scaled by the screen's height, not by the camera.

## Consequences

- A blizzard or ash storm gains a bolt by a `BoltRule` in its stretch, no code.
- In co-op a friend far from the drawn hero gets no bolt that storm; one who leaves the zone in the glow's last moment may still
  be shocked (a round trip late). Accepted.
- Lightning leaves no loot. A lightning rod and a suit that drinks the strike are ideas, not designed.
