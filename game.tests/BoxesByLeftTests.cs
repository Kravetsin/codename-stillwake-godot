using FsCheck;
using FsCheck.Xunit;
using Stillwake.Game.Features.Flora;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>The glowing plants near the view found by their boxes, as a walk over every plant would find them (planet-frame-rate).</summary>
public class BoxesByLeftTests
{
    [Fact]
    public void ABoxIsFoundByAViewItReachesIntoFromTheLeftAndNotByOneItOnlyTouches()
    {
        var boxes = new BoxesByLeft<string>();
        boxes.Add(1, 0, 0, 100, 10, "wide");
        boxes.Add(2, 90, 0, 95, 10, "narrow");
        var found = new List<string>();
        boxes.Meeting(96, 0, 200, 10, found);
        Assert.Equal(["wide"], found);
        found.Clear();
        boxes.Meeting(100, 0, 200, 10, found);
        Assert.Empty(found);
    }

    [Fact]
    public void ABoxTakenAwayIsFoundNoMoreAndItsNeighbourStill()
    {
        var boxes = new BoxesByLeft<int>();
        boxes.Add(7, 5, 0, 10, 10, 7);
        boxes.Add(8, 5, 0, 10, 10, 8);
        Assert.True(boxes.Remove(7));
        Assert.False(boxes.Remove(7));
        var found = new List<int>();
        boxes.Meeting(0, 0, 20, 20, found);
        Assert.Equal([8], found);
    }

    /// <summary>Whatever was added and taken away, a search finds exactly what a walk over every box held would.</summary>
    [Property(MaxTest = 300)]
    public void ItFindsWhatAWalkOverEveryBoxFinds(byte[][] made, byte[] gone, byte[] view)
    {
        made ??= [];
        var boxes = new BoxesByLeft<int>();
        var held = new Dictionary<int, (float L, float T, float R, float B)>();
        for (int key = 0; key < made.Length; key++)
        {
            if (made[key] is not { Length: >= 4 } b) continue;
            var box = ((float)b[0], (float)b[1], (float)b[0] + 1 + b[2] % 64, (float)b[1] + 1 + b[3] % 64);
            boxes.Add(key, box.Item1, box.Item2, box.Item3, box.Item4, key);
            held[key] = box;
        }
        foreach (byte key in gone ?? []) Assert.Equal(held.Remove(key), boxes.Remove(key));
        if (view is not { Length: >= 4 }) return;
        float l = view[0], t = view[1], r = l + view[2] % 100, bottom = t + view[3] % 100;
        var found = new List<int>();
        boxes.Meeting(l, t, r, bottom, found);
        var walked = held.Where(h => h.Value.R > l && h.Value.L < r && h.Value.B > t && h.Value.T < bottom).Select(h => h.Key);
        Assert.Equal(walked.Order(), found.Order());
    }
}
