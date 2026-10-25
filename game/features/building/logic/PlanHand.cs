using System.Collections.Generic;
using System.Linq;
using Stillwake.Game.Shared;

namespace Stillwake.Game.Features.Building;

/// <summary>
/// The plan, its mode, its tab and the card in hand (tools.ts's stores, 0022, 0030), kept in step with
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

    /// <summary>The build menu's tab, building: kept while the menu is closed so B opens on it again.</summary>
    public Store<string> Tab { get; } = new(Tools.Tabs[0].Id);

    /// <summary>The layer the open plan works in (0110): a card picked sets it to its own, the middle button or a layer's button turns it.</summary>
    public Store<Focus> Focus { get; } = new(Building.Focus.Front);

    /// <summary>Whether this player's hero is on the ship (0110): away from it the layers neither toggle nor fade, and the plan only builds.</summary>
    public Store<bool> Aboard { get; } = new(true);

    /// <summary>What the open plan works on (plan modes), kept while it is away so B opens in it again: Z, X, C or its keys pick it.</summary>
    public Store<PlanMode> Mode { get; } = new(PlanMode.Build);

    /// <summary>The mode the plan works in now: <see cref="Mode"/> aboard; away from the ship, building, for cables and pipes are laid aboard only.</summary>
    public PlanMode ModeShown => Aboard.Get() ? Mode.Get() : PlanMode.Build;

    /// <summary>Whether the open plan builds: its tabs, search, layers and card in hand are this mode's alone.</summary>
    public bool Builds => ModeShown == PlanMode.Build;

    /// <summary>The focus the world is drawn by: the open plan's aboard, building; none otherwise, so nothing fades and the ground hides what is in it.</summary>
    public Focus? Shown => PlanOut.Get() && Aboard.Get() && Builds ? Focus.Get() : null;

    /// <summary>Whether the front fades round the pointer as a hand <paramref name="digsBack"/>: with the plan away, aboard only (0110).</summary>
    public bool SeesThrough(bool digsBack) => digsBack && !PlanOut.Get() && Aboard.Get();

    /// <summary>The wiring's or the piping's card picked (0110): what the left button lays; the mode's first as it is entered.</summary>
    public Store<HandCard?> ToolCard { get; } = new(null);

    /// <summary>The cards of the mode's menu with the plan out: the wiring's or the piping's; none while building, the plan away or away from the ship.</summary>
    public IReadOnlyList<HandCard> ToolCards => PlanOut.Get() ? Tools.CardsOf(ModeShown) : [];

    /// <summary>The card each layer had in hand when the focus left it, by <see cref="Building.Focus"/>; and the card in hand.</summary>
    private readonly Tool?[] _kept = new Tool?[2];
    private Tool? _held;
    /// <summary>The card in hand when building was left for wiring or piping: back in hand on coming back.</summary>
    private Tool? _building;

    /// <summary>Q pressed with the menu open: the building takes the card under the pointer at its next frame.</summary>
    public bool PickWanted { get; set; }

    public PlanHand(Ui ui)
    {
        // A card of the other layer picked takes the focus there, the one in hand kept for coming back (0110).
        Selected.Subscribe(() =>
        {
            if (Selected.Get() is { } tool && Tools.FocusOf(tool) != Focus.Get())
            {
                _kept[(int)Focus.Get()] = _held;
                Focus.Set(Tools.FocusOf(tool));
            }
            _held = Selected.Get();
        });
        void FirstCard() => ToolCard.Set(ToolCards.Count > 0 ? ToolCards[0] : null);
        Mode.Subscribe(FirstCard);
        Aboard.Subscribe(FirstCard);
        // Off the ship, wiring or piping, the plan builds: the card put aside comes back to hand, and goes aside again aboard.
        Aboard.Subscribe(() =>
        {
            if (Mode.Get() == PlanMode.Build) return;
            var back = Aboard.Get() ? null : _building;
            _building = Aboard.Get() ? Selected.Get() : null;
            Selected.Set(back);
        });
        PlanOut.Subscribe(FirstCard);
        PlanOut.Subscribe(() =>
        {
            if (PlanOut.Get()) return;
            Selected.Set(null);
            System.Array.Clear(_kept);
            _building = null;
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
        ui.OverlayShown = () => Tools.OverlayShown(PlanOut.Get(), ModeShown, Tab.Get(), ui.AirOn.Get());
    }

    /// <summary>
    /// Z, X, C or a mode's key with the plan out, aboard: the plan to <paramref name="mode"/>. The card in hand is put aside
    /// leaving building and back in hand on coming back, so wiring and piping never draw a ghost.
    /// </summary>
    public void SetMode(PlanMode mode)
    {
        if (!PlanOut.Get() || !Aboard.Get() || mode == Mode.Get()) return;
        if (Mode.Get() == PlanMode.Build)
        {
            _building = Selected.Get();
            Selected.Set(null);
        }
        Mode.Set(mode);
        if (mode == PlanMode.Build)
        {
            Selected.Set(_building);
            _building = null;
        }
    }


    /// <summary>Puts the plan away: the menu goes and the card with it.</summary>
    public void PutPlanAway() => PlanOut.Set(false);

    /// <summary>B: takes the plan out, or puts it away.</summary>
    public void Toggle() => PlanOut.Set(!PlanOut.Get());

    /// <summary>Whether the surface has a back layer to build in: none on a planet, where the focus stays on the front.</summary>
    public bool Layered { get; set; } = true;

    /// <summary>The middle button with the plan out: the focus to the other layer.</summary>
    public void ToggleFocus() => FocusOn(Tools.Toggled(Focus.Get()));

    /// <summary>
    /// A layer's button, or the middle button, with the plan out: the focus to <paramref name="focus"/>, the card in hand kept
    /// for the layer it leaves and the one kept for <paramref name="focus"/> back in hand, so a card always is of the focused layer.
    /// </summary>
    public void FocusOn(Focus focus)
    {
        if (!PlanOut.Get() || !Builds || focus == Focus.Get() || !Layered && focus == Building.Focus.Back) return;
        _kept[(int)Focus.Get()] = Selected.Get();
        var back = _kept[(int)focus];
        _kept[(int)focus] = null;
        Focus.Set(focus);
        if (back != null) Tab.Set(Tools.TabOf(back));
        Selected.Set(back);
    }

    /// <summary>A card of the wiring's or the piping's menu picked: what the left button lays from now on. A card the mode does not offer is let be.</summary>
    public void PickToolCard(HandCard card)
    {
        if (ToolCards.Contains(card)) ToolCard.Set(card);
    }

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
        not null => Shared.Holding.Card,
        null => PlanOut.Get() ? Shared.Holding.Plan : Shared.Holding.Free,
    };
}
