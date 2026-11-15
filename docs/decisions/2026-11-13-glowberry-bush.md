# Spores grow a glowberry bush

- Status: accepted
- Date: 2026-11-13

## Context

The grow capsule drew one plant, the gourd vine ([gourd-vine](2026-11-13-gourd-vine.md)), whatever was planted. A spore
gives glowberries, not gourds, so the vine bearing pears over a crop of berries lied about what would come out.

The pick came from generated references again, this time through Codex (OpenAI's plugin for Claude Code), given our vine
as a reference picture. It drew three really different plants in four stages: a vine with berry clusters, a fungal stalk
and a stiff upright bush. The user picked the bush; Claude redrew it in code.

## Decision

- **Each crop its own plant.** The capsule draws `{kind}_{crop}_{stage}`, the crop being what its input holds:
  `growCapsule_seed_*` is the gourd vine, `growCapsule_spore_*` the glowberry bush. A test asks every grow station for a
  plant per crop and stage, so a new crop cannot ship without one.
- **The stake is the capsule's.** Both plants stand on the same rusted stake (`stake` in
  `tools/anime-art/subjects/machines.js`).
- **The bush** (`bushStage`): a brown trunk before the stake, four branches, narrow grey-green leaves in pairs up each,
  a crown of three at the top. Younger stages are the grown bush drawn shorter and narrower from the same foot; the sprout
  is three leaves. At the last stage four clusters of four berries hang on short spurs, drawn and lit from within as the
  glowberry item is.

## Omissions

- The berries' inner light is painted, as the item's; they light nothing in the room.
- The plants still do not sway or grow between their four pictures.
