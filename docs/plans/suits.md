# Plan: Suits worn, and a person underneath

## Why

The suit is built into the hero (`Crew.Suit`, the hero's own 45 units), so it cannot come off and no other suit can
exist. This plan makes the suit an item worn in its own slot, its numbers data, so later suits (light, heavy, thermal,
the glass "aquarium" helmet) are rows of content; puts a person under it, whose face shows behind the visor; and lets
each player choose that person's appearance, which the lifepod prints the next time it makes the hero. Agreed with
the user on 2026-10-27, after a design and its review; the design's reasoning goes into S-1's ADR. One pool of air
rather than lungs beside a suit: every danger already drains one pool, and the user wants nothing else to change.

**Order and PRs.** S-0 is under way in the session `hero-look-art`; S-1 needs nothing from it; S-2 needs S-0 merged;
S-3 needs S-2. Each task is its own branch and PR. S-1 touches `sim/Crew`, `sim/Items` and many tests through
`Crew.Suit`: whoever takes it tells the other developer, who keeps out of `sim/Crew` until it merges.

**Words.**
- *Air* is the hero's one pool of oxygen (today `Hero.Suit`). Its *capacity* is the worn suit's `Tank`, or `Crew.Lungs`
  when *bare* (nothing in the `suit` slot).
- *Aired* is Atmosphere's `AiredAt(cell)`: the cell holds room air. Space, a vented room and a planet's open air
  (thin or none) are not aired; the pod's air does not make a cell aired.
- A *look* is the four indices a player picks: skin, hair, hair colour, face. Look 0 is `Look(0, 0, 0, 0)`.
- The pod *prints* a hero whenever Crew makes one: an `Enter` from a peer with no hero (its first, or after `Leave`),
  and waking after passing out. Moving between surfaces (`Arrive`) carries the hero as it is: not a print.
- The *figure* is the drawn hero (`game/features/crew/Figure.cs`), a *body* the one left where a hero died (0061); the
  *frost* is the cold drawn over the screen's edges (`Frost.cs`).

## Rules (the user's)

- The suit is an item in a new equipment slot, `suit`; on and off by dragging, as any equipment (`Move`).
- Bare, the hero holds `Crew.Lungs` = 20 units. On an aired cell it breathes as now.
- What a breath costs a hero, resting (running doubles the breath, as now):

  | Where | Suited | Bare |
  | --- | --- | --- |
  | Aired | the room pays | the room pays |
  | Not aired, aboard or in space | 1 | 1 + `Crew.BareCold` (1) |
  | Past the field | 1 + edge cold | 1 + `BareCold` + edge cold |
  | Planet, thin air (`ThinAir` n) | 1 on every n-th breath | 1 + `BareCold` on every n-th breath, nothing between |
  | Lava | adds `Lava.Burn` | adds `Lava.Burn` |

  Bare and not aired, the frost shows at once. Paying the bare cold on thin air's beat only is the user's call, to
  revisit after playing.
- Everything else draws on the one pool as before: the jetpack's thrust (bare, it kills fast: accepted), shocks, rams.
  The canister, the room and the pod fill it, up to capacity.
- The Fabricator gives the basic suit for nothing: a convention, the hero has to go and take it.
- A peer's first hero is printed bare, since its inventory holds no suit; later prints wear what the hero died in. The
  suit is `Gear`, so it stays with the hero, as every tool and wearable does. A harder mode losing everything is not in
  this plan.
- Bare, the figure wears a jumpsuit in its player colour, a bare head (skin, hair, a face of a few pixels) and skin
  hands. Suited, its face shows behind the visor, lit by the helmet lamp.
- The look editor: skin 7, hair 5, hair colour 6, face 3; colours stay the player colour (`PickColour`). It opens any
  time; a new look shows only from the next print.

## Tasks

### S-0 — The art (session `hero-look-art`)

Templates fixed: the bare head inside today's 13×12 helmet frame on the same neck pivot; skin and hair each drawn in one
reference ramp and recoloured by shade swap as `SuitArt.Of` does. Painted by a script in `tools/hero-art/`, approved
by the user on a concept page. **Its PR holds:** `game/art/hero_head.png`, `hero_face_0..2.png`, `hero_hair_0..4.png`,
`hand` and jumpsuit parts in the `hero_*` family (names as the session settles them, listed in its README row); the
skin and hair reference ramps and the seven skin and six hair ramps, as the painter's data. S-2 moves the ramps into
`game/Content/logic`: they are client data, since the sim checks no index.

### S-1 — The suit is an item; the hero breathes from one pool ✅

**Built, but:** `Cold(peer)` leaves the lava out (the frost is not drawn on lava), the breath adds it. Scenarios suit a
peer on its first `Enter` aboard unless `Bare` (`Scenario.Suit`, straight into the slot), so old tests read as before.
The gauge counts a hit only above the dearest breath (bare lungs are small); the frost's floor is `Report.BareFrost`.
The testbed's crates hold no suit. `--suit` sends its craft and moves as commands, not clicks.


**Content.**
- `ItemSpec` gains `Tank` (0 for none). `Inventory.Equipment` gains `"suit"`, appended last, so old slots keep their
  indices; the panel places it by the body wherever it likes (`game/features/items/logic/Equipment.cs`).
- The item `suit`: `Equip: "suit"`, `Gear: true`, `Group: Wearable`, `Tank: 45`. Its craft in `Crafts`: cost `[]`,
  `Ticks: 20`, so 100 ticks (5 s) at the pod's `Speed: 200`. `Crafting` takes an empty cost as it is (checked). Known
  from the start: `suit` joins `Content.Research.StartKnown`, and the lifepod's `Rows` in `Stations`. That breaks
  0057's "the pod's list stops growing": the user's call, recorded in the ADR.
- `Crew.Suit` goes; `Crew.Lungs = 20`, `Crew.BareCold = 1`. Comments naming a full suit (`Field.EdgeCost`, `Lava.Burn`,
  `Jetpack.TicksPerUnit`) say "the basic suit's tank".
- Content tests: `Tank > 0` exactly on `Equip: "suit"` rows, each suit's `Tank` above `Crew.Lungs`; `Field.EdgeCost` at
  least the largest `Tank` (off the grid ends any pool in a breath); `1 + Crew.BareCold <= Crew.PodRefill` (the 1 the
  breath: a resting bare hero by the pod in vacuum holds); the suit's craft costs nothing.

**Items.** One query, `WornIn(peer, kind)`: the item in the peer's first equipment slot of that kind, or null. Crew
cannot ask `Equipped(peer, item)` for every suit when suits are data. `Fuzz` gains a move of a held suit into and out of
the `suit` slot, outside creative, so the generic desync suites swap suits.

**Crew.**
- `Hero.Suit` is renamed `Air`. `CapacityOf(peer)`: the `Tank` of `WornIn(peer, "suit")`, else `Crew.Lungs`; public,
  for the gauge.
- Each tick, before thrust and breath, a conscious hero's `Air` is cut to its capacity: taking the suit off loses what
  the lungs cannot hold; putting one on keeps the air, which then fills as ever. No hook from Items.
- `ColdOf(hero)`, one function: `EdgeCost(cell)`, plus `Lava.Burn` where it burns, plus `BareCold` when bare and not
  `AiredAt(cell)` (crew gains `IAtmosphereQueries`); the bare part is 0 off thin air's beat
  (`Rules.ThinAir is int n && Clock / BreathEvery % n != 0`), whatever the breath's shortfall. The breath pays it as it
  pays `cold` now; `ICrewQueries.Cold(peer)` returns it for the frost: what the next breath would cost in cold.
- The breath's `lack` and every fill read capacity instead of `Crew.Suit`.
- Every print sets `Air` to capacity, after `Kit`: a new peer, its inventory empty, is printed bare with 20; a peer
  coming back after `Leave` with a suit worn in its kept inventory, or waking in the suit it died in, with 45. Creative
  fills to capacity. `Refill` fills to capacity; `crew.suitFull` keeps its key, meaning "full".

**Save.** The next free `World.SaveFormat` at merge (34 now), one step in `sim/Registry/Migrations.cs`, rebased if the
other developer bumps first:
- each hero's `Suit` becomes `Air`, unchanged;
- every peer inventory in `Inventories` (machines have no equipment) gets the new slot with a `suit` in it, heroes
  present or not, as they were all suited;
- a hero of any surface with no saved inventory gets one made, in peer order, holding only the `suit` in its slot.

**Client.**
- `SuitGauge` reads `Air` over `CapacityOf`. `Report.Cold` reads `ICrewQueries.Cold`, with a floor so bare and not
  aired shows thin frost at once (the floor a drawing constant in `Report`, not a rule).
- The equipment panel shows the `suit` slot. The figure still draws the suit while bare until S-2: accepted.
- Demos and dev modes that send a hero out (`SortieDemo`, `FarDemo`, `AsteroidDemo`, `DieDemo`, `Main.cs`) put a suit
  on first; the normal start is bare at the pod.

**Tests.** A `Scenario.Suit(peer)` helper puts the basic suit on through the real commands, and existing tests that
assume a suit use it. New scenarios, one per row of the rules' table and: first print bare, 20; suit off on an aired
cell, cut to 20, breathing on; suit on keeps the air, then fills to 45; death suited → printed suited, full, no suit on
the body; death bare → printed bare; `Leave` suited and `Enter` again → 45; bare by the pod in a vented ship, resting,
holds; a canister bare fills to 20; thrust bare drains the 20; the free craft; an old save loads suited, a left peer's
inventory too (`Migrations` test). Invariant (FsCheck): `Air` within 0 and capacity after every tick, through swaps,
refills, cold and thrust; prove it bites by skipping the cut once.

**Docs.** An ADR `2026-10-27-suits-are-worn` (the slot, the pool, the bare cold and its table, the first print bare, the
free suit and 0057's exception); the Crew and Items READMEs; the concept's spawn line ("with a full suit") and
dangers; glossary "скафандр".

**Frames:** `--suit` (a new demo, `new-demo` skill): printed bare at the pod, the suit taken at the Fabricator and
worn, taken off in the starting room, a walk out bare: frost at once, the gauge at 20 emptying.

### S-2 — The person under the suit

- The figure gains head, face, hair and hand parts from S-0, a jumpsuit in place of the suit's torso, arms and legs
  while bare (the helmet and air pack gone), and the face behind the visor while suited, lit by the helmet lamp where
  one is worn.
- Skin and hair recoloured by shade swap from their ramps, made once a part and colour, as `SuitArt` does; the ramps
  and counts in `game/Content/logic`. Until S-3, every figure wears look 0.
- Bodies drawn the same way, suited or bare as their peer is now (a body keeps no record of its gear).
- **Frames:** bare figures in the four colours; a suited one in a dark room, the face lit by its lamp; a body bare and
  one suited. Compared against the approved S-0 page.

### S-3 — Looks, chosen and printed

**Sim.**
- `Look(Skin, Hair, HairColour, Face)`, a record in `Players`. `PlayersState.Looks`, a list separate from `Colours`:
  `(Peer, Worn, Next)` in peer order; an entry may wait with no hero. Added with an empty initializer: no save bump.
- `IPlayersOps.Choose(peer, look)` sets `Next`, inserting the entry in peer order; `IPlayersOps.Print(peer)` sets
  `Worn = Next`; `IPlayersQueries.LookOf(peer)` returns `Worn`, look 0 with no entry (an old save, a peer gone).
- `ChooseLook(Look)`, a Crew command (Players has none), routed as `PickColour` is, never refused, with or without a
  hero: `Choose`. Applying it twice changes nothing more. No index check: a payload shape (CLAUDE.md); the client draws
  an index by a non-negative modulo of its palette.
- Every print calls `Print` (Crew).
- Fuzz draws `ChooseLook`. Scenarios: chosen before the first `Enter` (or in its tick, commands first) → printed with
  it; chosen while alive → unchanged until passing out and waking; `Leave` and `Enter` again → its last choice; an old
  save → look 0. Invariant: a worn look changes only on a print.

**Client.**
- The look editor, a Control panel with the figure turning in it: four pickers, its text in `logic/LookEditor.En.cs`,
  a line saying the pod prints the new look next time. Opened from the game menu at any time.
- Local settings keep the look. On a first launch with none, the crew feature holds its first `Enter` until the editor
  closes; on every join, it sends `ChooseLook` before its first `Enter`.
- Figures and bodies read `LookOf(peer)`.
- **Frames:** the editor; a hero choosing, dying, and printed in its new look.

**Docs.** The ADR's look section; Players' README (no longer "one entry per peer with a hero" for looks); ADR 0003's
"appearance is sent once on join" superseded by the new ADR.

## Not in this plan

More suits (light, heavy, thermal, aquarium); suit stats beyond `Tank`; moving or working faster bare; a mode that
loses gear on death; a helmet as its own item; suit wear and repair; body shapes; colours beyond the player colour.

## Deliberately not handled

- **A stored suit holds no air**: swapping suits in vacuum loses what the lungs cannot hold. Suits are swapped aboard.
- **Running bare by the pod in vacuum** drains a unit a breath (2 + 1 against the pod's 2); standing still, it holds.
- **Bodies show the peer's current look and gear**, look 0 once it has left; a body from before a reprint shows the new
  face.
- **After a load**, a saved hero keeps its worn look; the local settings' look shows from its next print.
- **Indices out of the palette** from a stale or altered client are saved and hashed as sent, drawn wrapped.
- **Free suits pile up** if crafted over and over: they are worth nothing.
- **The bare cold is paid up to a second late**, as the edge's is (the reported cell, 0050).
- **No softlock bare:** the pod's Fabricator is never taken apart and works unpowered (0046), so a suit is always
  five seconds away.
