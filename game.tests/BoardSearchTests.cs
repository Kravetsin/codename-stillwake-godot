using System;
using System.Linq;
using Stillwake.Game.Features.Research;
using Xunit;

namespace Stillwake.Game.Tests;

public sealed class BoardSearchTests
{
    private static readonly BoardFound[] Board =
    [
        new("iron", "Iron", "Железо", true),
        new("ironOre", "Iron ore", "Железная руда", true),
        new("furnace", "Furnace", "Печь", true),
        new("hullPlate", "Hull plate", "Плита корпуса", true),
        new("wallIron", "Wall: iron", "Стена: железо", true),
        new("pipe", "Pipe", "Труба", false),
        new("scrap", "Scrap", "Лом", true),
        new("heatPlate", "Heat plate", "Термоплита", true),
        new("plateRoller", "Plate roller", "Вальцы", false),
    ];

    /// <summary>Iron's ties, as the recipes would give them: made at the furnace, of ore, into plates, walls and pipes.</summary>
    private static Tie? Ties(string named, string id) => (named, id) switch
    {
        ("iron", "furnace") => Tie.MadeAt,
        ("iron", "ironOre") => Tie.From,
        ("iron", "hullPlate" or "wallIron" or "pipe") => Tie.UsedIn,
        ("furnace", "iron") => Tie.UsedIn,
        _ => null,
    };

    private static BoardHit[] Hits(string query) => BoardSearch.Matching(Board, query, Ties).ToArray();

    private static string[] Ids(string query) => Hits(query).Select(h => h.Found.Id).ToArray();

    [Fact]
    public void An_empty_query_names_nothing() => Assert.Empty(Ids(" .,- "));

    [Fact]
    public void A_word_start_is_found_case_ignored() => Assert.Equal(["scrap"], Ids("SCR"));

    [Fact]
    public void The_middle_of_a_word_finds_nothing() => Assert.Empty(Ids("cra"));

    [Fact]
    public void A_name_is_found_by_its_other_language() => Assert.Equal(["scrap"], Ids("лом"));

    [Fact]
    public void A_material_brings_in_where_it_is_made_what_it_is_of_and_what_takes_it()
    {
        Assert.Equal(["iron", "furnace", "ironOre", "hullPlate", "wallIron", "pipe"], Ids("iron"));
        Assert.Equal([null, Tie.MadeAt, Tie.From, Tie.UsedIn, Tie.UsedIn, Tie.UsedIn], Hits("iron").Select(h => h.Tie));
        Assert.All(Hits("iron").Skip(1), h => Assert.Equal("iron", h.Named?.Id));
        Assert.Null(Hits("iron")[0].Named);
    }

    [Fact]
    public void The_other_language_brings_in_the_same_ties() => Assert.Equal(Ids("iron"), Ids("железо"));

    [Fact]
    public void A_machine_brings_in_what_it_makes() => Assert.Equal(["furnace", "iron"], Ids("furnace"));

    [Fact]
    public void A_slip_of_a_letter_still_finds_a_long_word() => Assert.Equal(["furnace", "iron"], Ids("furnase"));

    [Fact]
    public void Swapped_letters_count_as_one_slip() => Assert.Equal("furnace", Ids("furnaec")[0]);

    [Fact]
    public void A_short_word_is_forgiven_no_slip() => Assert.Empty(Ids("pup"));

    [Fact]
    public void Yo_is_read_as_ye()
    {
        BoardFound[] board = [new("hedgehog", "Hedgehog", "Ёж", true)];
        Assert.Single(BoardSearch.Matching(board, "еж", Ties));
    }

    [Fact]
    public void Punctuation_is_ignored_and_every_word_must_match()
    {
        Assert.Equal("wallIron", Ids("wall iron")[0]);
        Assert.Empty(Ids("wall copper"));
    }

    [Fact]
    public void Too_many_names_bring_in_no_ties()
    {
        var hits = BoardSearch.Matching(Board, "p", (_, _) => Tie.UsedIn);
        Assert.True(hits.Count > BoardSearch.MostNamed);
        Assert.All(hits, h => Assert.Null(h.Tie));
        Assert.DoesNotContain(hits, h => h.Found.Id == "iron");
    }

    [Fact]
    public void Notes_come_before_shapes_then_by_name() => Assert.Equal(["heatPlate", "hullPlate", "plateRoller"], Ids("plat"));

    [Fact]
    public void An_exact_name_outranks_names_starting_with_it_and_those_come_after_its_ties()
    {
        var ids = Ids("iron");
        Assert.Equal("iron", ids[0]);
        Assert.True(Array.IndexOf(ids, "ironOre") > Array.IndexOf(ids, "furnace"));
    }

    [Fact]
    public void A_thing_is_listed_once() => Assert.Equal(Ids("iron").Length, Ids("iron").Distinct().Count());

    [Fact]
    public void Only_what_is_shown_is_offered()
    {
        BoardFound[] board = [new("furnace", "Furnace", "Печь", true)];
        Assert.Equal(["furnace"], BoardSearch.Matching(board, "furnace", Ties).Select(h => h.Found.Id));
    }

    [Fact]
    public void At_most_a_list_of_rows_is_given()
    {
        var many = Enumerable.Range(0, 20).Select(i => new BoardFound($"t{i}", $"Thing {i:00}", $"Вещь {i:00}", true));
        Assert.Equal(BoardSearch.Most, BoardSearch.Matching(many, "thing", Ties).Count);
    }

    [Theory]
    [InlineData(null, 1, 3, 0)]
    [InlineData(null, -1, 3, 2)]
    [InlineData(2, 1, 3, 0)]
    [InlineData(0, -1, 3, 2)]
    [InlineData(1, 1, 3, 2)]
    public void Arrows_step_round_the_rows(int? lit, int by, int count, int? want) => Assert.Equal(want, BoardSearch.Step(lit, by, count));

    [Fact]
    public void No_rows_light_nothing() => Assert.Null(BoardSearch.Step(0, 1, 0));
}
