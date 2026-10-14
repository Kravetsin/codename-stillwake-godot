# 0026 — Light from emitters, by cells

- Status: rejected — the Godot lights of `game/features/light` stay
- Date: 2026-10-14
- Code: branch `exp/cell-light` (not merged), its full proposal in this file there

## Context

The TypeScript game lit the ship cell by cell (0019) and its furnace felt better than the port's:
sharp, crackling, embers lighting the wall. The team also wanted light that is easy to give to a new
thing: put an emitting layer on it, choose only strength and colour.

## What was tried

A second mode beside the `PointLight2D`s, switched live with F9: emitter nodes whose glowing pixels
put light into cells, spread by the TypeScript sweeps and shaded by `Lit.cs`; embers and sparks drew
soft spots a pixel at a time. It cost 0.1–0.3 ms a frame at playing zooms. The relief by a fire first
warped, its side swinging with every flame picture, until it was smoothed over 300 ms.

## Decision

Both developers played the two modes side by side and kept the Godot lights: sharp shadows from walls
and open doors and an evenly shaded ship outweigh the cells mode's crackling fire. The branch stays
on the remote for its ideas — per-pixel ember spots, the emitter-mask table — should one be wanted
in the Godot mode.
