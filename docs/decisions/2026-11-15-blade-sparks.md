# A struck wreck sparks where the blade's trail lies on it

- Status: accepted; extends [blade-trail](2026-11-10-blade-trail.md) and [drill-contact](2026-11-14-drill-contact.md) to
  struck tools; supersedes where a struck wreck's sparks start in [damage-looks](2026-11-05-damage-looks.md); its ground
  left as it was, extended by [cutter-contact](2026-11-15-cutter-contact.md)
- Date: 2026-11-15

## Context

The grinder's and the drill's sparks come off their haze where it meets a wreck. A wreck struck by the axe, the sledgehammer
or the pickaxe still threw its sparks from one point, its side facing the nearest hero, wherever the blade went in. The
user's idea: the swing's haze, the blade's own trail, throws them where it lies on the wreck.

## Decision

- **Where:** crew walks the stretch each blade swept last (`BladeTrail.Newest`), its edge points and the middle of each
  point's sweep, against every wreck's drawn pixels whose box the stretch reaches (`BladeBites`), only while a blade sweeps.
  A wreck's last bite, its points inside and the way the blade went through them, is kept the trail's life (170 ms) and
  published as `Ui.Bites`, so a friend's hit felt a tick after its swing still finds it.
- **Thrown:** when a hit lands, debris throws the wreck's hit look from up to three of the bite's points, spread along it,
  a third of the hit each, along the swing. How many sparks and when stay the hit's: a trail across a wreck without a hit
  throws nothing.
- **Else as before:** a hit with no bite (the reach is longer than the blade, so the blade may not touch the wreck; the
  spear leaves no trail) throws from the side facing the nearest hero.

## Deliberately not handled

- A hit whose blade never touched the wreck throws from its side, not from the trail's nearest point, as the drill does.
  It reads well enough at a reach a little past the blade; revisit if a longer weapon makes it show.
- The ground's and the hull's struck sparks are unchanged.
- The bite is the last stretch on the wreck within 170 ms; two heroes swinging at one wreck at once share it.
