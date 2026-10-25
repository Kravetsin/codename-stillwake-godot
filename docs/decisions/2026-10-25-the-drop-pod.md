# The drop pod is a bell a hero boards on a launch platform

- Status: accepted
- Date: 2026-10-25

## Context

The launch platform (`2026-10-24-the-launch-platform`) waits for what drops through it. The user chose the bell of the
drop capsule concepts (2C: a dome with a round porthole on a wide skirt, its thruster under it) and asked for its first
slice: a card on the Ship tab, planned only on a launch platform, a hero able to sit in it with nothing happening yet,
and a slot for biofuel.

## Decision

- **A machine with two new traits** (0113): `dropPod`, 4 × 6 cells (the platform's two pieces wide; a hero, five cells
  tall, sits in it), `Seats` and `Lands`. It costs four frames, eight plates, two glass, three tubes, a processor and a
  display: guided as the ship's computer is, so its blueprint opens with the computer's parts analysed, late in space.
  Its card is on the Ship tab after the launch platform. No power: the platform's is what gates a launch.
- **`Lands`: only on a launch platform** (Grid's placement, `grid.noPlatform`): every cell of the row under the footprint a
  platform's, shut or open, so the pod stands over one run. Planning checks it as building does, so a ghost elsewhere is red.
- **`Seats`: boarding is Landing's** (`Seats` saved, by pod): `Board(pod)` and `Alight`, one hero a pod and a pod a hero; a
  seat freed when its pod is taken apart, its peer leaves or its hero passes out. Heroes are client-authoritative (0002): a
  seated hero's client holds it at the pod's floor, takes no keys but E, and hides its body; every peer draws it behind the
  porthole (`dropPod_seated.png`, `PodSeats`).
- **E boards and opens at once**: E on a pod sends `Board` and opens its slot under the inventory, as a crate's; refused
  (`landing.taken`, `landing.seated`), it does neither and says why. Seated, E anywhere sends `Alight` and shuts the slot.
- **Its slot takes biofuel only** (`MachineSpec.Takes`, a list of items; Items refuses the rest `items.notTaken`). No
  `FuelSlot`: nothing burns it yet, and a fire would. Drones do not feed it.

## Deliberately not handled

- The launch, the descent, landers, the return: the next slices (the launch platform design).
- The suit's colour behind the porthole: the seated picture is one hero for every peer.
- Boarding from out of reach is not refused by the sim: the client asks only within reach, and peers are trusted.
- A pod left over a platform taken apart, or opened, stays where it is: nothing falls until a launch.
