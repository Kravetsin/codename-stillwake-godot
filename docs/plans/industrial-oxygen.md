# Plan: Industrial oxygen

## Why

After [scarce-oxygen](../decisions/2026-11-07-scarce-oxygen.md) air is short, but oxygen is still only air: the tank network
is plumbing, the electrolyser runs only while a hero pours bottles into it, and a fire in a room at its budget breathes
for free. After this plan a fire costs its room air visibly or runs on the tanks' liquid and does more for it, the
electrolyser is a planet-stage machine that turns ice into oxygen with no hand on it, and a tank's liquid can be carried
away as canisters.

**Decided with the user** (2026-11-07):
- A fire breathes `Furnace.Breath` = 3 units a second (in the scarce-oxygen change, already made).
- Ice comes only from planets, as today; no icy rock in space.
- The electrolyser comes later in the game: its build cost needs copper, which only a planet gives. It stays understood by
  the rule, once its parts are obtained; it is not sold on the black market.
- A lit fire reserves its share of its room's budget; heroes reserve nothing.
- A blown fire (one fed liquid oxygen through its port): works with no room air; a furnace smelts at 1.5×, a generator's
  fuel lasts 1.5× and it makes 1.5× power.
- A blown fire takes `Furnace.BlownBreath` = 5 units of liquid a second from the tanks on its port.
- A hero can take a canister out of a tank.
- Hydrogen: nothing for now.

**Decided with the user** (2026-11-08), for P5:
- Liquids are stored in tanks and cisterns, not crates: a canister and a water bottle stack to 5, ice to 50.
- A cistern holds 50 bottles, a tank 50 canisters.
- Ice is easy to farm on a planet: an ice vein and an ice crystal give 4 (the user's 3 to 5 at its middle; nothing a wall
  or a plant gives is rolled today).

**Order:** E-1 stands alone. P1 before P2 (a blown fire is one that reserves nothing). P3 and P4 after P2, in either order.
P5 stands alone; best after P4, so a tank is already where canisters are taken from.

## Steps, one PR each, in order

### E-1: the electrolyser needs copper
- `Machines.cs`: the electrolyser's cost gains `("copper", 4)`, its electrodes: glass 6, plate 4, tube 4, circuit 2,
  copper 4. Copper comes only of a planet's copper veins, so the electrolyser is understood and built there first.
- The cistern is unchanged: useless alone, harmless.
- Tests: the electrolyser is not understood with every part but copper; with copper too it is.

### P1: a lit fire reserves its share of its room's budget
- **Units.** A burning, unblown fire reserves `Furnace.Breath × Air.LeakEvery / Furnace.BreathEvery` = 1800 units per
  `Air.LeakEvery`: 1,800,000 in the budget's thousandths, 15% of a vaporizer at full power. Reserves are summed in `long`.
- **Where.** The reserve comes off the room's total budget after its networks' shares are summed, never below 0; it is not
  split over the vents. A fire reserves from the room of the cell `Breathe` uses, its first aired cell.
- **How.** `IAtmosphereOps.Reserve(cell)`: Atmosphere reads the rate from `Content.Furnace` itself. An Atmosphere system in
  the construction phase clears the reserves; Furnace, registered after it, calls `Reserve` for each burning fire in the
  same phase, as it reports heat to Crafting and rest to Power. Reserves are a list ordered by cell, `[JsonIgnore]`;
  `Budgets` finds each one's room when it runs, every fill beat, so a wall built this tick counts. Nothing new is saved: a
  loaded world's first construction phase refills them before any budget is read.
- Heroes reserve nothing: standing still costs no room.
- The room overlay's `RoomAir.Budget` shows the budget after reserves; a room whose bubble stops short because of reserves
  says `atmosphere.burning`.
- **Accepted limitation.** In a room with little to spare (room A has some 480 a period) a fire's reserve can take the air
  from its own cell: it goes out (`furnace.noAir`), the reserve lifts, the bubble grows back and it relights, a beat later.
  The overlay says why; the cure is a vaporizer or glass. A reserve that brings a budget to 0 empties the room, as any room
  with no budget does.

### P2: the blown fire
- `furnace`, `burnerGenerator` and `biofuelGenerator` get an input port of `Fluids.LiquidOxygen`.
- **The tanks on its port** are the tanks whose output is on the port's network, the vaporizer's rule; their liquid is
  summed. Vaporizers, run by Atmosphere before Furnace, draw first in a tick.
- **Blown, a saved flag.** `Fire` gains `Blown`, saved, default `false` (no format bump). Each breath decides it: when the
  tanks on the port hold `Furnace.BlownBreath` = 5, the fire takes it from them in tank id order and is blown; otherwise
  it is not, and breathes room air at its plain rate as today, going out with no aired cell (`furnace.noAir`). The flag
  only changes when the tanks run dry or are filled again: it does not flicker.
- **What blown does,** read from the flag the last tick left, as heat is today:
  - a blown fire reserves nothing (P1) and lights with no aired cell when its tanks hold a breath;
  - a furnace's speed × 3 / 2 (1000 → 1500, exact);
  - a generator's output × 3 / 2, rounded down;
  - fuel burns down at two thirds: `Fire` gains a saved counter, default 0, and a blown fire's fuel loses no tick on every
    third. Fuel lit plain and burning on blown, or the other way, simply counts down at the rate of each tick.
