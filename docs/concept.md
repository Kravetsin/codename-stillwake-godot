# Concept

A living document: what is decided about the game and what is still open. Technical decisions live in
[decisions/](decisions/).

## Pillars

- Raft in space: an always-moving ship salvages debris on its way. Within a place it only drifts: it
  is never steered or turned.
- Factorio-style automation turns salvage into production that grows and upgrades the ship.
- Side view: the ship is seen in cross-section.
- Online co-op for 4+ players, a hero each, in host-owned worlds.
- A meditative pace with real dangers.

## Decided

- **The arc:** a castaway, alone in a small fragment of a wrecked ship, drifts and survives on what
  floats by; sets up the first processes and builds floor upon floor, filling them with machines.
  The first stage ends with an engine and a radar. The radar finds what happens within its range —
  places and events; the engine takes the ship there. It is a jump between places, never steering
  inside one, and every place is drifted through like the first. From then on the ship is improved
  and production automated, to bring the technology that fends off what is out there and reaches
  further places.
- **Places and their dangers:** the starting space is safe — debris only, now and then a random
  event such as a great cluster of debris or another wreck; rare meteors may come once the ship has
  grown. Harder places bring their own dangers (see meteors below).
- **No hand-holding:** no tutorial text and no highlighted "click here". The player learns by trying,
  picks their own path and pace; the world hints by colour, light and motion only.
- **Start:** the hero is left alive in a wreck, a fragment of a ship, and has to make it livable to
  survive: one small room a weak oxygen generator holds, the lifepod in it, and beside it a room torn
  half away, an analyzer surviving in it. Everything is caught as debris: a furnace built of caught salvage burns
  planks from cargo crates and breathes its room's air, and smelts the rest into materials; a crate
  comes after, of planks and iron ([0016](decisions/0016-breathe-where-the-air-is.md),
  [0023](decisions/0023-a-furnace-burns-fuel.md)).
- **The world** is 2048×1024 cells of space with the wreck in its middle; the ship is built only
  within the middle 1024×512, the build zone, and the space round it is there to fly out into
  ([0048](decisions/0048-a-big-world-a-build-zone-inside-it.md)).
- **Blueprints are learned,** Grounded's way: a blueprint or a craft opens once every item of its
  cost is analysed in a powered analyzer, raw or made. The wreck knows only the furnace, the
  analyzer, the crate, scrap walls and the wire; smelting scrap, making wire, cabling the torn room's
  analyzer and analysing iron is the first lesson. Only what goes into some cost is analysed; tools,
  the jetpack and anything else that goes into nothing are known from the start. An item not analysed
  yet has a violet "?" in its slot. The analyzer's screen shows a check when done, and its panel every blueprint and craft the
  item in it goes into, opened or still closed; nothing pops up over the hero. Every electric thing
  is built of circuits: some salvaged from the electronics racks that drift by, most made at the circuit
  assembler of silicon from asteroid ore and wire: the battery and the charging station a circuit each, the drill
  two. The solar panel is built of solar cells, silicon and glass (smelted on from silicon in the electric smelter)
  assembled with wire, so a first new panel takes the whole chain
  ([0024](decisions/0024-blueprints-are-learned-by-analysis.md),
  [0027](decisions/0027-what-is-analysed-opens-what-is-made-of-it.md),
  [0041](decisions/0041-wreckage-of-many-kinds-and-circuits.md)).
- **The lifepod,** as Subnautica's Lifepod 5, is the floor that is always there: built into the ship,
  never taken apart. Heroes are made at its hatch, at the start and after dying, with a full
  suit; standing by it, a suit slowly fills with the pod's own air, even in a ship open to space, and
  the room gets none ([0028](decisions/0028-the-lifepod-and-crafting-by-machine-only.md)).
- **Parts are made by machines only,** as in Subnautica and RimWorld: wire of scrap; plates, gears and
  tubes of iron; and of those parts a rotor (a gear and wire), a frame (plates) and a coil (wire). A
  recipe shows what a thing is made of: the harpoon is a tube, a coil and its line of wire
  ([0046](decisions/0046-recipes-show-what-a-thing-is-made-of.md), [0068](decisions/0068-the-harpoon-before-the-jetpack.md)). The first machine is the pod's
  Fabricator, opened with E under the inventory, where every machine opens ([0058](decisions/0058-one-place-for-every-machine-window.md)): no power, a fifth of full speed, a queue the crew
  shares, paid when queued, made one at a time into its outputs. Most builds cost parts, so the
  furnace feeds the Fabricator before it feeds the hull. Machines for parts the player builds are
  powered, faster and with longer queues: the Former first, presses for the parts at five times the
  pod's speed; then the electric smelter, twice the furnace's speed and hotter: what needs more heat than
  a machine has goes in, and the machine shows it is too cold, cold blue, rather than refusing it; then the
  circuit assembler, circuits of silicon and wire and solar cells of silicon, glass and wire
  ([0057](decisions/0057-stations-one-model-for-every-crafting-machine.md)).
