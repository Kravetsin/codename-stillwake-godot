using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using Stillwake.Sim.Contacts;
using Xunit;
using Rules = Stillwake.Content.Contacts;

namespace Stillwake.Sim.Tests;

/// <summary>The radar's contacts (2026-11-08-radar-contacts): when they come up, how long they stay, where they lie.</summary>
public class ContactsTests
{
    /// <summary>The module alone in a world of one cell: its rules need nothing else, and hours of ticks run fast.</summary>
    private static (World World, ContactsModule Contacts) Alone(uint seed = 1)
    {
        var contacts = new ContactsModule();
        return (World.Create(seed, Layout.Parse(["."]), [contacts]), contacts);
    }

    /// <summary>
    /// <paramref name="ticks"/> ticks of <paramref name="world"/>: for each contact, the tick it came up and the tick it went
    /// (null while up), and the most up at once.
    /// </summary>
    private static (List<(Contact Contact, int Up, int? Gone)> Seen, int Most) Watch(World world, ContactsModule contacts, int ticks)
    {
        var seen = new List<(Contact Contact, int Up, int? Gone)>();
        int most = 0;
        for (int t = 1; t <= ticks; t++)
        {
            world.Step([]);
            foreach (var c in contacts.All)
                if (!seen.Exists(s => s.Contact.Id == c.Id)) seen.Add((c, t, null));
            for (int i = 0; i < seen.Count; i++)
                if (seen[i].Gone == null && contacts.Find(seen[i].Contact.Id) == null) seen[i] = seen[i] with { Gone = t };
            most = Math.Max(most, contacts.All.Count);
        }
        return (seen, most);
    }

    [Fact]
    public void TheFirstComesUpAtFirstAndStaysExactlyLasts()
    {
        var (world, contacts) = Alone();
        var (seen, _) = Watch(world, contacts, Rules.First + Rules.Lasts);
        var first = Assert.Single(seen);
        Assert.Equal(Rules.First, first.Up);
        Assert.Equal(Rules.First + Rules.Lasts, first.Gone);
        Assert.Equal(Rules.Wreck, first.Contact.Kind);
        Assert.Equal(Rules.Lasts, first.Contact.Left);
    }

    [Fact]
    public void EachNextComesEveryMinToEveryMaxAfterTheLastAndNeverTwoAtOnce()
    {
        var (world, contacts) = Alone();
        var (seen, most) = Watch(world, contacts, Rules.First + 10 * Rules.EveryMax);
        Assert.True(seen.Count >= 10, $"only {seen.Count} contacts");
        for (int i = 1; i < seen.Count; i++)
            Assert.InRange(seen[i].Up - seen[i - 1].Up, Rules.EveryMin, Rules.EveryMax);
        Assert.Equal(1, most);
        Assert.Equal(Enumerable.Range(0, seen.Count), seen.Select(s => s.Contact.Id));
    }

    [Fact]
    public void ContactsLieOnBothSidesBetweenNearestAndReach()
    {
        var offsets = new List<int>();
        for (uint seed = 1; seed <= 6; seed++)
        {
            var (world, contacts) = Alone(seed);
            offsets.AddRange(Watch(world, contacts, Rules.First + 6 * Rules.EveryMax).Seen.Select(s => s.Contact.Offset));
        }
        Assert.All(offsets, o => Assert.InRange(Math.Abs(o), Rules.Nearest, Rules.Reach));
        Assert.Contains(offsets, o => o < 0);
        Assert.Contains(offsets, o => o > 0);
    }

    [Fact]
    public void ContactNowAddsOneAtOnceTheCountdownUntouched()
    {
        var w = new Scenario(".");
        w.Run(10);
        int next = w.M.Contacts.NextIn;
        w.Do(new ContactNow(Rules.Wreck, -Rules.Reach)).Run();
        var c = Assert.Single(w.M.Contacts.All);
        Assert.Equal((Rules.Wreck, -Rules.Reach), (c.Kind, c.Offset));
        Assert.Equal(next - 1, w.M.Contacts.NextIn);
        Assert.Empty(w.Refusals);
    }

    [Theory]
    [InlineData("nothing", 0, "contacts.unknownKind")]
    [InlineData(Rules.Wreck, Rules.Reach + 1, "contacts.outOfReach")]
    [InlineData(Rules.Wreck, -Rules.Reach - 1, "contacts.outOfReach")]
    public void ContactNowRefuses(string kind, int offset, string reason)
    {
        var w = new Scenario(".").Do(new ContactNow(kind, offset)).Run();
        Assert.Equal(reason, Assert.Single(w.Refusals).Reason);
        Assert.Empty(w.M.Contacts.All);
    }

    [Fact]
    public void AContactUpLoadsWithWhatItHasLeft()
    {
        var w = new Scenario(".").Do(new ContactNow(Rules.Wreck, 40)).Run(100).Reload();
        var c = Assert.Single(w.M.Contacts.All);
        Assert.Equal(Rules.Lasts - 100, c.Left);
        w.Run(c.Left - 1);
        Assert.NotNull(w.M.Contacts.Find(c.Id));
        // Its last tick is the world's 18 000th: the first drawn contact comes up as it goes.
        w.Run();
        Assert.Null(w.M.Contacts.Find(c.Id));
        Assert.Equal(c.Id + 1, Assert.Single(w.M.Contacts.All).Id);
    }

    [Fact]
    public void TheRulesKeepOneUpAtATime()
    {
        Assert.True(Rules.Lasts <= Rules.EveryMin);
        Assert.True(0 < Rules.Nearest && Rules.Nearest <= Rules.Reach);
        Assert.True(Rules.EveryMin <= Rules.EveryMax);
    }

    /// <summary>
    /// Whatever the seed and the contacts placed by hand, every contact keeps 1..Lasts ticks left and lies within reach, and
    /// those come up by themselves are never two at once. Bites: with <c>Lasts</c> above <c>EveryMin</c>, two overlap.
    /// </summary>
    [Property(MaxTest = 20)]
    public Property EveryContactIsInReachWithTimeLeftAndTheDrawnOnesComeOneAtATime() =>
        Prop.ForAll(Gen.Choose(1, int.MaxValue).ToArbitrary(), seed =>
        {
            var (world, contacts) = Alone((uint)seed);
            int most = 0;
            for (int t = 0; t < Rules.First + 4 * Rules.EveryMax; t++)
            {
                world.Step([]);
                most = Math.Max(most, contacts.All.Count);
                foreach (var c in contacts.All)
                    if (c.Left < 1 || c.Left > Rules.Lasts || Math.Abs(c.Offset) > Rules.Reach) return false;
            }
            return most == 1;
        });
}
