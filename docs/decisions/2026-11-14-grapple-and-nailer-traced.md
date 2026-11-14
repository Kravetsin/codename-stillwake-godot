# The grappling gun and the nailer traced from picked references

- Status: accepted; redraws the grappling gun of [grapple-climbs](2026-11-07-grapple-climbs.md) and the nailer of [shots are the sims](2026-10-26-shots-are-the-sims.md); drawn raw since [raw tool pictures](2026-11-14-raw-tool-pictures.md)
- Date: 2026-11-14

## Context

Weapons move one by one to pictures traced from a picked reference, as the [railgun](2026-11-14-railgun-traced.md) and
those before it did. Both guns still shared the old plank stock. The user picked «Моторная лебёдка» for the grappling gun and
«Клепальщик» for the nailer. The Figma generator refused for the day, so these references were drawn by GPT through Codex.

## Decision

- **The grappling gun**: a black winch body with worn yellow edges, a drum of black rope seen through a round window, a crank
  arm on its hub, a green lamp, a rubber pistol grip. Traced with its grapnel cut away (`refs/grapple.png`), 26 × 15 pixels.
  The four-pronged grapnel is traced alone (`refs/grapnel.png`, cut from the same reference) at the gun's scale: loaded, it
  sits at the muzzle (32 × 15); in flight, its shank and the eye the line ties to are drawn in `hero.js` (10 × 9).
- **The nailer**: a heavy steel riveter, a ribbed collar and a cone nose, a lever trigger, a black rubber grip, a red air bottle
  clamped under the barrel. Traced with the rivet mended out of its nose (`refs/nailer.png`), 26 × 12 pixels. Loaded, a nail is
  drawn out of the nose.
- Both are held one-handed by their pistol grips, as before. Grip, edge and clamp are measured from the traces.
- The plank stock, the gears and their paints went with the old pictures.

## Drawn in code

- The rope drum was too dark for the trace and read as a mess: its window is mended flat and the drum drawn in code, a plain spool with four turns of rope and the crank arm on its hub.
- The nail in flight was still the pixel era's: a line 2 pixels wide under a rim 2 more, larger than the nail the nailer
  shows. It is now 6 pixels long and 1 wide, its dark rim half a pixel each side.
