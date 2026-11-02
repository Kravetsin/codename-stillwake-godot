using System.Runtime.CompilerServices;
using Stillwake.Game.Features.Building;
using Stillwake.Game.I18n;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>Russian beside English: every key has its text, each feature's in its own file, and the mechanism under <c>T</c>.</summary>
public class RuFilesTests
{
    private static string Here([CallerFilePath] string path = "") => path;

    [Fact]
    public void EveryEnglishKeyHasARussianTextWithTheSameNames()
    {
        var problems = new List<string>();
        foreach (var (key, english) in En.Table.OrderBy(p => p.Key, StringComparer.Ordinal))
        {
            if (!Ru.Table.TryGetValue(key, out var russian)) problems.Add($"{key}: not translated");
            else if (!NumberForms.NamesIn(english).SetEquals(NumberForms.NamesIn(russian))) problems.Add($"{key}: names differ from \"{english}\"");
        }
        Assert.True(problems.Count == 0, string.Join('\n', problems));
    }

    [Fact]
    public void NoRussianKeyIsOutOfTheEnglishTable()
    {
        Assert.Empty(Ru.Table.Keys.Where(k => !En.Table.ContainsKey(k)).Order(StringComparer.Ordinal));
    }

    [Fact]
    public void EveryFeatureFileHasARussianFileBesideIt()
    {
        var game = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(Here())!, "..", "game"));
        var missing = Directory.EnumerateFiles(game, "*.En.cs", SearchOption.AllDirectories)
            .Where(f => !f.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).Any(p => p is "bin" or "obj" or ".godot"))
            .Where(f => !File.Exists(f[..^".En.cs".Length] + ".Ru.cs"));
        Assert.Empty(missing);
    }

    // One game word, one Russian word: and what a label says again on a switch has one answer.
    [Fact]
    public void WhatEnglishSaysAlikeRussianSaysAlike()
    {
        var split = En.Table.GroupBy(p => p.Value, StringComparer.Ordinal)
            .Where(g => g.Select(p => Ru.Table.GetValueOrDefault(p.Key)).Distinct().Count() > 1)
            .Select(g => $"{g.Key}: {string.Join(", ", g.Select(p => $"{p.Key} = {Ru.Table.GetValueOrDefault(p.Key)}"))}");
        Assert.Empty(split);
    }

    // Else a label told again on a switch back says another key's English: "Выход" once came back as Output, not Quit.
    [Fact]
    public void WhatRussianSaysAlikeEnglishSaysAlike()
    {
        var split = Ru.Table.GroupBy(p => p.Value, StringComparer.Ordinal)
            .Where(g => g.Select(p => En.Table.GetValueOrDefault(p.Key)).Distinct().Count() > 1)
            .Select(g => $"{g.Key}: {string.Join(", ", g.Select(p => $"{p.Key} = {En.Table.GetValueOrDefault(p.Key)}"))}");
        Assert.Empty(split);
    }

    [Fact]
    public void AMissingTextFallsBackToEnglishThenToTheKey()
    {
        Assert.Equal("Удалить", En.In(Language.Russian, "worlds.delete"));
        Assert.Equal("Delete", En.In(Language.English, "worlds.delete"));
        Assert.Equal("no.such.key", En.In(Language.Russian, "no.such.key"));
    }

    [Theory]
    [InlineData(1, "1 клетка отсюда")]
    [InlineData(2, "2 клетки отсюда")]
    [InlineData(5, "5 клеток отсюда")]
    [InlineData(11, "11 клеток отсюда")]
    [InlineData(21, "21 клетка отсюда")]
    public void ANumberTakesItsWordsForm(int cells, string said)
    {
        Assert.Equal(said, En.In(Language.Russian, "markers.cells", ("cells", cells)));
    }

    [Theory]
    [InlineData(1, 0)]
    [InlineData(2, 1)]
    [InlineData(4, 1)]
    [InlineData(5, 2)]
    [InlineData(11, 2)]
    [InlineData(12, 2)]
    [InlineData(14, 2)]
    [InlineData(21, 0)]
    [InlineData(22, 1)]
    [InlineData(100, 2)]
    [InlineData(101, 0)]
    [InlineData(111, 2)]
    [InlineData(0, 2)]
    public void RussianPicksItsForm(int n, int form)
    {
        Assert.Equal(form, NumberForms.Russian(n));
    }

    [Theory]
    [InlineData(1, "Наполняется · 1 из 1 клетки")]
    [InlineData(2, "Наполняется · 1 из 2 клеток")]
    [InlineData(5, "Наполняется · 1 из 5 клеток")]
    [InlineData(11, "Наполняется · 1 из 11 клеток")]
    [InlineData(21, "Наполняется · 1 из 21 клетки")]
    public void AFormFollowsItsOwnNumberAmongOthers(int cells, string said)
    {
        Assert.Equal(said, En.In(Language.Russian, "air.filling", ("aired", 1), ("cells", cells)));
    }

    [Fact]
    public void WhatIsNoWholeNumberTakesTheManyForm()
    {
        Assert.Equal("1.5 клеток", NumberForms.Fill("{n} {n|клетка|клетки|клеток}", [("n", "1.5")]));
    }

    [Theory]
    [InlineData("ru", Language.Russian)]
    [InlineData("RU", Language.Russian)]
    [InlineData("en", Language.English)]
    [InlineData("uk", Language.English)]
    [InlineData("", Language.English)]
    public void AFirstRunSpeaksRussianOnlyOnARussianSystem(string locale, Language language)
    {
        Assert.Equal(language, Languages.OfSystem(locale));
    }

    [Fact]
    public void ALanguageIsKeptByItsName()
    {
        foreach (var language in Enum.GetValues<Language>()) Assert.Equal(language, Languages.Named(Languages.NameOf(language)));
        Assert.Null(Languages.Named(""));
    }

    [Theory]
    [InlineData("Delete", Language.English, Language.Russian, "Удалить")]
    [InlineData("Удалить", Language.Russian, Language.English, "Delete")]
    [InlineData("Host\nOpen this world to friends", Language.English, Language.Russian, "Хост\nОткрыть мир друзьям")]
    [InlineData("a world's own name", Language.English, Language.Russian, "a world's own name")]
    [InlineData("", Language.English, Language.Russian, "")]
    public void TextOnScreenIsToldAgainLineByLine(string shown, Language from, Language to, string said)
    {
        Assert.Equal(said, Languages.Retold(shown, from, to));
    }

    [Fact]
    public void ACardNamesItselfInTheLanguageSpoken()
    {
        var card = Tools.All.OfType<PlaceTool>().First(t => t.Material != null);
        Assert.Contains(": ", card.Label);
        Assert.Equal($"{En.T(card.Name)}: {En.T(card.Material!)}", card.Label);
    }
}
