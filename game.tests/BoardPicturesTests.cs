using System.Linq;
using Stillwake.Game.Features.Research;
using Stillwake.Sim;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>The board's pictures: a tool's icon baked oversampled, so it stays sharp at the board's zoom.</summary>
public class BoardPicturesTests
{
    [Fact]
    public void EveryCraftedToolFromItsRecipeIsBakedLargerAndNothingElseIs()
    {
        var m = new Registry();
        World.Create(1, HeroTests.Wreck, m.All);
        var recipes = m.Research.Recipes(0);
        var tools = recipes.Where(r => HeldLook.Of(r.Id) != null).ToList();
        Assert.NotEmpty(tools);
        // A craft's recipe names it with no placement: the board looks a tool up by that recipe.
        foreach (var tool in tools) Assert.Equal(40 * BoardPictures.ToolOver, BoardPictures.Side(tool.Id, tool.Placement, 40));
        foreach (var other in recipes.Where(r => HeldLook.Of(r.Id) == null)) Assert.Equal(40, BoardPictures.Side(other.Id, other.Placement, 40));
    }
}
