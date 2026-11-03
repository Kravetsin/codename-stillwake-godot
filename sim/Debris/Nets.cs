using Stillwake.Content;
using Stillwake.Sim.Items;

namespace Stillwake.Sim.Debris;

/// <summary>
/// Empties net <c>Machine</c> into the sender's inventory, the E on a debris net (docs/decisions/2026-11-03-debris-net.md): every entry, in catch order,
/// gives what fits; what does not stays in the net. What each entry gave is reported as a grab's catch is (<see cref="DebrisModule.CaughtFrom"/>).
/// </summary>
public sealed record Collect(int Machine) : Command;

/// <summary>The debris nets (docs/decisions/2026-11-03-debris-net.md): pieces caught into them, collected out of them, dropped with a net gone.</summary>
public sealed partial class DebrisModule
{
    /// <summary>The grid's <c>Version</c> the nets were last looked at by <see cref="DropGone"/>: looked at again only when it moves.</summary>
    private int _heldAt = int.MinValue;

    /// <summary>What every debris net holds, in catch order; live and read-only.</summary>
    public IReadOnlyList<NetEntry> Held => State.Held;

    /// <summary>What net <paramref name="net"/> holds, in catch order: the pieces to draw in it and its count. A new list a call.</summary>
    public IReadOnlyList<NetEntry> HeldIn(int net) => State.Held.FindAll(e => e.Net == net);

    /// <summary>
    /// Why peer <paramref name="peer"/>'s <see cref="Collect"/> of net <paramref name="net"/> would be refused: no such machine, or
    /// one that catches nothing (<c>debris.noNet</c>), a net holding nothing (<c>debris.netEmpty</c>), or no room in the inventory
    /// for one of anything it holds (<c>debris.bagFull</c>). Reach is the client's, through the wall too.
    /// </summary>
    public string? CollectProblem(int peer, int net)
    {
        if (!IsNet(net)) return "debris.noNet";
        bool any = false;
        foreach (var entry in State.Held)
        {
            if (entry.Net != net) continue;
            any = true;
            foreach (var stack in entry.Stacks)
                if (_held.FullFor(peer, [(stack.Item, 1)]) == null) return null;
        }
        return any ? "debris.bagFull" : "debris.netEmpty";
    }

    private bool IsNet(int machine) => _grid.MachineById(machine) is { } m && Machines.Is(m.Kind, MachineTrait.Catches);

    private void Apply(Collect c)
    {
        var held = State.Held;
        int kept = 0;
        for (int i = 0; i < held.Count; i++)
        {
            var entry = held[i];
            if (entry.Net == c.Machine)
            {
                // On past an entry that does not fit: a later one may.
                var left = _items.GiveWhatFits(c.From, [.. entry.Stacks.Select(s => (s.Item, s.Count))]);
                var gave = new List<(string Item, int Count)>();
                // An entry's stacks are by item, each once (LootOf), so what is left of each is its one line of left.
                foreach (var stack in entry.Stacks)
                {
                    int given = stack.Count - left.Where(l => l.Item == stack.Item).Sum(l => l.Count);
                    if (given > 0) gave.Add((stack.Item, given));
                }
                // Reported as a grab is, for the collector's popups; an entry that gave nothing, not at all.
                if (gave.Count > 0) Report(entry.Id, c.From, gave);
                if (left.Count == 0) continue;
                entry.Stacks = [.. left.Select(l => new Stack(l.Item, l.Count))];
            }
            held[kept++] = entry;
        }
        held.RemoveRange(kept, held.Count - kept);
    }

    /// <summary>
    /// Catches <paramref name="piece"/>, just moved, if a net takes it: its kind one of <c>Debris.NetCatches</c>, not reeled, its
    /// middle in a net's cell, the net holding fewer than <c>Debris.NetHolds</c>. A container's loot is rolled then, once the
    /// place is sure, so a full net draws nothing; the kind is recorded as found, with no popup. True when caught.
    /// </summary>
    private bool Catch(Piece piece)
    {
        if (piece.Reel != null || !Content.Debris.NetCatches.Contains(piece.Item, StringComparer.Ordinal)) return false;
        int cell = CellOf(piece.X, piece.Y, _grid.Width);
        if (_grid.MachineAt(cell) is not { } net || !Machines.Is(net.Kind, MachineTrait.Catches)) return false;
        int count = 0;
        foreach (var entry in State.Held)
            if (entry.Net == net.Id) count++;
        if (count >= Content.Debris.NetHolds) return false;
        var loot = LootOf(piece.Item, Random);
        Record(piece, loot);
        State.Held.Add(new NetEntry
        {
            Net = net.Id,
            Id = piece.Id,
            Kind = piece.Item,
            Cell = cell,
            Stacks = [.. loot.Select(l => new Stack(l.Item, l.Count))],
        });
        return true;
    }

    /// <summary>
    /// Every entry whose net is no longer a debris net, a meteor's or a screwdriver's doing, dropped where it was caught, in catch
    /// order, as floor stacks anyone may take. A pure function of the grid and what is held, so it runs only when the grid's
    /// <c>Version</c> has moved: once more after a load, it changes nothing.
    /// </summary>
    private void DropGone()
    {
        if (_grid.Version == _heldAt) return;
        _heldAt = _grid.Version;
        var held = State.Held;
        int kept = 0;
        for (int i = 0; i < held.Count; i++)
        {
            var entry = held[i];
            if (!IsNet(entry.Net))
            {
                _items.DropAt(FloorStack.Nobody, [.. entry.Stacks.Select(s => (s.Item, s.Count))], entry.Cell);
                continue;
            }
            held[kept++] = entry;
        }
        held.RemoveRange(kept, held.Count - kept);
    }
}
