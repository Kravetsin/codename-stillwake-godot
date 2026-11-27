# Design: the drone's abilities, its core charge and its first gun form

## Problem

The drone has two modes and one active, the throw, which only works with a weapon in hand. It has no ranged attack, and
today's split ("the item in hand decides RMB", "the F wheel picks a mode") leaves no place for a second active or passives.

## Words

- **Mode:** what the drone does all the time, chosen on F: lamp, scanner.
- **Active:** what it does on RMB, one chosen at a time: the throw, or a gun form.
- **Gun form:** a shape the drone copies from a gun its hero fires (drone-lessons), and fires itself.
- **The wheel's branches:** the wheel on F has two sides, actives and modes.
- **Charge:** what gun forms spend, filled by loot. The drone's core glows by it.
- **Draining:** the core is open, with shots spent and shots left, and it does not fill.
- **Loot's worth:** `Market.WorthOf`, the sum `ICrewOps.Loot` already gets. It also fills the catcher meter (`Fill`), which
  stops at full.

## Where it is heading

The drone learns gun forms by watching its hero shoot, as it learns every other form (drone-lessons). All forms share one
charge, filled by what the hero breaks and kills. Full, the core opens; it fills again only once emptied (Warframe's
Incarnon weapons work so).

- **Nail form** (this plan): copied from the nailer, ten strong shots to a full core.
- **Rail palm** (the next plan): copied from the railgun. The threads wind round the arm and the core sits in the open palm.
  One shot spends the whole core; it pierces everything and loses nothing per pierce.
- **Later:** the build decides which known abilities fit the wheel; passives come with it.

So this plan builds the shared pieces whole: the charge and its draining, a gun form as a `Gun` row with a core cost and
a lesson row, and the two-branch wheel. The rail palm then needs only its own row, lesson and look.

## Player experience

1. The hero fires the nailer. From the first shot counted, a question mark flickers over the drone now and then. After
   enough shots it studies the nailer, and a vision plays as for every form. The nail form is learned.
2. With the drone, breaking wrecks and killing creatures fills the core. It glows brighter, and embers fly into it from
   each loot.
3. Hold F and the wheel opens on the branch used last. RMB inside it switches between actives (throw, nail form) and modes
   (lamp, scanner). A right-button glyph on the wheel shows the switch.
4. With the nail form chosen and the core full, holding RMB turns the drone into the nailer's shape. It fires ember nails at
   the pointer from where it flies, anywhere the nailer fires. Each shot dims the core by a tenth.
5. While shots remain, loot does not fill the core. Emptied, it goes dark and starts filling again.
6. Tapping F still cycles modes.

**Goes wrong:** with the core neither full nor open, RMB does nothing. The drone gives a short dim flicker and no text is
shown.

## Out of scope

- The rail palm: the next plan.
- Building the build: capacity, fitting abilities, passives.
- The magnet and other modes.
- The forms' final look and sound: a pick page (Steps, item 5).
- Rebinding.

## Scale

- World: one `Fire` per shot, one charge addition per `Loot`, one deed per nailer shot.
- Players: each hero acts alone.
- Frequency: a shot each `Pace` (a gun's least ticks between shots, the client's) at most; ten to empty a core.

## Approach

**The charge belongs to Players, beside the pairing.** A pairing lasts through death and leaving, and the charge is the
drone's, so it lasts as long. `Hero` is removed on `Leave`, so Crew cannot keep it.

- Players' slice gains `Cores`: one `{ Peer, Charge, Draining }` per paired peer, sorted by peer. It is saved only while
  any exists, like `Drones`, so no format bump.
- `Charge` runs 0..`Abilities.Full`. On load it is clamped to `Full`, in case `Full` changed.
- Players gains an op `Charge(peer, worth)` and a query `CoreOf(peer)` returning `(Charge, Draining)`.
- **Filling:** Crew's `Loot` calls `Charge(peer, worth)` beside `Fill`, not inside it. Both are skipped while the hero is
  passed out (`Out > 0`); in creative only the catcher meter is, so creative can try the core. Players adds the worth only for a paired peer that is not draining, clamped to `Full`.
- **Spending:** an op `Discharge(peer, cost)`. A shot of cost `c` is allowed when `Charge == Full` or `Draining`, and
  `Charge >= c`. After subtracting: `0 < Charge < Full` sets `Draining`, and `Charge == 0` clears it. So a nail (cost
  `Full / 10`) opens the core and nine more empty it, and a rail (cost `Full`) fires only from full.
- Every drone gun's core cost divides `Full` (a content test), so a draining core always ends at exactly 0.

**A gun form reuses `Fire` and shots.** `Content.Abilities` lists the active ids (`throw`, `droneNailer`) and `Full`.
`Content.Shots` gets:

- a `Gun` row whose `Item` is `droneNailer`, with placeholder numbers stronger than the nailer and `Pierce.Broken`;
- a projectile `ember`;
- a new field `Gun.CoreCost`, 0 for every held gun. `Gun.Charge` stays the railgun's hold-to-charge time.

`FireProblem` sends a gun with `CoreCost > 0` down the drone's path:

- `creatures.noForm`: no drone, or the form not known (`Knowledge.Knows`). Creative skips the knowledge check.
- `creatures.out`: the hero passed out.
- `creatures.blocked`: as today.
- `creatures.noCharge`: the charge does not allow the shot. In creative the charge is spent as anywhere.

Creatures gains `IKnowledgeQueries`, `IPlayersQueries` and `IPlayersOps`; Knowledge and Players are built before it. The
refusals show only as the flicker; their i18n keys still get En and Ru text for the log and the refusal list.

