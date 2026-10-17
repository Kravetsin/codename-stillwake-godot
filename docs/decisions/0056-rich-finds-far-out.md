# 0056 — Rich finds far out

- Status: accepted
- Date: 2026-10-17
- Amends: [0052](0052-debris-by-zones-around-every-hero.md)'s tier weights (four times over, the same odds, the
  derelicts added) and [0054](0054-asteroids-and-the-drill.md)'s band of one kind; of
  [0041](0041-wreckage-of-many-kinds-and-circuits.md), the cable rack as the only source of circuits

## Context

The far field (0052) is the stream's wreckage again, a little richer the farther out; the asteroid band (0054)
is one grey rock. Nothing out there says "fly farther": a hero sees the same kinds at 130 rows up as at 30. The
jetpack, the canisters and the batteries are a budget, and a budget wants something to be spent on. Hardness
is only hit points (0054), so bigger finds are simply more hit points, and the weapons already break them.

## Decision

- **Kinds, not a mechanic.** Everything here is content: new wreckage kinds with their hit points
  (`Wreckage`), loot (`Loot`) and weights in the zones' tiers (`Debris.Zones`). No new module, command, zone
  anchor, saved field or resource: the zone model (0052) already draws a kind by tier.
- **The asteroid's kinds.** Bigger is harder and gives more rock, a little more for each hit point; the rich one,
  ore-dense, the most for its size. Its pictures show gold veins and a glint or two.

  | Kind | Picture | HP | Rock | Drill | Axe |
  |---|---|---|---|---|---|
  | asteroidSmall | 24 × 24 | 50 | 1–2 | 5 s | 12.5 s |
  | asteroid | 32 × 32 | 120 | 3–5 | 12 s | 30 s |
  | asteroidRich | 40 × 40 | 160 | 8–12 | 16 s | 40 s |
  | asteroidLarge | 48 × 48 | 200 | 6–9 | 20 s | 50 s |

- **The band gets a far edge.** A second tier in the band, from 72: every band piece spawns `Lead` (64) columns
  right of the hull, so `d = max(64, 2·rows)` and the tier from 72 is rows 36 to 44 out, the band's outer third.
  Near: small 3, asteroid 4, large 1. Far edge: small 1, asteroid 3, large 3, rich 2. The beat and cap stay
  (one every 4 minutes, 2 out); some 4.6 rock an asteroid on average where it was 4.
- **Derelicts:** three big wrecks, drawn from what they drop, found only in the far field's tiers past 128:

  | Kind | Picture | HP | Loot (one-in) | Drill | Axe |
  |---|---|---|---|---|---|
  | engineBlock | 80 × 48 | 180 | gear 10–16, hullPlate 5–8, scrapChunk 5–8, wire 4–8, rotor 1–2, circuit 1 (1 in 3) | 18 s | 45 s |
  | cargoHold | 96 × 48 | 140 | planks 36–54, hullPlate 3–5, scrapChunk 4–6, gear 3–5, wire 6–10 (1 in 2), canister 1–2 (1 in 3) | 14 s | 35 s |
  | podCluster | 64 × 64 | 110 | canister 5–8, scrapChunk 5–8, hullPlate 3–5, wire 6–10, circuit 1–2 (1 in 2) | 11 s | 28 s |

  Each is some three ordinary wrecks' worth of items or more (counted on average) in one place, so a sortie's
  air goes on breaking rather than searching; the pod cluster's canisters are the air home.
- **Where.** The far field's weights are 0052's four times over, so the rare fit in whole numbers:

  | Kind | d<128 | 128–255 | ≥256 |
  |---|---|---|---|
  | scrapChunk | 48 | 24 | 8 |
  | hullPlate | 32 | 24 | 12 |
  | cargoCrate | 24 | 20 | 12 |
  | supplyPod | 16 | 20 | 20 |
  | hullSection | 16 | 16 | 12 |
  | cargoContainer | 12 | 16 | 16 |
  | machineBlock | 8 | 12 | 16 |
  | cableRack | 8 | 12 | 20 |
  | tankCluster | 8 | 12 | 16 |
  | shieldSlab | 4 | 8 | 12 |
  | engineBlock | | 1 | 2 |
  | cargoHold | | 1 | 1 |
  | podCluster | | | 2 |
  | asteroidLarge | | | 2 |
  | asteroidRich | | | 2 |

  Derelicts are 1 spawn in 83 past 128 and 1 in 31 past 256; the pod cluster and the far field's rock only past
  256. **Why the far field and not the band:** the far field spawns round the hero, so it is what flying out meets;
  its tiers are distance from the hull, the sortie's own measure; and its rate is measured (0052). The band holds
  2 pieces on a 4-minute beat 22 to 44 rows out, a suit's reach without a canister: a derelict there would be an
  hour apart or would crowd out its rock, and would be no sortie.
- **Pacing, measured** (`DebrisDensityTests.ShowsASortie…`): a hero flies up by jetpack, looks about slowly for
  15 s, stays 15 s, and flies back, some 60 s, the suit and one or two canisters. Thirty sorties each: 100 rows
  up (the 128 tier) see 1.0 derelicts in the farthest view, none in a third of them; 130 rows up (into the 256)
  2.1, none in 1 of 30, 0.9 in the default view. 160 rows up, 2.9. Rows count double, so up is the cheap way out:
  128 rows up and back is a suit and a canister, 256 cells across and back a suit and three.
- **Circuits stay rare.** A rack still gives one, always, the sure source. The engine block a third of one, the pod
  cluster three quarters, on average: a tenth of the rack's circuits for each hit point or fewer (`ContentTests`).
  Past 256, racks are 1 spawn in 8 and derelicts 1 in 31, so a sortie's circuits are still the racks'.
- **Seen.** The pictures are a pixel of art a pixel of the world, cracking as every wreck's (`render/Cracks.cs`, a
  crack runs at most 40 pixels, so on the biggest the cracks reach some way, not across); a derelict's break bursts
  with up to half as much dust and sparks again. Far behind the ship, the backdrop's wrecks take them as cores too,
  its reach sized from the largest picture.
- **Dev:** `-- --sortie` puts the hero 140 rows above the hull with a jetpack, the drill, the axe, ten canisters and
  two full batteries, one of each derelict and new asteroid round it, still; a script drills the engine block till
  it breaks. `--manual` leaves the hands to the player.

## Rejected

- **A zone or an anchor of their own** ("rich", round heroes past some line): the far field already draws by
  distance; a tier is a weight, not a mechanic.
- **New resources** (ore, a derelict-only part): the ask is more of the same; new resources need their own uses first.
- **Derelicts in the band's far edge:** see "Why the far field" above.
- **A deeper tier at 384:** 192 rows up and back is some three canisters; the sortie this serves is one or two.
  The 256 tier is where it peaks; more depth can come with bigger air.
- **Fractional weights or a draw in two steps** (wreck class, then kind): the same odds by scaling the tier by four,
  one draw a spawn as before, so the Rng is drawn exactly as often.

## Consequences

- No save format change: a save holding a new kind does not load in an older build, as with every item since the first.
- The weapons' cards list every wreckage kind they break: thirteen now.
- A derelict half over the hull, or reeled in by the harpoon, is judged by its middle cell as every wreck is (0034).
- Exact draws in the far field's tiers are unchanged in number but not in kind: tests pinning kinds far out change once.
