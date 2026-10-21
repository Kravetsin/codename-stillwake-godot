# 0095 — A planet is a type and a seed

- Status: accepted
- Date: 2026-10-21

## Context

The game may come to hundreds of planets. The agreed way there: a small hand-authored set of planet types, 8 to 15 in time,
written as data; a planet is a type and a seed (and later a palette variant); art in palette ramps, backdrops and flora
assembled from parts; set pieces such as ruins placed by the generator. Today there is one planet and its small twin for
tests. What makes it that planet is spread over `Ground` (and its defaults), `Surfaces` (gravity, air, sky, builds; 0076),
`Flora` (depth bands and the caves' patch numbers; 0079, 0080), material ids written into `Terrain`, and the client's band
tint and backdrop pictures. A second type would mean editing all of those places.

## Decision

- **`PlanetType` is a surface kind that grows from a seed** (`sim/Content/PlanetTypes.cs`): a record deriving from
  `Surfaces.Kind`, so a planet's rules (gravity, thin air, sky, no debris, what may be built) are the kind's, as every module
  already reads them through `Rules`. To them it adds what its ground grows from:

  | Field | What |
  | ----- | ---- |
  | `Id` | the kind's id, saved with each surface of it |
  | `Gravity`, `ThinAir`, `Sky`, `Debris`, `Builds` | its rules (0076), inherited from `Kind` |
  | `Ground` | its terrain: size, surface, ranges, hills, soil depth, caves, landmarks, ruins' counts and places, core rows (0077, 0085, 0087, 0091) |
  | `Soil`, `Stone`, `Bedrock`, `Core` | the materials of its strata: the top layer, the rock under it, the floor and sides, the core over the floor; what each yields is the material's (`Materials`) |
  | `Plants` | its surface growths with their chances (0079) |
  | `Caves` | its cave growths, veins among them, with their chances by band (0080) |
  | `Bands`, `PatchWave`, `PatchMost`, `Hall` | its caves' depth bands and how their growth gathers |

  `PlanetTypes.First` holds every number of the first planet, `Ground`'s included (its record has no defaults now);
  `PlanetTypes.Small` is the first grown small, for tests and the fuzz. `Surfaces.All` lists the strip and every type.
- **Per type or per planet.** A type is everything above. A planet is its type's id and a seed, as a `Surface` already saves
  them; a palette variant, when the art has ramps, is drawn from the seed on the client and saved nowhere.
- **Saves.** The two types keep the ids saves hold (`planet`, `smallPlanet`): no migration, no format change. Generation is
  unchanged: the ground, flora and world hash of every seed tried are byte-identical before and after.
- **Growths stay one catalog.** `Flora.All` and `Flora.Cave` remain the list of what can grow anywhere: what each is (tall,
  anchor, yields, strength), which Building fells and the client draws by id. Their chances are the first planet's. Another
  type takes kinds from the catalog and re-weighs them (`with { Chance = … }`); a content test holds every type's kinds to
  the catalog's in all but where and how often they grow.
- **Looks stay on the client, keyed by the type id** (`game/Content/PlanetLooks.cs`): its backdrop set (the pictures of the
  sky behind its ground, `art/planet/…`) and its stone's tint by band. The sim names no look. A test fails for a type with no
  looks, or a growth of it with no pictures.
- **A second type is only data.** A test-only cold type (lower gravity, thin soil, three bands, its own chances) grows
  through the same generator; it is not shipped.
- **Tests pick a type** by name: `PlanetTypes.Small`, or one changed with `with` (`Small with { Ground = Small.Ground with
  { Spires = 0 } }`). `Terrain.Generate` and `Terrain.BandOf` take the type.

## Consequences

- A new type is one entry in `PlanetTypes.All` and one in `PlanetLooks.Of`, plus any new materials or growths in their
  catalogs; the content tests say what is missing.
- `Surfaces.Planet` and `Surfaces.SmallPlanet` are gone: `PlanetTypes.First` and `PlanetTypes.Small` in their place.

## Deliberately not handled

- **Which set pieces a type places.** Counts and places are in its `Ground`; the pieces are the one spire and the homes'
  schemes (`Ruins`), and their own materials (alloy, plaster, the plug's soil). A type picks its pieces once a second set exists.
- **Lava's burn** (`Lava.Burn`, `Lava.Reach`) is the lava material's, wherever it lies; a type only says its core is lava.
- **Choosing a type when a planet opens.** `OpenSurface` names the kind, as before; the drop pod and the galaxy choose later.
- **Palette ramps.** There is no art in ramps yet, so no palette id; the looks keyed by type are where it goes.
- **Looks by material or growth** (ground colours, grass on soil, sounds, flora and cave pictures) stay keyed by their ids, as
  catalogs a type draws from.
- **The test strip** is a drawn kind under the first type's rules, not a type.
