# Catcher combat

- Status: accepted; built for the planet. Supersedes in part [catcher-paths](2026-11-09-catcher-paths.md) (the drill, Cut and Fly
  steps, the leap, breaking built walls) and [tracker-catchers](2026-11-09-tracker-catchers.md) (its look while drilling)
- Date: 2026-11-09

## Context

Played, a catcher was easy: a player kept it off with a gun and met its pounce rarely; it drilled through anything and leapt
in long flights, reading as a drone rather than a foe. The user asked for a miniboss: it presses the player all the time, shooting
on the run; it catches up mostly by an animated teleport in front of the player; it takes off only for an area attack, every
take-off a salvo of cluster shells over a middle area, breaking blocks a little, not often; no drill (teleporting, it needs no
tunnel), and against a shelter it cannot get into it throws the blasting charge the player already has. Space is slice 2.

## Decision

- **Runs only:** the search (`Paths.Find`) keeps only Run steps, from grounded places; Fly and Cut steps, the leap, the drill
  and `Building.Strikes`/`Wear` are gone. A search falling short keeps `Short` in `Mem`.
- **The blink:** lagging (its distance not below its closest for `LagAfter` 60 ticks) or short of a path, at most every
  `Blink.Every` (120), it gathers `Gather` (12) ticks standing, its spot kept (drawn as its ghost), then is set there if its body
  still fits, spent `Spent` (10). The spot: the lead point (the quarry's drift over `Sample` ticks times `Gather`, plus `Ahead`
  20 cells its way, at most `LeadMost` 96; a standing quarry, half a pounce short of it), raised out of the ground by `StepUp` or
  dropped to it within `Margin`; short of a path, or with no lead spot, the free spot on ground nearest the quarry within `Near`
  (8) cells that sees it.
- **Shots on the run:** not a move (a move stops the body). In sight and within the arc's reach it aims (`Aiming`); `Tell` (6)
  ticks before each shot, every `Gunfire.Every` (50), it holds the quarry's reported cell (drawn brightened), then strikes the
  sentry's arc along it, judged by the hero's client (0034).
- **The salvo:** every `Salvo.Every` (240), landed, within the arc's reach and with `Headroom` (6) cells clear overhead (not
  under a roof), it rises at `Speed` up to `Hover` (20) cells, into the sky (played, 6 read as a hop), or till it meets solid, and
  throws `Shells` (7: an odd count puts one on a quarry standing still) `shell` shots spread evenly over `Spread` (48) cells round
  the quarry, 8 apart with room to stand between, each onto the floor of its column, the first landing in `Flight` (24) ticks and
  each next `Stagger` (2) after; their landing cells are kept (the marks) till each lands; it falls and runs on. A shell weighs 1,
  so from the sky it falls rather than first rising off the grid, and bursts as `Blasting.Shell` (150, reach 3).
- **The charge:** short of a path with no spot in sight, every `Lob.Every` (160) it lobs a `blastCharge` at the first solid cell
  on the line from the quarry to it.
- **A creature's bursts hurt heroes:** a shot with no thrower (`By` none) bursting takes its projectile's `Damage` off every awake
  hero within its reach whose cell it sees (`Blow`), judged by the sim. A hero's bursts still hurt no one.
- **Drawn:** `FightOf` (aiming, the held cell, the ghost, gather left, the last blink) and `MarksOf` (each mark's cell, ticks
  left and flight) are the client's queries; an `Arc` keeps the creature that struck it (`By`), so a catcher draws its own bolt
  from its gun's muzzle as it runs on (played, one drawn from where it fired was left behind it); a blink is drawn as a jump; a
  charge looks the same whoever threw it. Looks and sounds: the user's picks below.

## Picks

On the pick pages (2026-11-09), from four candidates each:

- **Aim, «Красная нить»:** a thin dim red thread from the muzzle to the hero's chest, wobbling, a red dot on the hero; in the
  tell it steadies, brightens and haloes; the bolt red.
- **Blink, «Красная помеха»:** the figure torn into jittering bands with red interference while it gathers, its red ghost
  flickering in bands where it will be; a red tear where it was; spent, still twitching.
- **Salvo, «Столбы света»:** a pale column of light over each landing spot, narrowing to a bright thread as its shell nears;
  feathered bombs with a short sparking trail.
- **Sounds:** a camera flash charging for the tell; the sentry's arc for the bolt; a laser charging to gather and a dry zap to
  blink; a mortar's shot as the salvo leaves (once a salvo); the blasting charge's burst for the shells. Credits in
  `game/audio/CREDITS.md`.

## Consequences

- Shelter holds a catcher again, but not for long: no drill, yet a blink reaches a cave it can see into, and a charge breaks a
  hut it cannot. A hero sealed in bedrock still outlasts it.
- No format bump: the hunt's new `Mem` keys and `Arc.By` default to none; old Fly or Cut steps in a saved `Path` are dropped at once (a
  step not a Run replans), old leap and drill keys lie unread.
- Not handled: firing while hovering in the salvo, shells hurting creatures, phases by health; space (one homing missile) is
  slice 2. The numbers are placeholders for a play pass.
