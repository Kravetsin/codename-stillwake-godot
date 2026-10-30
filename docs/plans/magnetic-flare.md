# Plan: The magnetic flare

## Why

The starting space has one danger, meteors, and they only break the hull. Once a machine is built nobody looks at it
again. A magnetic flare is a second danger that strikes inside: an aurora spreads across the sky, lamps flicker, cables
glow, a white flash, and a share of the ship's electric machines stop working until someone repairs them. Repair costs
part of what the machine was built of, so spares and walking the ship become a steady cost, as hull patches are.

The mechanic's next users, named so this plan leaves room for them and builds none of them: **fuses** and **shielded
walls** (both shrink the set a flare picks from), and **stronger flares in harder places** (a stronger record on their
surface kind).

**Order and PRs.** F-1 is the sim, with its dev command and tests; F-2 the client, with the admin button. Each is its own
branch and PR, F-1 merged first.

**Words.** A *flare* is the event (`MagneticFlares.Flare` is its numbers, a record, not the event); its *warning* the ticks
from the aurora to the strike; *broken* a machine a flare has stopped; *repair* the work that mends it. *Electric* is
`MachineSpec.Powered` (makes, draws or stores power). Times are in ticks (20 a second). A range `a..b` includes both ends.

## Rules

Marked (U) where the user set the rule; the rest is design, approved with it.

- **Armed** (U) once the player can build the oxygen generator: `IResearchQueries.Knows("generator")`, the blueprint the
  tech graph's block `generator` opens (`MagneticFlares.ArmedBy`). Creative mode knows everything, so flares come from the
  start there. There is no grace before the first flare: it may come as soon as the generator is known.
- **Where** (U): a surface kind with a `Flares` record has them: the ship (`Surfaces.Kind.Ship`),
  `MagneticFlares.StartingSpace`. Planets none. A `Calm` world (tests, dev worlds) draws none: `Calm` now means no hazards.
- **When:** each tick in construction, in this order: (1) `FlareIn` > 0 → it counts down by 1, armed or not; reaching 0 it
  strikes on that tick. (2) Then, if `FlareIn` is 0, armed, not `Calm` and the kind has `Flares`, the next is drawn:
  `FlareIn` = `Rng` evenly in `EveryMin`..`EveryMax` (start: 9 600..18 000, 8–15 min), not counted down on the tick drawn.
  The warning is `FlareIn` in 1..`Warning` (start: 200, 10 s).
- **The strike** (U: 10–30%, at random): the candidates are the electric machines a cable ends on as the tick's cables lie and not broken, in machine
  id order (`n` of them). The `Rng` draws `p` evenly in `PercentMin`..`PercentMax` (10..30) first, then `k = ceil(n × p /
  100)` distinct picks: each `Rng.Next(remaining)` indexes the list left, and the pick is removed from it. Rounding up means
  every flare breaks one at least; three candidates lose a third. A machine with no cable is never picked: it works neither
  way. Nothing protects a candidate yet.
- **Broken** (U): the machine stays where it is, its cables, pipes, slots, queue and progress with it. It makes, draws and
  stores nothing (its store neither takes in nor gives out, keeping what it holds); `Satisfaction(id)` is 0 and
  `Needed(id)` false, both checking `IsBroken` directly, so they hold on the strike's own tick. What reads them stops: a
  generator's air, a station's crafting, a charger, a lamp's light; except the cases under "Deliberately not handled".
- **Repair cost** (U: part of the build cost, never circuits, a one-item cost needs that item): each item of the build
  cost but `circuit`, × `RepairRules.Share` / 1000 rounded up (start: 250‰). A lamp (2 wire, 2 scrap) takes 1 wire and 1
  scrap; the arc lamp (1 arc lamp) its arc lamp, so with no spare it stays broken; the oxygen generator a frame, a rotor, a
  tube and a wire; the engine one of each of its rare parts. A cost of circuits only repairs for nothing.
