# The hull first, no station in a square, plans under the heroes

- Status: accepted; built
- Date: 2026-11-04
- Partly supersedes [driver-icons](2026-11-04-driver-icons.md): the square takes no whole thing at all, not only when the
  pointer's own is one. Partly supersedes [builder-aim](2026-11-03-builder-aim.md): the plans are drawn under the heroes, so
  the aim's outline is over them.

The user, played: beside a station's blueprint the square still showed, taking part of the station; a meteor holing the wall
behind a station planned before it left the hole unpatched until the station was built or cancelled, the oldest plan in the
cell being worked first; and blueprints were drawn over the hero.

**The hull first.** In a cell, a back wall's blueprint is worked before the rest, then the order made (`Aiming.BackFirst`), by
the screwdriver's one cell and by the square alike. A back wall stands behind what is built in front of it, and a hole in it
lets the air out: patching it never waits for a station. Marks keep the order made.

**No whole thing in a square.** `WorkedSquare` drops every target `IsWhole` gives (a station's plan or mark, a piece of several
cells, a repair); a square with nothing else gives none. A station is worked by aiming at it, at one cell, its outline the aim.
So over a station with a holed wall behind, the square patches the back walls and leaves the station; the walls done, the
pointer aims at the station.

**Plans under the heroes.** `Layers.Plans` and `PlanMarks` lie over the world and under the aim's outline, tethers and heroes;
a hero stands before a blueprint. The ghost of a card in hand and the hands' sparks stay over the heroes: they are what the
player does now.

## Deliberately not here

- Ordering by urgency (a room venting first): the pointer already picks the cell; within one, the hull is enough.
- A key to pick a layer for the builder: a key nobody would find, for a case the order settles.
- Marks ordered by layer: taking things apart in either order leaves the same.
