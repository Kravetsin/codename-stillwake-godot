using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using Stillwake.Content;
using Stillwake.Sim.Places;
using Xunit;
using Rules = Stillwake.Content.Contacts;

namespace Stillwake.Sim.Tests;

/// <summary>The radar's own contacts (2026-11-08-radar-contacts): when places finds them, how long they stay, where they lie.</summary>
public class RadarContactsTests
{
    /// <summary>A ship of one cell: places needs nothing more to watch, and hours of ticks run fast.</summary>
    private static Scenario Ship(uint seed = 1) => new(".", seed);

    /// <summary>
    /// <paramref name="ticks"/> ticks of <paramref name="w"/>: for each contact, the tick it came up and the tick it went (null
    /// while up), and the most up at once.
    /// </summary>
    private static (List<(Contact Contact, int Up, int? Gone)> Seen, int Most) Watch(Scenario w, int ticks)
    {
        var seen = new List<(Contact Contact, int Up, int? Gone)>();
        int most = 0;
        var places = w.M.Places;
        for (int t = 1; t <= ticks; t++)
        {
            w.Run();
            foreach (var c in places.Contacts)
                if (!seen.Exists(s => s.Contact.Id == c.Id)) seen.Add((c, t, null));
            for (int i = 0; i < seen.Count; i++)
                if (seen[i].Gone == null && !places.Contacts.Any(c => c.Id == seen[i].Contact.Id)) seen[i] = seen[i] with { Gone = t };
            most = Math.Max(most, places.Contacts.Count);
        }
        return (seen, most);
    }

    [Fact]
    public void TheFirstComesUpAtFirstAndStaysExactlyLasts()
    {
        var (seen, _) = Watch(Ship(), Rules.First + Rules.Lasts);
        var first = Assert.Single(seen);
        Assert.Equal(Rules.First, first.Up);
        Assert.Equal(Rules.First + Rules.Lasts, first.Gone);
        Assert.NotNull(Vessels.Class(first.Contact.Class));
        Assert.Equal(Rules.Lasts, first.Contact.Left);
    }

    [Fact]
    public void EachNextComesEveryMinToEveryMaxAfterTheLastAndNeverTwoAtOnce()
    {
        var (seen, most) = Watch(Ship(), Rules.First + 5 * Rules.EveryMax);
        Assert.True(seen.Count >= 5, $"only {seen.Count} contacts");
        for (int i = 1; i < seen.Count; i++)
            Assert.InRange(seen[i].Up - seen[i - 1].Up, Rules.EveryMin, Rules.EveryMax);
        Assert.Equal(1, most);
    }

    [Fact]
    public void TheRadarFindsEachClassAsOftenAsItsWeightSays()
    {
        // The first contact of each of many worlds, each a draw of its own: a class's share of them its weight's, give or take.
        // Twenty worlds for each unit of weight: a class of weight 1 still comes up about twenty times.
        int sum = Vessels.Weights(Vessels.Classes), worlds = 20 * sum;
        var found = new Dictionary<string, int>(StringComparer.Ordinal);
        for (uint seed = 1; seed <= worlds; seed++)
        {
            var w = Ship(seed).Run(Rules.First);
            string cls = Assert.Single(w.M.Places.Contacts).Class;
            found[cls] = found.GetValueOrDefault(cls) + 1;
        }
        foreach (var c in Vessels.Classes)
        {
            int expected = worlds * c.Weight / sum, seen = found.GetValueOrDefault(c.Id);
            Assert.InRange(seen, expected * 6 / 10, expected * 14 / 10 + 1);
        }
    }

    [Fact]
    public void ContactsLieOnBothSidesBetweenNearestAndReachOfEveryClass()
    {
        // Found by hand, placed as the radar's own are drawn: a full radar on each of a few seeds.
        var offsets = new List<int>();
        for (uint seed = 1; seed <= 5; seed++)
        {
            var w = Ship(seed);
            for (int i = 0; i < Vessels.MaxContacts; i++) w.Do(new Find(Vessels.Classes[i % Vessels.Classes.Count].Id)).Run();
            Assert.Empty(w.Refusals);
            offsets.AddRange(w.M.Places.Contacts.Select(c => c.Offset));
        }
        Assert.All(offsets, o => Assert.InRange(Math.Abs(o), Rules.Nearest, Rules.Reach));
        Assert.Contains(offsets, o => o < 0);
        Assert.Contains(offsets, o => o > 0);
    }

    [Fact]
    public void AContactFoundByHandStaysLastsTheCountdownUntouched()
    {
        var w = Ship();
        w.Run(10);
        int next = w.M.Places.NextIn;
        w.Do(new Find("shuttle")).Run();
        var c = Assert.Single(w.M.Places.Contacts);
        Assert.Equal(Rules.Lasts - 1, c.Left);
        Assert.Equal(next - 1, w.M.Places.NextIn);
        // The radar's own first comes on the 18 000th tick, before this one goes: watched by id.
        w.Run(c.Left - 1);
        Assert.Contains(w.M.Places.Contacts, k => k.Id == c.Id);
        w.Run();
        Assert.DoesNotContain(w.M.Places.Contacts, k => k.Id == c.Id);
    }

