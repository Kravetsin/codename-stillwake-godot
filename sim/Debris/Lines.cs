namespace Stillwake.Sim.Debris;

/// <summary>
/// The harpoons' lines and the host player's feed (debris-physics): a line hooks a piece for the host player's rope to pull, and
/// <see cref="Moved"/> brings what its bodies did into the sim. The sim keeps poses and lines; it pulls and eases nothing.
/// </summary>
public sealed partial class DebrisModule
{
    /// <summary>The host player's peer (0002): the one whose client runs the bodies, the only one whose <see cref="Moved"/> is taken.</summary>
    public const int HostPlayer = 0;

    /// <summary>The surface the host player's bodies run for and its <see cref="Moved"/> is addressed to: the ship's, the only one debris drifts by.</summary>
    public const int BodiesOn = 0;

    /// <summary>The line of each hero and on each piece, kept with <c>Lines</c>: derived, rebuilt after a create and a load. Looked up only.</summary>
    private readonly Dictionary<int, Line> _lineOf = [], _lineOn = [];

    public IReadOnlyList<Line> Lines => State.Lines;

    public Line? LineOn(int piece) => _lineOn.GetValueOrDefault(piece);

    public Line? LineOf(int peer) => _lineOf.GetValueOrDefault(peer);

    private void RebuildLines()
    {
        _lineOf.Clear();
        _lineOn.Clear();
        foreach (var line in State.Lines) (_lineOf[line.By], _lineOn[line.Piece]) = (line, line);
    }

    /// <summary>Why peer <paramref name="peer"/>'s <see cref="Hook"/> of piece <paramref name="id"/> would be refused: as a grab's reach, the piece gone or one of them not in space. A line on it already is no reason: the last hook wins.</summary>
    public string? HookProblem(int peer, int id) => Reachable(peer, id, out _);

    /// <summary>Why peer <paramref name="peer"/>'s <see cref="Wind"/> or <see cref="LetGo"/> would be refused: it holds no line.</summary>
    public string? LineProblem(int peer) => LineOf(peer) == null ? "debris.noLine" : null;

    /// <summary>The box a hook into <paramref name="piece"/> is clamped into: a wreck's turned box, a small piece's a cell about its middle.</summary>
    private static TurnedBox HookBox(Piece piece) =>
        ShapeOf(piece) ?? TurnedBox.Of(piece.Id, piece.X, piece.Y, Unit / 2, Unit / 2, piece.Angle);

    private void Apply(Hook c)
    {
        var piece = PieceById(c.Piece) ?? throw new InvalidOperationException("debris.hook: applied to a piece that is gone");
        var hero = _crew.HeroOf(c.From) ?? throw new InvalidOperationException("debris.hook: applied with no hero");
        var box = HookBox(piece);
        int hookX = Math.Clamp(c.HookX, -box.HalfW, box.HalfW), hookY = Math.Clamp(c.HookY, -box.HalfH, box.HalfH);
        var (x, y) = box.At(hookX, hookY);
        long dx = x - (hero.Cell % _grid.Width * (long)Unit + Unit / 2), dy = y - (hero.Cell / _grid.Width * (long)Unit + Unit / 2);
        int length = (int)Math.Min(TurnedBox.Root(dx * dx + dy * dy), Content.Harpoon.Range);
        DropLineOf(c.From);
        DropLineOn(c.Piece);
        var line = new Line { By = c.From, Piece = c.Piece, HookX = hookX, HookY = hookY, Length = length };
        State.Lines.Add(line);
        (_lineOf[line.By], _lineOn[line.Piece]) = (line, line);
    }

    private void Apply(Wind c) => LineOf(c.From)!.Winds = c.On;

    private void Apply(LetGo c) => DropLineOf(c.From);

    /// <summary>
    /// Each body, in id order, set as fed and carried on its own motion from <c>ForTick</c> to this tick; one carried off the grid is
    /// gone, as the drift would have it. Then each line's length, where that hero's line is still on that piece.
    /// </summary>
    private void Apply(Moved c)
    {
        long ticks = Math.Max(0, State.Clock - c.ForTick);
        long width = (long)_grid.Width * Unit, height = (long)_grid.Height * Unit;
        foreach (var body in c.Bodies.OrderBy(b => b.Id))
        {
            int at = IndexOf(body.Id);
            if (at < 0) continue;
            var piece = State.Pieces[at];
            long x = body.X + body.Vx * ticks, y = body.Y + body.Vy * ticks;
            if (x < 0 || y < 0 || x >= width || y >= height)
            {
                Lost(at);
                continue;
            }
            (piece.X, piece.Y, piece.Vx, piece.Vy) = ((int)x, (int)y, body.Vx, body.Vy);
            (piece.Angle, piece.Spin) = ((int)((body.Angle + body.Spin * ticks) & 0xFFFF), body.Spin);
        }
        foreach (var fed in c.Lines)
            if (LineOn(fed.Piece) is { } line && line.By == fed.By) line.Length = Math.Max(0, fed.Length);
    }

    /// <summary>The piece at index <paramref name="at"/> gone with no catch: off the grid or past its end. A delivery is lost with its credits, and its good may be bought again (2026-11-08-mafia-deliveries).</summary>
    private void Lost(int at)
    {
        var piece = State.Pieces[at];
        if (piece.Delivery != 0) _knowledgeOps.Delivered(piece.Delivery, lost: true);
        RemoveAt(at);
    }

    /// <summary>The piece at index <paramref name="at"/> gone, for any reason, and the line on it with it.</summary>
    private void RemoveAt(int at)
    {
        DropLineOn(State.Pieces[at].Id);
        State.Pieces.RemoveAt(at);
    }

    private void DropLineOn(int piece)
    {
        if (LineOn(piece) is { } line) Drop(line);
    }

    private void DropLineOf(int peer)
    {
        if (LineOf(peer) is { } line) Drop(line);
    }

    private void Drop(Line line)
    {
        State.Lines.Remove(line);
        _lineOf.Remove(line.By);
        _lineOn.Remove(line.Piece);
    }

    /// <summary>Every line whose holder has left, passed out or was reported outside space dropped, in the order hooked.</summary>
    private void DropLetGo()
    {
        var lines = State.Lines;
        int cells = _grid.Width * _grid.Height;
        for (int i = lines.Count - 1; i >= 0; i--)
        {
            var hero = _crew.HeroOf(lines[i].By);
            if (hero == null || hero.Out > 0 || hero.Cell < 0 || hero.Cell >= cells || !Outside(hero.Cell)) Drop(lines[i]);
        }
    }
}
