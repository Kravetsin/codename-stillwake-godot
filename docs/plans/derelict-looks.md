# Plan: derelict looks

The user saw [the mockup](derelict-looks-mockup.html) (published: https://claude.ai/artifact/BDRspiJRSZ3MBMfp4A7LsE) and
said to build all of it (2026-11-16). The design, [derelict-looks-design.md](derelict-looks-design.md), is background
only: this plan replaces it wherever they differ (listed under Changes from the design).

## Rules (confirmed with the user, 2026-11-15/16)

- Only intact abandoned vessels (`VesselCause.None`); wrecked vessels are out of scope. Outside whole, damage inside only.
- Space stays empty, as in the main world.
- Silhouette: modules bulging 0–2 rows up and down, a rib between modules, a housing behind a freighter's bridge, a bow
  with a straight slope instead of a staircase. The grid stays cells.
- The engine bells go. Every deck below the first holds a main engine against the stern, each with a nozzle out
  through the stern hull.
- Outside parts, all dismantlable: nozzles, RCS blocks at four corners, a mast with a red beacon, a comms dish; a shuttle
  also a solar panel and a tank under its belly; a freighter also 1–3 radiators, a docking collar, a keel truss with
  containers (some slots empty), a sensor boom at the bow tip with a white strobe.
- A nozzle gives plates and copper, never the `nozzle` item (the Former keeps that gate).
- Some inner doors stand open and some of those are jammed half-way; the rest shut.
- The mockup is the art pick for every outside part: drawn in ink and watercolour after its forms, shown in game, fixed
  where the user asks. No candidates page per part.
- Names and codes in Latin for every player (`PELICAN · KT-0412`): content, not i18n keys.
- Skin: one ink outline round the vessel, panels with seams and rivets, a livery stripe, name and code painted, rust,
  pits. Star light from the upper left in the art.
- Dark inside: red emergency lamps, one dying lamp, screens, a red dot over each shut door; beacons blink outside.
- Signs of abandonment inside from: frost, a sparking cable, soot over a burnt console, a fallen ceiling panel, a toppled
  crate, gas from a burst pipe. They are pictures and particles, not things.

## Changes from the design

Doors open and jammed (design: shut). Names in Latin, content (design: i18n). The mockup is the pick (design: open). Skin
comes third (design: second). No amber RCS beacon or engine indicator. One neutral atlas tinted per livery (design: an
atlas per livery). `VesselDress` beside `WreckDress`, no `IHullDress`. Name and code cut in cell pieces (design: per
letter). Bow: one diagonal tile per slope (design: half-cell tiles). Rust and pits baked into panel variants. Light in
`game/features/light`, dress in `game/features/grid`: no new feature.

## Shape and parts (sim)

All in `Shipyard.Describe`, so `Vessel` carries the grown frame (`Width`, `Height` include the parts) and `Space`/`Door`
stay right. `Stamp` lays what `Describe` placed. Rooms are drawn from stream 0 as now; everything below comes from stream 2,
in this order: modules (width, bulge up, bulge down, left to right), the bow slope, inner doors in `Doors` order (the
airlock is not drawn: always shut), then outside parts in the table's order. So a seed's rooms stay as they were.

- **Modules:** past the aft rooms, every 22–38 columns until 14 short of the bow. Each bulges 0–2 rows up and 0–2 down
  (the stern module 1–2 each way); the module under the bridge or the housing bulges 0 up.
- **Ribs:** 2 columns at each module boundary, standing 1 row above the higher neighbour's top and 1 below the lower
  neighbour's bottom.
- **Housing:** behind a raised bridge, 6 columns, from 4 rows below the frame's top down to the body; dropped when it would
  reach the aft rooms.
- **Bow:** the body's height kept for `BowFlat` columns (class content: shuttle 3, freighter 8), then the top falls 1 row
  per `BowRun` columns (1 or 2, class content) and the bottom rises likewise, to a tip `BowTip` rows tall.
- **Doors:** each inner door is open with chance 1 in 5, laid as its open structure (the door's `Toggle`).
- **Outside parts** are rows `Vessels.Outside` (Hp, footprint, `Gives`, no menu card), in `Machines.All` and
  `Vessels.Machines`. Each stands in cells with no structure and no back wall, touching a hull wall. Numbers are tunables:

| Part | Class | Cells | Where | Gives (tunable) |
| --- | --- | --- | --- | --- |
| `vesselNozzle` | both | 8×7 | left of the stern armour, its right column against it, centred on its engine's deck | plate 4, copper 2 |
| `vesselRcs` | both | 3×2 | four: on top and under, at the stern and at the body's fore end | tube 2, scrap 3 |
| `vesselMast` | both | 2×8 (freighter 2×10) | on top mid-body; a freighter's on its bridge | wire 3, coil 1 |
| `vesselDish` | both | 5×5 | on top near the bow; a freighter's on its bridge | circuit 1, plate 2, wire 2 |
| `vesselSolar` | shuttle | 11×6 | on top near the stern | crystal 1, wire 2, frame 1 |
| `vesselTank` | shuttle | 16×3 | under the belly past the aft rooms | plate 2, tube 2 |
| `vesselRadiator` | freighter | 16×6, 1–3 | on top, from the aft rooms toward the housing | copper 4, plate 3, tube 2 |
| `vesselCollar` | freighter | 6×3 | on top after the radiators, else past the bridge | plate 4, gear 2 |
| `vesselTruss` | freighter | 11×3 a segment | under the keel, from the aft rooms to 6 short of the bow | frame 2, scrap 2 |
| `vesselContainer` | freighter | 10×4, 6 colours | under a truss segment, 4 in 5 segments, its colour drawn | plate 6, planks 8, wire 3, tube 2 |
| `vesselBoom` | freighter | 8×2 | at the bow tip, mid-height | wire 2, circuit 1 |

  Item ids are the nearest existing ones; the implementer checks each against `Items` and says where one differs.
- **Engines:** every deck below the first gets a main-engine interior (`Main: true`).
- **Yield:** `VesselClass.Yield` is re-measured by its content test over the seeds it uses, rounded out to the hundred.
- **Reach:** outside parts are reached by jetpack: the open space of a vessel world has no gravity.

## Looks (client)

- **Determinism of looks:** every look draws from `LookRng(seed, salt, cell)`, a fixed SplitMix64 in
  `game/Content/logic/VesselLooks.cs`; never `GetHashCode`, `HashCode`, `GD.Rand*` or `Random`. Candidates (lamps,
  consoles, doors, sign cells) come only from `Shipyard.Stamp(Shipyard.Describe(cls, seed))` in reading order; the live
  grid decides only whether each is shown. A test pins one seed to literal values (name, code, dying-lamp cell).
- **Content:** liveries (4: hull tint, stripe, paint colour), 14 Latin names, two-letter code prefixes, lamp and sign odds,
  in `VesselLooks.cs`, beside the lamp looks in `game/Content` (client-only numbers live there). Codes formatted with
  `CultureInfo.InvariantCulture`.
- **Skin:** `VesselDress.Of(...)` beside `WreckDress.Of`, GridView picking by surface kind. One grey atlas of hull panels
  (column within an 8-cell panel × row role: lit top, middle, shaded bottom; rust and pit variants picked by `LookRng`),
  rib tiles, one diagonal wall tile per bow slope (mirrored top and bottom), a stripe tile in the bottom armour row. The
  vessel's dress layer is modulated by the livery's hull tint; the stripe tile takes the stripe colour. Heavy ink where a
  side faces space, light where it faces a room.
- **Name and code:** rendered once as the surface loads, cut into cell pieces, each drawn only while its cell is a hull
  wall in the live grid. A half-cut letter is clipped at the cell edge (said to the user at stage 3's try).
- **Light** (`game/features/light`): vessel kinds have no ambient; the helmet lamp is the fallback when every lamp is gone.
  Each stamped lamp takes a state from `LookRng`: red emergency (the `emergencyLamp` colour), dead, or the vessel's one
  dying lamp (at most one; none if no lamp is left); the airlock's lamp always red. Lit consoles' screens: 3 in 4, 1 in 4
  of those glitching. A red dot over each door the live grid says is shut. A jammed door: an open door whose stamped cell
  `LookRng` picks (1 in 3 of open ones), drawn half-shut while the grid says open; shut by a hero it is a shut door with
  its dot, reopened it is jammed again. Beacons: red on each mast, white strobe on the boom or the shuttle's bow tip.
- **Signs:** 3–5 on a freighter, 2–3 on a shuttle, fewer where no room fits one, each on cells of rooms other than the
  airlock and bridge, hidden once its cells change.

## Fixes after play (confirmed with the user, 2026-11-17)

- `salvageContainer` stands only inside. Under the keel hang `vesselContainer`s: corrugated, dark corner castings, a stencilled
  number, in the mockup's six colours, a look each (`Vessels.Coloured`), the colour drawn from stream 2.
- Inside, the mockup's furniture (`Vessels.Furniture`, Hp, footprint, Gives, no menu card), by role: quarters bunks, a locker, a
  table; life tanks; the engine room a cable rack and a machine block; bridge and cockpit a console and a pilot's seat beside a
  salvage console; holds a cargo container (`vesselCargo`, the keel's colours) beside crates. Salvage stays, mixed in.
- No soil clod on a vessel (`salvageClod` kept in no room, so a world saved on a vessel still loads).
- One lamp on vessels, `vesselLamp`: flat, 2×1 on the ceiling row, pale, no power; `VesselLights` and the bolt filter key on it.

## Stages

Each is a try: build, its tests, a commit on `feat/derelict-looks`, the user plays `--vessel` and `--vessel=shuttle`.

1. **Shape:** bells gone (`VesselClass.Bells` removed), engines on every lower deck, `vesselNozzle` and its picture,
   modules, ribs, housing, bow slope, open doors, yields re-measured. Seeded vessel tests updated where the stern moved.
2. **Outside parts:** the other rows and pictures (anime-art subject `vessel`), placement, the two beacons' light.
3. **Skin:** `VesselDress`, the atlas and tint, outline, bow tiles, stripe, name and code.
4. **Light inside:** no ambient on vessels, lamp states, screens, door dots, the jammed door.
5. **Signs.**

Ship, once the user has played them all: an ADR superseding generated-vessels in part (bells, engines, outside parts,
looks), Places' README, map and glossary lines, the full suite, a PR.

## Tests

- **Scenarios** (`sim/Tests/VesselTests`): no wall left of the stern armour but nozzles; one `vesselNozzle` per lower deck,
  against the stern armour and centred on its deck; a main engine on every lower deck; each class's outside kinds present;
  a container only under a truss segment; bulges within 0–2; a rib at each module boundary; the housing behind the bridge;
  bow steps of `BowRun`; the airlock door shut; the spawn just inside the first door, with the largest stern bulge; every
  outside kind broken by a tool gives its `Gives`; `Places.Opened` lays open doors open.
- **Invariants** (FsCheck, class × seed): parts never overlap; every outside part on cells with no structure and no back
  wall, touching the hull; all within `Vessel.Width`/`Height`; decks' open rows untouched; the class yield in its range;
  `Opened` lays every part. Prove one bites by breaking it once.
- **Client** (`game.tests`): one seed pinned to literal looks; the same seed gives the same dress in two runs; signs within
  their class's count and at most one dying lamp; a look piece hidden once its cell changes; bow tiles only on the slope.

## Deliberately not handled

- Chamfer cells stay full cells: a hero flying along the bow bumps a corner the picture does not show (no slopes, 0106).
- A part on a hull cut away floats: machines need no floor.
- Signs hold nothing and the pointer names nothing on them; gas is not air, frost is not cold.
- Blink and flicker phases run on each client's clock, out of step between screens.
- With every lamp taken, a vessel is dark but for the helmet lamp.
