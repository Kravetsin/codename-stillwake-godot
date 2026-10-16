using System.Collections.Generic;
using System.Linq;
using Stillwake.Game.Shared;
using Stillwake.Sim.Research;
using static Stillwake.Game.I18n.En;

namespace Stillwake.Game.Features.Items;

/// <summary>What an analyzer's panel says it is doing: empty, at work, without power, or done with what it holds.</summary>
public enum Stage { Empty, Working, Unpowered, Done }

/// <summary>A blueprint or craft the analysed item goes into: opened, or closed for the items of its cost not analysed yet.</summary>
public sealed record Lesson(Recipe Recipe, bool Opened)
{
    /// <summary>Its name: a craft's item, a blueprint's tool and material.</summary>
    public string Name => Recipe.Placement is { } p
        ? p.Material is { } m ? $"{T($"tool.{p.Kind}")}: {T($"material.{m}")}" : T($"tool.{p.Kind}")
        : T($"item.{Recipe.Id}");

    /// <summary>The one tooltip over its picture: opened, or each item still to analyse.</summary>
    public Tip Tip
    {
        get
        {
            if (Opened) return new Tip(Name, new TipLine(T("research.opened"), Tone.Good));
            var lines = Recipe.Unanalysed.Select(item => new TipLine(T("research.notAnalysed", ("item", T($"item.{item}"))), Tone.Unknown, item)).ToList();
            return new Tip(Name, lines);
        }
    }
}

/// <summary>
/// What an analyzer's panel shows (0027): its stage, how far its work is, and once what it holds is
/// analysed, every blueprint and craft whose cost has that item in it, opened or not. Nothing about
/// any other item: an empty analyzer lists nothing.
/// </summary>
/// <param name="Percent">How far the analysis is, rounded down; a hundred once done.</param>
public sealed record Lessons(Stage Stage, string? Item, int Percent, IReadOnlyList<Lesson> Recipes)
{
    /// <summary>
    /// The panel of an analyzer whose slot shows <paramref name="item"/>, this peer's own moves made, and
    /// whose work the world says is <paramref name="at"/>: an item just put in is at work from nothing until
    /// its tick runs, one analysed already is done at once.
    /// </summary>
    public static Lessons Of(IResearchQueries research, string? item, Analyzing? at)
    {
        if (item == null) return new Lessons(Stage.Empty, null, 0, []);
        if (research.Analysed.Contains(item))
        {
            var recipes = research.Recipes()
                .Where(r => r.Cost.Any(c => c.Item == item))
                .Select(r => new Lesson(r, research.Knows(r.Id)))
                .ToList();
            return new Lessons(Stage.Done, item, 100, recipes);
        }
        if (at == null || at.Item != item) return new Lessons(Stage.Working, item, 0, []);
        // Rounded down, and short of a hundred until the world says it is done.
        int percent = at.Work > 0 ? (int)((long)at.Progress * 100 / at.Work) : 0;
        var stage = at.Problem == "research.unpowered" ? Stage.Unpowered : Stage.Working;
        return new Lessons(stage, item, System.Math.Clamp(percent, 0, 99), []);
    }

    /// <summary>What the panel shows of its recipes, as text: their pictures are built again only when it changes.</summary>
    public string Key => $"{Stage}|{Item}|" + string.Join(",", Recipes.Select(r => $"{r.Recipe.Id}:{r.Opened}:{string.Join("+", r.Recipe.Unanalysed)}"));
}
