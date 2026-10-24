# research (client): what an analyzer's screen says

Knowledge without a word ([0024](../../../docs/decisions/0024-blueprints-are-learned-by-analysis.md),
[0027](../../../docs/decisions/0027-what-is-analysed-opens-what-is-made-of-it.md)). An analyzer is
worked as a crate is, through the items feature's panel (`features/items/AnalyzerPanel.cs`), which
speaks only of the item in its slot: how far its analysis is, why it stands, and once it is analysed
every blueprint and craft it goes into, opened or still closed. Nothing pops up over the hero: with
many blueprints opening at once a list would cover the screen.

## Files

| File                 | Holds                                                                     | Godot |
| -------------------- | ------------------------------------------------------------------------- | ----- |
| `AnalyzerScreens.cs` | each analyzer's screen, drawn over its picture                            | yes   |
| `logic/Research.En.cs` | Its player text (`En`) | no |

## Guarantees

- `IResearchQueries.At` is read for every analyzer every 100 ms through the current registry
  (`Main.ShowWorld` makes the view again for a new one). Its screen shows lines of text running across
  it while it works, a green check once what it holds is analysed (`research.nothingNew`), and stays
  dark when it is unpowered. Empty on a powered network, or holding an item in no cost
  (`research.nothingToLearn`, 0027 amended), a cursor blinks: there is nothing to analyse. Drawn
  unlit, for a screen gives its own light; the lines run every frame.

## Differences from the TypeScript

- The "New blueprints" popup (`Learned.tsx`, `news.ts`) is gone (0027): the screen's check says the
  analyzer is done, and its panel says what opened.
