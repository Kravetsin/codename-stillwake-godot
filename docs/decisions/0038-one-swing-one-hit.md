# 0038 — One swing, one hit

- Status: accepted; a swing begun only on a wreck in reach, and its one hit to it, superseded by
  [0039](0039-melee-by-contact.md)
- Supersedes: of [0034](0034-wreckage-and-the-axe.md), the hit sent at once on the press and its
  consequence "the swing hides the round trip"; of [0037](0037-others-see-the-hands.md), the swing taken
  from the sample drawn and its consequence "the blade coming down and the wreck's jolt may be up to a
  swing apart"
- Date: 2026-10-16

## Context

Hits went out at most every `Axe.SwingTicks` on the world's tick, but the figure swung on a clock of its
own that began again at every press. Clicking fast on a wreck played strike after strike with no hit
behind most of them. A friend's figure swung at its own pace from when its chop was first seen, the
blade and the jolt up to a swing apart.

## Decision

- **One clock for the blade and the hit** (`AxeSwing`, client only): a swing begun on a wreck in reach
  plays through `Axe.SwingTicks` as real time, wound up, struck, eased back, and sends its one `Hit`
  the frame the blade meets the wreck. A press or a hold during a swing does nothing; held as it ends,
  the next begins at once. The hit at the strike, not at the press: the jolt then follows the blade by
  the time a command takes to come back (a tick alone), where at the press the blade would wait for it
  or run ahead of it.
- **Let go, the swing still hits.** A click is shorter than the wind-up; a swing cancelled on release
  would never hit from a click. Only putting the axe away, the hand taken or passing out stops a swing
  before its strike, and then nothing is sent.
- **Others swing once per swing sent, as heard.** `Busy.Swings` counts the swings a hero has begun
  (left out of the wire while none); another's figure swings once for each number it has not shown,
  starting when the number arrives rather than 150 ms behind with the body, so its strike meets the
  jolt the hit causes. Numbers passed during a swing are swung as one.

## Consequences

- The first hit of a click lands about a quarter second after it (the wind-up), where it used to go at
  once; held, the pace is unchanged.
- A swing begun on a wreck that drifts over the hull or is broken by a friend before the strike still
  strikes; the sim refuses the hit (`debris.notOutside`, `debris.gone`) and nothing jolts.
- Another's swing runs up to 150 ms ahead of its body; chopping, a hero barely moves, so it does not show.
- A friend's first swing just after taking the axe out may show from partway: the axe is drawn with the
  body, 150 ms behind.