- **Ops.** `ICraftingOps.Heat` gains the speed in thousandths; `IPowerOps` gains `Boost(machine, permille)`. Both are
  refilled every tick in the construction phase and `[JsonIgnore]`. `IAtmosphereOps.BurnLiquid(machine, amount) → bool`
  is built on Atmosphere's existing `Draw` and `Gather`, not a second path: it resets the held cache (`_readFor`) as
  `LoadCanister` does and drops the tanks it empties.
- **Accepted limitation.** A blown fire takes from the same tanks as the vaporizers, with no priority: a forgotten furnace
  can drain the ship's air. The furnace's panel says "blown"; the room overlay says `atmosphere.dry` once they are empty.
  A furnace in an old save whose new port lands on a liquid pipe joins that network and starts blowing: accepted, the
  panel shows it.
- Client: a blue flame while blown, "blown" on the furnace's panel.

### P3: hoppers
- `tank` gets a slot taking only canisters, `cistern` one taking only water bottles (`SlotTakes`, as the bay's).
- **Save:** a machine's slots are saved only while they hold something (`Stacks.SetCrate`), and tanks and cisterns hold
  none today, so no format bump. A hopper's item when its machine comes apart goes as any machine's slots do.
- In the simulation phase, after the line pours, each in id order pours a held item when it fits: `LoadCanister`'s and
  `PourWater`'s rules without the hand.
- **Logistics, new rule:** a `SlotTakes` slot wants its item even when empty, under the half-a-stack rule. Today `Wants`
  asks only for what a slot holds or remembers (`sim/Logistics/Jobs.cs`), so an empty hopper would never ask. The drone
  bay keeps its own path (`Salvage.cs`).

### P4: canisters out of a tank
- A command `DrawCanister(Tank)`: takes `Air.Canister` of liquid from the tank and gives the sender a canister. Refused with
  `atmosphere.noTank` (not a tank), `atmosphere.tankLow` (it holds less than a canister) or the items' reason when the
  sender has no room for it. A canister is the liquid in a bottle: no empty canister is needed.
- Like `BurnLiquid`, it resets the held cache and drops a tank it empties.
- Client (decided with the user, 2026-11-08: the client has no hand to tell by): E opens a tank's panel, its hopper a
  slot a canister is put into by hand, and its key `tank.draw` taking one out, dark with why by the pointer.
- What it opens: an electrolyser's oxygen carried to a suit (`Crew.CanisterRefill`), a jetpack, a drone bay or a ship
  far away; and ice sold as canisters at 117 credits a 3000 units. The plan for P4 checks that price against ice's 30.
- Tests: a canister drawn takes `Air.Canister` and gives one; each refusal; drawn and loaded back leaves the tank as it was.

### P5: liquids live in tanks and cisterns
- **Why.** A slot of 20 canisters is 400 units of a hero's air (800 upgraded) against a helmet's 45: carried or crated,
  air never runs short on a sortie or a planet, and a cistern is only a source for pipes.
- **Stacks** (`Items.cs`): `canister` 20 → 5, `water` 50 → 5, `ice` 250 → 50, so ice is no loophole for keeping water.
- **Storage:** `Water.CisternBottles` 8 → 50 (225 000 units, an electrolyser's three hours); `Air.TankCapacity` 36 000 →
  150 000, 50 canisters. More room only: oxygen still comes of canisters and electrolysis.
- **Ice:** `iceVein`'s `Cost` and `crystalIce`'s `Yields` give 4 ice instead of 1.
- **What moves with it,** checked in the step, none changed unless a test says so: a drone carries a stack at most, so
  5 a trip and a hopper kept from 3; a bay's fuel slot holds 5 canisters, 25 minutes of trips; a trade station sends
  fewer canisters a capsule; a pod cluster's 5 to 8 canisters fill one or two slots.
- **Saves:** no format step if Items keeps a stack over its item's new `Stack` as it is till it is moved, and only a move
  splits it; the step checks that with a save from before, and adds a migration only if a load or a move breaks.
- **Tests:** each stack and capacity in `ContentTests`; a cistern takes 50 bottles and refuses the 51st; a tank 50
  canisters; an ice vein broken and an ice crystal felled each give 4; a world saved with a slot of 20 canisters loads
  and its stack moves whole into an empty slot or splits into 5s, as Items decides.

## Not done

- Ice in space, a melter machine (the furnace melts ice and drones feed it already), hydrogen, refilling suits from the
  tanks through a port (an aired room refills them), a liquid port on the drone bay, heroes in the budget.
- A blown fire gives no more heat: Heat 2 stays the smelter's.

## Tests

- Per step, as above; P1 also: a lit furnace shrinks its room's bubble by its reserve, and out it grows back; a room's
  overlay says `atmosphere.burning`. P2 also: a blown furnace lights in vacuum, takes 5 a second and smelts at 1.5×; a
  blown generator burns a plank 1.5× as long and makes 1.5× power; tanks dry, the fire is plain again; a world saved
  blown and loaded goes on alike.
- Invariants: liquid and gas grow only by canisters loaded and electrolysis, and fall by a canister drawn; a budget is
  never below 0; a blown fire never draws room air.
