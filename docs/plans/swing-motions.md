# Plan: Swing motions

## Why

Every melee stroke is one straight arm turning at the shoulder (`AxeSwing.Lift`), the same for every weapon and every aim;
aimed up or under the feet, `ShapeFor` cuts the same arc short. Nothing better can be drawn, since a stroke may point
anywhere. And a swing aims at a fixed point of the world (0039): a hero who drifts or is knocked mid-swing has the arm re-aim
at that point every frame and twist. More weapons are coming, and one that moves differently means new code each time.

**Decided with the user** (2026-11-07):
- A **motion** (how the arm moves) is apart from a **weapon** (which motion it plays): a new weapon that moves like an old one
  is a row of data, as Terraria's items each name a use style. A motion is drawn for the middle of an **aim sector** (up,
  straight, down, under the feet) and turned to the exact aim within half its sector, so it never plays where it looks wrong.
- **A stroke's aim travels with the hero:** it is kept as an offset from the hero, so a hero carried away carries the stroke
  along. The offset keeps the world's axes: a body turning under it (afloat) does not turn it.
- Hits still fly along the exact aim; one hit a stroke; each weapon's pace and hit moment unchanged; the sim untouched.
- **Today's 50 ms arm stop on contact stays** (`AxeSwing.Shown`): a hit needs a response. No shake, no camera kick.
- **No combos for now:** one motion per sector.
- One hand holds everything (one-arm-holds); new motions are small deltas from today's pose, each picked by the user on an
  Artifact page, one at a time.

## Step 1: the framework, nothing new on screen

This step ports today's two strokes into motions and fixes the twist. The game looks exactly as it does now, except that the arm
no longer twists. Step 2 adds new motions, one pick each.

### Terms

- **Aim angle:** the angle from the front shoulder to the stroke's aim point in the arms' frame (the body as placed, turned,
  lifted and faced, `Figure.FromShoulder`), radians from straight ahead, **down positive**. The front half is −π/2 to π/2.
- **Fan:** the weapon's hit spread, `Stroke.Half` either side of the aim (unchanged). Not to be confused with `HalfSector`.

### Motions (data in `game/Content/logic/SwingMotions.cs`; played in `game/features/crew/logic/MotionPlay.cs`)

- **`SwingMotion`** holds:
  - a `Kind` (`MotionKind`), either Swing or Thrust;
  - its keys;
  - the share its whoosh peaks at.

  The strike's share is not on the motion: `Stroke.HitShare` stays the one source. A test checks that every Swing row's strike
  key sits at its stroke's `HitShare`.
- **`MotionPlay`** is a motion as one stroke plays it, chosen at the press and kept: its sector, motion, keys (the overhand's
  built then), the item's `Lead`, and a fixed motion's sampled shares. The stroke's kind is its chosen motion's `Kind`.
- **A key** is `(Share, Shoulder, Elbow, Ease)`:
  - `Shoulder` is a turn in radians **relative to the aim line**, down positive.
  - The arm is drawn at `ArmAt(aim + Lead(item) + Shoulder, Elbow)`, as today's `AngleAt` is `line + Lift`.
  - `Ease` is the easing into the key from the one before:
    - `Linear`;
    - `Sine`, which is (1 − cos πt) / 2.

  Those are the only two eases the overhand uses.
- **Every motion begins and ends at rest:** its first and last keys have `Shoulder` 0, the strike's elbow, at shares 0 and 1. A
  held next stroke, whatever its sector, then begins unbroken (`SwingArm` eases any hand-over, as today). A test checks every
  weapon row.
- **The overhand** (`Overhand`, beside `MotionPlay`, with the limits `Overhead`/`Behind`) is today's swing. Its keys are
  built at the press from `ShapeFor(aim + Lead, Stroke.Half)` (raised, through):
  `(0, 0, e, —)`, `(WindShare, −raised, e, Sine)`, `(HitAt, 0, e, Linear)`, `(HitAt + through / DownSpeed, through, e, Linear)`,
  `(1, 0, e, Sine)`, where `e` is the item's `HeldLook.Elbow` (0 for the axe).
  - Played, these keys give `Lift` exactly.
  - It is the one motion whose keys are shaped by its aim, so the port is exact at every aim. Motions drawn later have fixed keys
    and are turned whole.
- **The jab** (`Jab`) is the spear's stroke, Kind Thrust, with no shoulder keys:
  - Each frame its shoulder is solved so the shaft's line runs through the aim point, as today (`HarpoonShot.ArmFor`).
  - Its elbow is today's own curve `Strokes.ElbowAt`, kept as the motion's elbow function (smoothstep and a square), not keys.
