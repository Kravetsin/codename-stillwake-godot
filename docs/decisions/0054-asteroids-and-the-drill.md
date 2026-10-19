# 0054 — Asteroids and the drill

- Status: accepted; superseded in part by [0055](0055-an-electric-tool-draws-while-it-runs.md): an electric
  weapon draws while it runs, not by the hit; the band's one kind of rock made four, with a far edge, by
  [0056](0056-rich-finds-far-out.md); its motor heard and its strikes held 0.1 s past a piece by
  [0059](0059-machines-heard-at-work.md)
- Supersedes: of [0034](0034-wreckage-and-the-axe.md) and [0039](0039-melee-by-contact.md), `Content.Axe`
  as the one item that deals damage and `Hit(Piece)` naming none; of
  [0052](0052-debris-by-zones-around-every-hero.md), the asteroid band as a zone still to come
- Date: 2026-10-17

## Context

The jetpack (0047) carries a hero past the tether, and batteries (0053) hold power for a tool, but nothing
out there asks for either: the far field is the stream's wreckage again. Rock to break is the next thing
worth flying to, it smelts into silicon in the slice after this one, and the drill is the first electric
tool the belt was made for. The fire axe was the only thing that dealt damage, its numbers in `Content.Axe`,
so a second one needed a shape for every weapon to come (0049).

## Decision

- **All damage works alike. Tools are weapons.** As in Terraria: everything that deals damage works the same
  on everything. The axe breaks asteroids, the drill bores hull sections; there is no tool gate, no
  "wrong tool" refusal and no tool named per kind. **Hardness is only hit points.** A better weapon is simply
  more damage a second; wrecks with a thousand hit points and more will ask for better gear later.
- **One shape for every weapon:** `Content.Weapons`, each a `Weapon(Item, Damage, Pace, Effects)`: the hit
  points a hit takes, and the ticks between hits while the button is held. The fire axe is 2 every 10 ticks
  (4 a second), the drill 1 every 2 (10 a second). `Weapons.Of(item)` is how the sim, the cards and the client
  find one. The axe's card tiles (DPS, DMG, HIT) are every weapon's. The card lists no kinds it breaks: with no gate, such a list says nothing and only grows.
- **`Hit(Piece, Tool)` says which weapon.** The damage comes from that weapon's content. Whether the sender
  holds it, how often it strikes and how far it reaches stay the client's word (0034, principle 6); a `Tool`
  that is no weapon is our bug and throws, as peers run one build.
- **Electric weapons pay as they hit.** A weapon whose item declares a `Draw` (0053) spends its draw for each
  tick of its pace on every hit (`DebrisModule.Spent`: the drill's 3 × 2 = 6 power-ticks). `Hit` checks the belt
  can pay (`IItemsQueries.CanDrain`), refused **`debris.flat`** when no battery on it holds a hit's worth, and
  its apply pays through `Drain(peer, …)` before the damage, in the host's order, so two hits on one tick and
  two peers' belts replay alike everywhere. The belt drains left to right, one battery paying a hit whole
  (0053); creative mode takes nothing. A full battery is 300 hits, 30 s of drilling.
- **The asteroid band.** A third zone, `asteroids`, by the hull with a gap (`HullBand(From 22, To 44, Lead)`):
  rows 22 to 44 past the hull's top row and as far past its bottom row, out of the tether's 12 cells, so a
  jetpack is how one gets there; across, the stream's way, from `Lead` right of the hull to as far left. A beat
  every 3 to 5 minutes (`Every` 3600, `Jitter` 2400: one every 4 on average), at most 2 out, drifting left at
  the stream's speeds. A row is drawn among the band's rows above and below, then a kind and a speed; a row
  off the grid spawns nothing, its draws taken. The band is no stream's box: the far field spawns in it too.
- **The asteroid:** `asteroid`, wreckage 2 × 2 cells, **120 hit points**: 12 s of the drill, 30 s of the axe.
  It breaks into 3 to 5 **`rock`**, salvage, a stack of 10 (50 since [0062](0062-drones-keep-machines-going.md)'s amendment), smelted into silicon in the next slice; for now an
  item with a kind and a name. Its pictures were placeholders at first, three grey rocks, drawn since (`features/debris/README.md`); `rock`'s icon is the one picked.
- **The drill:** `drill`, a stack of one, `Draw` 3, made at the lifepod of a frame, a rotor, a coil, two gears
  and a tube in 160 ticks, opened by analysis. Held, the energy bar shows (0053). It is aimed as the harpoon
  is, the straight arm turning the bit's line through the pointer (`HeldLook.Drill`: its handle the grip, held
  as a pistol, its bit the edge). **Held on the left button it runs** while the belt can pay a hit: the bit
  spins and, on the tick it starts and every `Pace` ticks after (`DrillPace`), a `Hit` goes for each wreck its
  bit touches, judged as the axe's blade is (0039, 0040): the bit held as the figure draws it, from the
  shoulder of the body upright, against each wreck's aim box. Flat, it winds down and sends nothing; the
  prompt reads "[Hold LMB] Break · Batteries flat". Others see it held through `Wields`, and aimed and spinning
  while it runs (`Handwork.Drill`, its point).
- **Heard apart from its hits:** a hammer drill strikes 4000 to 5500 times a minute, a buzz, not ten
  clicks a second; so while this player's drill works on a piece the client plays its strike, a pickaxe on
  metal cut to 50 ms, 18 times a second, each its own pitch (±12%) and loudness (±1.5 dB), at the bit on the
  Suit bus, own only (0042), and stops the moment it stops (`StrikeTrain`). The rate and the spread are
  tunables in `Sound`. Its hits play no chop and throw fewer sparks, as they come ten a second.
- **No save format change.** Nothing saved changed shape: a hit's weapon is in the command, never in state, and
  a new zone gets its beat on its first tick (0052). A save holding rock or an asteroid does not load in an
  older build, as with every item added since the first.
- **Dev:** `-- --asteroid` puts the hero in the band with a jetpack, the drill, the axe and a half-full
  battery, two asteroids beside it, and a script drilling one to rock, the next till the belt is flat, and
  chopping that one with the axe; `--manual` plays it by hand.

## Rejected

- **A tool gate** (each kind naming the tool that breaks it, `debris.wrongTool`): a rule players learn by
  being refused, and a second axis beside hit points that says the same thing worse. Hit points and damage a
  second already say what is hard and what is better.
- **Drain by the tick, while the button is held:** the drill would spend power swinging at nothing, and the
  sim would need a command for "running" beside the hits. Paying with the hit spends only what bores rock.
- **A separate `Drain` command before each hit:** two commands that must apply together, and a hit landing
  after its payment was refused.
- **A weapon table keyed by item with the axe's constants kept apart:** two places for one number.

## Consequences

- The axe on an asteroid works, slowly; the drill on a hull section works, quickly. Nothing is refused for the
  weapon in hand but a flat belt.
- The client checks the belt against the sim's state, not its own hits in flight: as a battery runs out, a hit
  or two sent may be refused `debris.flat`; the drill winds down a frame or so after the last that paid.
- The drill spins aboard as in space when held, touching nothing and spending nothing.
- An asteroid reeled by a harpoon leaves its band, as any piece leaves its path.
- Rare and capped, the band draws from the world's Rng about once every 4 minutes; tests pinning exact draws
  over that long a run on a map tall enough for the band change once.
- With hit points the only hardness, the axe can do anything the drill can given time: the drill earns its
  place as 2.5 times the damage a second, paid in power.
