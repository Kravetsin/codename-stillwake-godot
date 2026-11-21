# Design and plan: every hand weapon its own character

Written by `design-author`, cut by `design-reviewer`. It needs `feat/hit-response` merged first: it builds on the blade
trail ([blade-trail](../decisions/2026-11-10-blade-trail.md)) and the 75% tools ([tool-size](../decisions/2026-11-10-tool-size.md)).
Client only. The design proposes candidates, and the user picks every look, sound and motion in play or on pick pages
(CLAUDE.md, "Art and sound picks").

## Problem

The weapons borrow from each other, quietly, through fallback branches:

- The pickaxe swings with the axe's whoosh (`Sound.SwingOf`, "any other the axe's"). It bites a wreck with the axe's chop
  (`WreckHitOf`).
- The harpoon's rod bites with the chop too.
- The grapple fires, bites and reels with the harpoon's sounds (`S.Shot`, `S.Chop`, `S.Drum`).
- The railgun's slug lands with the nailer's thud (`ShotEnds`).
- Every gun throws the same puff (`RodLight.Fired`) and moves no arm when it fires.
- The axe, pickaxe and sledgehammer hang alike.

In Warframe no two weapons move, sound and look alike in use. Here several do, and play feels dull.

## Player experience

1. The player takes up the sledgehammer. It hangs head down by the boot. A slow heave, its own whoosh, the blade's haze broad
   and slow, its own big blow on a wreck.
2. The pickaxe hangs spike down. Quick pecks with a whip of their own (or none), a thread of haze, and a high pang of its own
   on a wreck.
3. The railgun charges and cracks, and the elbow is thrown back. The rails glow and fade. The slug lands with its own punch,
   not the nail's thud.
4. The grapple fires with a heavy thunk and its arm jolts slower than the harpoon's. Its claws clatter and catch, and its
   crank ratchets as it pulls.

Where it stays alike: the axe and the spear struck into stone sound the same, because the ground speaks for every tool
(0093). They differ in the stroke (motion, whoosh, haze or none) and on wrecks.

## Signatures, weapon by weapon

`=` means keep it, it is already its own. `Δ` means change it. `pick:` lists the candidates, and the user may always keep
today's.

**Rest.** Sound is silent for every weapon (accepted). Effect is as today: lamps off, loaded and charged pictures.

| Weapon | Motion at rest |
| --- | --- |
| Fire axe | = square hang (`Hold` π/2) |
| Pickaxe | Δ pick: spike down by the boot / today's |
| Sledgehammer | Δ pick: head down, the block by the boot / today's |
| Spear | = across the fist (`Rest`) |
| Drill, grinder, cutter | = pistol hang; the pictures tell them apart |
| Nailer, railgun, harpoon, grapple | = pistol hang; the loaded and charged pictures tell them apart |

**Stroke** (a swing, a thrust, a run or a shot).

| Weapon | Motion | Sound | Effect |
| --- | --- | --- | --- |
| Fire axe | = chain / overhand / chop | = axe-swing | = haze off a 9 px edge |
| Pickaxe | = peck | Δ pick: a short thin whip / a stick's flick / none (0.3 s pace; the hit speaks) | = haze, a thread off the spike |
| Sledgehammer | = heave | = «slow stick» | = haze off the 10 px face, slow |
| Spear | = jab | = «bamboo» | = no trail (the rule for aimed tools) |
| Drill | = aimed, bit spinning | = whir grains and motor | = sparks |
| Grinder | = aimed, wheel spinning | = run and bite | = haze round the wheel |
| Plasma cutter | = steady; the jet is its motion | = strike, hiss, off | = violet jet |
| Nailer | Δ kick: a small snap of the wrist up | = shot | Δ pick: today's puff at half / a side jet of dust / none |
| Railgun | = charge; Δ kick: the elbow thrown back hard | = charge, ready, crack | Δ pick: the rails glowing and fading / today's puff |
| Harpoon | Δ kick: a spring jolt | = shot | = today's puff |
| Grapple | Δ kick: the harpoon's jolt, heavier and slower | Δ pick: a heavy thunk and the spool rattling / a spring twang and a gear clack / a deep pneumatic chuff | = today's puff |

Each gun's kick is one number: the radians the elbow is thrown and eased back. The user sets the sizes in play.

**Hit.** Every melee hit keeps the shared 50 ms arm stop (accepted). Its look is the object's own (0094).