**The lesson is a `Lessons.All` row:** `new(Abilities.Nailer, new Deed(Lessons.NailerFired, 40))`.

- Creatures' `Fire` apply calls `Knowledge.Did(peer, NailerFired)` for every applied `Fire` of the held nailer, never in
  `FireProblem`, so a refused shot counts nothing.
- It counts in creative too, and before pairing, as the scanner's deed counts broken wrecks.
- The board's companion district shows it, as it does every lesson row.
- `FormVision.Of` gets a nail-form vision: a stand-in from the scanner's frames until the pick.
- The question mark shows from the first counted shot until the form is learned, read from the deed count that `Lessons`
  already gives the client (`DroneThoughts`).

**Breakers:** `Yields` change to `scrapChunk 1..1` with no `OneIn`.

**Client (`game/features/crew`):**

- A new `logic/DroneWheel` holds the branch and the chosen active, the throw by default. Both are client-only, never
  commands, never saved. A chosen form no longer known falls back to the throw.
- `Wheel<T>` is reused with its sides switched by branch, and `WheelView` draws the hint. The wheel needs a drone. The
  actives branch shows the throw and every known gun form.
- RMB goes to the chosen active unless the item in hand has its own RMB use (canister, food, power driver, harpoon). It
  fires a form only when the hands may act, as the throw does: not seated, passed out, held by a meeting or a vision, or
  with the drone window open. Held, a form fires each `Pace`, as the nailer does (hold-to-fire).
- The muzzle is the drone's lens, the client's word (0034).
- `DroneSwarm` sets the core's brightness from `CoreOf` and adds the embers. While a gun form fires, it takes a stand-in
  shape until the pick.
- No dev flag: creative knows the form, and its core fills and drains there as anywhere, so the user tries it in the plain game.

## Determinism and co-op

- A shot enters the sim only as `Fire`. The muzzle and pace are the client's word.
- **Order in a tick:** `Fire` applies in the commands phase, before the systems that break wrecks and kill creatures. So a
  shot from a full core opens it first, and loot from that tick's breaks adds nothing.
- Charge is an int. Worth is clamped to `Full`, and a shot subtracts a whole cost that divides `Full`. No rounding.
- Every peer reads every hero's `CoreOf`, so a friend's core glows the same with nothing replicated.
- A player joining mid-session gets the charge, `Draining` and shots in flight from the save. The chosen active and branch
  are not sent; only shots matter.

## Trade-offs accepted

- **No refill while draining:** loot gathered with shots left is lost to the charge. A core left half spent stays so
  until the player fires the rest, which they may do at any time. This is the Incarnon rule the user asked for.
- **Choosing a gun form means no throw** until the player switches back. This was the user's call.
- **A form's kills fill the next core, not this one:** they come while draining. Once the core is empty, kills fill it
  again. A breaker is worth 20 and a placeholder `Full` 300.
- **Nailer shots count before pairing:** a hero who fired a lot early learns the form at the next shot after pairing.

## Tests

**Scenarios:**

- Each `Loot` caller fills the charge, up to `Full` and no further, a full catcher meter too. Nothing fills unpaired,
  while passed out, or while draining; creative fills.
- The charge lasts through death and through leaving and coming back.
- Two heroes' charges are apart: one's loot and shots touch only its own.
- `Fire` of the nail form:
  - refused `noForm` without a drone or with the form unknown;
  - refused `out` while passed out;
  - refused `noCharge` below `Full` when not draining;
  - one shot sets `Draining` and spends `Full / 10`; ten empty the core and clear `Draining`; then loot fills it again;
  - a shot and a break on the same tick: the core opens, the break's loot adds nothing;
  - in creative known without its lesson, its charge spent as anywhere;
  - its kill's loot goes to the shooter's `Loot`.
- The lesson: 39 nailer shots leave the form unknown and the 40th teaches it. A refused nailer `Fire` and another gun's
  shots count nothing.
- Breakers always drop scrap.

**Content:** every drone gun's `CoreCost` divides `Full`.

**Invariants (FsCheck):** `0 ≤ Charge ≤ Full`, and `Draining` implies `0 < Charge < Full`. Prove the test bites by breaking
the clamp once. Creatures' `Fuzz` pairs a hero, grants it the form and fills its charge, then draws the nail form's `Fire`.

## Cost

- **Modules changed:** Players (the cores), Crew (`Loot`), Creatures (the drone's path, the deed); game `features/crew`.
- **Content:**
  - `Abilities`: active ids, `Full`.
  - `Shots`: `droneNailer`, `ember`, `Gun.CoreCost`.
  - `Lessons`: one row, `NailerFired`.
  - `Creatures`: breaker yields.
- **Estimated diff:** ~1100 lines with tests.

## Open questions

- **Numbers:** `Full`, the nail's damage, pace and reach, and the lesson's count are placeholders until a play pass.
- **The form's look and sound:** a pick page (Steps, item 5).

## Deliberately not handled

- A charge refund for a refused or missed shot.
- Friends seeing the drone's question mark; they see the new form once it is learned.
- A form forgotten, or a drone lost: forms and pairings are for good.

## Steps

1. The charge, with breakers always dropping scrap: kill things and watch the core brighten, embers flying in.
2. The two-branch F wheel with its RMB hint. The actives side shows only the throw until a form is learned.
3. The nail form, known in creative: holding RMB fires stand-in ember nails, ten to an empty
   core, then the core charges again.
4. The lesson: nailer shots counted, the question mark, the vision (`-- --form-vision`), the board note.
5. The nail form's look and sound picked on a pick page, then shipped whole.