- **The air cut's sweep** asks the motion for `ShareAt(turn)`, the share at which the edge's middle is `turn` from the aim line:
  - the overhand keeps today's formula;
  - a fixed-key motion samples its keys once at the press, every 1/100, and keeps the two shares in the swing's state.

### Sectors (`SwingMotions.cs`)

| Sector   | From (excl.) | To (incl.) | Middle | HalfSector |
| -------- | -----------: | ---------: | -----: | ---------: |
| Up       |         −90° |       −45° | −67.5° |      22.5° |
| Straight |         −45° |        20° | −12.5° |      32.5° |
| Down     |          20° |        60° |    40° |        20° |
| Under    |          60° |        90° |    75° |        15° |

- A bound belongs to the sector with the smaller angle (−45° is Up).
- `Sectors.Of(aim)` takes any angle. An angle in the back half is mirrored to the front first, so a friend's replay that lands
  there still gets a sector.
- The sector is chosen by the raw aim angle, without `Lead`: it is about where the player aims, not how the item sits.
- **The sector, and with it the motion, is chosen once at the press and kept with the stroke** (`AxeSwing`). The shoulder
  moving mid-stroke never switches motions.
- A fixed-key motion is turned by aim − middle, clamped to ±`HalfSector`.

### Weapons (`Strokes.cs`)

- `Stroke` gains `Motions`, one motion for each sector, and `Look`, the `HeldLook` it is drawn with; it has no `Kind`:
  - axe, pickaxe and sledgehammer name the overhand in all four;
  - the spear names the jab in all four.
- Pace, length, `HitShare` and `PeakS` stay on `Stroke` as they are, the hammer's slowed swing included.
- `Stroke.WhooshMsOf(motion)` reads the motion's whoosh share instead of `AxeSwing.WindShare`/`StrikeShare`.
- `AxeSwing.WhooshMs`, which is the axe's alone, goes; its readers use `Strokes.Axe.WhooshMsOf(Overhand.Motion)`.
- The jab's times are named: `JabCockMs`, `JabHitMs`, `JabHoldMs`, `JabMs`.
- `SwingArm.BlendMs` becomes the constant 175 ms. That is today's value, the axe's wind-up (0.35 × 500 ms), now named apart from
  `AxeSwing`.

### The aim travels with the hero (fixes the twist)

- **The offset.** `AxeSwing.Begin` takes the hero's place, `Hero.X`/`Hero.Y` in thousandths of a cell. It keeps the offset
  `Aim = point − place` and the world point `At`, which is still what `Busy.At` sends.
- **Every `Begin` takes a fresh offset,** including the held repeat inside `Advance`. So `Advance` gains `place`, the
  hero now, and `angle`: `Advance(elapsedMs, wanted, at, place, stroke, angle)`, `Follow(elapsedMs, sent, place, stroke, angle)`.
- **The aim angle** comes from the figure, `Figure.AimAngleOf(offset, facing)`, faced as the stroke will face it; none, the
  offset's own. It is worked out only when a stroke may begin: the own hero's button wanted, a friend's chop heard.
- **Drawing.** Each frame the arm aims at the hero's place now plus `Aim`, which is what `Figure.Chop` receives from
  `HeroView.ShowHands`.
- **Hitting.** At the strike, the fan is cast from the hero then toward the same offset (`StrokeOf` in `HeroView.Hands`,
  `StruckBy` in `HeroView.Others`).
