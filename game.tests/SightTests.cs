using Stillwake.Game.Features.Unseen;
using Stillwake.Sim;
using Xunit;
using V = Stillwake.Content.Vessels;

namespace Stillwake.Game.Tests;

/// <summary>
/// A derelict's inside dark till seen (hidden-rooms). Maps: '#' a wall, 'D' a shut door, 'd' an open one, '.' space inside,
/// '@' a hero's body in it, ' ' space outside (no back wall).
/// </summary>
public class SightTests
{
    private sealed class Map
    {
        public char[] Cells;
        public readonly int Width, Height;
        public readonly Sight Sight;

        public Map(params string[] rows)
        {
            (Width, Height) = (rows[0].Length, rows.Length);
            Cells = string.Concat(rows).ToCharArray();
            Sight = new Sight(Width, Height, c => Cells[c] is '.' or '@' or 'd', c => Cells[c] is '#' or 'D');
        }

        public int[] Heroes => Enumerable.Range(0, Cells.Length).Where(c => Cells[c] == '@').ToArray();
        public bool Look(bool changed = false, double ms = 0) => Sight.Look(Heroes, changed, ms);
        public void Set(int x, int y, char what) => Cells[y * Width + x] = what;
        public bool Dark(int x, int y) => Sight.Dark(y * Width + x) > 0;
    }

    private static Map TwoRooms() => new(
        "            ",
        " ########## ",
        " #@...D...# ",
        " #....D...# ",
        " ########## ",
        "            ");

    [Fact]
    public void TheFirstLookShowsTheRoomTheHeroStandsInAtOnceAndNotOnePastAShutDoor()
    {
        var m = TwoRooms();
        Assert.True(m.Look());
        Assert.Equal(0, m.Sight.Dark(2 * m.Width + 3));
        Assert.False(m.Dark(5, 3));
        Assert.True(m.Dark(7, 2));
        Assert.True(m.Dark(9, 3));
    }

    [Fact]
    public void ADoorOpenedShowsTheRoomPastItFadingOutAndItStaysSeenOnceShutAgain()
    {
        var m = TwoRooms();
        m.Look();
        m.Set(6, 2, 'd');
        m.Set(6, 3, 'd');
        m.Look(changed: true);
        Assert.Equal(1, m.Sight.Dark(2 * m.Width + 8));
        m.Look(ms: Sight.FadeMs / 2);
        Assert.InRange(m.Sight.Dark(2 * m.Width + 8), 0.4f, 0.6f);
        m.Look(ms: Sight.FadeMs / 2);
        Assert.False(m.Dark(8, 2));
        Assert.False(m.Sight.Hides(2 * m.Width + 8));
        Assert.False(m.Look(ms: 16));

        m.Set(6, 2, 'D');
        m.Set(6, 3, 'D');
        m.Look(changed: true);
        Assert.False(m.Dark(8, 3));
        Assert.False(m.Dark(6, 2));
    }

    [Fact]
    public void RoomsJoinedThroughAnOpenDoorShowTogether()
    {
        var m = new Map(
            " ############# ",
            " #@..d...D...# ",
            " #...d...D...# ",
            " ############# ");
        m.Look();
        Assert.False(m.Dark(7, 1));
        Assert.True(m.Dark(11, 1));
    }

    [Fact]
    public void TheHullAndADoorBesideASeenRoomStayDrawnAShutDoorBetweenUnseenRoomsGoesDark()
    {
        var m = new Map(
            " ############### ",
            " #@..D....D....# ",
            " #...D....D....# ",
            " ############### ");
        m.Look();
        Assert.False(m.Dark(5, 1));
        Assert.True(m.Dark(10, 1));
        // The hull over and under the unseen rooms and at the far end: space past it.
        Assert.False(m.Dark(12, 0));
        Assert.False(m.Dark(12, 3));
        Assert.False(m.Dark(15, 2));
    }