- **The farm** grows what the ship burns: seeds come from supply pods, one or two in every other pod, and from
  soil clods, earth torn from some ship's farm drifting by now and then with a sprout, broken into one to three
  seeds and a little soil; soil is pressed at the Former of asteroid ore and planks; a grow capsule, a glass capsule on a base built of soil, glass, a frame,
  a circuit and wire, grows a planted seed under its pink lamp on power in eight minutes, the plant rising through
  four pictures, into 4 biomass and none, one or two seeds, so the farm slowly eats its seeds and salvage refills
  them. Biomass smelts into biofuel at the furnace or the smelter, the fuel the engine will burn on the first jump
  ([0060](decisions/0060-the-farm-a-grow-capsule-is-a-fed-station.md)).
- **The engine** is the last thing built, of a planet's parts: a nozzle of the ruins' alloy and copper, a fuel pump round a
  motor of the deepest magnetite, an igniter of a crystal. It stands at the stern, its nozzle out through the hull into
  space, its tank taking biofuel alone, cabled to power; it does not fire yet, the jump to come
  ([the-engine-through-the-hull](decisions/2026-10-25-the-engine-through-the-hull.md)).
- **Spacewalks:** on a tether from the hull at first, swimming slowly; an oxygen jetpack worn on the back
  frees the hero from it, its thrust paid in the suit's air, and canisters breathed refill the suit
  ([0047](decisions/0047-equipment-the-jetpack-and-the-slower-swim.md)).
- **Catching:** by hand on the tether, within reach; farther out, a hand harpoon made at the Fabricator
  shoots a rod on a line up to 20 cells into a piece, small or a wreck, and a second click reels it in to just before
  the gun, to catch with E or chop with the axe; it fires again 5 s after its shot
  ([0044](decisions/0044-the-harpoon-reels-a-piece-to-a-point.md)).
- **Wreckage** drifts among the small debris, too big to catch: a hull section, a cargo container,
  a machine block, an electronics rack, oxygen tanks, a shield slab and, rarely, a soil clod, each broken with the fire axe
  into what its picture shows it holds, floating where it was
  ([0034](decisions/0034-wreckage-and-the-axe.md), [0041](decisions/0041-wreckage-of-many-kinds-and-circuits.md)).
- **Death** is a suit run dry: the hero wakes at the pod with its tools, its jetpack and its batteries, and
  everything else it carried lies where it fell, a body in its suit with a beacon blinking, pointed at from the
  screen's edge. Anyone may pick it up, as any stack; out in space it drifts with the rest and leaves the world
  at its edge, and wherever it is, it is gone ten minutes after the death
  ([0061](decisions/0061-death-leaves-a-body.md)).
- **Dangers:** yes — the game can hurt you. Which ones is open.
- **Time runs only while the game runs.** Nothing happens offline and there is no offline progress,
  unlike the Factory in Tossit.
- **Characters** are assembled from parts with simple keyframed movement; the parts exist mainly for
  customization ([0003](decisions/0003-character-rig.md)).
- **Pixel art,** a pixel of art a pixel of the world, eight to a cell (0104; drawn at sixteen till redrawn), on trial until the team
  settles the style: doors, pipes and machines are tiles and pictures (first drawn as text in the
  TypeScript prototype, now PNGs in `game/art`); a wall is cut from its material's one picture where it stands, so walls
  side by side make one surface; the hero's parts are pictures too, moved and turned smoothly by
  its rig.
- **Look: simple textures carried by light.** Light also tells the ship's state: an unpowered room is
  dark but for helmet lamps, a venting room flashes red, starlight comes in through windows.