- **A zero offset** (the pointer on the hero's own place) aims straight ahead the way the hero faces.
- **Others' heroes.** `Busy.At` keeps its meaning on the wire: the world point at the press.
  - The body of another's hero is drawn 150 ms behind (`Remote.BehindMs`), while its swings are swung as heard, so the offset is
    taken from **the hero sent in the first packet that brought the swing's number**.
  - `Busy.Hear` keeps, beside the last chop, the place of the packet in which that swing number first came (`HeardChop`).
  - `AxeSwing.Follow(elapsedMs, sent, place, stroke, angle)` begins with `Aim = sent.At − place`. This is exact to the
    sender's own offset, up to the 50 ms between its packets.
- **A replay's facing.** While another's hero swings, its figure faces the side of the offset's x, as the own hero faces by
  `_swingFace`. Its aim then lies in the front half however its delayed body was turned.

### Readers moved over

- **`Figure.Chop`** and the chop block in **`Figure.Show`** (where `swung` is set): the arm angle comes from
  `Motion.AngleAt(aim, phase)`. The hit-stop `Shown` is applied to the phase as today.
- **The air cut's start in `Figure.Show`** (where `AirCuts.Strike` is called): it reads the motion's `ShareAt`.
- **`HeroView.ShowHands`**, its two callers (`HeroView.Hands`' own hero, `HeroView.Others`'), **`StrokeOf`** and **`StruckBy`**:
  they pass the offset and the hero's place, not the world point.
- **`HarpoonShot`**: keeps `ArmAt`/`EdgeAt`; nothing else of `AxeSwing`'s shape.
- **Code that moves:**
  - `AxeSwing.Lift`, `ShapeFor`, `ShareAt`, `AngleAt` and the limits `Overhead`/`Behind` move into the overhand.
  - `AxeSwing` keeps the clock (`Advance`, `Follow`, `Phase`, `Count`, `Shown`), the stroke's `Play` and offset,
    `ArmAt`, `EdgeAt`, `Lead`.

## Tests (`game.tests`)

- **The twist, first, failing:**
  - A swing is begun at a point and the hero is moved three cells before the strike. The arm's angle and the fan's heading at the
    strike equal those at the press.
  - A held run re-takes the offset at each stroke.
  - This fails on `main`.
- **The port is exact:**
  - Before the move, a copy of today's `Lift`/`ShapeFor`/`AngleAt`/`ShareAt` goes into the test as the reference.
  - After the move, for each weapon, at aims every 5° over the front half and phases every 1/100, the arm angle and `ShareAt`
    equal the reference.
  - The jab's elbow equals a copy of today's `ElbowAt` every 5 ms.
  - Changing one key makes the test fail, shown once.
- **Rows:** every weapon row's motions begin and end at rest, and every Swing row's strike key sits at its `HitShare`.
- **Sectors** (FsCheck `[Property]`), for any angle, back half included:
  - `Sectors.Of` gives the sector its mirrored angle lies in, bounds to the smaller angle;
  - a fixed-key motion is turned at most `HalfSector`.
- **Others:**
  - A clock following a `Busy` takes its offset from the hero of the packet that brought it, while the hero shown lags that
    packet by 150 ms.
  - A held run of three swings replays back to back, each with its own offset.
  - A replay whose body faces away swings toward the offset's side.
- **Rewritten:**
  - The `AxeSwingTests` that read `At` as a vector from the shoulder (`Atan2(At.Y, At.X)`) or feed world points read `Aim`, with
    the hero's place given.
  - `StrokesTests` reads `WhooshMsOf` from the row's motion.
  - `RemoteTests` and `AimTests` stay as they are.

## Step 2: new motions, one pick each

Each motion goes through an Artifact page of candidates, drawn from the current rig, with frames looked at before publishing.
Then it becomes a row of keys.
- **Under the feet:** a short chop down between the feet, body upright. It replaces the overhand in Under for the axe and the
  pickaxe.
- **The pickaxe's peck:** from the elbow, the shoulder nearly still.
- **The axe's chop with a wrist:** the first motion that needs the item turned against the forearm. It adds a `Wrist` to the key
  and to `Rig.HeldAt`/`EdgeAt`, so the hit still follows the drawn edge (0040).
- **The sledgehammer's circle:** back low behind the body, up over the head, down.

## Docs

- **A new ADR `2026-11-07-swing-motions`:** motions apart from weapons, sectors, the aim travelling with the hero. It supersedes
  0039 in part: the stroke's aim is kept against the hero's place, not the world. 0039 gets the line "superseded in part by …".
  0075's arm stop stays.
- **`game/features/crew/README.md`:** the "Struck tools" line of "The hands", and the Files table
  (`MotionPlay.cs`; `SwingMotions.cs` sits in `game/Content/logic`).
- **`docs/glossary.md`:** «движение удара» → `SwingMotion`, «сектор удара» → `Sectors`.
- **`docs/map.md`:** if the guard asks.

## Deliberately not handled

- **Combos:** the user set them aside. There is one motion per sector, and nothing counts strokes to pick a motion.
- **The wrist, torso lean, legs, the back arm:** none of today's strokes uses them; each comes with the first motion that does.
- **Reworking the hit-stop:** it is kept as it is. The user may want it done better later, not here.
- **A body that turns mid-swing** (afloat): the offset keeps the world's axes, so the stroke keeps its world direction while the
  body turns under it, as today.
- **A friend's offset** is exact up to the 50 ms between its packets: a running friend's stroke may be a cell-fraction off.
  It is only drawn; their hits are their own client's.
- **Aimed tools** (guns, drills, the harpoon) are not motions.

## Done when

- The full suite is green.
- `dotnet format --verify-no-changes` is clean.
- The docs guard is green.
- In the `--melee` dev room the user plays, the three swings and the jab look as before.
- A hero pushed off mid-swing keeps aiming at the same point relative to itself.
