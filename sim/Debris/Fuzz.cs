using Stillwake.Content;

namespace Stillwake.Sim.Debris;

public sealed partial class DebrisModule
{
    /// <summary>
    /// A grab, a hit with a weapon drawn among them all from its middle, a hook, a wind or let-go, or a feed, about one in five each,
    /// at a piece that is out most of the time — a wreck or a small piece, whichever is drawn — else one already gone or never
    /// spawned. Most are refused, as few fuzzed heroes stand in space; those that are not take the piece, chop at the wreck till it
    /// breaks, or hook it anywhere from twice its box out, so the clamp works. A feed is as full as the host player sends, of pieces
    /// out and gone, moved a little or flung off the grid, and lines kept and stale; from a guest it is refused. A quarter of the
    /// draws instead collect from a debris net, mostly one that stands, else any machine id or none: on a map without nets, refused.
    /// </summary>
    public override Command? Fuzz(Rng rng, int from)
    {
        if (rng.Below(4) == 0)
        {
            var nets = _grid.MachinesWith(MachineTrait.Catches);
            int net = nets.Count > 0 && rng.Below(4) != 0 ? nets[(int)rng.Below((uint)nets.Count)].Id : (int)rng.Below((uint)_grid.Machines.Count + 2);
            return new Collect(net) { From = from };
        }
        var pieces = State.Pieces;
        uint kind = rng.Below(5);
        int piece = AnyPiece(rng);
        // Struck with any weapon, the drill too, refused unless its sender runs it with charge (0055): from about a wreck's
        // middle, mostly, aimed anywhere, so wrecks break within a run.
        if (kind == 0)
        {
            string tool = Content.Weapons.All[(int)rng.Below((uint)Content.Weapons.All.Count)].Item;
            int at = IndexOf(piece);
            int x = at >= 0 ? pieces[at].X : (int)rng.Below((ulong)(_grid.Width * Unit));
            int y = at >= 0 ? pieces[at].Y : (int)rng.Below((ulong)(_grid.Height * Unit));
            // Half the time a running tool's edge on it (tool-edge): the piece twice, a neighbour id, any other, small or gone.
            if (rng.Below(2) == 0) return new HewPieces(tool, new List<int> { piece, piece, piece + 1, (int)rng.Below((uint)State.NextId + 2) }) { From = from };
            return new HewWrecks(tool, x, y, (int)rng.Below(Content.Headings.Turn)) { From = from };
        }
        if (kind == 1) return new Grab(piece) { From = from };
        if (kind == 2)
        {
            var box = PieceById(piece) is { } p ? HookBox(p) : default;
            int hookX = (int)rng.Below((ulong)(4 * box.HalfW + 1)) - 2 * box.HalfW, hookY = (int)rng.Below((ulong)(4 * box.HalfH + 1)) - 2 * box.HalfH;
            return new Hook(piece, hookX, hookY) { From = from };
        }
        if (kind == 3) return rng.Below(3) == 0 ? new LetGo { From = from } : new Wind(rng.Below(2) == 0) { From = from };
        return FuzzMoved(rng, from);
    }

    /// <summary>A piece out most of the time, else one gone or never spawned, by id.</summary>
    private int AnyPiece(Rng rng)
    {
        var pieces = State.Pieces;
        return pieces.Count > 0 && rng.Below(4) != 0 ? pieces[(int)rng.Below((uint)pieces.Count)].Id : (int)rng.Below((uint)State.NextId + 2);
    }

    /// <summary>
    /// A feed of <c>Debris.FeedMost</c> bodies, out and gone, each moved up to a cell and turned, most at a drift's pace, one in eight
    /// flung off the grid; for some tick up to a second back; with every line's length changed and a stale one; addressed to the ship.
    /// </summary>
    private Moved FuzzMoved(Rng rng, int from)
    {
        var bodies = new List<Body>();
        for (int i = 0; i < Content.Debris.FeedMost; i++)
        {
            int id = AnyPiece(rng);
            var p = PieceById(id);
            int x = (p?.X ?? 0) + (int)rng.Below(2001) - 1000, y = (p?.Y ?? 0) + (int)rng.Below(2001) - 1000;
            int vx = (int)rng.Below(401) - 200, vy = (int)rng.Below(401) - 200;
            if (rng.Below(8) == 0)
            {
                int way = rng.Below(2) == 0 ? 1 : -1;
                (vx, vy) = (way * _grid.Width * Unit, -way * _grid.Height * Unit);
            }
            bodies.Add(new Body(id, x, y, vx, vy, (int)rng.Below(0x10000), (int)rng.Below(641) - 320));
        }
        var lines = State.Lines.Select(l => new LineLength(l.By, l.Piece, (int)rng.Below(Content.Harpoon.Range + Content.Harpoon.PayOut + 1))).ToList();
        lines.Add(new LineLength((int)rng.Below(4), AnyPiece(rng), (int)rng.Below(Content.Harpoon.Range)));
        // Addressed as the host player's client addresses it: drawn on a planet too, it goes to the ship's pieces.
        return new Moved(State.Clock - (int)rng.Below(World.TicksPerSecond + 1), bodies, lines) { From = from, Surface = BodiesOn };
    }
}
