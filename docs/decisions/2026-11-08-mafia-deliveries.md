# The mafia delivers what it sells

- Status: accepted
- Date: 2026-11-08
- Amends [mafia-reputation](2026-11-08-mafia-reputation.md): a purchase is no longer granted at once
- Amends [the-regen-suit](2026-11-03-the-regen-suit.md), [the-heavy-helmet](2026-11-03-the-heavy-helmet.md), [salvage-tools](2026-11-04-salvage-tools.md):
  the regen suit, the heavy helmet, the plasma cutter, the scanner and the power driver are sold as items, never crafted

## Context

A purchase appeared in the crew's knowledge the moment it was paid for: nothing to see, nothing to do, and gear, once its
blueprint was bought, was crafted as often as wanted, so the market sold each good once. The design
([black-market](../plans/black-market.md), steps 2–4) makes a purchase something that arrives, and gear something bought
again. Contraband and consumables were left for later by the user: the base first.

## Decision

- **One path for every purchase.** `BuyBlock` spends and queues a delivery in `Knowledge` (`Delivery(Id, Good)`, ids never
  reused); it grants nothing. On the ship, before the zones' beats, the first queued delivery with no piece out spawns at the
  stream's start on the middle row of the trade station with the lowest id, once no delivery piece lies within `DeliveryGap`
  (8 cells) of that start, so a row of purchases spaces itself. No Rng draw, no push out of a hero's view: the crew's own
  purchase. Its zone `delivery` is none of `Debris.Zones`: no beat, cap, net or despawn but the stream's end.
- **What it comes as.** An upgrade is a `parcel`, grabbed whole; a blueprint or gear a `deliveryCrate` (15 HP), broken;
  the confiscated container itself (30 HP), broken into its loot. Caught or broken, a blueprint or upgrade is granted crew-wide and
  shown to whoever caught or broke it as any loot is, its picture and "+1" where the piece was, flying into the hero
  (`NoteDelivered` into `Items.Gains`); gear falls under the crate as a wreck's loot does, anyone's. Only the container is
  recorded as found: the others are purchases, not salvage.
- **Lost:** a delivery piece past its `End` leaves the queue with its credits, and its good may be bought again. A tile reads
  as bought while its good is on its way, so an upgrade or blueprint is not paid for twice (`research.bought`).
- **No station, no delivery:** deliveries wait queued, and the market says so in its own colour. A purchase made on a
  planet comes on the ship's next tick.
- **Gear as items:** `scanner`, `powerDriver`, `heavyHelmet`, `plasmaCutter`, `regenSuit` (`Content.Research.GearSold`) have
  no craft; their worth is a hand worth in `Content.Market`, the one their recipe gave, and they cost what their blueprints did
  (`Times` 3 to 8: the scanner 1 500, the power driver 3 000, the plasma cutter 4 200, the heavy helmet 5 000, the regen
  suit 12 000), bought again and again. Twice their worth was tried and turned down by the user: the old prices stand.
- **The confiscated container** on the third level's ring, 1 500 credits, bought again and again: plate, wire, circuits and
  coils of about its price, glass one time in two, now and then an arc shard or a piece of gear.
- **Looks, the user's picks:** parcel A, a tarp bundle tied with cord under a lead seal; crate D, a steel box in cargo
  straps with a blinking amber beacon; container A, a sealed container. Each wears an amber beacon blinking as a body's does, lighting
  round it, so it is found on the screen as the edge marker gives way: the good's picture over it was tried first and
  turned down by the user as ugly.
- **Markers:** each delivery piece off the screen is pointed at from the edge in the market's amber, its good's picture in
  the badge, for every player. One lost turns red where it pointed and fades over 2.5 s: the game has no toast, and an
  edge marker already says where without words. A count of losses since the load drives it, never saved.
- **Saves** of format 59 lose the five gear ids from what is known and every queued gear craft, its station's progress reset
  when it was at work; paid materials are not returned, the game being pre-release.

## Deliberately not handled

- Two purchases on one tick: host order; the second spawns once the first is `DeliveryGap` along.
- An upgrade step bought while the one before is in flight: allowed; one lost is bought again.
- A delivery spawns in a hero's view: wanted, the stream's rule being for drifting salvage.
- A lost delivery missed by a load shows no red marker.
- The pictures have no damage states of their own: the game's cracks and wedges apply as to any wreck.
- On a planet the market assumes a station: deliveries go to the ship's.
