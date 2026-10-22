# 0101 — A network stores its spare power

- Status: accepted; amends [0045](0045-a-burner-generator-shares-the-furnaces-fire.md): a burner generator no longer
  burns whatever its network draws
- Date: 2026-10-22

## Context

A network made what its panels and fires made and its users took a share of it at once: whatever nothing drew was lost,
and a network short slowed everything on it (0017). The user: "while there's no player or nothing works, energy
accumulates, then it goes into use." The arc module doubles a station's draw (0086), so an arc smelter on one panel runs
at five eighths; with a store, the panel's quiet hours pay for its busy ones. Batteries already store energy for tools
(0053), in power-ticks; this is the same energy, kept by the network rather than by an item.

Where it is heading: this store is the first of a kind. A bigger arc-tier store (Arc is the energy branch) and
whatever later sources there are (a reactor, a thermal branch) settle through the same rule, as a `StoreSpec` with
its own numbers; a battery stays an item, charged by a station.

## Decision

- **The capacitor bank** (`capacitorBank`, 2×2): a plated cabinet of two banded capacitor cans and a gauge, a socket on
  top. A node cables end on; it neither makes nor draws. It holds **12000** power-ticks and takes in or gives out at
  most **10** a tick (`MachineSpec.Store`, a `StoreSpec(Capacity, In, Out)` in `Content.Machines`): a minute of its
  intake fills it, two of a spare panel's; a full one runs an arc smelter (8) alone for 75 seconds.
- **It costs common parts:** a frame, two coils, two plates and a circuit, built aboard, opened by analysing them (0027).
  Not arc-gated: it is basic infrastructure, like the charger. An arc-tier store can come later as another machine with
  a bigger `StoreSpec`.
- **How a network settles a tick** (`PowerNodes.Settle`), in the derivation phase, from what each store holds as the
  tick begins:
  - **Spare** (supply at or above demand): every user gets its full share; what is spare goes into the stores in the
    reading order of their cells (top to bottom, then left to right), each up to its `In` and the room it has left;
    the rest is wasted.
  - **Short:** the stores give out in the same order, each up to its `Out` and what it holds, until the shortfall is met;
    satisfaction is `floor(1000 × (supply + given) / demand)`, so users slow alike only once the stores are spent or at
    their rate.
  - A store never takes from another: only spare supply goes in. Order, not shares: the first bank fills and empties
    first, as the belt's batteries drain left to right (0053), and there is no rounding to write down.
- **What it holds is saved and hashed** (`PowerState.Stores`, by machine id, only stores holding something); the
  flows are derived. The simulation phase applies them, so a world loaded settles the tick as it would have. A bank
  taken apart (its cables cut first) loses what it held. Save format 26, an identity step.
- **A burner generator lights its next fuel only while its network takes more than its panels make:** its draw and
  what its stores could take in this tick, against what its makers that burn nothing make (`IPowerQueries.Needed`,
  `PowerNetwork.Steady`). A fuel lit burns to its end. So a bank full and nothing drawing lets a fire go out and keep
  its planks; a bank with room keeps it burning to fill; a generator on no network never lights, its panel saying
  "Nothing needs its power" (`furnace.unneeded`). Fires are not weighed against each other: two on a network burn
  together, so they never hand over with a tick dark between, which 0045 cared about.
- **On screen, without a word:** the bank's gauge, six segments lit from the foot as far as it is full, the top one as
  bright as it is full within it, green, amber under a quarter as a battery's bar; a spark runs up the lit segments
  while it takes power in and down while it gives out. It gives light in its colour, more the fuller it is, none when
  empty (`StoreLights`). The power overlay's tip on a node of a network with stores adds "Stored: 4520 / 12000" under
  what it makes and draws. No panel: there is nothing in it to move.
- Its picture is a placeholder painted by `tools/ship-art/machines/capacitor-bank.js`; `-- --store` records it filling
  from a panel and emptying into an arc smelter.

## Rejected

- **Pro-rata sharing between stores:** every store's share rounded, the remainders handed out by a rule, for nothing a
  player sees; in order is what batteries already do.
- **A store charging from a store, or levelling two:** energy moving in circles costs nothing and shows nothing.
- **Burning on demand by weighing every maker,** fires included: two burners on one network would hand over one tick
  dark each time a fuel ran out. Weighing panels only keeps fuel where a panel suffices and costs no blink.
- **Banking a lit fire** (pausing it mid-fuel): a fire has no paused state, and a plank's 20 seconds wasted at most is
  the rule's price.
- **A module of its own:** power already finds the networks, their supply and demand; a store is part of how a network
  settles.

## Consequences

- An idle ship fills its banks; a busy one runs on them, its stations at full speed until they run dry.
- A burner fed and idle keeps its fuel; the player sees its fire out and its panel saying why.
- Flows are found in the derivation every tick, as the networks are: a few stores, no cost to speak of.
- The overlay tip is the only place numbers show; the gauge is read at a glance.

## Deliberately not handled

- No priorities between users when short, no switches to keep a bank for later, no leak while it holds.
- A store's rate is the same in and out for the bank; `StoreSpec` keeps them apart for a store that differs.
- A generator lit for a store with room but no demand fills it and then wastes what the store no longer takes, until its
  fuel ends.
