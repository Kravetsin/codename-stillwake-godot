# Trace

Turns a picked AI reference into an engine drawing: the reference is never shipped, its shapes are, drawn by `engine.js` and
finished by `wash.js` as any subject. Redrawing a reference by eye lost its silhouette; tracing keeps it.

## Files

| File | What |
| --- | --- |
| `trace.js` | `node trace.js <ref.png> <palette.json> <out.json> [debug.png]`: the silhouette off a flat background, cut into parts by the reference's ink or, with `"segment": "colour"`, by the material nearest each pixel; each part traced (marching squares, RDP, beziers) in the reference's pixels |
| `convert.js` | `node convert.js <out.json> <W> <H> <ops.js>`: the traced parts moved into a W × H picture, fitted along its length, as `[op, material, path, options]` for a subject |
| `<subject>.palette.json` | per subject: the background, ink and colour settings, each material's reference colours and our paint |

Needs `ffmpeg` on the path.

## A reference that traces

Ask the model for one object in strict side view on a flat dark background, its parts closed shapes with an ink line, our
style's rules from `../README.md` in words. Check `debug.png`: every part its own colour, nothing magenta (unlabelled).
The grip, edge and other points of the game's look are measured from the trace, never by eye.
