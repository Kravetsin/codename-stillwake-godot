using Stillwake.Content;

namespace Stillwake.Sim.Items;

/// <summary>
/// Fits the module in <c>Module</c>, a slot of the sender's or of a machine, into machine <c>Machine</c> (0086): one of
/// each kind a machine, out of its slots; at an arc bench, into the tool on its first slot, onto that stack (0099).
/// </summary>
public sealed record Fit(Place Module, int Machine) : Command;

/// <summary>
/// Takes module <c>Module</c> out of machine <c>Machine</c>, or out of the tool on an arc bench (0099), into the sender's own
/// slot <c>Slot</c>, an empty one, or with none wherever it fits there (0086).
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

    /// <summary>This and what <paramref name="module"/> adds, summed: the one step of every fold of modules, a machine's and a tool's (<see cref="ToolStats"/>).</summary>
    public Boost Plus(ModuleSpec module) => new(Speed + module.Speed, Draw + module.Draw, Damage + module.Damage, Heat + module.Heat);
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
            sum = sum.Plus(Modules.Get(State.Fitted[at].Module));
        return sum;
    }

    /// <summary>
    /// The stack of tool <paramref name="item"/> <paramref name="peer"/> works with (0098): the first it carries, in slot order,
    /// with a module fitted, else the first it carries; none while it carries none. With two drills, the fitted one drills.
    /// </summary>
    public Stack? ToolStack(int peer, string item)
    {
        var slots = Inventory(peer);
        Stack? first = null;
        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i] is not { } s || s.Item != item) continue;
            if (s.Module != null) return s;
            first ??= s;
        }
        return first;
    }

    /// <summary>
    /// What a hit of weapon <paramref name="tool"/> by <paramref name="peer"/> takes (0098): the damage of the stack it works
    /// with (<see cref="ToolStack"/>), its modules in it (<see cref="ToolStats"/>); the weapon's own while the peer carries
    /// none, as a hit's sender is not checked for holding its weapon (0034).
    /// </summary>
    public int Damage(int peer, string tool)
    {
        int damage = Weapons.Of(tool)?.Damage ?? throw new InvalidOperationException($"items.damage: {tool} deals no damage");
        return ToolStack(peer, tool) is { } stack ? ToolStats.Of(stack).Damage : damage;
    }

    /// <summary>What <paramref name="stack"/>, an electric item, draws a tick it runs, its modules in it (<see cref="ToolStats"/>, 0098).</summary>
    private static int DrawOf(Stack stack) => ToolStats.Of(stack).Draw;

    /// <summary>
    /// Why the sender's <see cref="Fit"/> of what <paramref name="from"/> holds into <paramref name="machine"/> would be
    /// refused: a client asks it while a module is held over a socket. At an arc bench (0099) the tool on it is the host:
    /// <c>items.noTool</c> while it holds none, <c>items.notModule</c> for what that tool takes not, <c>items.fitted</c> for
    /// one holding a module already, as a stack holds one.
    /// </summary>
    public string? FitProblem(int peer, Place from, int machine)
    {
        if (_grid.MachineById(machine) is not { } m) return "items.noMachine";
        if (from.Crate is int id && !IsCrate(id)) return "items.noCrate";
        var slots = SideOf(peer, from);
        if (from.Slot < 0 || from.Slot >= slots.Count) return "items.noSlot";
        if (slots[from.Slot] is not { } stack) return "items.empty";
        if (Tunes(machine))
        {
            if (ToolOn(machine) is not { } tool) return "items.noTool";
            if (!Modules.Fits(stack.Item, tool.Item)) return "items.notModule";
            return tool.Module != null ? "items.fitted" : null;
        }
        if (!Modules.Fits(stack.Item, m.Kind)) return "items.notModule";
        return FittedIndex(machine, stack.Item) >= 0 ? "items.fitted" : null;
    }

    private void ApplyFit(Fit c)
    {
        if (Stacks.TakenFrom(SideOf(c.From, c.Module), c.Module.Slot, 1, out var left, out var taken) != null)
            throw new InvalidOperationException("items.fit: refused in apply");
        SetSide(c.From, c.Module, left);
        ForgetEmptied(c.Module);
        if (Tunes(c.Machine))
        {
            // Read after the take: the module may have come out of the bench's own slots.
            var bench = Crate(c.Machine).ToArray();
            bench[0] = bench[0]! with { Module = taken.Item };
            SetMachine(c.Machine, bench);
            return;
        }
        State.Fitted.Insert(~FittedIndex(c.Machine, taken.Item), new Fitted(c.Machine, taken.Item));
    }

    /// <summary>The tool on arc bench <paramref name="bench"/>'s first slot (0099); none while it holds none.</summary>
    private Stack? ToolOn(int bench) => Crate(bench) is { Count: > 0 } slots ? slots[0] : null;

    /// <summary>Whether machine <paramref name="machine"/> is an arc bench (<c>MachineSpec.Tunes</c>, 0099).</summary>
    private bool Tunes(int machine) => HolderOf(machine)?.Tunes == true;

    private string? UnfitProblem(Unfit c)
    {
        if (Tunes(c.Machine))
        {
            if (ToolOn(c.Machine) is not { } tool) return "items.noTool";
            if (tool.Module != c.Module) return "items.notFitted";
        }
        else if (FittedIndex(c.Machine, c.Module) < 0) return "items.notFitted";
        var slots = Inventory(c.From);
        if (c.Slot is not int slot) return FullFor(c.From, [(c.Module, 1)]);
        if (slot < 0 || slot >= Own) return "items.noSlot";
        return slots[slot] == null ? null : "items.full";
    }

    private void ApplyUnfit(Unfit c)
    {
        if (Tunes(c.Machine))
        {
            var bench = Crate(c.Machine).ToArray();
            bench[0] = bench[0]! with { Module = null };
            SetMachine(c.Machine, bench);
        }
        else State.Fitted.RemoveAt(FittedIndex(c.Machine, c.Module));
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
