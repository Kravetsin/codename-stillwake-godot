using Stillwake.Content;

namespace Stillwake.Sim.Items;

/// <summary>
/// Fits the module in <c>Module</c>, a slot of the sender's or of a machine, into machine <c>Machine</c> (0086): one of
/// each kind a machine, out of its slots.
/// </summary>
public sealed record Fit(Place Module, int Machine) : Command;

/// <summary>
/// Takes module <c>Module</c> out of machine <c>Machine</c> into the sender's own slot <c>Slot</c>, an empty one, or with
/// none wherever it fits there (0086).
/// </summary>
public sealed record Unfit(int Machine, string Module, int? Slot = null) : Command;

/// <summary>
/// What the modules fitted into one machine add, summed, in thousandths added to 1000 (0086): its speed, its draw, a
/// tool's damage; and its heat, whole. Gains round down, costs up.
/// </summary>
public readonly record struct Boost(int Speed, int Draw, int Damage, int Heat)
{
    /// <summary><paramref name="value"/>, a gain (a station's speed), × (1000 + <paramref name="added"/>) / 1000, rounded down.</summary>
    public static int Gain(int value, int added) => value * (1000 + added) / 1000;

    /// <summary><paramref name="value"/>, a cost (what a machine draws), × (1000 + <paramref name="added"/>) / 1000, rounded up.</summary>
    public static int Cost(int value, int added) => (value * (1000 + added) + 999) / 1000;
}

public sealed partial class ItemsModule
{
    /// <summary>The modules fitted into machine <paramref name="machine"/>, by item, ordinal; none for none.</summary>
    public IReadOnlyList<string> FittedTo(int machine)
    {
        var fitted = new List<string>();
        for (int at = FittedFrom(machine); at < State.Fitted.Count && State.Fitted[at].Machine == machine; at++)
            fitted.Add(State.Fitted[at].Module);
        return fitted;
    }

    /// <summary>What the modules fitted into machine <paramref name="machine"/> add, summed (0086); nothing for none.</summary>
    public Boost Boost(int machine)
    {
        var sum = new Boost();
        for (int at = FittedFrom(machine); at < State.Fitted.Count && State.Fitted[at].Machine == machine; at++)
        {
            var m = Modules.Get(State.Fitted[at].Module);
            sum = new Boost(sum.Speed + m.Speed, sum.Draw + m.Draw, sum.Damage + m.Damage, sum.Heat + m.Heat);
        }
        return sum;
    }

    /// <summary>
    /// Why the sender's <see cref="Fit"/> of what <paramref name="from"/> holds into <paramref name="machine"/> would be
    /// refused: a client asks it while a module is held over a socket.
    /// </summary>
    public string? FitProblem(int peer, Place from, int machine)
    {
        if (_grid.MachineById(machine) is not { } m) return "items.noMachine";
        if (from.Crate is int id && !IsCrate(id)) return "items.noCrate";
        var slots = SideOf(peer, from);
        if (from.Slot < 0 || from.Slot >= slots.Count) return "items.noSlot";
        if (slots[from.Slot] is not { } stack) return "items.empty";
        if (!Modules.Fits(stack.Item, m.Kind)) return "items.notModule";
        return FittedIndex(machine, stack.Item) >= 0 ? "items.fitted" : null;
    }

    private void ApplyFit(Fit c)
    {
        if (Stacks.TakenFrom(SideOf(c.From, c.Module), c.Module.Slot, 1, out var left, out var taken) != null)
            throw new InvalidOperationException("items.fit: refused in apply");
        SetSide(c.From, c.Module, left);
        ForgetEmptied(c.Module);
        State.Fitted.Insert(~FittedIndex(c.Machine, taken.Item), new Fitted(c.Machine, taken.Item));
    }

    private string? UnfitProblem(Unfit c)
    {
        if (FittedIndex(c.Machine, c.Module) < 0) return "items.notFitted";
        var slots = Inventory(c.From);
        if (c.Slot is not int slot) return FullFor(c.From, [(c.Module, 1)]);
        if (slot < 0 || slot >= Own) return "items.noSlot";
        return slots[slot] == null ? null : "items.full";
    }

    private void ApplyUnfit(Unfit c)
    {
        State.Fitted.RemoveAt(FittedIndex(c.Machine, c.Module));
        if (c.Slot is not int slot)
        {
            Give(c.From, [(c.Module, 1)]);
            return;
        }
        var slots = Inventory(c.From).ToArray();
        slots[slot] = new Stack(c.Module, 1);
        Stacks.SetInventory(State, c.From, slots);
    }

    /// <summary>Where <paramref name="module"/> fitted into <paramref name="machine"/> is in <c>Fitted</c>, or the complement of where it would go.</summary>
    private int FittedIndex(int machine, string module)
    {
        int lo = 0, hi = State.Fitted.Count - 1;
        while (lo <= hi)
        {
            int mid = (lo + hi) / 2;
            var f = State.Fitted[mid];
            int order = f.Machine != machine ? f.Machine.CompareTo(machine) : string.CompareOrdinal(f.Module, module);
            if (order == 0) return mid;
            if (order < 0) lo = mid + 1;
            else hi = mid - 1;
        }
        return ~lo;
    }

    /// <summary>Where machine <paramref name="machine"/>'s first fitted module is in <c>Fitted</c>, or would be.</summary>
    private int FittedFrom(int machine)
    {
        int at = FittedIndex(machine, "");
        return at >= 0 ? at : ~at;
    }
}
