using System.Linq;
using Godot;
using Stillwake.Game.Features.Building;
using Stillwake.Game.Render;
using Stillwake.Game.Shared;

namespace Stillwake.Game.Dev;

/// <summary>
/// <c>-- --testbed --card=&lt;id&gt;</c>: a second in, the plan taken out on the tab of the build card <c>id</c>, and
/// that card hovered, its tooltip shown by a pointer held over the menu: for recording a card's cost and use (the
/// solar panel's).
/// </summary>
public partial class CardDemo(Hands hands, string id) : Node
{
    private const double OpenAt = 1.0;
    /// <summary>Where the pointer is held, on the screen: over the menu, so the tooltip stands clear of it.</summary>
    private static readonly Vector2 Held = new(440, 420);
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
        hands.Menu.Hover(card);
        hands.Mouse.Script = () =>
        {
            var world = hands.GetViewport().GetCanvasTransform().AffineInverse() * Held / ShipCamera.Cell;
            return (world.X, world.Y);
        };
    }
}
