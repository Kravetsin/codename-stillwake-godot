using Stillwake.Game.Shared;

namespace Stillwake.Game.Features.Building;

/// <summary>
/// The plan and the card in hand (tools.ts's stores, 0022), kept in step with <see cref="Ui.Hand"/> and
/// <see cref="Ui.OverlayOn"/>: a card in hand is the plan out, and the plan put away takes the card with
/// it; the hand freed from elsewhere puts the plan away; each card picked shows its overlay, or none,
/// and the plan put away gives back the overlay before it; another overlay puts the cable away.
/// </summary>
public sealed class PlanHand
{
    private readonly Ui _ui;
    private string? _before;
    private bool _chose;

    /// <summary>The card in hand, shared by the menu that sets it and the layer that uses it.</summary>
    public Store<Tool?> Selected { get; } = new(null);

    /// <summary>
    /// Whether the plan is out, its menu shown the while: B takes it out and puts it away, a card keeps it
    /// out, and a card picked by the middle button takes it out.
    /// </summary>
    public Store<bool> PlanOut { get; } = new(false);

    public PlanHand(Ui ui)
    {
        _ui = ui;
        // In tools.ts's order: which listener runs first decides what a change leaves behind.
        Selected.Subscribe(() =>
        {
            if (Selected.Get() != null) PlanOut.Set(true);
        });
        PlanOut.Subscribe(() =>
        {
            if (!PlanOut.Get()) Selected.Set(null);
        });
        Selected.Subscribe(() => ui.Hand.Set(Held()));
        PlanOut.Subscribe(() => ui.Hand.Set(Held()));
        // Freed from elsewhere, a hotbar slot picked, the hand puts the plan away.
        ui.Hand.Subscribe(() =>
        {
            if (ui.Hand.Get() == Shared.Holding.Free && Held() != Shared.Holding.Free) PutPlanAway();
        });
        Selected.Subscribe(ShowFor);
        PlanOut.Subscribe(ShowFor);
        // The cable is laid in the power overlay: another overlay, or none, puts it away and stays.
        ui.OverlayOn.Subscribe(() =>
        {
            if (Selected.Get() is not CableTool || ui.OverlayOn.Get() == Tools.Power) return;
            Selected.Set(null);
        });
    }

    /// <summary>Puts the plan away: the menu goes and the card with it.</summary>
    public void PutPlanAway() => PlanOut.Set(false);

    /// <summary>B: takes the plan out, or puts it away.</summary>
    public void Toggle() => PlanOut.Set(!PlanOut.Get());

    private Holding Held() => Selected.Get() switch
    {
        CableTool => Shared.Holding.Cable,
        not null => Shared.Holding.Card,
        null => PlanOut.Get() ? Shared.Holding.Plan : Shared.Holding.Free,
    };

    /// <summary>Each card picked shows its overlay, or none; the plan put away gives back the one before it.</summary>
    private void ShowFor()
    {
        if (Selected.Get() is { } tool)
        {
            if (!_chose) _before = _ui.OverlayOn.Get();
            _chose = true;
            _ui.OverlayOn.Set(Tools.OverlayFor(tool));
        }
        else if (_chose && !PlanOut.Get())
        {
            _chose = false;
            _ui.OverlayOn.Set(_before);
        }
    }
}