    [Fact]
    public void AContactUpLoadsWithWhatItHasLeft()
    {
        var w = Ship().Do(new Find("freighter")).Run(100).Reload();
        var c = Assert.Single(w.M.Places.Contacts);
        Assert.Equal(Rules.Lasts - 100, c.Left);
        Assert.Equal(Rules.First - 100, w.M.Places.NextIn);
    }

    [Fact]
    public void NoneComeOffTheShip()
    {
        var w = Ship().Do(new OpenSurface(Surfaces.TestStrip.Id)).Run();
        var strip = w.On(1);
        for (int t = 0; t < Rules.First + 1; t++) w.Run();
        Assert.Empty(strip.Places.Contacts);
        Assert.Equal(0, strip.Places.NextIn);
        Assert.Single(w.M.Places.Contacts);
    }

    // Forget and the course cleared (the radar's «Forget» and «Clear route»).

    [Fact]
    public void AForgottenContactLeavesTheRadarTheOthersAndTheCourseKept()
    {
        var w = Ship().Do(new Find("liner")).Do(new Find("tanker")).Run();
        var (first, second) = (w.M.Places.Contacts[0], w.M.Places.Contacts[1]);
        w.Do(new SetCourse(second.Id)).Run();
        w.Do(new Forget(first.Id) { From = 1 }).Run();
        Assert.Empty(w.Refusals);
        Assert.Equal([second.Id], w.M.Places.Contacts.Select(c => c.Id));
        Assert.Equal(second.Id, w.M.Places.Course!.Id);
    }

    [Fact]
    public void ForgettingTheCoursesContactClearsTheCourse()
    {
        var w = Ship().Do(new Find("freighter")).Run();
        int id = w.M.Places.Contacts[0].Id;
        w.Do(new SetCourse(id)).Run();
        w.Do(new Forget(id)).Run();
        Assert.Empty(w.Refusals);
        Assert.Empty(w.M.Places.Contacts);
        Assert.Null(w.M.Places.Course);
        Assert.Null(((PlacesState)w.M.Places.Slice).Course);
    }

    [Fact]
    public void ClearCourseLeavesNoCourseAndTheContactUp()
    {
        var w = Ship().Do(new Find("shuttle")).Run();
        int id = w.M.Places.Contacts[0].Id;
        w.Do(new SetCourse(id)).Run();
        w.Do(new ClearCourse { From = 2 }).Run();
        Assert.Empty(w.Refusals);
        Assert.Null(w.M.Places.Course);
        Assert.Equal([id], w.M.Places.Contacts.Select(c => c.Id));
        // With none set it is pressed for nothing: two players pressing it at once.
        w.Do(new ClearCourse()).Run();
        Assert.Empty(w.Refusals);
    }

    [Fact]
    public void ForgetRefusesAContactTheRadarDoesNotHold()
    {
        var w = Ship().Do(new Find("shuttle")).Run();
        w.Do(new Forget(999)).Run();
        Assert.Equal(["places.noContact"], w.Refusals.Select(r => r.Reason));
        Assert.Single(w.M.Places.Contacts);
    }

    [Fact]
    public void OffTheShipNothingIsForgottenNorTheCourseCleared()
    {
        var w = new Scenario(Layout.Parse("..#..").OnPlanet());
        w.Do(new Forget(1)).Do(new ClearCourse()).Run();
        Assert.Equal(["places.notAboard", "places.notAboard"], w.Refusals.Select(r => r.Reason));
    }

    [Fact]
    public void TheRulesKeepOneUpAtATime()
    {
        Assert.True(Rules.Lasts <= Rules.EveryMin);
        Assert.True(0 < Rules.Nearest && Rules.Nearest <= Rules.Reach);
        Assert.True(Rules.EveryMin <= Rules.EveryMax);
    }

    /// <summary>
    /// Whatever the seed, every contact keeps 1..Lasts ticks left and lies within reach, and the radar's own are never two at
    /// once. Bites: with <c>Lasts</c> above <c>EveryMin</c>, two overlap.
    /// </summary>
    [Property(MaxTest = 6)]
    public Property EveryContactIsInReachWithTimeLeftAndTheRadarsOwnComeOneAtATime() =>
        Prop.ForAll(Gen.Choose(1, int.MaxValue).ToArbitrary(), seed =>
        {
            var w = Ship((uint)seed);
            int most = 0;
            for (int t = 0; t < Rules.First + 3 * Rules.EveryMax; t++)
            {
                w.Run();
                var contacts = w.M.Places.Contacts;
                most = Math.Max(most, contacts.Count);
                foreach (var c in contacts)
                    if (c.Left < 1 || c.Left > Rules.Lasts || Math.Abs(c.Offset) > Rules.Reach) return false;
            }
            return most == 1;
        });
}
