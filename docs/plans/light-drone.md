# Design: the light drone

By design-author, cut by design-reviewer. The look is picked: "Диск" (https://claude.ai/artifact/U15TWJa1rX5Bpwi7yEfGRo).
Tried so far on feat/gear-roles: every player helmet bolted still, the head turning inside it and the upper body leaning a
third of the look; a stand-in orb by the head carrying the lamp and the scanner.

## Problem

The helmet is bolted still and nothing mounts on it any more, so the lamp and the scanner have no place. They are also two
items fighting over one slot. They should be one tool that grows. Gestures take the hand off the held item and are hard to
read from far away.

## Player experience

1. The player crafts the light drone at the workbench and wears it. A lens disc floats behind the head and lights where the
   pointer points.
2. Holding F opens the mode wheel. Flicking to the scanner puts the light out and shows a wreck's hit points.
3. Research adds the magnet to the wheel. In that mode loot in reach flies to the drone, then into the pack.
4. The gesture key opens the gesture wheel. The drone shows the sign as a hologram, and friends see it.

Gone wrong: a player switches to the scanner in a cave. It goes dark for them and their friends. The choice is meant to cost
something.

## Out of scope

- Drone skins ("Обойма"): a picture swap through `Skins`, later.
- Alarms: they need their own short design (below).
- The drone's place in the sim, and its collision with walls.
- Power draw, breaking or losing the drone.
- Scanning creatures, sounds for modes, rebinding wheel slots.

## Approach

**Who uses it.** This stage: lamp, scanner, magnet, gesture holograms; alarms next. Later: the planet's black caves (0077),
storm warnings (surface-storms), scanning creatures.

**The item.** The item `lightDrone` (Content `LightDrone`: its modes, `MagnetReach`) replaces `helmetLamp` and `scanner`. It
is gear in today's `mount` slot: the slot keeps its id and only its label changes, so saves keep their slots.

**The mode is the client's.** Lamp and scanner are client light and overlay; the sim never reads the mode. The client keeps
it and sends it with the hero, as it sends the hero's look and `Busy`; a friend draws that beam. A reload starts on the lamp.
The unlocked modes are read from the research the crew has, so a locked side of the wheel is shown shut, not refused.

**Unlocks.** Each mode beyond the lamp names the research node that opens it, crew-wide like slot upgrades. The scanner's
node is today's black-market scanner blueprint.

**Client.**

- `LightDrone` draws the picked disc; the stand-in's float (`DroneFloat`) stays.
- `GestureWheel` takes its sides as a list with a picture each, for its two users: gestures and modes.
- The light and the scanner read the mode.
- The magnet is `Reach.PickUpsOf` called with the drone's drawn place and `MagnetReach`: no second path.
- Holograms play from the `Busy` (`Handwork`: Point, Wave, Yes, No) friends already send.

## Determinism and co-op

Nothing new enters the sim but the item swap. The drone's place, its mode, the magnet's reach and holograms are the client's,
as pick-up reach (Items README) and hero movement (0002) already are. Among friends that is enough (principle 6).

## Trade-offs accepted

- **The mode is not saved or hashed.** It changes nothing in the sim; a command, a saved field and refusals would buy
  nothing but a Fuzz entry. If a mode ever changes the sim, it becomes a command then.
- **Magnet reach from the drone's drawn place**, as reach already is the client's. From the body the drone would be a picture.
- **The drone ignores walls**, so the magnet pulls through them; quick stash does too.
- **Save format bump** for the item swap: a saved lamp or scanner becomes a drone in the step.

## Alarms, to design next

The sources exist, in different places: air (`Danger`, client, air only: None, Low, Out), venting rooms (`StatusOf`), a
coming flare (`FlareComing` on Hazards), being hunted (`Hunted(peer)` on Crew). An alarm is a new client aggregator over
them, said by light and motion (a red flash, a dart toward the danger). Proposed: always on once unlocked, not a mode,
since danger comes unasked. Fire has no sim state outside furnaces, so no fire alarm.

## Tests

- Scenario: the migration step turns a saved lamp or scanner into a drone; a drone worn in the mount slot.
- Client: the wheel's side pick, a locked side not picked; the magnet's reach measured from the drone.

## Build order, each step playable

1. The item and the disc, lamp only: `lightDrone` replaces `helmetLamp` in Content, with the migration; the stand-in becomes
   the disc (tools/anime-art `drone.js`: the body per angle; the band, lens and haze drawn by `LightDrone`). The scanner
   item stays until step 2, its node being what opens the mode.
2. The F wheel with the client's mode, lamp and scanner; the `scanner` item goes (a saved one becomes a drone, its node
   the mode's unlock); gestures move to their own key in the same step, or the keys clash.
3. Holograms from `Busy`.
4. Magnet, behind its research node.
5. Alarms, after their short design.

## Open questions

- **Gestures.** Which key (H)? The arm keeps playing them alongside the hologram until holograms ship.
- **Body pull.** Does the body keep today's pull outside magnet mode? (Recommended: yes.)
- **Magnet.** Its reach, price and where its node sits.
