# The drone's meeting is a vision, and the sim spares the hero meanwhile

- Status: accepted
- Date: 2026-11-21

## Context

Pairing with the alien core ([drone-companion](2026-11-21-drone-companion.md)) is the moment the hero gets a companion for
good. A press of E that just made a drone appear said nothing about what the core is. The user picked a vision on a pick
page: «Стена», `wallStory` ([page](https://claude.ai/artifact/TAUMbtDo6q3uEPZ7yCKnQE)).

## Decision

- **The vision, this player's alone** (`Meeting`, `MeetingView`, `HeroView.Meeting`), in the pick's seconds from E
  (`Meeting.Times`), about 24.5 s:
  1. E holds the hero; the HUD goes and a grim, muted grade comes over the screen (`CutsceneView`). The core's halo
     wakes, and the core lights and rises from its pit.
  2. The wreck crumbles into flakes that go out as stars, while the core grows where it is.
  3. In the void the hero floats before the dull giant core. Its blood flies from the palm and winds into three tilted
     rings, one after another, then turns black with embers as the lens opens. The screen swims meanwhile, lighter than
     a last breath. They look at each other.
  4. The wreck comes back and the core shrinks to where it began, its rings now its cloud. It pulls in its plates one
     at a time along ember arcs and rises to the head. The grade lifts and the HUD comes back.

  The hero kneels for it (`Pose.Kneel`), its near palm reaching to the core. Nothing cancels the scene once begun.
  `MeetingView` draws it on the world's layers in the wreck, and on a screen layer of its own in the void, the hero's
  own figure moved onto it. It is unlit, never a node per drop or flake.
- **The sim spares the hero** for `Crew.MeetingTicks` (490 ticks, the scene rounded up) from the `PairDrone` that pairs
  it. `Hero.Meets` counts them down, saved, so it carries across surfaces. Meanwhile the hero spends no air, meals do not
  run down, thrust is off, and shocks, blows and lava do nothing. `Succumb` is refused, so cold and heat cannot kill it.
  `Spared(peer)` tells the client, which keeps the body well too.
- **Friends see none of it.** A friend's hero just stands where it pressed E for the scene's length, its drone hidden;
  as the scene ends there, the drone fades in by its head. The core keeps lying for every player who has not paired. A
  hero already paired when first seen plays nothing.

## Deliberately not handled

- The vision is not shown to friends, and cannot be skipped.
- Heroes paired by the core's loss get no vision.
- The scene runs on the client's clock and the sparing on the sim's ticks; they are matched by length only.
