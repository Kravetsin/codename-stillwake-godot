using System.Linq;
using Godot;
using Stillwake.Game.Features.Building;
using Stillwake.Game.Render;
using Stillwake.Game.Shared;

namespace Stillwake.Game.Dev;

/// <summary>
/// <c>-- --testbed --card=&lt;id&gt;</c>: a second in, the plan taken out on the tab of the build card <c>id</c>, and
/// that card hovered, its tooltip shown by a pointer held over the menu: for recording a card's cost and use (the
/// solar panel's). With <c>--pick</c> the card is taken in hand and the pointer held over the ship instead: its ghost,
/// and a bay's zone (0062).
/// </summary>
public partial class CardDemo(Hands hands, string id, bool pick = false) : Node
{
    private const double OpenAt = 1.0;
    /// <summary>Where the pointer is held, on the screen: over the menu, so the tooltip stands clear of it; over the ship with a card in hand.</summary>
    private static readonly Vector2 Held = new(440, 420), Over = new(1220, 330);
    private double _t;
    private bool _shown;

    public override void _Process(double delta)
    {
        _t += delta;
        if (_shown || _t < OpenAt) return;
        _shown = true;
        var card = Tools.All.First(t => t.Id == id);
        hands.Plan.Toggle();
        hands.Menu.ShowTab(Tools.TabOf(card));
        if (pick) hands.Plan.Pick(card);
        else hands.Menu.Hover(card);
        var at = pick ? Over : Held;
        hands.Mouse.Script = () =>
        {
            var world = hands.GetViewport().GetCanvasTransform().AffineInverse() * at / ShipCamera.Cell;
            return (world.X, world.Y);
        };
    }
}
