# Hologram keys and the bulb at the top

Item 6 of the 2026-11-04 UI batch, the user's idea, weighed and accepted. Written by `design-author`, cut by `design-reviewer`.
Supersedes [tablet-key](../decisions/2026-10-31-tablet-key.md), [bulb-signal](../decisions/2026-11-02-bulb-signal.md) and the looks of
[key-lens](../decisions/2026-11-02-key-lens.md), [edge-lenses](../decisions/2026-11-03-edge-lenses.md) and [market-key](../decisions/2026-11-03-market-key.md).
Client only: nothing in `sim/` or `net/`. Deleted once done, after the ADR takes what stays true.

## What the player meets

1. A world opens: three dim holograms over the rim's middle, air, build, journal. No empty slots.
2. The pointer goes near the screen's foot: they brighten. Under the pointer a key's tip names it and its hotkey, as today.
3. A find is noted (a crewmate's too): a big bulb at the top of the screen heats and bursts; the find's own picture hatches out and flies
   down into the research hologram, which appears for the very first find. On arrival its number counts up, and it glows amber while
   anything waits in the pile.
4. The first sale: the market hologram appears, the credits in gold on it, counting. The regen suit worn: the suit hologram flickers in;
   taken off, it fades.
5. **Going wrong:** T mid-flight puts the bulb and the find out, the number jumps to the real count, the board opens on its pile.

## Rules

**Shown keys**, in this order, the row centred where the saddle's middle was, a stride apart, laid out at once as a key comes or goes
(a `Tween` slides the rest, the key flickers in or out):

| Key | Shown while |
|---|---|
| air, build, journal | a world is played |
| research | the board holds any note, read or not (never leaves once there) |
| market | `Credits > 0` or any market good bought (credits fall only by buying, so it never leaves) |
| suit | its mode can be switched (`SuitModeProblem == null`) or is on |

A hidden key's hotkey does what it does today: T opens a blank board, M the market, V refuses as now.

**The edge.** The rim stays: tablets rise out of it. The saddle and the bezel's credit display go; the art's charge shows again. The holograms
float over the top of whichever tablet is up, or the rim when none is. The row's span still takes clicks off the world and keeps crew markers out.

**Glow.** Dim (about 40%) unless the pointer is within 64 px of the screen's foot above the row's top, anywhere along the width, or over a
tablet that is up. Brightens in 0.12 s, dims in 0.5 s (`KeyGlow`, its times passed in). States kept: hover (brighter, the tip), held (sinks a
pixel, dims), on (air shown, plan out, board or market out, suit on, journal out). Amber: research while a note is unread
(`BoardSeen.Lit`), market while an order met is unseen. The refused look goes with the suit key's hiding.

**Numbers.** Research: the finds waiting (`Unread.Count` less the finds still flying), hidden at 0. Market: the credits, 0 included.
Both count and flare as the credits do now (`CountFlare`, `FlareText`).

**The bulb** keeps `BulbSignal`'s timing and rules: one at a time, this player's only, a find while it burns makes it burn on, opening the
tablet puts it out, nothing for what waited when the world was opened or for a late guest, an order met lights the market and plays no bulb.
New:
- It is drawn on a screen layer at the top centre (`ScreenBulb`), big; `HeroBulb` and its world light go.
- At the burst one picture hatches: the note that lit it. It carries every find gathered while it burned.
- It flies to the research key's position, read each frame (the key may ride a tablet), in about 0.9 s (`FindFlight`).
- On arrival the carried count joins the number and flares. A find after the burst joins it at once.

## The pick page

One page, three sections, every state over a real frame of the HUD, each candidate a click:
1. **The hologram**: treatment (hue, texture), the six icons, the number's size and place; dim, bright, hover, held, on, amber.
2. **The bulb**: its size and whether it hangs, through its whole burn and burst.
3. **The flight**: how the find hatches, its path down, how it lands on the key.

Grim throughout: no bounce, no stars, no squash. Projection source one per candidate; more only if asked.

## Steps

1. Pick page published; picks read from its `db`.
2. Pictures: icons exported by `tools/anime-art/subjects/edgekeys.js`, the bulb by `bulb.js`; the old lens pictures removed.
3. `logic/`: `EdgeRow` (slots of the shown keys), `KeyGlow` times, `BulbSignal` carried finds and flight phase, `FindFlight` path, the
   nearness band. Example tests for each, one with all six keys shown.
4. Godot: `EdgeKey` as a hologram with a number (additive `CanvasItemMaterial`; a shader only if the pick has texture), `TabletEdge` without
   saddle and display, `TabletKey` reading into a plain `EdgeKey`, `ScreenBulb`, `Main` wiring with availability.
5. Frames: `tools/snap.ps1 -Movie` of `--bulb` and `--bulb=two`, a sheet looked at frame by frame; the row with six and with three.
6. ADR `hologram-keys`, superseding the five above; research README guarantees, map lines.

## Deliberately not handled

- Sound, rebinding, a key order of the player's own, a number on any other key, light from the holograms or the bulb on the world.
- A key appearing while the pointer is on the row slides the row under it: it takes a hover in the 0.25 s of a rare change.
- Several finds: one picture flies; the number tells how many.
- The market hidden before any sale: M opens it; 0 credits say nothing.
- A switched suit never worn: its key never shows; V still answers.
- The bezel's battery: art that nothing runs down, as before market-key.
