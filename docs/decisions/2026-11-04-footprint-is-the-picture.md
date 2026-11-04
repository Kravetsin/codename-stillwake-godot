# The workbench's footprint is its whole picture

- Status: accepted
- Date: 2026-11-04

The workbench was raised to the hero's waist by its picture alone: 32×40 over a 4×2 footprint, three cells drawn over
the cells above as the arc bench's vise is (0099). The sim did not know those cells were taken, so the user built another
station inside the bench's picture. The picture's rise was patched into the aim twice (`Risen`) before the footprint itself
was questioned.

**The workbench is 4×5 cells**, its picture's size: nothing is built, planned or placed in any of it, as with every other
machine, and it rises over nothing (`ShipTiles.RiseOf` reads 0).

**A 16 px map draws a machine of even width and odd height a character taller** (`Layout.Doubled`): the top row of cells
of its top character stays open, so it stands on the floor its bottom row drew. The workbench is three `I` rows tall.

**Saves of format 49 move every workbench, built or planned, up three rows** (`Migrations`, 49 → 50): it keeps the floor it
stood on and grows over the cells its picture covered.

Not handled:

- **A machine an old save built inside a bench's picture** overlaps the bench once upgraded; nothing is released, so no
  save of a player's has one. Take one of the two apart.
- **The arc bench's vise and the drop pod still rise** (two cells and one) over cells others may build in: the vise as the
  user drew it (0099), the pod with its seat and flight drawn from its rise. `Risen` keeps the aim and the building tools on them.
- **A map at the 16 px scale cannot draw a machine of odd width**; the piles are placed by the wreckage, not drawn.