    [Fact]
    public void ADecksFloorBetweenTwoUnseenDecksIsDarkUnderASeenOneDrawn()
    {
        var m = new Map(
            " ######### ",
            " #@..#...# ",
            " #...D...# ",
            " ######### ",
            " #.......# ",
            " #.......# ",
            " ######### ");
        m.Look();
        Assert.True(m.Dark(7, 4));
        Assert.False(m.Dark(2, 3));
        Assert.True(m.Dark(7, 3));
    }

    [Fact]
    public void TheFloorUnderAShutDoorBetweenUnseenRoomsOverAnUnseenDeckIsDark()
    {
        var m = new Map(
            " ############# ",
            " #@..D...D...# ",
            " #...D...D...# ",
            " ############# ",
            " #...........# ",
            " ############# ");
        m.Look();
        Assert.True(m.Dark(9, 2));
        Assert.True(m.Dark(9, 3));
        Assert.True(m.Dark(10, 3));
        Assert.False(m.Dark(5, 3));
    }

    [Fact]
    public void AHeroWalkingIntoAnUnseenRoomSeesIt()
    {
        var m = TwoRooms();
        m.Look();
        m.Set(2, 2, '.');
        m.Set(8, 2, '@');
        m.Look(ms: Sight.FadeMs);
        Assert.False(m.Dark(9, 3));
    }

    [Theory]
    [InlineData("shuttle", 1u)]
    [InlineData("shuttle", 9u)]
    [InlineData("freighter", 2u)]
    [InlineData("freighter", 5u)]
    public void AVesselShowsTheRoomItsDoorLetsIntoAndNoneOfItsHull(string cls, uint seed)
    {
        var vessel = Shipyard.Describe(V.Class(cls)!, seed);
        int w = vessel.Width, h = vessel.Height;
        var parts = Shipyard.Stamp(vessel);
        var back = new bool[w * h];
        var seals = new bool[w * h];
        foreach (var p in parts)
        {
            if (p.Layer == "back") back[p.Y * w + p.X] = true;
            if (p.Layer != "structure") continue;
            if (p.Kind == "wall") seals[p.Y * w + p.X] = true;
            if (p.Kind == "door" && !p.Open)
                for (int y = p.Y; y < p.Y + 2; y++)
                    for (int x = p.X; x < p.X + 2; x++) seals[y * w + x] = true;
            if (p.Kind == Content.Builds.Hatch) seals[p.Y * w + p.X] = seals[p.Y * w + p.X + 1] = true;
        }
        var sight = new Sight(w, h, c => back[c] && !seals[c], c => seals[c]);
        var (fx, fy) = Shipyard.Door(vessel);
        int hero = (fy - 2) * w + fx;
        sight.Look([hero], changed: false, 0);

        Assert.Equal(0, sight.Dark(hero));
        // Below the first deck, past its shut hatches: dark.
        var low = vessel.Compartments.First(c => c.Deck == 1);
        Assert.Equal(1, sight.Dark((low.Top + 2) * w + low.Left + low.Width / 2));
        // A shut hatch between two unseen decks: dark with them.
        foreach (var hatch in vessel.Hatches)
            if (sight.Dark((hatch.Y - 1) * w + hatch.X) == 1 && sight.Dark((hatch.Y + 1) * w + hatch.X) == 1)
                Assert.Equal(1, sight.Dark(hatch.Y * w + hatch.X));
        // Every compartment seen or dark whole, and every wall with outside space beside it drawn.
        foreach (var room in vessel.Compartments)
        {
            var cells = Enumerable.Range(room.Top, V.Open).SelectMany(y => Enumerable.Range(room.Left, room.Width).Select(x => y * w + x))
                .Where(c => back[c] && !seals[c]).ToList();
            Assert.Single(cells.Select(c => sight.Dark(c)).Distinct());
        }
        for (int c = 0; c < w * h; c++)
        {
            if (!seals[c]) continue;
            int x = c % w, y = c / w;
            bool outside = (x == 0 || !back[c - 1]) || (x + 1 == w || !back[c + 1]) || (y == 0 || !back[c - w]) || (y + 1 == h || !back[c + w]);
            if (outside) Assert.Equal(0, sight.Dark(c));
        }
    }
}
