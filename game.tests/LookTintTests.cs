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
    public void A_darker_skin_darkens_a_head_and_a_skin_picture_but_no_clothes()
    {
        int deepest = HeroLooks.Skins.Length - 1;
        Assert.True(LookTint.Of("head", deepest, 0)!.Value.Apply(0.8f, 0.7f, 0.2f).R < 0.8f);
        Assert.True(LookTint.Of("outfit_1_upperArm_skin", deepest, 0)!.Value.Apply(0.8f, 0.7f, 0.2f).R < 0.8f);
        Assert.Null(LookTint.Of("outfit_1_upperArm", deepest, 0));
    }
}