| Weapon | Sound on a wreck (the ground's own elsewhere, 0093) |
| --- | --- |
| Fire axe | = chop |
| Pickaxe | Δ pick: a spike's high pang / a punch into sheet steel / a dry tink |
| Sledgehammer | = hammer blow |
| Spear | = spear hit |
| Drill, grinder, cutter | = chop under the motor / bite / the jet |
| Nailer | = nail hit |
| Railgun | Δ pick: the crack of a punched plate / a hiss of hot metal / a dull punch-through |
| Harpoon | Δ bite pick: a barb thunking into sheet / a rivet clank |
| Grapple | Δ bite pick: claws clatter and catch / an iron hook's clank. Δ reel pick: a ratchet crank / a winch whine |

**After.** No two weapons share a cue or a muzzle look in the same moment. These overlaps are accepted:

- silence at rest;
- the pistol hangs;
- the shared arm stop;
- the ground's own sounds, as 0093 has them;
- the object's hit looks, as 0094 has them.

## The shared module: the weapon's row

Principle 2. A weapon's character is its row in the table of its kind. Each of today's three kinds already has a row:

- **Struck tools: `Stroke`** (`Strokes`). It holds the motions per sector, the look and the pace. It gains `Whoosh` (a cue,
  null for none) and `WreckHit` (a cue).
- **Guns: `GunFeel`**, new, in `game/Content/logic/GunFeels.cs`. It holds `Shot`, `Hit` (the slug's or nail's landing, or
  the rod's bite), `Reel` (null for a gun with no line), `Kick` and `Muzzle` (a puff or the rails' glow). There is one row
  for each of the four guns. The rods' cues sit there and not on `HarpoonShot`, because the nailer and railgun need the
  same three things.
- **Run tools: as today.** The drill, grinder and cutter already have their own loops (`RunSounds`), sparks (`ToolSparks`)
  and pictures (`HeldLook.Spin`, `Lamp`). Nothing of theirs collides, so they get no new row.

What goes:

- the three fallback switches, `SwingOf`, `WreckHitOf` and `FiredBy`, whose silent `: Chop` / `: Swing` / `: NailShot`
  arms are how weapons borrow;
- the one-player-per-cue fields that mirror them (`_hammerSwing`, `_spearThrust`, `_spearHit`, `_hammerBlow`,
  `HitBy`'s switch).

A row's cues play through `Voices` (`game/shared/Voices.cs`), which plays any cue. So a new weapon or cue needs no new
player field. Reuse first: `Voices` already plays `ShotEnds`' hits.

**The blade trail is the shared layer over every swung row.** The form comes from the row's `HeldLook` edge. Matter and
light laid over the form come from the stack's modules, and later from a skin, through one slot beside `MountFx`. No row
holds a trail colour, so a module or a skin never edits a row.

**Users now:** the eleven weapons.

**Users next:**

- the salvage tools waiting for a design (a lance, a bow grinder, a cutting charge): each is a row of its kind;
- skins: a skin swaps a row's cues, muzzle and trail layer.

A skin is the reason the cues move into the rows and out of `Sound`'s switches.

## Out of scope (deliberately)

- **The sim:** damage, pace, reach, rays, saves, commands.
- **Hit looks per weapon:** point sparks, haze pulses, dust skirts, glowing faces, nails left sticking. The object decides
  how it looks hit (0094). Some of these come close to the rejected "particles off the blade". Revisit this once the
  sounds and kicks are in, if weapons still read alike at the hit.
- **Accents under the ground's sound**, which would mean superseding 0093. Only the pickaxe and the sledgehammer would use
  them, and both get their difference on wrecks.
- **A drill jitter and a grinder lean in the hand.** A jitter reads as shake (rejected), and a lean is a new pose
  (rejected).
- **Equip and holster motions, idle fidgets, sounds at rest, combos, new sector motions.**
- **Anything already rejected:** fans and cones, wide swings, rebound by material, particles off the blade, screen shake,
  arm stops over 50 ms, schematic outlines, poses reworked from scratch, feel tuned per module (0098).
- **Trail layers from modules.** The arc fits no swung tool (its `coil` mount is on the drill, grinder and spear), so the
  slot is named, not built.
- **The blasting charge, the screwdriver and the power driver.**
- **New held pictures.**
- **A friend's whoosh.** It stays silent, as today. A whoosh for every friend doubles the noise four players make.

## Determinism and co-op

Client only. Nothing enters the sim, nothing is sent and nothing is saved.

A friend's weapon plays from what is already sent, on its figure as drawn here (0002):

- its item;
- its `Busy`;
- its rod.

The friend's own client fires the friend's kick. Here it is drawn from the shot heard, with no kick sent. Floats and the
client's random picks are fine, because every piece is presentation.

## Trade-offs accepted

- **The kick plays afloat on top of the body's recoil (`Hero.Kicked`).** The kick moves the arm and the recoil moves the
  body, so they add up as a real shot's would.
- **An aimed tool's stroke has no look of its own**, so the spear and the run tools differ by motion and sound alone.
- **Two pick pages and one in-play comparison cost the user's time.** They are batched.

## Plan

### Step 1. The rows, with today's cues (no audible change)

1. `Stroke` gains `Whoosh` (`S.Cue?`) and `WreckHit` (`S.Cue`).
   - Axe: `S.Swing` and `S.Chop`.
   - Sledgehammer: `S.HammerSwing` and `S.HammerBlow`.
   - Spear: `S.SpearThrust` and `S.SpearHit`.
   - Pickaxe: `S.Swing` and `S.Chop`, for now.
2. Remove `Sound.SwingOf` and `Sound.WreckHitOf`. Their callers read `Strokes.Of(tool)`:
   - `HeroView.Hands.cs` (the whoosh);
   - `DebrisView.HitBy`;
   - `GroundSounds` (`WreckHitOf` at the wreck and wall branches).

   An electric tool's wreck hit stays where it is: `ChopsWrecks` and `_drillChop`.
3. Play the whooshes and wreck hits through `Voices`. Drop `_hammerSwing`, `_spearThrust`, `_spearHit` and `_hammerBlow`.
   - Keep each cue's voice count: the axe's whoosh 2, the chop 4.
   - Keep the Suit bus.
4. Add `GunFeel` (`game/Content/logic/GunFeels.cs`) with `Shot`, `Hit`, `Reel`, `Kick` and `Muzzle`, and
   `GunFeels.Of(item)`. Its rows hold today's cues and a kick of 0.
   - Nailer: `NailShot` / `NailHit`.
   - Railgun: `RailShot` / `NailHit`.
   - Harpoon and grapple: both `S.Shot` / `S.Chop` / `S.Drum`.
5. `FiredBy` keeps only the thrown charge. The gun shot (`HeroView.Guns.cs`), the rod's shot, bite and drum
   (`HeroView.cs`, `HeroView.Others.cs`) and `ShotEnds` read `GunFeels.Of`.

   `ShotEnds` is told the projectile's gun. A shot already carries its tool, so this needs no new data on the wire.
6. Tests (`game.tests`, logic only):
   - every weapon in `Weapons.All` not run on the belt (its item has no `Draw`) has a `Stroke`;
   - every gun has a `GunFeel`;
   - no two rows share a non-null cue in the same field (whoosh, wreck hit, shot, hit, reel).

   The test starts with a list of today's collisions (axe and pickaxe; harpoon and grapple; nailer and railgun hit; chop
   as a bite) and asserts that list exactly. Step 2 empties it.

   Grep proves nothing else calls the removed switches.

### Step 2. The sound picks

7. Collect CC0 candidates per `CREDITS.md`, three or more for each subject:
   - pickaxe whoosh (with "none");
   - pickaxe wreck hit;
   - railgun hit;
   - harpoon bite;
   - grapple shot, bite and reel.

   Put them on two published pick pages, each with a `db`: one for the pickaxe and railgun, one for the harpoon and
   grapple. A subagent writes the pages, the main session publishes them, and the picks are read with `ArtifactData`.
8. Cut, level and credit the picks: each beside the cue it replaces, at its level, with its peak (`PeakS`) for a whoosh.
   Fill the rows. The collision list in the test is now empty.

### Step 3. Motions and muzzles, picked in play

9. **Rest hangs.** The pickaxe and sledgehammer each get a `HeldLook.Rest` candidate (spike or head down). It is the
   field the spear already uses: a delta on the current pose, nothing new. Run `RigTests` to check the hand still
   closes on the grip.
10. **Kicks.** `GunFeel.Kick` throws the elbow back on a shot and eases it home in about 150 ms, applied in
    `Rig`/`Pose` as a delta. It plays wherever the gun fires, and for friends from the shot heard.

    The candidates are the table's (nailer small, railgun hard, harpoon a jolt, grapple heavier and slower). Test: the
    kick ends at rest, and it never moves the elbow past the overhand's limits.
11. **Muzzles.** `GunFeel.Muzzle` picks `RodLight.Fired`'s puff at a size, the rails' afterglow (the railgun's charged
    picture faded out over about 300 ms) or none.
    - Nailer: half puff, a side jet or none.
    - Railgun: afterglow or puff.
12. A dev key in the testbed cycles each candidate of steps 9 to 11. The user picks in play: Claude prepares the room and launches it.
    Frames are read only to check that a candidate draws at all.

### Step 4. Docs

13. Write an ADR, `docs/decisions/YYYY-MM-DD-weapon-characters.md`, covering:
    - the rows as the shared module;
    - the picks;
    - what is left out (above);
    - the trail-layer slot named for modules and skins.
14. Update `game/features/crew/README.md`, "The hands": "every weapon's whoosh, wreck hit, shot, bite, reel, kick and muzzle
    are its row's (`Stroke`, `GunFeel`); none is borrowed".
15. Delete this plan.

### Size

About 300 lines with tests: step 1 is about 150, step 3 about 120, plus the sound files and the ADR. Steps 1 and 2 are one PR
(sounds). Step 3 is a second PR (motion and muzzles). Step 4 goes with the second.

## Open questions

- Should the rest hangs (step 9) go into the second PR, or wait for a stance design of their own? The design assumes the
  former, as a delta on `Rest` only.
