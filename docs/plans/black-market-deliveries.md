# Black market: deliveries, gear as items, the confiscated container (plan)

Status: plan for one PR, the design's build steps 2–4 ([black-market.md](black-market.md)), reviewed and revised;
contraband and consumables are left for later (the user's call: the base first, layers after). Numbers follow the
design's table, a first pass. Looks picked by the user: parcel **A** (a tarp bundle tied with cord, a lead seal),
crate **D** (a steel box in cargo straps, a beacon blinking on top), container **A** (a sealed container).

## Terms

- **Good**: a market node a purchase is for: an upgrade step, a blueprint, a piece of gear, the container.
- **Delivery**: a purchase paid for and not yet caught, broken or lost: `{ Id, Good }` in `Knowledge`.
- **Delivery piece**: the debris piece carrying it: a `parcel` (upgrades), a `deliveryCrate` (blueprints, gear) or
  the `confiscated` container. The piece's new saved field `Delivery` names its delivery; 0 on every other piece.
- **The stream**: the ship's debris zone `ship`, pieces drifting right to left past the hull; a piece starts `Lead`
  (128) cells right of the hull's right edge and is gone past its `End`, `Lead` cells left of the hull's left edge,
  once no hero sees it.
- **The lane**: the middle row of the trade station with the lowest machine id (its top row + its height / 2).
- **Gear**: `scanner`, `powerDriver`, `heavyHelmet`, `plasmaCutter`, `regenSuit`: items, never blueprints from now.
- **Done** (Research): a node the market shows as had. **Owned** (client): a tile all of whose steps are done.
- **Hand worth**: a worth written in `Market`'s raw table, not computed from a recipe.

## Steps

1. **Content.**
   - `Debris`: kinds `parcel` (grabbed whole), `deliveryCrate` (`Wreckage` 15 HP), `confiscated` (`Wreckage` 30 HP).
     Their pieces carry zone id `delivery`, which is no entry of `Debris.Zones`: not in `NetCatches` (no net or
     salvage drone takes one), no guard comes with one. `DeliveryGap` 8 cells; `DeliverySpeed` = `DriftMin`.
   - `Research`: the `confiscated` node on the third level's ring (gate `order3`, as the design's table), its price
     1 500 (a hand worth, `Times` 1).
   - `Crafts`: the five gear crafts go. `Market`'s raw table gets each gear item's hand worth, the number today's
     recipe gives; a gear node's `Times` is 2, so gear costs twice its worth (the design's rule).
   - `Loot` of `confiscated`, parts of about its price: plate 4–8, wire 6–10, circuit 1–2, coil 1–2 (1 in 2), glass
     4–6 (1 in 2), arcShard 1 (1 in 12), scanner 1 (1 in 24), powerDriver 1 (1 in 24), plasmaCutter 1 (1 in 30).
2. **Knowledge** keeps, saved: `Deliveries` (`[]` by default) and `NextDelivery` (1 by default; ids only grow, never
   reused). Both added with a "none yet" initializer: no format bump of their own. Ops `Deliver(good)` (queues) and
   `Delivered(id)` (removes; an unknown id does nothing); queries `Deliveries` and `InFlight(good)`. Records only.
3. **Research.** `BuyBlock` spends and calls `Deliver(block.Id)`; it grants nothing. `Done(node)`: an upgrade or
   blueprint granted or in flight; gear and the container never, so their tiles are never `Owned`. Refusals as
   today. `Grant` of an id already granted does nothing (Knowledge's sorted insert), so a blueprint granted twice is
   harmless.
4. **Debris spawns**, the ship's surface only (`Rules.Id == Surfaces.Ship.Id`), in its system after the drift and the
   catch and before the zones' beats, so piece ids stay ascending:
   - the first delivery in queue order with no piece out (a scan of `Pieces` for its id: a handful at most) spawns,
     if a trade station stands and no delivery piece's `X` lies within `DeliveryGap` (in thousandths of a cell) of the
     spawn point, reeled ones included; one a tick at most, so a row of purchases spaces itself;
   - at the stream's start on the lane, `DeliverySpeed`, `End` as the stream's; no Rng draw; not pushed out of a
     hero's view (it is the crew's own purchase);
   - kind by good: an upgrade → `parcel`, `confiscated` → `confiscated`, anything else → `deliveryCrate`;
   - with no trade station, deliveries wait queued; a purchase made on a planet spawns on the ship's next system run.
   `ZoneOf`, `CountOf` and `Kept` pass zone `delivery` by: it has no beat, no `Max`, no despawn but its `End`.
