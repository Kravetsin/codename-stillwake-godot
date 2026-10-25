using System.Linq;
using Stillwake.Content;
using Stillwake.Game.Features.Dev;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>The dev panel's item picker finds items by name or id, case ignored.</summary>
public class ItemPickerTests
{
    [Fact]
    public void ListsEveryItemForNoQuery()
    {
        Assert.Equal(Content.Items.All.Select(i => i.Id).Order(), ItemPicker.Matching("  ").Select(i => i.Id).Order());
    }

    [Fact]
    public void FindsAnItemByAPartOfItsNameOrItsIdCaseIgnored()
    {
        Assert.Contains(ItemPicker.Matching("OXYGEN can"), i => i.Id == "canister");
        Assert.Contains(ItemPicker.Matching("helmetla"), i => i.Id == "helmetLamp");
        Assert.DoesNotContain(ItemPicker.Matching("oxygen"), i => i.Id == "iron");
        Assert.Empty(ItemPicker.Matching("no such thing"));
    }

    [Fact]
    public void SortsByGroupThenName()
    {
        var all = ItemPicker.Matching("");
        Assert.Equal(all.OrderBy(i => i.Group).ThenBy(i => ItemPicker.NameOf(i.Id), System.StringComparer.OrdinalIgnoreCase), all);
    }

    [Fact]
    public void NamesEveryItem()
    {
        Assert.All(Content.Items.All, item => Assert.NotEqual($"item.{item.Id}", ItemPicker.NameOf(item.Id)));
    }
}