- **Oxygen works like a power grid.** A pipe network holds one pool of oxygen; a vent of a network
  with oxygen in it fills its room cell by cell, a unit of oxygen a cell, outward from the vent. A
  room is a space closed by walls and closed doors around it and back walls behind it, recomputed
  only when something is built, broken or a door moves; a cell holds air or not, and a room's air
  gathers around its vents and stays where it is ([0011](decisions/0011-air-lies-around-the-vents.md)).
  A hero breathes room air standing where it is, and the suit refills there; the tanks behind
  the room's vents pay for each breath, and without them the breath takes a cell from the patch's
  far edge. A breach or a door left open to space makes a room vent: its air goes and its network
  drains fast until a valve cuts the room off or the hole is patched. A door opened onto an empty
  room joins the two, and the vents fill the rest through the doorway. A room leaks a little all the
  time through what its air touches, the back wall behind each of its cells and every outer hull cell,
  each as its material decides ([0020](decisions/0020-walls-of-materials-front-and-back.md)), paid like a breath: from the tanks,
  or from the room's own air without them. Growth is held back by the fill, the leak and breathing. Without air a hero breathes from the suit tank.
- **Power runs on cables** laid point to point between machines and anchors, hanging inside the
  ship and taut out in space; a network short of power slows everything on it alike. Solar panels
  work only under the stars, out in space with no back wall behind them ([0043](decisions/0043-a-solar-panel-needs-the-stars.md)); the oxygen generator is the first thing that needs power
  ([0017](decisions/0017-power-runs-on-cables.md)). The first power the player builds is the burner
  generator, of plates, gears, a rotor and wire: the furnace's fire turning a flywheel, it burns
  planks and breathes its room's air, and makes power while it burns, wherever it stands
  ([0045](decisions/0045-a-burner-generator-shares-the-furnaces-fire.md)).
- **Building is cell by cell, in layers.** Structure: hull walls of different materials, floors,
  ladders, doors, airlocks. Back walls, of the same materials: behind every cell of a room, or it is
  open to space.
  A cell is 8 px; doors are built of 2×2 segments, hatches of 2×1 ([0104](decisions/0104-the-cell-is-8-px.md)).
  Networks: pipes and cables are straight spans between joints, pegs and machines' ports, laid at
  once and passing through walls ([0109](decisions/0109-pipes-are-spans.md)). Machines stand on floors. Materials differ in strength
  against meteors, air leaks and cost. The plan (B) works on one layer at a time, the front or the
  back walls, switched with the middle button: the left button plans, the right marks for taking
  apart ([0110](decisions/0110-the-plan-has-a-focused-layer.md)). With it put away, the screwdriver held on a plan
  builds it by hand, putting in an item from the hero's pockets as each part of the work begins,
  and the pale plan turns into the real thing a part at a time, bottom up, a floor or a pipe from the
  left ([0063](decisions/0063-blueprints-are-paid-a-part-at-a-time.md)). Run out and the work stops
  where it is. Crafted tools will build faster than bare hands; drones come later.
- **The screen speaks without words.** What is always on screen shows the ship's state by colour
  and motion: a room losing air pulses red, pipes carrying oxygen glow, a low suit reddens the
  screen's edges, air escaping through the hull is seen leaving. Text is on demand, in overlays and by
  the pointer.
- **Light comes from sources, air has a view of its own.** Lamps on the power network, helmet lamps
  and starlight through windows light the ship; a dark room means no light, never no air. Air is
  seen in its own view, as in Oxygen Not Included: the air overlay, cell by cell.
- **Meteors belong to places.** The starting space is calm; meteors come where the ship goes, such as
  the rings around a planet. A place throws meteors of some power, and a hull cell breaks only if the
  power reaches its material's strength: a strong hull is safe early and gives way in harder places,
  which asks for a better one.

## Open

- Which dangers besides meteors, how often, how they are telegraphed; a peaceful mode as in Raft?
  Directions so far: enemies living in other wrecks, pirate raids.
- What the jump between places costs in biofuel, and how the radar shows where it goes.
- How resources move between machines: belts, drones, or hired crew carrying them. In side view
  this shapes the ship's layout; carrying by hand comes first, so automation is felt as relief.
- Co-op start: one shared wreck, or separate wrecks that dock later?
- Art direction: style and mood; where sprites and character parts come from (drawn or rendered from
  3D).
- Which customization is cosmetic and which comes with equipment.
- What else catches: a harpoon or hook on the deck; drones later?
- Planets: landing as in Starbound, for new resources and maybe growing plants; what it gives and how
  it works.
- Parallel activities for 4+ players on one ship.
