using System.Linq;
using Stillwake.Game.Features.Research;
using Xunit;

namespace Stillwake.Game.Tests;

public sealed class BoardSearchTests
{
    private static readonly BoardFound[] Board =
    [
        new("hullPlate", "Hull plate", true),
        new("plateRoller", "Plate roller", false),
        new("scrap", "Scrap", true),
        new("heatPlate", "Heat plate", true),
        new("rus", "Плита корпуса", true),
    ];

    private static string[] Ids(string query) => BoardSearch.Matching(Board, query).Select(f => f.Id).ToArray();

    [Fact]
    public void An_empty_query_names_nothing() => Assert.Empty(Ids("  "));

    [Fact]
    public void A_name_holding_the_query_is_found_case_ignored() => Assert.Equal(["scrap"], Ids("CRA"));

    [Fact]
    public void Names_starting_with_it_come_first_then_notes_before_shapes_then_by_name() =>
        Assert.Equal(["plateRoller", "heatPlate", "hullPlate"], Ids("plate"));

    [Fact]
    public void Notes_come_before_shapes_among_names_starting_alike()
    {
        BoardFound[] board = [new("a", "Plate b", false), new("b", "Plate c", true)];
        Assert.Equal(["b", "a"], BoardSearch.Matching(board, "plate").Select(f => f.Id));
    }

    [Fact]
    public void Russian_names_match_case_ignored() => Assert.Equal(["rus"], Ids("плит"));

    [Fact]
    public void Nothing_is_found_by_a_name_not_shown() => Assert.Empty(Ids("roller x"));

    [Fact]
    public void At_most_a_list_of_rows_is_given()
    {
        var many = Enumerable.Range(0, 20).Select(i => new BoardFound($"t{i}", $"Thing {i:00}", true));
        Assert.Equal(BoardSearch.Most, BoardSearch.Matching(many, "thing").Count);
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
