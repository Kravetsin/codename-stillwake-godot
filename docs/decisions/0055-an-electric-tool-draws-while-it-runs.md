# 0055 — An electric tool draws while it runs

- Status: accepted
- Supersedes: of [0054](0054-asteroids-and-the-drill.md), "electric weapons pay as they hit" and its rejected
  "drain by the tick, while the button is held"
- Date: 2026-10-17

## Context

The drill paid its draw for each tick of its pace inside every `Hit`, so it spent nothing spinning at empty space
or aboard. The user's call: a running motor draws power. Pressing the left button with the drill starts it
anywhere, and it uses energy the whole time it runs, touching something or not. Cutters and laser guns come next,
on the same belt (0053), so the running is a mechanic of every electric item, not of the drill (0049).

## Decision

- **A run is the items' state.** `Run(Item, On)`, in `Items`, which owns charge: the sender's hero runs an electric
  item it carries, or stops. Like `Thrust(On)` (0047), the client sends it while its own run differs from the sim's.
  Refused `items.notElectric` for an item with no `Draw`; turning on, `items.notHeld` when the sender carries none (any
  slot: which one is in hand is the client's word, principle 6) and `items.flat` when no belt battery holds a tick of
  the draw. A hero runs one thing; off stops whatever it runs. `ItemsState.Running` keeps the runs, a peer and an item
  each, in peer order.
- **It draws by the tick.** Every tick each run draws its item's `Draw` through `Drain` (0053): the belt left to
  right, one battery paying a tick whole. The draw is whole power-ticks a tick, so no remainder is carried (thrust
  keeps one only because its unit spans ten ticks). A run whose belt cannot pay, or whose item is no longer carried,
  stops by itself, and the client sees it in `Running`. Creative mode draws nothing.
- **Crew stops it** as its hero passes out, every tick it stays out, and as it leaves (`IItemsOps.StopRunning`):
  items cannot see heroes, crew already writes items through its ops. A run left by a peer gone would drain a belt
  with no one at it.
- **A hit spends nothing.** `Hit` with an electric weapon is taken only while the sender runs it
  (`debris.notRunning`) and the belt can pay a tick (`debris.flat`); `HitProblem`, what the client asks, leaves the
  run out, so the frame that starts the drill sends its `Run` and then its hits, applied in that order on one tick.
  Hits stay client-judged contact every `Pace` ticks.
- **The client** (`Motor`): the drill held on the left button, not a hold begun by a grab, with a belt that can pay
  a tick, is run, aboard or at nothing too; let go, put away, passed out or flat, it is stopped. The bit spins while
  it runs; the strikes play only while the bit is on a piece. Others' drills spin while the sim says they run. Running
  free it makes no sound: a motor's whir waits for the user's audition.
- **Format 13**: the items' slice gains `running`; its step gives an older save none.

## Rejected

- **The run in `Crew`, on the hero, beside `Thrusting`:** passing out and leaving would be its own, but charge and
  what is electric are the items', and every electric tool to come is an item. Two calls from crew are cheaper than
  a second owner of the belt.
- **Paying by the hit** (0054): a motor spinning at nothing spent nothing.
- **A run refused to a hero passed out:** items cannot see crew. A `Run` landing on a hero already out pays one tick
  (3 power-ticks) before crew stops it; its client sends none while out.

## Consequences

- A full battery runs the drill 600 ticks, 30 s, whatever it touches; held at nothing it drains as fast as drilling.
- The energy bar falls while the drill runs, not only while it bores.
- A run is saved and hashed: a guest joining mid-run sees it, and a world loaded with friends away stops theirs as
  the host sends their `Leave`.
- What is left in a battery short of a tick stays until it is charged; the run moves on to the next battery.
