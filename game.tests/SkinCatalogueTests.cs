using System.Collections.Generic;
using System.Linq;
using Stillwake.Game.Features.Crew;
using Stillwake.Game.Shared;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>
/// Every helmet's picture, an item's or a skin's (hero-skins), has where the lamp and the scanner sit on it and how it fits the
/// suit: a helmet skin drawn without them would wear the basic helmet's lamp and crest on its own dome.
/// </summary>
public class SkinCatalogueTests
{
    private static IEnumerable<string> HelmetPictures =>
        Content.Items.All.Where(i => i.Equip == Content.Helmet.Slot).Select(i => i.Id)
            .Concat(Skins.All.Where(s => s.Kind == Skins.Head).Select(s => s.Id));

    [Fact]
    public void EveryStillHelmetHasItsShellGlassAndCollar()
    {
        // light-drone (a try): nothing mounts on a helmet; it sits still on a rubber collar, the head seen through its glass.
        foreach (var helmet in HelmetPictures)
        {
            string drawn = helmet == Content.Helmet.Basic ? "helmet" : helmet;
            foreach (string part in (string[])["shell", "collar"])
                Assert.True(Png.Has($"hero_{drawn}_{part}"), $"hero_{drawn}_{part}");
            Assert.True(Png.Has(helmet == Content.Helmet.Basic ? "hero_visor" : $"hero_{helmet}_visor"), $"{helmet}'s glass");
            if (BodyFit.Of(helmet).Sealed) Assert.True(Png.Has($"hero_{drawn}_seal"), $"hero_{drawn}_seal");
        }
    }

    [Fact]
    public void EveryHelmetPictureFitsTheSuit()
    {
        foreach (var helmet in HelmetPictures)
            Assert.Contains(helmet, BodyFit.Fitted);
    }

    [Fact]
    public void EverySkinIdIsOneOnceAndOfAKnownKind()
    {
        Assert.Equal(Skins.All.Count, Skins.All.Select(s => s.Id).Distinct().Count());
        Assert.All(Skins.All, s => Assert.Contains(s.Kind, Skins.Kinds));
    }
}
