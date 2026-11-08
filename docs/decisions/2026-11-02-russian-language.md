# The game speaks Russian

- Status: accepted; the user asked for it, 2026-11-02.

## Decision

The whole game can be played in Russian, chosen in the menu's settings; English stays complete and unchanged, and is what a missing text falls back on, then the key.

- **The setting** is the machine's (`ui/language` in `settings.cfg`, `Settings.Language`), like the slots' size. On a first run it follows the system: Russian on a Russian system, English on any other.
  Nothing about language reaches the sim, a save or a hash: refusals are the sim's keys, told by the same table.
- **`T(key, ...)` keeps its signature** and no caller changed for it. `En.Speaking` is the language now; `En.In(language, key, ...)` is the same lookup without that global, for tests.
- **A feature's Russian lies beside its English**, `logic/<Name>.Ru.cs` by `logic/<Name>.En.cs`. `RuFilesTests` fails on a key with no Russian, on different `{placeholders}` and on a missing file, so a new English text fails until it is translated.
- **A switch takes hold at once** without any feature knowing of it. `Main.Retell` walks the tree and tells every label, button and placeholder again, line by line: a line that is a whole text of the outgoing language
  becomes the same key's in the new one; anything else, as a world's name, is left. Panels read on a timer and put the rest right themselves. This needs each text to have one answer both ways, so two tests hold what is said alike in one language alike in the other.

## Number forms

Russian has three forms by the number, and nothing else here needs them, so they are in the text and nowhere else: `{cells} {cells|клетка|клетки|клеток}`, one, few, many, the number written apart.
`NumberForms` picks by the rule (21 takes one, 11 to 14 take many). What is no whole number takes many. English texts are untouched and pass through the same fill.

## Joined texts

Places that join `T(...)` pieces stay as they are while the Russian reads right. The one that did not is the build menu's cost, now `research.taken`: English still "{n} {what}", Russian "{what} ×{n}".

## Deliberately not done

- **A dictionary of plural rules or a third language:** two languages, one rule. A third brings its own and the enum takes it.
- **A line joined at run time is not retold on a switch:** the red prompt's reason, a line built once with its numbers. It says the new language the next time it changes. A player switches language in a menu, with such lines rarely up.
- **The dev panel stays English,** as does the name STILLWAKE; the item names in its picker follow the language, being the game's.
- **No grammar for names put in sentences:** an item's name goes in as its nominative, and the Russian around it is written so that reads ("нужно: {what}").
- **Board notes clipping long names when zoomed out** is the same in both languages and left.
