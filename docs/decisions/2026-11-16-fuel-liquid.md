# Fuel is a liquid

- Status: accepted; supersedes "biofuel keeps its count" of [liquid-volumes](2026-11-09-liquid-volumes.md), the drop pod's
  "one slot holds the biofuel" of [the-drop-pod](2026-10-25-the-drop-pod.md) and [the-descent](2026-10-26-the-descent.md), and
  "`FuelSlot` and `Takes` together" of [the-power-chain](2026-11-06-the-power-chain.md); amends [crew-shuttle](2026-11-15-crew-shuttle.md)
- Date: 2026-11-16
- Decided by the user over a plan (`design` → `/review-plan`), two plays and a pick page

## Context

Oxygen and water were liquids kept in tanks and moved by pipes; biofuel was still an item, a can stacking to 100, put by hand
or drone into the slots of the drop pod, the biofuel generator and the engine, made into the fermenter's output slot. The user
wanted it a liquid like the others: stored in litres, piped from the fermenter to tanks and on to whatever burns it, so cans need
not be carried each time.

## Decision

- **A can is a litre** (`Content.Fuel.Can`, millilitres as oxygen and water) and stacks to 20.
- **Every machine that holds fuel has a store**, its size a row's `FuelHolds` (`MachineTrait.HoldsFuel` follows from it): the
  new fuel tank 100 l, the crew shuttle 200, the drop pod, the biofuel generator, the engine and the fermenter 20. A small module,
  `Fuel`, owns them; Crafting, Furnace and Landing change them only through `IFuelOps`.
- **Cans go in by a hopper** (`SlotTakes`), a can a tick when a litre fits, by hand or by drone; **`DrawFuel`** gives them
  back from any store. **A line pours on** as tanks do, 20 ml a tick a store, in id order.
- **The fermenter** makes a litre into its store, begins a unit only with a litre of room, and idles, still, when full; its
  output slot is gone. Drones never draw a store: its fuel leaves by hand or by pipe (the user's word).
- **The biofuel generator burns a tick's share of a litre each tick** (`Fire.Owed`), so its store drains as it burns; a dry
  store puts its fire out. The furnace and the burner generator still burn cans.
- **The drop pod** launches with 5 l in its store and flies the rest down and back (`Flight.Fuel`, `Carried.Fuel`); laid as its
  cost, its fuel is laid as whole cans. **The engine** holds fuel and burns none yet; its input is on its right, the one
  exception to ports-left-to-right, for its left is the stern's wall. **The shuttle** has a fuel input at its stern and opens
  on E with the stores' panel; it still does not fly.
- **A store says how long it lasts** at what it gave out, poured or burnt, over the last five seconds (`Lasts`, derived).
- **A machine holding over a litre of liquid is not taken apart** (`building.holdsLiquid`, `Builds.TakenApartHolding`): a tank,
  a cistern or a store is drained first.
- **Sight glasses show any level** to a hi-res pixel (`SightGlass`, `GlassLevel`), not fifths or quarters: one picture of the
  glass full and its surface, the glass found in it. Tanks, cisterns and the fuel tank sit on the storage tab.
- **Water's store code is shared** (`sim/Core/Stores.cs`); Pipes answers which machines stand on a port's network
  (`NetworkOf`, `MachinesOn`, by fluid). A new save format, 67: old saves pour their cans into the new stores.
- **The looks are the user's picks** (pick page, its `picks` docs): the fuel tank «Бочка на ложементе», a mustard barrel lying
  on two saddles, a striped hopper on top, a window on its side; biofuel «Ржавый оранжевый», #b0501c / #f0965c / #d46a30, the
  last its pipe sockets' colour. Turned down: a riveted drum, a cage round a welded can, a pressure tank with a float; amber,
  olive oil and dark honey.

## Deliberately not handled

- No pumps or power for flow; no spills, leaks or fires of fuel. A store taken apart (a litre or less) loses its fuel.
- The furnace and the burner generator take no fuel pipe. Atmosphere's tanks keep their own code.
- A part litre in a store is not drawn as a can; a pod laid as cans loses it.
- How long a store lasts reads nothing for five seconds after a load, and differs between peers until then: display only.
- A store loaded over capacity from an old save keeps its fuel, takes none in until it drains below.
