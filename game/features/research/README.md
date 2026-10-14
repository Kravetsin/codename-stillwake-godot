# research (client): what the world has just learned

Knowledge without a word but its title (ADR 0024), ported from `apps/game/src/features/research/`
(G-08). An analyzer is worked as a crate is, through the items feature's panel, which shows its slot,
how far its analysis is and why it stands (`IResearchQueries.At`).

## Files

| File             | Holds                                                                             | Godot |
| ---------------- | --------------------------------------------------------------------------------- | ----- |
| `News.cs`        | `LearnedSince`: what is known now and was not at the last read (`news.ts`)        | no    |
| `LearnedView.cs` | the "New blueprints" popup with the blueprints' pictures, for six seconds (`Learned.tsx`) | yes |

`News.cs` is compiled again into `game.tests/`, where `news.test.ts` is ported (`NewsTests`).

## Guarantees

- `Known` is read every 250 ms through the current registry's research; what is new since the last
  read shows on every peer, the pictures the build menu's cards are (`BlueprintArt.Thumbnail`). Nothing at the first read, nor
  after a load that knows less.

## Differences from the TypeScript

- The pictures are composed from `art/*.png` by `render/BlueprintArt.cs` (`placementPicture` in
  `render/blueprint-art.ts`), the one thumbnail maker, which the build menu uses too.
