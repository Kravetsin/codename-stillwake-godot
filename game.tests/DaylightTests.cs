using System;
using Xunit;

namespace Stillwake.Game.Tests;

public class DaylightTests
{
    private static readonly PlanetLooks.Look Look = PlanetLooks.First;
    private static readonly Stillwake.Content.Sky.Cycle Day = Stillwake.Content.Sky.First;
    private static int Line => Day.Length - Day.Night;

    private static void Near((float R, float G, float B) want, (float R, float G, float B) got)
    {
        Assert.Equal(want.R, got.R, 4);
        Assert.Equal(want.G, got.G, 4);
        Assert.Equal(want.B, got.B, 4);
    }

    [Fact]
    public void Whole_by_day_from_its_start_to_dusk()
    {
        foreach (int t in new[] { 0, 1, Line / 2, Line - Daylight.Dusk })
        {
            Near((1, 1, 1), Daylight.Sky(Look, t, Day));
            Near((1, 1, 1), Daylight.Backdrop(Look, t, Day));
        }
    }

    [Fact]
    public void Dusk_ends_on_night_at_the_line_and_night_holds_till_dawn()
    {
        Near(Look.NightSky, Daylight.Sky(Look, Line, Day));
        Near(Look.NightSky, Daylight.Sky(Look, Day.Length - Daylight.Dawn, Day));
        Near(Look.NightBackdrop, Daylight.Backdrop(Look, Line + 1, Day));
        // Halfway through dusk, the look's own dusk.
        Near(Look.DuskSky, Daylight.Sky(Look, Line - Daylight.Dusk / 2, Day));
        Near(Look.DuskBackdrop, Daylight.Backdrop(Look, Day.Length - Daylight.Dawn / 2, Day));
    }

    [Fact]
    public void Dawn_ends_on_day_where_the_day_starts_again()
    {
        var last = Daylight.Sky(Look, Day.Length - 1, Day);
        Assert.InRange(last.R, 0.99f, 1f);
        Near((1, 1, 1), Daylight.Sky(Look, Day.Length, Day));
        Near(Daylight.Sky(Look, 5, Day), Daylight.Sky(Look, Day.Length + 5, Day));
        Near(Daylight.Sky(Look, Line + 7, Day), Daylight.Sky(Look, Line + 7 - 2 * Day.Length, Day));
    }

    [Fact]
    public void The_sun_rises_halfway_through_dawn_and_sets_halfway_through_dusk_crossing_steadily()
    {
        int rise = Day.Length - Daylight.Dawn / 2, set = Line - Daylight.Dusk / 2;
        Assert.Equal(0f, Daylight.Sun(rise, Day));
        Assert.Equal(1f, Daylight.Sun(set, Day));
        Assert.Null(Daylight.Sun(set + 1, Day));
        Assert.Null(Daylight.Sun(rise - 1, Day));
        Assert.Null(Daylight.Sun(Line + Day.Night / 2, Day));
        // Noon halfway between, the same a day later or earlier.
        float up = set + Day.Length - rise;
        Assert.Equal(0.5f, Daylight.Sun((int)(rise + up / 2) % Day.Length, Day)!.Value, 4);
        Assert.Equal(Daylight.Sun(100, Day), Daylight.Sun(100 + 3 * Day.Length, Day));
        Assert.Equal(Daylight.Sun(100, Day), Daylight.Sun(100 - Day.Length, Day));
        float? was = null;
        for (int t = rise; t <= rise + (int)up; t++)
        {
            float now = Daylight.Sun(t, Day)!.Value;
            if (was is float w) Assert.InRange(now - w, 0.99f / up, 1.01f / up);
            was = now;
        }
    }

    [Fact]
    public void No_step_between_neighbouring_ticks_is_more_than_a_fades_share()
    {
        // The largest change a tick may bring: from whole to night's darkest over half a fade.
        float most = 1f / (Daylight.Dusk / 2) + 1e-4f;
        var was = Daylight.Sky(Look, 0, Day);
        for (int t = 1; t <= Day.Length; t++)
        {
            var now = Daylight.Sky(Look, t, Day);
            Assert.True(Math.Abs(now.R - was.R) <= most && Math.Abs(now.G - was.G) <= most && Math.Abs(now.B - was.B) <= most, $"a step at {t}");
            was = now;
        }
    }
}
