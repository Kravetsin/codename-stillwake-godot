# 0052 — Debris by zones, around every hero

- Status: accepted; the asteroid band as a zone to come, and its weapon, settled by [0054](0054-asteroids-and-the-drill.md); its tier weights scaled four times and the derelicts added by [0056](0056-rich-finds-far-out.md); amended 2026-10-18: no zone spawns or removes a piece in a hero's view (below); its cable rack weights cut to 2 a tier, below the stream's share, by 0056's amendment
- Date: 2026-10-17
- Supersedes: in [0009](0009-debris-around-the-ship.md), the stream as the only place debris spawns,
  and its rejection of spawning where players are, for the cells heroes report

## Context

The world is 2048×1024 cells (0048) and a hero with a jetpack can fly far out into it, its client
reporting its cell about once a second (0050). Debris spawned only in the stream past the hull, so
the dark beyond the ship was empty: nothing to fly out for, and no reason to pay the suit's oxygen
there. The mechanic will grow: the asteroid band round the ship, mined with the drill, is the named
next user (principle 2, 0049), so where debris spawns has to be data, not a second hand-written
stream.

## Decision

- **Zones are content.** `Content.Debris.Zones`, in order, each a `DebrisZone`: an id, an anchor,
  its own beat (`Every`, `Jitter`), cap (`Max`), drift range and tiers of kinds by weight. The anchor
  is a record of its kind: `ByHull(Margin, Lead)` for the stream, `AroundHeroes(...)` for the far
  field. The asteroid band will be a third entry, by the hull with a gap: a content entry and at most
  an anchor kind.
- **Two zones now.** `ship`, the stream as it always was, one tier, `Debris.Kinds`. `far`, round every
  hero: a beat every tick, at most `PerHero` (110) in a hero's keep box (68 × 48 cells either way) and
  640 in all. The numbers are tuned by measurement (below), not by eye.
- **Each piece names its zone** (`Piece.Zone`) and keeps the clock it was last in some hero's keep
  box (`Piece.SeenAt`). Each zone keeps its own beat (`DebrisState.Beats`); a zone with none, one added
  to content later, is given one on its first tick, so a new zone needs no migration.
- **Far spawning.** On the far beat, per hero in id order: none while the zone is at its cap, for a
  hero in a stream's box (the hull grown by `Lead` across and `Margin` up and down), nor for one whose
  keep box holds its share; and none without a hull. Else a point on a ring round its reported cell,
  from half-box (61, 36) out to (64, 42): just past what the farthest zoom shows (60, 34), so what
  spawns on the right drifts into sight soon. Five draws, always taken: the side, the offset
  along it, the depth across, the kind, the speed. A point off the grid, in a stream's box or in any
  hero's view spawns nothing. The piece starts there, `Start` its `X`, `End` 0: it is
  gone at the left edge as every piece is.
- **Tiers by distance from the hull's box,** `d = max(dx, 2·dy)` in cells past it, rows counting double
  as the world is half as tall: from 0, 128 and 256 cells, small salvage near the ship, more and
  bigger wrecks out far, cable racks the most of all at 256 and beyond.

  | Kind | ship | far d<128 | 128–255 | ≥256 |
  |---|---|---|---|---|
  | scrapChunk | 18 | 12 | 6 | 2 |
  | hullPlate | 9 | 8 | 6 | 3 |
  | cargoCrate | 6 | 6 | 5 | 3 |
  | supplyPod | 3 | 4 | 5 | 5 |
  | hullSection | 3 | 4 | 4 | 3 |
  | cargoContainer | 2 | 3 | 4 | 4 |
  | machineBlock | 1 | 2 | 3 | 4 |
  | cableRack | 1 | 2 | 3 | 5 |
  | tankCluster | 1 | 2 | 3 | 4 |
  | shieldSlab | 1 | 1 | 2 | 3 |

- **Despawn, far pieces only.** In some hero's keep box a piece is seen; out of every keep box it is
  gone `Linger` (600 ticks, 30 s) after it was last seen, and at once out of every hero's hard box
  (100 × 64). The stream's pieces are gone only past their end, never by distance.
- **Drift** is left for every zone, as everything outside the moving ship; the far field at the stream's
  30 to 55 thousandths a tick.
- **How full, measured.** Space must not feel empty: `DebrisDensityTests` hovers a hero and flies one
  straight at the jetpack's 15 cells a second and slowly at 3, counting far pieces in the default
  zoom's view (40 × 22 cells) and the farthest's (120 × 68). The numbers above give, hovering, some
  4.7 in the default view and 40 in the farthest, a new piece in the default view every 11 s; flying by
  jetpack some 4 and 31, the default view never empty more than 1.4 s, a new piece every 0.7 s; flying
  slowly some 4.4 and 39. Pieces spread evenly put about a ninth of the farthest view's count in the
  default one, so 4 in the default view means some 36 in the farthest: the farthest runs a little over
  the 25–35 aimed at, to keep the default view full. A beat every tick is what flying needs: only the
  ring's side ahead, 85 rows tall, meets a 23-row view; a hovering hero is held by its share instead.
