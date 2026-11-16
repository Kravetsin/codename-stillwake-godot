using Stillwake.Game.Features.Crew;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>
/// The hand is its own picture hung at the wrist (hand-at-wrist): strips of frames, the glove and the bare hand, each near and far, and every arm cut on
/// one whole row of its picture.
/// </summary>
public class HandTests
{
    [Theory]
    [InlineData("hero_hand_glove")]
    [InlineData("hero_hand_skin")]
    [InlineData("hero_hand_far_glove")]
    [InlineData("hero_hand_far_skin")]
    public void EachHandIsAStripOfItsFrames(string hand)
    {
        Assert.True(Png.Has(hand), hand);
        // A frame 6 by 7 px and a margin of 3 round it (Figure's joint "hand"), its four frames side by side.
        Assert.Equal((4 * 12 * Png.Scale, 13 * Png.Scale), Png.Size(Png.Of(hand)));
    }

    [Fact]
    public void TheWristCutsTheArmOnARowOfItsPicture()
    {
        // The arm's picture: a margin of 3, the shoulder 2.5 down, the elbow a bone below it, the wrist below that.
        double row = (3 + 2.5 + Rig.Arm.Upper + Rig.Wrist) * Png.Scale;
        Assert.Equal(66, row, 9);
    }

    [Fact]
    public void EveryOutfitSaysWhichHandItWears() => Assert.Equal(HeroLooks.Outfits, HeroLooks.OutfitGloved.Length);
}
