# 0113 — Systems ask a machine's traits, not its kind

- Status: accepted; supersedes [0112](0112-structure-traits.md)'s machine traits as fields and its limitation that a system working one kind names it
- Date: 2026-10-24

## Context

0112 gave machines `Container`, `Gas`, `Vents`, `Zone` and `Glow` as fields, but the systems still walked them by name:
air walked `MachinesOf("vent")`, `"tank"` and `"generator"`, and a vent was fed by a tank or by `m.Kind == "generator"`;
drones walked crates and bays by name; the furnace module merged `"furnace"` and `"burnerGenerator"` by hand; the client
picked a machine's panel, its prompt, a lamp's light and a pipe's feed by name. A new machine whose behaviours all
existed, a second tank or a brighter lamp, still touched four to eight files.

## Decision

- **Traits are flags.** `MachineSpec.Has` holds `MachineTrait`s, one bit a trait; `Traits` adds the ones a field
  implies, `Is(trait)` and `Machines.Is(kind, trait)` ask them.

  | Trait | Machines | Read by |
  | --- | --- | --- |
  | `Container` | crate | items (moves into it), drones (store, take), its sort key |
  | `Gas` | tank | canisters, the starting tanks, tank looks |
  | `AirSource` | tank, generator | a vent is `Fed` when a pipe joins it to one; air draws from them all in one id order; one that is no `Gas` store makes its air on power |
  | `Vents` | vent | air, the vents' hum and breath |
  | `Zone` | drone bay | drones, the bay's panel, slot marks, zone outlines |
  | `Glow` | lamp, arc lamp | light, its colour and reach from the client's `Light.Lamps` row |
  | `Fire` | furnace, burner generator | the furnace module; implied by `FuelSlot`, never declared |

  `Fixed`, `FuelSlot`, `Inputs`, `Makes`, `Charges` and a station's `Fed` stay fields: a fire with inputs smelts them
  (the furnace), one that `Makes` power rests when out (the burner generator); a charging station that is no `Zone` is the
  charger.
- **The grid lists machines by trait.** `IGridQueries.MachinesWith(trait)` is one trait's machines in id order, derived
  beside `MachinesOf(kind)` and kept current on every build, take-apart and load. Walking a trait instead of merging
  two kinds by hand keeps the same id order, so the simulation is unchanged.
- **The client asks too.** A machine's panel is chosen by its traits (a fire with inputs, a fire, a fed station, a zone,
  a charging station, else a crate's grid); its prompt is `use.open.<kind>`, a crate's when it has none; its bolt is what
  it `Makes`, else what it `Draws`.
- **Only a machine's own rule names it.** `MachineKindGuardTests` fails on `Kind == "<machine>"`, `MachinesOf("<machine>")`
  or `Machines.Get("<machine>")` outside its allowlist: the analyzer's system, screens and reading, and the smelter's and
  former's looks and sounds. Tests, `game/dev` and `*Demo.cs` may name kinds.

A new machine whose behaviours exist is a row of `Machines.All`, its art and its name (`tool.<kind>`, and `use.open.<kind>`
if it opens), plus its build-menu card; a new lamp also a row of `Light.Lamps`.

## Rejected

- **A trait per machine (`Generator`, `Analyzer`):** that is a kind under another name. The generator is the air source
  that stores no gas.
- **An `AirMaker` trait for the generator:** one machine makes air; `AirSource` without `Gas` says it.
- **Bool fields per trait with a predicate walk of every machine:** the grid could not list them, and each system would
  scan all machines a tick.

## Accepted limitations

- The build menu (`Tools.Tabs`) lists its cards by kind: its order is a choice of the menu's.
- Per-kind art stays keyed by kind: a station's frames and lights (`StationView`), a fire's mouth and the burner's wheel
  (`FurnaceView`), the arc bench (`BenchView`), the arc lamp's glass (`ArcLampLooks`).
- The analyzer, the smelter's heat and the former's press keep their names: each is that machine's own rule.
- The guard reads code, not meaning: a kind behind a constant passes it.
