# 0045 — A burner generator shares the furnace's fire

- Status: accepted
- Date: 2026-10-17

## Context

The only power a player could add needed nothing to run, and could not be built until electronics
exist: fuel mattered to the furnace alone. The crafting plan's second task adds the **burner
generator**, the first power the player builds, made of parts (4 plates, 2 gears, a rotor, 4 wire) and
opened by analysing them (0027). It burns as the furnace does; the question was where its fire lives.

## Decision

- **One fire, two machines.** The `furnace` module keeps every fire by machine id and lights a burner
  generator by the furnace's rules (0023): fuel in its fuel slot, air in a cell of it, a breath every
  `BreathEvery` ticks, out with its fuel lost when the air runs short. It smelts nothing, so it needs
  nothing in an input to light. A plank burns in it as in the furnace, 300 ticks.
- **The fuel slot is the machine's**, `MachineSpec.FuelSlot`: the furnace's second, the generator's
  only one. The constant `Furnace.FuelSlot` went.
- **It makes 6 while it burns** (a solar panel makes 5), wherever it stands: no stars needed. Power
  learns this without depending on the furnace: `furnace` rests a generator whose fire is out
  (`IPowerOps.Rest`), and `power` counts no resting maker. The rest is set in `furnace`'s construction
  phase, before power's derivation, so power follows the fire a tick late and a generator just built
  never makes a tick.
- **It burns while it has fuel,** drawn on or not. Taking the fuel out stops it.
- **A fire fed in time relights at once:** its last tick of fuel burnt, the next fuel is lit on the
  same tick, for the furnace too. Without it every fuel burnt out left a tick dark, a lamp blinking
  and the crackle starting over every fifteen seconds.
- Format 8: a fire names its `machine`, no longer its `furnace`; the step renames the key.

## Rejected

- **A module of its own, or the fire copied:** the same rules twice, for one machine; and `power`
  reading a generator module would need a dependency the module order forbids.
- **Burning only on demand:** needs `power` to tell the fire what its network draws, a loop between
  the two modules; the player stops it by taking the fuel out.
- **A hum of its own:** sounds are chosen by the user from candidates; the furnace's crackle plays.

## Consequences

- The generator's fire, embers, light and crackle are drawn by the furnace's client feature, which
  mirrors the module that owns both fires: no shared place was needed.
- Its panel is the furnace's without the input and outputs: the fire over its fuel slot, and a bolt
  with what it makes, lit while it burns.