5. **Grab a parcel**: refusals as any grab; applied, `Grant(good)` crew-wide and `Delivered(id)`. No items, no
   `Found`.
6. **Break a crate**: at its hit points, `Delivered(id)`, then a blueprint is granted, or the gear goes to the hero
   whose blow broke it (`GiveOrDrop`: what does not fit floats at that hero). Broken by a creature or meteor, with no
   striker: the gear drops under the crate's middle (`DropAt(Nobody)`). Struck as any wreck.
7. **Break the container**: at its hit points, `Delivered(id)`; its `Loot` is drawn and falls as a wreck's does, and
   `Found` records it.
8. **Lost**: a delivery piece gone past its `End` calls `Delivered(id)`; its good is buyable again. A piece removed
   any other way it is not: it has none (zone `delivery` has no despawn). The market shows a toast "The mafia's
   delivery drifted past" to every player, once, read from `Knowledge` (a lost count, unsaved: a toast missed by a
   load is accepted).
9. **Reel**: the harpoon reels a delivery piece as any piece; arrived, it drifts on. Nothing else changes for it.
10. **Saves**: `World.SaveFormat` 58 → 59, its step: the five gear ids leave the ship's `knowledge.granted`; every
    craft queue entry whose `Recipe` is gear is removed, its queue's `Progress` set to 0 when it was the first. Paid
    materials of a removed entry are not returned (accepted: pre-release, as the blueprints are not refunded).
11. **Client: debris.** The three kinds drawn with the picked looks (`DebrisLooks`, exported through
    `tools/anime-art`); over a delivery piece its good's icon (item icon or upgrade sign), above it, not turning; the
    crate's beacon blinks.
12. **Client: the marker.** `MarkerIcon.Delivery`: each delivery piece off the screen is pointed at from the edge,
    its good's icon in the badge, in the market's amber, for every player.
13. **Client: the market.** A tile in flight reads as bought; gear and the container stay buyable. The strip says
    "Arrives along the trade station's row", or with no station "Build a trade station to receive it". Gear's card
    says "Gear · per hero" and shows no recipe.
14. **Client: crafting.** The workbench lists no gear (it reads `Crafts`); Handbook pages naming a gear recipe say
    it is sold by the mafia.
15. **Dev room** `--market`: the trade station, credits for one of each kind.

## Deliberately not handled

- Two purchases on one tick: host order; both queued, the second spawning once the first is `DeliveryGap` along.
- An upgrade step bought while the one before is in flight: allowed; if the first is lost it is bought again,
  the second granted meanwhile (`Upgrades.Added` counts the granted steps of a kind, not their order).
- Gear dropped where no hero is (a creature's or meteor's break) floats where it fell, as any dropped stack.
- A queued gear craft in an old save is lost with its materials.
- A delivery spawning in a hero's view: wanted; the stream's rule is for drifting salvage.

## Tests

- Scenarios, one per rule: a purchase on the ship queues a delivery and spawns its piece on the lane the same tick;
  held with no station; spawned on the ship for a purchase made on a planet; never on a planet; spaced by
  `DeliveryGap`; a parcel grants its upgrade; a crate grants its blueprint, or gives its gear to the striker, or drops
  it with no striker; the container breaks into its loot; a lost delivery makes its good buyable again; an upgrade
  in flight is `research.bought`; gear and the container buy twice; two buys on one tick; a delivery piece is no
  stream piece for `Max`; the migration strips gear blueprints and gear queue entries (queued and at work).
- Invariant (FsCheck): over any buys, grabs, hits and ticks, every delivery is one queued entry until its piece is
  caught, broken or gone; no piece names a delivery not queued; piece ids ascend; credits never negative. Proven to
  bite once.
- Generic suites: the fuzz map gets a trade station and credits, and a check that a delivery piece appears in it.
- `DebrisFoundTests`: the container is found; parcel and crate are not, and are not spawnable by any zone.
- Client: `MarketGoodsTests` for in-flight tiles and gear; `Markers` for the delivery badge.

## Docs

ADR `2026-11-08-mafia-deliveries` (one path for every purchase; gear as items; the picks); READMEs of
`sim/Knowledge`, `sim/Research`, `sim/Debris`, `sim/Crafting`, `game/features/research`, `game/features/debris`,
`game/features/crew`; concept's market line; `black-market.md`'s build order (2–4 done, contraband deferred).