- **The hull's box is derived,** found again only when the grid's `Version` moves, not walked on
  every beat. `Grab`, `Hit` and `Reel` find their piece by a binary search in id order; no spatial
  index.
- **Determinism.** The only input is the reported cell, through `Enter`. Zones run in content order,
  the stream first; the far beat has no jitter, so it draws nothing unless it spawns, and a hero in a
  stream's box takes no draws: a world whose heroes are all by the ship draws exactly as before zones.
  Box tests are integers; heroes run in id order; each spawn's draws are fixed. A joiner gets the pieces
  in its welcome; the hull's box is found again after a load.
- **Format 11.** Its step makes every piece of an older save the stream's, never seen (`SeenAt` 0),
  and its `NextSpawn` the stream's beat; the far field's beat is given on the first tick.
- **The client draws only what the camera sees** (`features/debris`): a picture for each piece in the
  camera's rect grown by 4 cells, from a pool that never frees, drawn between ticks only then; the
  cracking material for wrecks alone, small pieces `Plain`, so their pictures take no instance
  uniforms.

## Rejected

- **A hand-written second stream for the far field:** the asteroid band would be a third; one model of
  zones serves all three.
- **Spawning by each peer's screen:** each peer has its own; the sim must not depend on one (0009). A
  hero's reported cell is sim state, the same on every peer.
- **A spatial index:** 600 far pieces, tested against four heroes, cost some 0.12 ms a tick; a
  grid of buckets would cost more to keep than it saves.
- **A far beat with jitter:** a draw on every beat, so even a world with no hero out would draw
  differently from before zones.
- **Far spawns for a hero by the ship:** its ring's top and bottom fall outside the stream's box, out
  of sight above and below, where no one sees them; the stream serves it there.

## Consequences

- Up to 20 stream pieces and 640 far; the tick is tested at 600 with four heroes. A save holds some
  145 bytes a piece.
- A ring drawn round a cell a second stale may spawn a piece near the screen's edge for a hero flying
  fast, inside the farthest view (15 cells a second outruns the ring's 1 to 4 cells past it) but never
  the default one; it shows at once, at full strength (no fade). A hero stopping where no far field was waits some 45 s for the
  first piece in the default view: nothing spawns in sight, and the drift is under a cell a second. A hero
  that hovered and then climbs or dives meets its ring's top or bottom band, a share's quarter in a few
  rows: up to some 20 pieces in the default view at once, a passing cloud; bands thick enough to spread it
  (tried: 61–84 by 36–56) thin the field everywhere else and slow it for a hero flying. Heroes together share one share; at the cap lower ids spawn first; a point
  off the grid or in a stream's box wastes that hero's beat; the tiers move with the hull.
- Far pieces drift through the stream without counting toward its cap. A half-chopped wreck left out
  of every keep box for 30 s is gone with its damage; one last hero leaving takes the far field with it.
- Scenario tests whose heroes stand out past the stream take the far field's draws, so expectations
  pinning exact draws there change once; those by the ship do not.

## Amendment, 2026-10-18: nothing appears or vanishes in sight

The first playtest flew right of the ship and saw the line the stream's pieces float out of; at the
farthest zoom, pieces appeared on it. The stream started on its lead, 64 columns right of the hull, and
the farthest view reaches 60: a hero five columns past the bow saw it, and inside the stream's box no far
field covered for it. The asteroid band started on the same lead, and the stream's pieces vanished on its
end, 64 columns left of the hull, for a hero past the stern.

- **One view for every zone:** `Content.Debris.View`, 60 × 34 cells either way round a hero's reported
  cell, what 1920×1080 shows at zoom 1; the far field's `View` moved there.
- **A piece by the hull starts out of sight:** on its lead, or, where a hero's view holds the lead's cell
  in its row, one column past that view's right edge, pushed on past any other view it lands in, kept on
  the grid. Only a hero whose view holds the start moves it, so a hero far ahead does not stretch the
  stream. The kind is drawn for the lead, so the band's tiers do not follow the heroes, and the draws are
  the same as before: a world draws exactly as it did, only its starts move.
- **A piece past its end stays while some hero sees it,** and is gone the tick none does; past the grid's
  left edge, at once. The far field was already gone only out of every keep box, which holds the view.
- **Limiting the zoom instead** (the playtest's fallback, a step less out) was not needed: no fixed lead
  is past every view, as a hero can fly to any lead, so spawning had to follow the heroes.

Consequences: the far field's density is unchanged (`DebrisDensityTests`). A stream pushed right has
farther to drift, so while a hero hovers by its lead the cap of 20 thins it, by up to some two fifths
for the wreck's hull; it drifts through the far field past the stream's box there. A stale reported cell
or a window bigger than 1920×1080 may still see a piece appear at the screen's edge. A grid too narrow to
push past a view starts the piece on its last column: test and dev maps only.
