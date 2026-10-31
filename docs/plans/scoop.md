# The scoop: a new starting ship

Steps 1 and 2 have landed: [the-scoop](../decisions/2026-10-31-the-scoop.md), which also says how the pod's air differs from the text below, and [hold-piles](../decisions/2026-10-31-hold-piles.md). Step 3 has landed too, in the-scoop. Piles break by axe, not by a mark, and so no longer pay for the first machines: hold-piles has it.

A new world starts in the torn-off scoop section of a scavenger ship. The hold's heaps are what its last crew had gathered. Picture:
[scoop.png](scoop.png), 2× zoom, about 66×33 cells, drawn from the game's art, not yet in the game.

Rests on [first-hour-pace](../decisions/2026-10-31-first-hour-pace.md) and [black-market](../decisions/2026-10-31-black-market.md).
Written by `design-author`, cut down by `design-reviewer`, then changed by the user; its claims about the code were checked by reading it, and
nothing but the swim fix has been run.

## What the player meets

1. The hero wakes by the pod in the iron room. Nothing else stands in it. The pod gives a soft light and makes the room's air, which fills in
   about 7 s.
2. **Right:** two doors, then open space on a tether, the debris stream coming straight at them. They catch by hand.
3. **Left:** two doors, then the dark hold under a red lamp, its piles of junk. They mark a pile with the screwdriver and work it, as they
   take apart anything, and its items land in the bag.
4. Items from the piles build the trade station, the furnace and the workbench. The piles pay for the three, but not for the 400 credits of
   their blocks, so the player must catch debris.
5. **Going wrong:** both doors of a lock open: the room's air goes at once, and returns once a door is shut. A furnace built in the hold will not
   light: it says so, as today. A hole cut in the room: it vents until walled up, with what the cutting gave back.

## Decisions

**The pod makes air, as a first generator.** It is a machine with `AirSource` and `Vents`, at `Air.GeneratorRate`, no power and no pipes; it is
`Fixed`, so it can never be taken apart, and so the room can always get its air back. This closes the dead ends with no other change: a fire
still needs air, and the pod's room always has it. `Air.cs:391` multiplies a generator's rate by its power's satisfaction, so a source that needs
none is the one code change; whether one machine forms a network by itself is for the plan (`Atmosphere.Fed`). It makes about 1 unit a tick against the
room's leak of about 0.45, so sealing the hold too is about what it can hold (about 280 cells); a bigger ship needs the real generator.
The pod's light is client-side, and needs no power.

**The room is empty.** No burner, generator, vent, lamp, cables or starting fuel. Nothing needs power before the `machinery` ring, which opens the
burner and the former together, so **the burner stays there**, unchanged. **The lamp moves from `firstMetal` to `machinery`:** before then it could
never be lit.

**A pile is a machine row with no menu card, taken apart by the ordinary mark.** `Cost` is what it holds, `BuildTicks` its work. A mark already
keeps its progress, pays once and goes twice as fast with two heroes. It pays on its last tick, so piles are small, 6 to 8 items each, standing side
by side; nothing is stacked, so nothing can float. A pile does not block a body, as no machine does; it holds the build slot, which is the reason
to clear it. Code: `Blueprints.cs` lists machines by `Menu != null`, not `!Fixed`.

**Stock.** In all 16 chunks, 8 plates and 6 planks over 4 or 5 piles, about 760 credits, about a minute of harpoon output. The three machines
cost 12, 6 and 4. Numbers in `sim/Content`.

**One layer of real iron round the room and its locks; everything else is scrap.** Walls refund as they do anywhere. Back walls are scrap too. A player
may take the ship apart for resources: that is logical, and they pay with their own hull. In numbers, from the sketch: about 100 iron walls are 100 plates,
past the press and machinery gates (8 and 18). About 430 scrap walls are 430 chunks, about 8 600 credits, at 4 ticks each. We watch this in the playtest.

**Four doors, nothing new.** Each lock is an ordinary room, empty at the start, filled through its inner door. Doors in a pair are parted by a
wall, as `PieceOf` pairs a run from its left end.

**The hold is space.** A hero floats in it and on the landing (`Rooms.cs:106`), on the tether, as outside today, and swims through its deck
([fix in PR 293](https://github.com/Kravetsin/codename-stillwake-godot/pull/293)). Sealing it is ordinary building, walling the throat and patching the
bite with `wallScrap`, about 50 chunks, and the reward is real: gravity, air, floor. There is no repair mechanic and there are no dead rooms to restore.

**The map is per cell, without `Doubled`** (`Layout.Parse` needs none, `Registry.cs:117`), at sketch +(50, 25), which keeps the pod, room A and the
lock doors on today's cells, so most tests survive. It uses a materials map, as a wreck in plain scrap cannot show iron (`WithMaterials`, `Legend.Materials`). A
test holds every hold cell `Afloat`: a one-cell slit would turn the hold into a room.

## Deliberately not handled

- **Fires without air:** kept. The furnace's and burner's need for air is a mechanic of the game.
- **Piles shrinking in stages:** a pile goes whole when worked. Stages would need a new command and saved progress for looks only.
- **The jaws catching debris:** scenery. The stream and its rules do not change.
- **A scrap tank:** the pod does its job.
- **A full bag in the hold:** the rest drifts off, as any drop in space. Piles are small and the bag is large.
- **Old saves:** they keep their wreck, as `World.Load` does not read the layout, and their pod does not start making air unless it is a change of the row: then
  it does, which only helps. No new saved state beyond the pod's generator entry, no format bump expected; the plan confirms it.

## Risks

- **Taking the ship apart pays too well** (above): about 8 600 credits in about 90 s of work against a first hour of catching. If the playtest shows it, a no-refund
  `wreckScrap` material is one row, as `plaster` has.
- **A cramped room:** 10×8 cells beside the pod for a furnace, a station and a bench. Intended as a push to seal the hold; the station needs no air, so it may stand
  elsewhere.
- **Debris density:** the hull is taller (14 to 33 rows), so the stream spreads over about 81 rows against 62, about 24% thinner. Measure it in the
  playtest; `Debris.Margin` is the dial.
- **Test churn:** `WreckTests`, `PowerTests`, `RoomsTests`, `ContentTests`, `StructureTraitsTests`, `HeroTests`, `JetpackTests`, `BuildMenuDataTests`, and the demos on the
  wreck (`FurnaceDemo`, `DebrisDemo`, `WreckScenarios`, `HeroAutopilot.Wreck`).

## Order

1. **The ship:** the map, the pod's air and light, the lamp's ring, tests. Playable by catching alone, seen in the game at once.
2. **Piles:** rows, script-made pictures from `art/debris`, their stock, the ADR, the concept.
3. **Looks:** rust, stripes, tears, arcs, the thread and the wires.

## Not verified

Cell counts come from the sketch and the room's leak and pile footprints are by hand; count them from the final map. The first-ten-minutes clock is arithmetic, not played.
