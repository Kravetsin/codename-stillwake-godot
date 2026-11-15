# The basic and heavy helmets, traced

- Status: accepted; supersedes the looks of [the-heavy-helmet](2026-11-03-the-heavy-helmet.md) in part
- Date: 2026-11-14

## Context

The basic helmet still wore the dark bands of the old player colour, which has moved to a tracker on the shin
([player-colour-mark](2026-11-01-player-colour-mark.md)), and the heavy bell was the first hand-drawn one. After the quilted helmet
([quilted-helmet](2026-11-14-quilted-helmet-and-traced-art.md)) the user wanted both redrawn the same way: a GPT-made reference
they pick, traced into shapes and finished by our engine. Picked on a page of candidates per helmet.

## Decision

- **The basic helmet, as picked** ("worn work helmet"): a rounded ochre shell, scuffed, an oval glass in a dark bolted frame, a small
  valve at the back, a dark rubber neck seal. No bands, nothing in the player's colour.
- **The heavy helmet, as picked** ("armoured"): a gunmetal bell with brass trim and a cap, a wide glass behind three brass bars, two
  air tubes at the back, a bolted brass collar on a shoulder piece with a brass arc. The reference's lamp on the cap is left out: the
  game draws its own helmet lamp, set where the reference's stood.
- **On the hero** as the quilted one: `hero_helmet_shell` and `hero_visor`, `hero_heavyHelmet_shell` and `_visor` (76 × 72, the
  `helmet` joint), the traced shapes scaled to the head, the glass the visor. The basic stands 13.3 px tall, front edge at 13.1, its
  seal on the neck; the bell 17 px from −2, front edge at 13.9, its collar and shoulders on the torso as the old breastplate. Both
  cover the head and every face; the glass shows the eye and the face. Their cards, `items/helmet` and `items/heavyHelmet`, are the
  traces' own fits; `hero_helmet`, the helmet lamp's card, the basic's drawing shrunk into its 13 × 12.
- **Lamp and scanner** rest on each trace's crown (`Traced.crown`, the outline's upper hull): the lamp at (8, −2) on the basic
  (`HeadLampArt.On`), on the bell's cap at (6, −4.8); the scanner's crest at (6.2, −1.9) and (6.2, −4.1) (`ScannerInk`).
- **The tracer** gains what these two need: `mend`, the reference repainted before tracing (the lamp erased, the cap it hid filled
  in), and `holes`, background a part wraps round (the tubes' loops) left open in every shape. The quilted trace is unchanged by both.
  Placing a trace on the head (`Traced.onHead`) and its crown are shared by the three helmets.
- **AI disclosure.** The references (`tools/anime-art/refs/helmet.png`, `heavyHelmet.png`) are AI-made, as the quilted one: the
  game's Steam AI disclosure must say so.

## Consequences

- Every helmet the player wears is now traced art; the catchers' helmets stay hand-drawn (`catchergear.js`), and hero.js keeps the
  bell's materials, hose and rust for them.
- The bell has no side port any more and its glass is the front; its silhouette is taller and boxier than the old dome.
- The basic's scuffs are patches of the reference's grey on the shell, smaller than drawn there: thin ones would melt at 16 px.
- The scanner's crest bridges the dip between the bell's cap and its glass frame, as it bridges the quilted hood's rolls.
