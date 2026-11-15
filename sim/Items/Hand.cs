namespace Stillwake.Sim.Items;

/// <summary>
/// The sender's hero holds hotbar slot <c>Slot</c> in its hand (tool-in-hand): sent while its client's chosen slot differs
/// from the sim's, as <c>Run</c> is, before any run or hit of the same frame.
/// </summary>
public sealed record Wield(int Slot) : Command;

/// <summary>A peer whose hand holds a hotbar slot other than the first (tool-in-hand).</summary>
public sealed record Hand(int Peer, int Slot);

public sealed partial class ItemsModule
{
    /// <summary>The hotbar slot <paramref name="peer"/>'s hero holds in its hand (tool-in-hand): the first until it wields another.</summary>
    public int Wielding(int peer) => State.Hands.Find(h => h.Peer == peer)?.Slot ?? 0;

    /// <summary>
    /// The stack of tool <paramref name="item"/> <paramref name="peer"/> works with (tool-in-hand): the one in its hand,
    /// while that is <paramref name="item"/>; none otherwise, whatever else it carries.
    /// </summary>
    public Stack? ToolStack(int peer, string item) =>
        Inventory(peer)[Wielding(peer)] is { } s && s.Item == item ? s : null;

    /// <summary><c>items.notHotbar</c> for a slot off the hotbar: a hand holds only what the hotbar shows.</summary>
    private static string? WieldProblem(Wield c) => c.Slot >= 0 && c.Slot < Content.Inventory.Hotbar ? null : "items.notHotbar";

    private void ApplyWield(Wield c) => SetHand(State, c.From, c.Slot);

    private void SetHand(int peer, int slot) => SetHand(State, peer, slot);

    /// <summary>Keeps <paramref name="peer"/>'s hand on <paramref name="slot"/>, in peer order; the first slot is no entry.</summary>
    internal static void SetHand(ItemsState state, int peer, int slot)
    {
        var hands = state.Hands;
        hands.RemoveAll(h => h.Peer == peer);
        if (slot == 0) return;
        int at = hands.FindIndex(h => h.Peer > peer);
        hands.Insert(at < 0 ? hands.Count : at, new Hand(peer, slot));
    }

    /// <summary>
    /// Now and then a wield: mostly of a hotbar slot holding something, so runs of what is there are taken; now and then of
    /// any hotbar slot, or one off it (<c>items.notHotbar</c>).
    /// </summary>
    private static Wield WieldSome(Rng rng, int from, Stack?[]? own)
    {
        var filled = new List<int>();
        for (int i = 0; i < Content.Inventory.Hotbar && i < (own?.Length ?? 0); i++) if (own![i] != null) filled.Add(i);
        if (filled.Count > 0 && rng.Below(4) != 0) return new Wield(filled[(int)rng.Below((uint)filled.Count)]) { From = from };
        return new Wield((int)rng.Below(Content.Inventory.Hotbar + 2) - 1) { From = from };
    }
}