- **Repair work:** `Repair(Machine)` is one tick of one hero's work, sent `Screwdriver.Work` times a tick while the
  screwdriver is held on a broken machine, as `Work` is on a plan. The repair is done after `ceil(BuildTicks ×
  RepairRules.Share / 1000)` such work ticks. The first command applied to an unpaid repair takes the whole cost from its
  sender, or is refused `items.notEnough` and takes nothing; the paying command adds its work tick too. Every later one,
  anyone's, carrying nothing, adds its tick; two heroes work twice as fast. Progress is kept, never decays, and anyone
  finishes it. Creative mode takes nothing. A `Repair` at a machine Power does not call broken (repaired already, taken
  apart, not electric) is refused `building.notBroken`; the client shows that refusal no more than `building.noPlan`
  after a plan is done.
- **Taken apart broken:** refunds its cost less its repair cost, item by item, never below 0, whether or not the repair
  was paid. Otherwise taking apart and rebuilding would beat repairing. A paid repair is not given back: one loss only.
- **Items lost:** a repair's payment and the share a broken machine does not refund leave the world.
- **What does not change:** cables, pipes, walls, heroes and what they carry; meteors.

## Tasks

### F-1 — Flares break machines, a screwdriver repairs them (sim) ✅

- **Content:** `sim/Content/MagneticFlares.cs`: `record Flare(int EveryMin, int EveryMax, int Warning, int PercentMin, int
  PercentMax)`, `StartingSpace`, `ArmedBy`; `RepairRules.Share` (‰) and `RepairRules.Never = "circuit"`.
  `Surfaces.Kind.Flares` (`Flare?`), set on `Ship`. Not `Flares`: the client's `render/Flares.cs` has the name (0075).
- **Power** owns *broken*: saved `Broken`, machine ids sorted. `IPowerOps.Break(id)` and `Mend(id)`, both seen by every
  query at once and by the networks from the next derivation (the same tick when called in construction or commands).
  Queries `IsBroken(id)` and `Broken`. A broken node joins its network as before and adds no supply, demand or store flow.
  `Unplug` drops it, as it drops `Resting`, and derivation drops ids of machines gone any other way. Audit every reader of
  `Satisfaction` and `Needed` (`Atmosphere/Air.cs`, `Crafting`, `Furnace`, Power's charging, the drone bay, the engine
  machine `"engine"`); the two exceptions below are decided, the audit confirms them or adds to the list.
- **Building** owns *repair*: command `Repair(Machine)` with the rules above. Saved `Repairs`, `(Machine, Progress)` sorted
  by machine, the entry made by the paying command; the last work tick calls `Mend` and drops it; one whose machine is gone
  is dropped in derivation. Queries `RepairCost(machine)` and `RepairProgress(machine)` for the client. The take-apart
  refund above. The items-conserved invariant names the two sinks. Building gains `IPowerQueries`.
- **Hazards** owns the flare, beside the shower, with the steps above: saved `FlareIn`. Queries `FlareComing` (`FlareIn` in
  1..`Warning`) and `FlareLeft`, the warning ticks left (0 outside it), for the aurora's ramp. Hazards gains
  `IPowerQueries`, `IPowerOps`, `IResearchQueries` and reads the machines through the grid; it is registered after Power and
  Research already.
- **Dev command `FlareNow(Ticks)`:** sets `FlareIn` to `Ticks` (≥ 1, else `hazards.badTicks`), replacing a flare drawn
  but not yet in its warning; refused `hazards.flareComing` during a warning. It works in any world, calm or not, armed or
  not; where the kind has no `Flares` the whole `StartingSpace` record gives the strike's numbers. The admin button sends
  the record's `Warning`, so the event starts with the aurora. After it strikes, the next is drawn only by the rule above.
