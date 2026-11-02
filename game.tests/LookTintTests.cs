using Stillwake.Game.Features.Crew;
using Xunit;

namespace Stillwake.Game.Tests;

public class LookTintTests
{
    [Fact]
    public void The_brown_hair_is_drawn_in_stays_as_drawn() => Assert.Null(LookTint.Of("hair_3", 0, HeroLooks.HairDrawn));

    [Fact]
    public void Skin_paint_becomes_the_ramp_chosen_keeping_it_in_order_from_light_to_deep()
    {
        for (int skin = 0; skin < HeroLooks.Skins.Length; skin++)
        {
            var (r, g, b) = LookTint.Of("head", skin, 0)!.Value.Apply(0xe2 / 255f, 0xc0 / 255f, 0x9f / 255f);
            int own = HeroLooks.Skins[skin][1];
            Assert.Equal((own >> 16 & 0xff) / 255f, r, 3);
            Assert.Equal((own >> 8 & 0xff) / 255f, g, 3);
            Assert.Equal((own & 0xff) / 255f, b, 3);
        }
    }

    [Fact]
    public void Gear_takes_nothing()
    {
        Assert.Null(LookTint.Of("torso", 0, 0));
        Assert.Null(LookTint.Of("face_1", 0, 0));
    }

    [Fact]
    public void Hair_at_its_painted_tone_is_the_colour_chosen_and_lighter_paint_lighter()
    {
        var blond = LookTint.Of("beard_1", 0, 3)!.Value;
        var (r, g, b) = blond.Apply(0.33f, 0.33f, 0.33f);
        Assert.Equal(0xd6 / 255f, r, 3);
        Assert.Equal(0xb2 / 255f, g, 3);
        Assert.Equal(0x62 / 255f, b, 3);
        Assert.True(blond.Apply(0.5f, 0.5f, 0.5f).R > r);
    }

    [Fact]
    public void A_hue_in_the_paint_does_not_reach_the_hair()
    {
        var black = LookTint.Of("hair_1", 0, 0)!.Value;
        var (r, _, b) = black.Apply(0.25f, 0.15f, 0.25f);
        Assert.True(b / r < 1.2f);
    }

    [Fact]
    public void A_darker_skin_darkens_a_head_all_over_and_a_bare_arm_only_where_skin()
    {
        int deepest = HeroLooks.Skins.Length - 1;
        var head = LookTint.Of("head", deepest, 0)!.Value;
        Assert.True(head.Apply(0.8f, 0.7f, 0.2f).R < 0.8f);
        var arm = LookTint.Of("jumpsuit_forearm", deepest, 0)!.Value;
        Assert.True(arm.Apply(226 / 255f, 192 / 255f, 159 / 255f).R < 226 / 255f);
        Assert.Equal((212 / 255f, 163 / 255f, 75 / 255f), arm.Apply(212 / 255f, 163 / 255f, 75 / 255f));
    }

    [Theory]
    [InlineData(226, 192, 159, true)] // skin, its base
    [InlineData(160, 102, 95, true)] // skin, its deep
    [InlineData(212, 163, 75, false)] // the jumpsuit's yellow
    [InlineData(225, 188, 108, false)] // lit
    [InlineData(118, 58, 49, false)] // in deep shadow
    [InlineData(60, 68, 84, false)] // gear
    public void Skin_is_told_from_the_jumpsuit_and_the_gear(int r, int g, int b, bool skin) => Assert.Equal(skin, LookTint.IsSkin(r / 255f, g / 255f, b / 255f));
}
