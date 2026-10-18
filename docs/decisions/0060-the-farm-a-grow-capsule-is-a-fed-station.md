# 0060 — The farm: a grow capsule is a fed station

- Status: accepted; amends [0057](0057-stations-one-model-for-every-crafting-machine.md) with a fed station's process
- Date: 2026-10-18
- Design: "Stage 1 — wreck to radar, farm and first jump", slice 1, with the user's decisions of 2026-10-18

## Context

Stage 1 sends the ship on its first jump, and the jump burns fuel the ship must make: something has to grow
aboard. The farm needs a machine that takes a seed, works on it for minutes on power, and gives back biomass;
biomass is then smelted into biofuel. The fed stations (0057) already take an item into an input, work it at a
speed times their energy's share, keep their progress when stopped, rest when idle or full, and have a panel. What
they could not do is tell a seed from rock: every fed station smelted whatever had yields, so a furnace would have
"smelted" a seed and a capsule would have grown rock. The crusher and the reactor to come have the same need.

## Decision

- **A fed station has a process,** `Station.Process`, and so does an item, `ItemSpec.Process`: one string each,
  "smelt" by default, matched by equality. A fed station works only what is of its process; anything else sits in
  its input as glass sits in a smelter: taken in, never worked, no complaint, no fire lit for it. No `Process`
  type, no table of yields by process: the item's `Yields` and `SmeltTicks` stay its yields and work wherever it is
  worked. Its named next users: the crusher and the reactor.
- **The grow capsule** is a fed station of process "grow": 2×3, an input and two outputs, draws 2, speed 1000,
  heat 1. It costs a frame, 2 glass, 4 soil, a circuit and 2 wire, and opens by analysis (0027). A seed takes
  9600 ticks, eight minutes, at a full share; short of power it grows at its network's share; unpowered it pauses
  and keeps its growth; with no room for a whole crop it pauses and rests. It is sealed and needs no air. All of
  this is the fed station's model unchanged: the capsule is content, art and a line in each client table.
- **Seeds are consumed** (the user's decision): planting uses the seed; a crop gives 4 biomass and none, one or two
  seeds, weighted 35, 50, 15, so 0.8 a crop on average and the farm slowly eats its seeds. The weights are the
  item's (`ItemSpec.Returns`, entry n the weight of n back), drawn from the world's Rng as the crop completes, in
  machine id order; an item without them draws nothing, so no other smelt moved the Rng. `IItemsOps.Smelt(id, back)`
  puts the yields and the seeds back into the outputs at once.
- **Room for the most:** a capsule grows only while its outputs take its yields and the most seeds it may give
  back (`IItemsQueries.CanSmelt`), so a crop never completes into a full slot whatever the draw.
- **Seeds come from supply pods** (the user's decision): one or two in every other pod. Nothing else gives seeds.
- **Soil** is pressed at the Former of a rock and a plank in 60 ticks, a new row of its own; the pod's list stays
  closed (0057). **Biomass** smelts into biofuel, one for one, heat 1, 160 ticks, at the furnace or the smelter.
  **Biofuel** has no use yet: the engine (slice 3) and a biofuel generator (slice 5) will burn it.
- `ICraftingQueries.WorkOf(machine)`: the work one of a fed station's input takes there, null for nothing of its
  process. The furnace's panel, the smelter's and the capsule's bar, and the capsule's plant read it, so a seed
  in a furnace no longer reads as a smelt waiting for room.
- **The client:** a glass capsule on a base, a soil bed in it, a grow lamp under its cap. While it grows
  (`Works`) the lamp is pink and lights the room pink; otherwise it is dark. The plant shows the quarter of its
  crop it has grown to, in four pictures, working or paused; an empty capsule, or one holding what it does not
  grow, shows none. Its panel is the smelter's with a pink mark, a green bar and two outputs; its empty input shows a
  faint seed (`SlotBox.Ghost`, the one item of its process: the engine's fuel slot is its next user). No sound
  yet: it waits for the user's audition.

## Rejected

- **A table of yields by (process, item):** two places for one item's yields; one string on each side says the
  same while an item has one process.
- **Seeds returned whole** (the reviewed draft): the user wants the farm to need salvage now and then.
- **Seeds drawn by the client or at planting:** only the crop's completion is a moment every peer agrees on.
- **Checking room for the seeds actually drawn:** the draw is made as the crop completes; checking for the most
  keeps the rule "work only with room" without drawing early.
- **Under the stars without power** (in the draft): not now; the user may want it later, slower.
- **A speed-up for the testbed:** there is no dev time mechanism, and the user said not to add one for this.

## Consequences

- A furnace takes a seed and a capsule rock without a word; the player sees nothing happen and moves it.
- Every fed station's outputs must take its process's yields with their most returns (`ContentTests`).
- No save format change: a smelt already keeps machine, item and progress; a station's process is content.
- The testbed is six cells longer: a grow capsule growing seeds on the assembler's panel and a second crate of the
  farm's items, rock and planks, the first crate being full.