- **Fuzz:** Hazards draws `FlareNow(1)` one turn in 64; Building draws `Repair`, mostly at a broken machine, else at any
  id. The generic suites seldom see a machine broken in their short runs (oftener flares starved the stations' coverage),
  so `MagneticFlareTests` loads a world struck and repaired before every tick and compares hashes.
- **Save:** three fields added with "none" initializers: no format bump.
- **Docs:** README rules of Power, Building (and its invariant), Hazards (`Calm`); `docs/glossary.md` (вспышка / flare,
  сломан / broken, ремонт / repair); `docs/map.md` if a line moves; ADR `docs/decisions/2026-10-30-magnetic-flare.md`; a line
  in `docs/concept.md` under places and their dangers, and the Open list's dangers updated.

**Tests** (scenario per rule and refusal):
- no flare before `generator` is known, then one after `EveryMin`..`EveryMax` and its warning; none in a calm world, none
  on a planet; a drawn flare still strikes after creative is switched off; a save written before the field, `generator`
  known, draws its first flare on its first tick;
- the strike: 4 candidates at 10% break 1, at 30% 2; a machine with no cable or already broken is never picked, so a
  repair in progress is safe; a fixed seed breaks the expected ids;
- broken: a generator's room gets no air and its demand leaves the network; a panel supplies nothing; a store keeps its
  charge and gives none; a burner is not `Needed` on the strike's own tick; a network still joins through a broken node;
- repair: `building.notBroken` (working, gone, not electric, after done); `items.notEnough` takes nothing; costs of lamp,
  arc lamp, generator, a circuit never taken; two heroes in one tick: the first pays, both work, done twice as fast;
  progress kept after a pause; creative takes nothing; done after its ticks, the machine works again;
- take apart: broken, paid or not, refunds cost less repair cost; the `Repairs` entry goes;
- `FlareNow`: starts the warning in a calm world, replaces a drawn flare, `hazards.flareComing` during one,
  `hazards.badTicks`; save and load mid-warning and mid-repair;
- FsCheck: `Broken` holds existing electric machines once, sorted; a broken machine's satisfaction is 0 and Power's
  balance holds; a repair takes its cost exactly once. Each proven to bite once.

**Done when:** `dotnet test Stillwake.sln` green, `dotnet format --verify-no-changes` clean, docs guard green.

### F-2 — The aurora, the flash, the dark machines (client)

- **hazards:** `AuroraView`, a line in `game/SurfaceViews.cs` behind the ship, drawn while `FlareComing`: bands across
  the sky, fading in by `FlareLeft` over the warning; at the strike, a white flash over the screen fading out in about
  half a second (reuse `render/Flares.cs` if it fits). A peer joining between the strike and its frame misses the flash.
- **light:** while `FlareComing`, lamps flicker (`LightView`'s `LampShare` through `Flicker.BrightnessOf`), harder towards
  the strike. A broken lamp is dark already, its satisfaction 0.
- **power:** while `FlareComing`, cables glow brighter, pulsing. A broken machine drawn dimmed over its footprint
  (`PowerView`), so it reads without words; a repair in progress shows its progress as a plan's does.
- **The screwdriver:** `crew/logic/Aim.cs` sends `Repair(machine)` `Screwdriver.Work` times a tick while held on a broken
  machine with no plan or mark on it, as it sends `Work`; a mark wins, so a broken machine marked for taking apart is taken
  apart. Under the pointer, as a plan's cost: "broken" and the repair cost (`building.repair` keys in `Building.En.cs`),
  and `items.notEnough` when short; `building.notBroken` is not shown.
- **Admin button:** "flare" beside "meteor" in `game/features/dev-panel/DevPanel.cs`, sending `FlareNow` with the
  surface's `Warning` (`StartingSpace`'s where it has none).
- **Docs:** the READMEs of `hazards`, `light`, `power`, `crew`, `dev-panel`.

**Player sequences, each a recording looked at frame by frame (`tools/snap.ps1 -Movie`):**
1. The admin button on the wreck: aurora rising, lamps flickering, cables glowing, the flash, some machines dimmed, a lamp dark.
2. The screwdriver held on the broken generator with its cost in the pockets: the pointer shows the cost, the dimming goes,
   the room's air comes back.
3. The same with an empty pocket: refused, nothing taken.

Compare against: the meteor's warning in `MeteorView` (how a hazard is seen coming) and a plan being built (how work shows).

**Done when:** as F-1, and the three recordings looked at.

## Not in this plan

Fuses, shielded walls, flares in other places or on planets, sound beyond what exists, a flare on the radar.

## Deliberately not handled

- Cables, pipes, walls, heroes and carried items (batteries, jetpack, drones) are never harmed.
- No partial damage or wear: a machine works or is broken.
- Repair is paid at once, not a part at a time as plans are.
- A broken drone bay still sends out docked drones; fuel already burning in a broken burner burns out for nothing.
- A machine just repaired may break in the next flare; no grace before the first.
- A rare-part repair costs nearly a new machine; an arc lamp with no spare stays broken (the user's one-item rule).
- Nothing points at machines broken off screen: the player finds them by their dimming or what stops.
- `Instant` (dev) does not speed repairs.
