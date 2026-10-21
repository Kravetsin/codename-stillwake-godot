# 0094 — A hit is seen on what it strikes

- Status: accepted
- Date: 2026-10-21

## Context

The hero digs and fells anything within its reach (0077), six cells, while the drill's bit is a cell and a half long. The
drill threw its bite (sparks, dust, chips, a glint, a warm light, `DrillLight`) at its bit whenever it dug, so a block
struck from four cells away was answered by sparks in the air at the bit: "sparks from nowhere", the user said. And
they were metal's sparks on soil and grass alike. Meanwhile the ground's own chips came from the middle of the struck
cell, inside the wall, the same few for every hit of every tool: ten a second under the drill. 0093 made the object
decide how a hit sounds, whatever the tool; this is the same for how it looks.

## Decision

- **Client only, the object decides.** One table by the natural wall's material and one by the growth's kind
  (`Content/HitLooks.cs`, beside `Sound.Ground` and `Sound.Growths`), each naming an `Impact`, and each impact's look:
  its throws (a `Luminous`, a count, a speed, a spread, how far turned up), its glint, shock line and flash. Metal (a
  wreck, the ruins' alloy and metal pieces) sparks and glints; stone throws a few short sparks with its chips and dust;
  soil and plaster crumbs and dust, no sparks; wood splinters, a tree splinters and leaves, a bush or grass leaves; a
  crystal cold glitter, shards and a glint; a glowcap spores; cloth fibres; glass shards and glitter; crockery chips. What
  lies in the rock (`Sound.Unheard`) is struck as its stone. A test fails for a growth with no look.
- **Where it is struck.** A dig's look is thrown at a point on the struck cell (`Impacts.On`): on a wall, its open face
  turned most toward the hero who struck (else the closed face turned toward it, so a wall behind another is struck on
  its top, not inside the ground), at the face's point nearest the hero, kept off its corners; a slope by its diagonal
  too. A growth's at its cell's middle drawn a quarter cell toward the hero. Its chips fall to the floor under that face.
  A wreck's: where a running drill's bit, as drawn, lies on its picture's box (`Ui.Bits`, set by crew), else its side
  facing the nearest hero, as before. Particles come out into the air, so none is drawn behind the walls.
- **The tool keeps only its own.** The drill's bite at its bit is gone; `DrillLight` is the arc drill's coil alone. If
  the bit does not reach the hit, it only spins. The axe's swing keeps its smear. A drill's item card, with nothing to
  bite, throws a wreck's hits at its point at its pace.
- **As dense a second, whatever the tool.** A whole hit is the axe's. A faster tool's hit throws its pace over the axe's
  (`Impacts.ShareOf`): the drill, ten a second, a fifth; the arc drill, five a second, two fifths. Each count is rounded
  by chance so a fifth of three still adds up; the glint and the shock ring come by the same chance; the light flares to
  the share's root, so ten small flares a second hold about the glow of one. The hit that breaks a wall or fells a growth
  throws the whole, at most once an object: a crystal, felled by any hit, shows its glints whatever felled it. All counts
  are times `Fx.Count`.
- **Shown as it is heard.** The crew throws a dig's look as it plays its sound: this hero's as it sends the dig, a
  friend's as its swing is played here or its drill's pace says (0093), so a friend's look comes from what the client
  already knows. A wreck's hit stays where the sim's damage rises (`DebrisView`), for every peer alike. The per-hit chips
  the views threw from the cracks (`DigView`, `FloraView`, `HomesView`) are gone; their shakes, cracks, breaks and falls
  stay.
- `-- --ground=hit-soil` (`-stone`, `-alloy`, `-tree`, `-crystal`) stands the hero at the edge of its reach from one and
  strikes it with the drill, or with `--axe` the axe, for recordings.

## Consequences

- Nine new looks of particles (`FallingSpark`, `StoneSpark`, `Splinter`, `WoodChip`, `Shard`, `Glitter`, `Fibre`,
  `CeramicChip`, `PaleDust`) in the luminous style: thin streaks and fine motes, light added for sparks and glitter.
- `Luminous.DrillSpark`, `Figure.Bites`, `Aiming.Bites`, `GroundLooks.Chips` and `FloraLooks.Knocked` are gone.

## Deliberately not handled

- **Which tool hit a wreck** is still not sent: a hit under a running drill's bit is taken for that drill's.
- **A tree's crown:** a hit lands in its struck cell, which may be the air between branches; nothing reads the picture.
- **A chip past a step's edge** rests on the air at the step's height for its life: a particle has one floor. Chips are
  thrown slow and close for that.
- **A friend's hit refused by the sim** still shows, as its sound is still heard (0093).
