using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Stillwake.Game.Shared;
using Stillwake.Sim;

namespace Stillwake.Game.Features.Research;

/// <summary>
/// What the research and market keys on the tablet's edge tell (hologram-keys), read for themselves on a timer, for the tablet reads nothing while away: the
/// research key shows from the first note, amber while one is unread, the finds waiting on it less those still flying down from the bulb; the market's
/// shows from the first credit or good bought, the credits on it. It tells the bulb what is new, and which find lit it.
/// </summary>
public partial class TabletKey : Node
{
    private const double RefreshS = 0.25;
    /// <summary>The market's orders, read here for the market's key: a sale moves no version, so they are read on the timer.</summary>
    private static readonly Content.TechNode[] Orders = [.. Content.Research.Nodes.Where(n => n is { Gate: true, Tab: Content.Research.Market })];
    /// <summary>Everything the market has: one of them done, it has been used.</summary>
    private static readonly Content.TechNode[] Market = [.. Content.Research.Nodes.Where(n => n.Tab == Content.Research.Market)];

    private readonly Func<Registry?> _world;
    private readonly Func<uint> _worldId;
    private readonly BoardKeep _keep;
    private readonly BulbSignal _bulb;
    private IReadOnlyCollection<string> _unread = [];
    private (object Knowledge, int Version)? _readAt;
    private double _sinceRead = RefreshS;

    /// <param name="world">The world the hero stands in; null with none played.</param>
    /// <param name="worldId">Which world is played: what the board is kept under.</param>
    /// <param name="keep">What is kept of the board, shared with the page.</param>
    /// <param name="bulb">The bulb at the top of the screen, told what is new.</param>
    /// <param name="press">Takes the tablet out on the board or puts it away.</param>
    /// <param name="boardOut">Whether the board is out.</param>
    public TabletKey(Func<Registry?> world, Func<uint> worldId, BoardKeep keep, BulbSignal bulb, Action press, Func<bool> boardOut)
    {
        (_world, _worldId, _keep, _bulb) = (world, worldId, keep, bulb);
        Cap = new EdgeKey("research", press, boardOut, () => _keep.Seen.Lit,
            () => _unread.Count - _bulb.Flying is var waiting and > 0 ? waiting : null);
    }

    /// <summary>The research key itself, for the edge to set in place.</summary>
    public EdgeKey Cap { get; }

    /// <summary>Whether the research key shows: the board holds a note.</summary>
    public bool Shows => _keep.Seen.Any;

    /// <summary>Whether the market's key shows: the crew has credits, or has bought from it.</summary>
    public bool MarketShows { get; private set; }

    /// <summary>The crew's credits as last read, for the market's key.</summary>
    public int Credits { get; private set; }

    /// <summary>The find that lit the bulb, what hatches from it; null before any.</summary>
    public string? Find { get; private set; }

    public override void _Process(double delta)
    {
        _sinceRead += delta;
        if (_sinceRead < RefreshS) return;
        _sinceRead = 0;
        var m = _world();
        if (m == null) return;
        _keep.Of(_worldId());
        var at = ((object)m.Knowledge, m.Knowledge.Version);
        if (_readAt != at)
        {
            _readAt = at;
            var read = BoardPage.ReadOf(m);
            _keep.Seen.Read(BoardView.NotesOf(BoardSlots.Of(read.Found, BoardThings.All, read.Granted), read));
        }
        _keep.Seen.ReadOrders(Orders.Count(m.Research.Done));
        Credits = m.Research.Credits;
        MarketShows = Credits > 0 || Market.Any(m.Research.Done);
        var unread = _keep.Seen.Unread;
        bool dark = _bulb.Time == null;
        _bulb.Read(_keep.Seen, unread.Count, _keep.Seen.Open);
        if (dark && _bulb.Time != null) Find = unread.Where(id => !_unread.Contains(id)).Order(StringComparer.Ordinal).FirstOrDefault() ?? Find;
        _unread = unread;
    }
}
