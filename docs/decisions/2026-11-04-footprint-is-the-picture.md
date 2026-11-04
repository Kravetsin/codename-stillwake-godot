# A bench's footprint is its whole picture

- Status: accepted
- Date: 2026-11-04
- Partly supersedes [0099](0099-the-arc-bench.md): "the footprint is one row, as the user drew it" (the vise rising over the cells above) no longer holds.

The workbench was raised to the hero's waist by its picture alone: 32×40 over a 4×2 footprint, three cells drawn over
the cells above as the arc bench's vise is (0099). The sim did not know those cells were taken, so the user built another
station inside the bench's picture. The picture's rise was patched into the aim twice (`Risen`) before the footprint itself
was questioned. The user asked for the arc bench to follow.

**The workbench is 4×5 cells and the arc bench 4×4**, their pictures' sizes: nothing is built, planned or placed in any of
them, as with every other machine, and they rise over nothing (`ShipTiles.RiseOf` reads 0).

**A 16 px map draws a machine of even width and odd height a character taller** (`Layout.Doubled`): the top row of cells
of its top character stays open, so it stands on the floor its bottom row drew. The workbench is three `I` rows tall, the
arc bench two `W` rows.

**Saves of format 49 move every workbench up three rows and every arc bench up two**, built or planned (`Migrations`,
49 → 50): each keeps the floor it stood on and grows over the cells its picture covered.

Not handled:

- **A machine an old save built inside a bench's picture** overlaps the bench once upgraded; nothing is released, so no
  save of a player's has one. Take one of the two apart.
- **The drop pod still rises a cell** over cells others may build in: its seat and flight are drawn from its rise.
  `Risen` keeps the aim and the building tools on it.
- **A map at the 16 px scale cannot draw a machine of odd width**; the piles are placed by the wreckage, not drawn.
