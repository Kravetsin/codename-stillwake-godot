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
    public void EveryHelmetPictureHasItsLampAndScannerMounts()
    {
        foreach (var helmet in HelmetPictures)
        {
            Assert.True(LampMounts.On.ContainsKey(helmet), $"{helmet} has no lamp mount");
            Assert.True(ScannerInk.On.ContainsKey(helmet), $"{helmet} has no scanner row");
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
