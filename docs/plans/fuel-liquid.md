# Plan: Fuel is a liquid (biofuel in litres, tanks, pipes)

Status: plan, 2026-11-15. Reviewed by `/review-plan` (completeness, clarity, determinism) the same day, its findings folded
in. Built in the two stages of CLAUDE.md's Workflow: **Try** (built, played by the user) and **Ship**.

**Try built (2026-11-15, `feat/fuel-liquid`).** What changed from the steps below while building:
- **No slice reorder.** The saved run order is the `surface` list, not the constructor order: `Fuel` is constructed after
  `Pipes` and before `Crafting` (which moved `Pipes`'s construction up), and runs and saves **last**. So the migration only
  adds the `fuel` slice; no slice moves. Format 66 → 67 (main had moved on to 66: the trash slot).
- **The engine rests while its store is empty**, through `IPowerOps.Rest` from `Fuel` (Power cannot read Fuel): a hopper
  would otherwise make it draw always.
- **`IPipesQueries.NetworkOf(machine, way, fluid)` and `MachinesOn(...)`**: Water's private helpers moved to Pipes, keyed by
  fluid too, for the generator's two inputs of different fluids.
- **`IItemsOps.SmeltOut`**: the fermenter's smelt taken from its input, its yields counted obtained, put in its store.
- **The fuel tank's picture is the cistern's, a placeholder** until the art pick; its map character is `!` (every letter is
  taken). The pipe's amber is a placeholder too. The four machines that gained a fuel port got its fitting drawn.
- **After the first play (the user's list):** tanks, cisterns and the fuel tank on the storage tab; every sight glass shows
  any level to a hi-res pixel (`SightGlass`, `GlassLevel`: one `{kind}_level` picture and its surface, the glass found in it),
  no more fifths or quarters; the biofuel generator pays a tick's share of a litre each tick (`Fire.Owed`), so its store
  drains as it burns; a fuel store says how long it lasts at what it gave out over the last 5 s (`Lasts`, derived outflow);
  a machine holding over a litre of liquid is not taken apart (`building.holdsLiquid`, `Builds.TakenApartHolding`).
- **The crew shuttle** holds 200 l: a hopper, a fuel input at its stern (0, 8), its panel the stores' (E opens it), its fuel
  up the sight glass its picture already had. It still does not fly or seat anyone.
- **Picked (2026-11-16)** on the pick page (https://claude.ai/artifact/3RXrpHnVpZTCJxrKDH7bW1, its `picks` docs): the fuel tank
  is B «Бочка на ложементе» (a mustard barrel lying on two saddles, a striped hopper on top, a narrow window on its side,
  glass [11, 11, 3, 12]); biofuel is 4 «Ржавый оранжевый» (#b0501c / #f0965c / #d46a30, the last its pipe sockets'). Turned
  down: A «Клёпаная бочка», C «Клетка с канистрой», D «Напорный бак с поплавком»; amber, olive oil, dark honey. Both go
  into the ADR at ship.
- **A demo**, `--fuel`: a fermenter piped to a 60 l tank piped to a pod; a generator to pipe by hand. The testbed has a fuel
  tank on the arc bench.

Biofuel is an item today: a jerrycan stacking to 100, put by hand or drone into the slots of the drop pod, the biofuel
generator and the engine, made into the fermenter's output slot. Oxygen and water are liquids held in tanks and moved by
pipes ([liquids-in-tanks](../decisions/2026-11-08-liquids-in-tanks.md), [tanks-in-a-line](../decisions/2026-11-02-tanks-in-a-line.md),
[liquid-volumes](../decisions/2026-11-09-liquid-volumes.md)). This plan makes biofuel the third liquid.

**Words used here.** A *store* is the fuel a machine holds, kept in the `Fuel` module's slice. A *fuel tank* is the new
machine `fuelTank`. A *hopper* is a store's one item slot that takes cans only (`SlotTakes`) and empties them into the
store. A *slot* is an Items crate slot. A *can* is the item `biofuel`. The fermenter is the user's "перегонный аппарат".
Time: 20 ticks a second.

## Decided with the user (2026-11-15)

1. **A biofuel can is 1 litre**; it stacks to **20** (20 l).
2. **A fuel tank**, a new machine, holds **100 l**.
3. **The drop pod holds 20 l** in a store, like a tank: cans put in its hopper are absorbed; a fuel pipe into its input
   fills it by itself. A descent burns 5 l, as it burnt 5 cans.
4. **The fermenter** gives its fuel out of a pipe into the stores on its output: fuel tanks, pods, generators, the engine.
   What does not fit stays in its own **20 l** store; once that is full it stops and its animation goes still. Its store
   is drawn out as cans by hand, through its panel, like any tank's.
5. **The biofuel generator holds 20 l**, filled by cans or a pipe, and burns from that store.
6. **The engine gets a 20 l store and a fuel input now** (build the mechanic once, for every user). It is a store only:
   the engine burns nothing yet.
7. **Drones** fill the hoppers from crates, as they fill oxygen tanks', but never draw fuel out of a store. The fermenter's
   fuel leaves by hand or by pipe only.
8. **Old saves:** the cans in the pod's, generator's and engine's slots and in the fermenter's output slot are poured
   into the new stores.
9. **The engine's fuel input is on its right column**, the one exception to ports-left-to-right: its left stands against
   the stern's wall. Its socket is drawn as every input's, the arrow pointing into the machine.

## Where it is heading (principle 2)

Users this stage: the fuel tank, the pod, the biofuel generator, the engine, the fermenter. Next: the engine burning fuel
for the jump, vessels' fuel salvage. So the mechanic is **a fuel store on any machine**, its size a row of data.

Water's ADR said a general liquids module waits for a third fluid. Fuel is that third fluid, but oxygen's tanks carry
rules of their own (leaks, vaporizers, blown fires, canisters breathed). So:

- **Reuse first:** the store mechanics Water already has — a hopper absorbing an item, a line pouring on in id order,
  drawing whole containers — move into one shared helper, `sim/Core/Stores.cs`. It is pure: it works on a
  `List<Held>` (`Machine`, `Amount`, sorted by machine id, as Water's is today) with capacities and the line's givers and
  takers handed in. Water switches to it; its saved JSON and its tests stay the same. Atmosphere keeps its own code.
- **A small `Fuel` module** (`sim/Fuel`, id `fuel`) owns every machine's store. Others change a store only through
  `IFuelOps`.

## Rules as the player meets them

1. **Units.** Fuel is counted in millilitres, as oxygen and water (`Content/Fuel.cs`: `Litre = 1000`, `Can = Litre`).
   A can's card says "1 l", a stack's "1 l · ×12 = 12 l" (`ItemCards.VolumeOf`). A store holds part litres; whatever
   takes fuel out (a draw, a burn, a descent) takes whole litres, so a remainder under a litre waits for more.
2. **Stores.** A machine row gets `FuelHolds` (litres): `fuelTank` 100, `dropPod` 20, `biofuelGenerator` 20, `engine` 20,
   `fermenter` 20. `MachineTrait.HoldsFuel` follows from it; `ContentTests` holds every `FuelHolds` above 0.
   A store holds at most its capacity, except one loaded fuller from an old save (rule 11).
3. **Hoppers.** The pod's, generator's and engine's slot 0 and the fuel tank's one slot take cans only
   (`SlotTakes: (0, "biofuel")`, replacing `Takes`). Each tick, each store in id order absorbs **one can** from its
   hopper when a litre fits, as a cistern absorbs a bottle. Cans waiting there stay until room comes; drones keep a hopper
   stocked as they keep a tank's (`Jobs.Wants`, its hopper branch). The fermenter has no hopper.
4. **Drawing.** `DrawFuel(Machine, Count)` gives the sender `Count` cans from any store, one at least, all or none.
   Refused, nothing changed: `fuel.noStore` (no machine with `HoldsFuel` there, taken apart included), `fuel.low` (the
   store holds less than `Count` litres; compared as `long`), `items.full` (the existing Items key: the sender has no room
   for all `Count`). E opens a store; its panel is the tank's: the hopper, a slider up to the whole litres held, the draw
   key, "12.00/20.00 l · 12 cans".
5. **Pipes.** A new fluid `Fluids.Fuel` (`"fuel"`). Ports: the fuel tank In (left) and Out (right); the pod and the
   generator In on their left; the engine In **on its right** (decided 9); the fermenter Out on its right. Every typed-port
   rule holds as it is. `ContentTests` keeps inputs on the left and outputs on the right, naming the engine's fuel input
   as its only exception; machines are placed unrotated, so the exception cannot flip.
6. **A line pours on**, as tanks do (tanks-in-a-line). Each store with an output port and fuel in it, in id order, gives
   up to `Fuel.Flow` (20 ml a tick, **its own**, not the network's: a litre in 2.5 s, a pod filled in 50 s) to the
   stores whose input port is on its output's network, in id order, as much as each has room, never to itself. A store
   that gave and takes (a fuel tank) pours on in the same tick when its turn comes, so tank A → tank B → pod fills the
   pod first. A loop moves fuel round and evens nothing. A network with no takers takes nothing. Nothing is made or lost.
7. **The fermenter** makes a litre where it made a can (one in 160 ticks at full power) into its own store. It begins a
   unit only while its store has a litre of room; nothing but it fills that store, so the room is still there when the
   unit ends. With no room it is idle as a station whose output slot is full: drawing no power, its bubbles and drip
   still. Its store pours down its pipe like any store's, from the next tick. Its output slot goes: `Slots: 1`. Only the
   fermenter makes biofuel (biomass and fat ferment nowhere else), so no recipe yields cans any more.
8. **The biofuel generator** keeps `FuelSlot: 0` (it is what makes it a fire to Power, Crafting and drones), but its fire
   lights from its store: a litre burns `biofuel.BurnTicks` (1600) ticks, as a can did, and `furnace.noFuel` means its
   store holds under a litre. The cans in its slot 0 are the hopper's, never burnt directly. The furnace and the burner
   generator hold no fuel: they still burn cans from their fuel slots, a litre a can.
9. **The drop pod** launches only with 5 l in its store (`landing.noFuel`), read as the `Launch` command applies. The 5 l
   burn aboard and the rest flies with it, down and back. If a second hero draws the pod's fuel before the seated hero's
   `Launch` applies, that `Launch` is refused with `landing.noFuel` and the seat holds: commands of a tick apply in
   stamped order. Landed on a planet, its store and hopper work there too.
10. **A machine taken apart loses its fuel**, as a cistern loses its water: no spill. A pod laid as its cost (its spot
    built over) lays its fuel as cans, whole litres rounded down, the part litre lost.
11. **A store loaded over capacity** from an old save keeps its fuel, absorbs nothing and takes nothing from its line
    until it is below; it pours on and burns as usual (the precedent of liquid-volumes' cistern).

## Steps

Try (one branch, `feat/fuel-liquid`, its own worktree):

1. **Content.** `Content/Fuel.cs` (`Litre`, `Can`, `Flow`, `Descent = 5 l`, `Item = "biofuel"`); `Fluids.Fuel`; biofuel
   `Stack: 20`; `MachineSpec.FuelHolds`, `MachineTrait.HoldsFuel`; the new row `fuelTank`, 4×4, placeholder cost
   `("plate", 6), ("tube", 2)`, the Power tab after the biofuel generator, a free `Char` (all three may change with the
   art); fuel ports on the pod, generator, engine (right) and fermenter (right); `Takes` → `SlotTakes` on the pod,
   generator and engine, and `Takes` removed if no row keeps it (Landing's `Fuel` reads `Fuel.Item`); the fermenter's
   `Slots: 1`; `Landing.DescentFuel` → `Fuel.Descent`. `ContentTests`: ports by side with the engine's exception,
   `FuelHolds` above 0, every store with a hopper but the fermenter.
2. **`sim/Core/Stores.cs`**, the shared helper; Water moved onto it, its `Held` shape and JSON unchanged.
   `dotnet test sim/Tests --filter Water` green with no test changed.
3. **`sim/Fuel` module** (`new-module` skill). State `Stores`, a `List<Held>` sorted by machine id, saved. Systems:
   construction — drop the stores of machines gone; simulation — the line pours, then the hoppers absorb (Water's order).
   `IFuelQueries`: `FuelIn`, `RoomIn`, `DrawProblem`. `IFuelOps`: `Fill(machine, amount)` (returns what fit), `Take(machine,
   amount)`, `TakeAll(machine)`. `DrawFuel` command; `Fuzz` drawing it on stores and on non-stores (refused). Scenario
   tests for every rule and refusal, the two-tank line and a loop included. FsCheck invariant, proven to bite: with no
   machine taken apart and no pod laid as its cost, the fuel in stores, cans and flights changes only by litres fermented
   minus litres burnt (generator, descent).
4. **Registration order.** `Fuel` needs `Pipes`; `Crafting`, `Furnace` and `Landing` need `Fuel`; `Crafting` is
   registered before `Pipes` today. New order: … `Research`, `Pipes`, `Fuel`, `Crafting`, `Liquids`, `Atmosphere`, …;
   every module stays after its dependencies (`Pipes`: Grid, Items; `Fuel`: Grid, Items, Pipes; Atmosphere and Building
   still after Pipes). Why a tick runs the same: `Pipes` has one derivation system and finds its lines lazily, on the
   first read after a change; check that `Crafting` (simulation only) and `Liquids` have no derivation system reading
   pipes. The order of slices in saves and hashes changes: step 8 rewrites it.
5. **Crafting**: a station that holds fuel begins a unit only with `RoomIn ≥ Litre` and puts its output through
   `IFuelOps.Fill`; otherwise idle. Test: the fermenter stops at 20 l with nothing piped, and runs on with a tank piped.
6. **Furnace**: for a fire on a machine that holds fuel (the biofuel generator only), lighting takes a litre with
   `IFuelOps.Take`, and `FuelIn` / `furnace.noFuel` read the store. Test the drones: they stock its hopper once, the
   fuel-slot and hopper wants not doubled (`Jobs.cs` `Wants`, `Takes`).
7. **Landing**, every path a pod crosses or leaves the grid:
   - `Launch` refuses below 5 l. `Fly` takes 5 l, then `TakeAll` **before** `Clear`, into `Flight.Fuel` (a new saved
     field, `0` by default).
   - `Depart` takes the flight's fuel, or, for a pod following its owner, `TakeAll` before `Clear`, into a new
     `Carried.Fuel`.
   - `Count` (a descent ended at once, its hero gone or passed out) → `Dock`; `Arrive` → `Dock` or `Land`; all three
     pass the fuel to `Stand`.
   - `Stand` puts it into the new pod through this surface's `IFuelOps.Fill`, or, laid as its cost, lays it as cans
     (rule 10).
   - Tests, fuel conserved: descent and return; a descent aborted mid-fall; a dev beam mid-flight and of a pod standing
     off the ship; a pod laid as its cost (whole litres). `DescentTests`' invariant (biofuel in slots, flights and floor)
     gains the stores and `Flight.Fuel`.
8. **Saves**: `World.SaveFormat` 63 → 64 and its step in `Migrations`, on every surface:
   - the slices rebuilt in the new order, `pipes` moved after `research` and `fuel` (`{ "stores": [] }`) after it, as
     the format-56 step does (`Inserted56`);
   - every `dropPod`, `biofuelGenerator` and `engine`: the cans in slot 0 poured into its store, **all of them** (an
     overfull store is rule 11), the slot emptied;
   - every `fermenter`: the cans in slot 1 poured into its store likewise, its slots array (and its kept-item memory)
     cut to one;
   - every `Flight`: the cans of its `Load` into `Flight.Fuel`, `Load` null.
   - Stacks over 20 elsewhere (crates, inventories, the floor) load as they are and are used from, as stacks did when
     liquids-in-tanks cut stacks: no split.
   - Test: a save with 64 cans in a fermenter, a 100-stack in a crate, a pod with 20 cans, and a pod in flight with 15.
9. **Client.** The tank panel's `DrawBody` for `HoldsFuel` (`DrawFuel`); the pointer's tip (`MachineTips`); the can's
   card litres; the fuel line's colour in `PipesView`; the pod, generator and engine show their store's level; the
   fermenter's animation stills when idle (check `StationView` follows idle); the engine's right input socket and its
   build ghost with the arrow in. Keys `fuel.noStore`, `fuel.low` and the panel's text in `*.En.cs` and `*.Ru.cs`
   (`RuFilesTests`).
10. **Art pick** (an Artifact page, CLAUDE.md): the fuel tank, drawn in ink and watercolour, several candidates, filling
    and emptying; and the fuel pipe's colour beside water's teal and oxygen's.
11. **Testbed**: a fuel tank piped between the fermenter, a pod and the biofuel generator; launch it for the user to play.

Ship, once the user has played it and said yes: the full `dotnet test Stillwake.sln`; the ADR
`YYYY-MM-DD-fuel-is-a-liquid.md` (supersedes the "biofuel keeps its count" line of liquid-volumes, the drop pod's "its
one slot holds the biofuel", and power-chain's "`FuelSlot` and `Takes` together"); `sim/Fuel/README.md`; README lines in
Water, Crafting, Furnace, Landing, Pipes, Logistics; `docs/glossary.md` (fuel tank, store, "перегонный аппарат" →
fermenter) and `docs/map.md`; this plan deleted. PR, merge on green.

## Open, to settle while building

- **The fuel tank's size, cost, tab and char:** placeholders in step 1 until the art is picked.

## Deliberately not handled

- **No pumps or power for flow:** fuel moves by itself, as oxygen and water do.
- **Drones never draw a store:** the fermenter's fuel goes by hand or by pipe (the user's pick).
- **No pouring with E onto a store:** the hopper does it, by hand or drone; Water's `PourWater` is not copied.
- **The furnace and the burner generator take no fuel pipe:** they burn any fuel by the can.
- **Atmosphere's tanks stay on their own code:** leaks, vaporizers and blown fires make them more than a store.
- **Spills, leaks and fires of fuel:** none. A store taken apart loses its fuel; a part litre is lost when a pod is laid
  as cans.
- **A remainder under a litre** in a store is not drawn or burnt until more joins it.
- **A pod on a planet with no fuel line** fills only from cans; that is the point of carrying them.
