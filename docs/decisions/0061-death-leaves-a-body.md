# 0061 — Death leaves a body

- Status: accepted; its drawing and taking back of a body superseded by [body-bag](2026-10-29-body-bag.md)
- Date: 2026-10-18

## Context

A suit run dry passed the hero out for five seconds and woke it at the pod with everything it carried. A
failed sortie cost nothing, so the cold past the field (0050), canisters and the jetpack's air (0047) were no
real risk. Death needs a price that sends the player back out, without taking what took long to make.

## Decision

- **Passing out is death**, in one place: crew's breath, where the suit hits zero. Crew calls one new items
  operation, `LeaveBody(peer, cell)`, with the hero's last reported cell (`Enter`), or the spawn when that cell
  is off the grid (nothing validates `Enter`). A later cause of death that drains the suit lands there too.
- **Gear is kept.** `ItemSpec.Gear` marks the fire axe, the harpoon, the drill, the jetpack and the battery. A
  content test holds that everything worn (`Equip`), electric (`Draw`) or a weapon is gear; the harpoon is marked
  by hand. Charge stays with the batteries (0053).
- **Everything else drops**, canisters too: every non-gear stack in the hero's own 36 slots falls on that cell,
  still, by `FloorStack.Nobody`, as ordinary floor stacks. Equipment slots hold only worn items, all gear.
  Creative mode, or nothing to drop, leaves no body.
- **A body is a tag, not a store.** Each stack carries `Body(Peer, Died)`, `Died` the items module's tick. So
  looting is the existing `PickUp` (as much as fits, anyone, the owner at once); the stacks lie where they fell
  inside the ship and drift in space and leave the world at its edge, as every floor stack (0008). Stacks merge
  only within one body, so two deaths stay two bodies. A body is gone when its last stack is.
- **Ten minutes, wherever it is:** a tagged stack is gone `Floor.BodyLasts` (12 000) ticks after the death.
- **On screen:** the hero lies limp in its suit at the sim's cell while out, and picks nothing up then. After it
  wakes, each body is drawn on its stacks as the hero's figure lying limp in the owner's suit, a beacon on its
  chest blinking in that colour and lighting what is round it. Taking it back is walking over it. An edge marker
  points to this player's newest body only, through the markers' list.
- **Saves:** format 15. The tag is written only when a stack has one, so the step changes nothing but the format;
  stacks of older saves are no one's body.

## Rejected

- **A `Bodies` store in items, with its own `Loot` command:** a second kind of thing holding stacks, a command,
  refusals, and its own drift and edge rules, for what floor stacks already do. The tag gives the same play.
  An anchor store would be wanted only if a body needed a place apart from its stacks (a grave marker that stays
  once picked clean); nothing does.
- **Bodies that never drift or expire:** a body past the field stays forever, and bodies pile up over a long
  world. Drifting as stacks do keeps one rule for everything let go; the limit keeps the world clean.
- **Losing gear too, or a share of it:** rebuilding tools is a chore, not a risk; and an axe-less hero cannot
  break its way back.
- **Markers to every body, friends' too:** a screen edge full of arrows. One, to your own newest, says where to go.
- **A body only its owner may loot:** friends trust each other (0002); carrying a friend's things home is play.

## Consequences

- Dying out past the field costs the trip back, through the same cold: 0050 doing its job.
- A body left in space drifts back past the moving ship at `Floor.SpaceDrift`, about 36 cells a minute: one
  that died far out toward the world's trailing edge may leave the world before its ten minutes. Out there it
  is a race.
- The body falls at the reported cell, up to a second and a round trip behind the hero; the client moves its
  figure there as it passes out, so it lies where the body falls.
- A move landing on a hero during its five seconds out finds its slots emptied: accepted, the client sends none.
- The look is a placeholder of the hero's own sprites; a picture of a body is for a later art pick.
