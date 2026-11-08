# Composed backdrops

- Status: accepted; supersedes [far-land-in-ink](2026-11-02-far-land-in-ink.md) and the painted mesas, ridge and hills of
  [0079](0079-a-planets-surface-has-its-look-and-grows-plants.md)
- Date: 2026-11-08

## Context

Playtest 2026-11-06 asked for Terraria-like parallax backgrounds on a planet. Four rounds of picks on the backdrops page
(https://claude.ai/artifact/Q4XTQZ5s7YyPCUFZ555KPw) settled it. Every painted backdrop, the ones in the game included, read
as a child's drawing. Backdrops **assembled from the game's own pictures** — its acacias, bushes, rocks and tufts, its soil
and stone walls and their grass tops, scaled down with distance and veiled toward the fog — were liked. The user then asked
that a planet's backdrop be **composed from its seed and its own content**, so every planet type gets its own without new art,
and that the land follow the terrain's height ("Горизонт по рельефу"), so a valley never shows a flat band of fill.

## Decision

- **The land is composed, not painted** (`game/features/backdrop`): the page's seeded generator (`surface.js`) ported one for
  one, a seed's draws checked equal to the page's. Three layers far to near; along the panorama, by smooth noise, a stone
  range here and there with outcrops at its feet, grassy hills with groves of three to seven trees and shrubs at their feet,
  open plain with lone trees, a forest edge in two rows. Grassy ridges get the grass's own green in shade, rows of grass
  tops along the crest, belts of bushes and tufts down the slope; under the mid and near crests, shelves of land about 50 px
  apart, each a step darker, so no wide flat face shows.
- **What is the planet's own is read where it is:** its plants and their chances (`PlanetType.Plants`), their pictures and
  sink (`FloraLooks`), its ground's waves (`PlanetType.Ground`), its soil and stone pictures, its fog. A plant's role (tree,
  shrub, small solid, cover) comes from its numbers (`Tall`, `Room`, `Chance`), never its name, so a planet with no tall plant
  gets groves of shrubs. The recipe's numbers are data (`gc/BackdropLooks.cs`): one recipe until a planet type names its own.
- **The seed is the surface's**, through an integer rng: every peer sees the same land, client only.
- **On the terrain:** the ground line is the surface's top row averaged over ±100 columns round the camera, read from the
  grid once a showing; each layer sits on it moved by its share of the camera's height, as the sky's gradient does.
- **Drawn once a showing:** a node per layer whose canvas item holds its draws, the 2048 px tile twice; a shader veils it
  toward a dark and the fog by depth, then the day's tint, so dusk and night darken it with the fog.
- **The sky stays:** gradient, gas giant, clouds and sun keep their pictures and places (`render/PlanetSky.cs`). The painted
  `far`, `ridge`, `hills` and their `backdrop` subject are deleted.

## Deliberately not here

- **Caves:** every cave keeps its back wall and none can be hewn, so an underground backdrop would show nowhere; the page's
  cave composer waits for halls without back walls, which also need daylight and surface creatures kept out.
- **Digging does not move the horizon:** the rows are read once a showing; a crater is small beside ±100 columns.
- **No ease of the ground line:** the mean of 201 columns moves smoothly as it is; an ease waits for frames that show steps.
- **The page's foreground walk, its placements and time buttons:** they were for choosing.
