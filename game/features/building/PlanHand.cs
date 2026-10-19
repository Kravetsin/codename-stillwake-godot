using Stillwake.Game.Shared;

namespace Stillwake.Game.Features.Building;

/// <summary>
/// The plan, its tab and the card in hand (tools.ts's stores, 0022, 0030), kept in step with
/// <see cref="Ui.Hand"/>: the plan put away takes the card with it, the hand freed from elsewhere puts
/// the plan away, and another tab lets the card go, for a card belongs to its tab. The overlay that
/// shows is read from these (<see cref="Ui.OverlayShown"/>), never kept: nothing here sets an overlay.
/// </summary>
public sealed class PlanHand
{
    /// <summary>The card in hand, shared by the menu that sets it and the layer that uses it.</summary>
    public Store<Tool?> Selected { get; } = new(null);

    /// <summary>Whether the plan is out, its menu shown the while: B takes it out and puts it away.</summary>
    public Store<bool> PlanOut { get; } = new(false);

    /// <summary>The build menu's tab: the mode while the menu is open, kept while it is closed so B opens on it again.</summary>
    public Store<string> Tab { get; } = new(Tools.Tabs[0].Id);

    /// <summary>
    /// The shape walls are planned in (0069): whole, or a <see cref="Sim.Grid.Shape"/> R turned to. It sticks, for every wall
    /// card, until turned again.
    /// </summary>
    public Store<int> Shape { get; } = new(Sim.Grid.Shape.None);

    /// <summary>The shapes R turns through, in order: whole, ◢, ◣, ◤, ◥.</summary>
    private static readonly int[] Turns = [Sim.Grid.Shape.None, .. Sim.Grid.Shape.Cut];

    /// <summary>Q pressed with the menu open: the building takes the card under the pointer at its next frame.</summary>
    public bool PickWanted { get; set; }

    public PlanHand(Ui ui)
    {
        PlanOut.Subscribe(() =>
        {
            if (!PlanOut.Get()) Selected.Set(null);
        });
        Tab.Subscribe(() =>
        {
            if (Selected.Get() is { } tool && Tools.TabOf(tool) != Tab.Get()) Selected.Set(null);
        });
        Selected.Subscribe(() => ui.Hand.Set(Held()));
        PlanOut.Subscribe(() => ui.Hand.Set(Held()));
        // Freed from elsewhere, a hotbar slot picked, the hand puts the plan away.
        ui.Hand.Subscribe(() =>
        {
            if (ui.Hand.Get() == Shared.Holding.Free && Held() != Shared.Holding.Free) PutPlanAway();
        });
        ui.OverlayShown = () => Tools.OverlayShown(PlanOut.Get(), Tab.Get(), ui.AirOn.Get());
    }

    /// <summary>The card in hand as it places: a wall's cut to <see cref="Shape"/>.</summary>
    public Tool? InHand => Tools.Shaped(Selected.Get(), Shape.Get());

    /// <summary>
    /// R, <paramref name="by"/> 1, or Shift+R, -1: with a hull wall's card in hand, the next shape round, or the one
    /// before. Answers whether it turned: with any other card, or none, R does nothing.
    /// </summary>
    public bool Turn(int by)
    {
        if (Selected.Get() is not PlaceTool { Placement.Kind: "wall" }) return false;
        int at = System.Array.IndexOf(Turns, Shape.Get());
        Shape.Set(Turns[((at + by) % Turns.Length + Turns.Length) % Turns.Length]);
        return true;
    }

    /// <summary>Puts the plan away: the menu goes and the card with it.</summary>
    public void PutPlanAway() => PlanOut.Set(false);

    /// <summary>B: takes the plan out, or puts it away.</summary>
    public void Toggle() => PlanOut.Set(!PlanOut.Get());

    /// <summary>A card into the hand, its own tab opened first; the same card again lets it go.</summary>
    public void Pick(Tool tool)
    {
        if (tool == Selected.Get())
        {
            Selected.Set(null);
            return;
        }
        Tab.Set(Tools.TabOf(tool));
        Selected.Set(tool);
    }

    private Holding Held() => Selected.Get() switch
    {
        CableTool => Shared.Holding.Cable,
        not null => Shared.Holding.Card,
        null => PlanOut.Get() ? Shared.Holding.Plan : Shared.Holding.Free,
    };
}
