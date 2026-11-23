# Design: the drone's abilities, and its gun as the first new active

## Problem

The drone has two modes and one active, the throw, and that active only works with a weapon in hand. There is no ranged
attack from the drone. Today's split between "the item in hand decides RMB" and "the F wheel picks a mode" leaves no place
for a second active or for passives.

## Player experience

1. A sentry's arc hits the hero, and a question mark flickers over the drone.
2. A later arc stops in the drone's threads. It is drawn into the core and sent back, and the sentry and its wreck burst.
   The usual study and vision follow, and then the gun is open.
3. Hold F and the wheel opens on the branch used last. RMB inside it switches between actives (throw, gun) and modes
   (lamp, scanner). A right-button glyph on the wheel shows the switch.
4. With the gun chosen, RMB fires an ember shot whatever is in hand. Breaking and killing things makes the core glow
   brighter, and embers fly from each hit into it.
5. Tapping F still cycles modes.

**Goes wrong:** the charge is too low. RMB does nothing, the core is dim and the drone gives a short dim flicker.

## Out of scope

- Building the build: capacity, fitting abilities and passives.
- Any passive.
- The magnet and other modes.
- The shot's final look and sound.
- Rebinding.
- A cooldown in the sim.

## Scale

- World: one shot per click, and one more `Loot` addition per break or kill.
- Players: each hero acts alone.
- Frequency: a shot each `Pace` at most. `Loot` fires as often as it does today.

## Approach

**Charge belongs to Crew.** `Loot(peer, yield)` already lands there for each hero. Players never receives loot, and a new
module would only copy the op.

- `Hero.Charge` is saved and defaults to 0, so the save format does not change.
- `Loot` adds the same worth to it, up to `Abilities.Gun.Full`, beside `Fill`, not inside it: `Fill` stops once the catcher
  meter is full, and the gun must charge while a catcher is due. Not while out; not in creative, where the gun fires free.
- New query `ChargeOf(peer)` and new op `Discharge(peer, cost)`.

**The gun reuses `Fire` and shots.** It is a `Gun` row in `Content.Shots` whose `Item` is the form id `droneGun`, plus a
new projectile `ember`. `FireProblem` handles the drone's gun on its own path:

- It checks `Knows` instead of the gun being held, and charge instead of ammo and the belt.
- Refusals: `creatures.noGun` (form not known), `creatures.blocked`, and new `creatures.noCharge`.
- In creative it fires free.

**The lesson is a `Lessons.All` row:** `Deed("sentryArc", Count)`.

- `Bolted` from a sentry's arc on a paired hero counts it. Creatures gains a dependency on `IPlayersQueries.HasDrone`.
- The catch is decided in the sim, in `Bolted`'s apply, when the deed reaches the count, never on the client's guess; two
  arcs on one tick are paid once (`Paid`). The client shows the question mark and the catch from the deed count `Lessons`
  already gives (a `Catches(peer)` query only if it gives none).
- A caught arc does no harm. Its sentry dies and its wreck breaks, both credited to the hero, so the first charge comes
  from the lesson itself.

**Breakers:** `Yields` change to `scrapChunk 1..1` with no `OneIn`.

**Client (`game/features/crew`):**

- A new `logic/DroneWheel` holds the branch and the chosen active. Both are client-only, never commands, never saved.
- `Wheel<T>` is reused with its sides switched by branch. `WheelView` draws the hint.
- `HeroView.Hands` sends RMB to the chosen active.
- `FormVision` gets a gun scene.
- `DroneSwarm` sets core brightness from `ChargeOf` and adds the embers.
- The question mark is a thought glyph.

**The build, later:** it will decide which known abilities are in the wheel; today every known one is (`Knows`). Each
ability's id is its form id. Nothing is built for it now.

## Determinism and co-op

- A shot enters the sim only as `Fire`. The muzzle and pace are the client's word (0034).
- Charge is an int. A sum of worth is clamped to `Full` and a shot subtracts a whole cost, so no rounding is needed.
- Shots fly in id order.
- Every peer reads every hero's `ChargeOf`, so a friend's core glows the same with nothing replicated.
- A player joining mid-session gets charge and shots in flight from the save. The chosen active is not sent, because only
  its shot matters.

## Trade-offs accepted

- **The sentry dies in the sim the tick the arc is caught.** The learner's client plays the return over it, and friends see
  only the burst. We accept this because the alternative is a saved pending return for a cosmetic beat.
- **Choosing the gun means no throw** until the player switches back. This was the user's call.
- **The gun's kills recharge it.** We accept this as long as a shot costs more than a breaker's worth (20).

## Tests

**Scenarios:**

- Each `Loot` caller fills charge, up to `Full` and no further, a full catcher meter too; nothing fills in creative.
- `Fire` of the drone's gun: refused `noGun` when the form is unknown; refused `noCharge` when charge is short; spends the
  cost when it fires; its kill credits the shooter.
- Breakers always drop scrap.
- Sentry arcs: below the count the hero is hurt and the deed is counted. At the count the arc is caught: no harm, sentry
  dead, wreck broken, gun known. An unpaired hero counts nothing.

**Invariants (FsCheck):** `0 ≤ Charge ≤ Full`, proven to bite by breaking the clamp once; Creatures' `Fuzz` draws the
drone's gun.

## Cost

- **Modules changed:** Crew, Creatures; game `features/crew`.
- **Content:** `Abilities` (active ids, `Gun.Full`, `Gun.Shot`), `Shots` (`droneGun`, `ember`), `Lessons` (one row),
  `Creatures` (breaker yields).
- **Estimated diff:** ~900 lines with tests.

## Open questions

- **RMB conflict.** With the gun chosen and a canister, food, power driver or harpoon in hand, which gets RMB? Proposed: the
  item's own use.
- **Muzzle.** Does the shot leave from the palm (companion-fights' note) or from the drone's core?
- **What fills the charge.** Digging soil and rock (worth 1), ore and harvests all call `Loot` today. Should any of them be
  excluded?
- **Gun numbers.** Damage, `Full`, `Shot`, range, and whether it breaks blocks: placeholders until a play pass.
- **Arc count.** How many sentry arcs teach the gun (placeholder 2)?
- **Death.** Should charge survive it? Kept by default, because it belongs to the drone.
- **The build.** Capacity counted in the drone's three orbit threads fits its body; changing the build at the capsule
  keeps it off the fight; which passives exist, and their costs, are undecided.
- **The shot's look.** Comes from a pick page later.

## Deliberately not handled

- Holding RMB to fire repeatedly: an active is one action per click.
- A charge refund for a refused or missed shot.
- Friends seeing the catch animation; a friend's caught arc shows the learner no burst.
- An arc caught when its sentry has already died: the gun is learned, and nothing bursts.

## Steps

1. Charge with breakers always dropping scrap: kill things and watch the core brighten and embers fly in.
2. The two-branch F wheel with its RMB hint. The gun's side is shut until learned.
3. The gun, granted by `-- --drone-gun` or in creative: RMB fires the stand-in ember shot.
4. The lesson: the question mark, the caught arc, the burst and the vision (`-- --form-vision`).
5. The shot's look and sound picked on a pick page, then shipped whole.
