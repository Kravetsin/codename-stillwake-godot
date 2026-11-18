using System;
using Godot;
using Stillwake.Game.Shared;
using Stillwake.Sim.Items;
using Stillwake.Sim.Players;

namespace Stillwake.Game.Features.Crew;

/// <summary>
/// This player's own hero standing on the equipment's screen (gear-tabs, pick B), the doll the slots are laid round: drawn live,
/// unlit, facing right as the look editor's, and dressed as the hero in the world is (<see cref="Suits.DressedAs"/>). Handed to the
/// items panel by <c>Main</c>; its dress is read when that panel reads (<see cref="Dress"/>), only while it shows; the one figure
/// moves every frame while it does.
/// </summary>
public partial class HeroDoll : SubViewportContainer
{
    /// <summary>What the doll wears: everything the world's figure is dressed in.</summary>
    private readonly record struct Worn(int Colour, Dressed Dressed, Look Look, bool Jetpack, bool HeadLamp, bool Scanner);

    private readonly Func<(IItemsQueries Items, IPlayersQueries Players, int Peer)?> _wearer;
    private readonly Figure _figure;
    private Worn? _worn;

    /// <param name="slot">The slot size it is laid out for (<see cref="DollLayout"/>).</param>
    /// <param name="wearer">This player's world and peer, none outside one.</param>
    public HeroDoll(int slot, Func<(IItemsQueries Items, IPlayersQueries Players, int Peer)?> wearer)
    {
        _wearer = wearer;
        var layout = DollLayout.Of(slot);
        MouseFilter = MouseFilterEnum.Ignore;
        Stretch = true;
        CustomMinimumSize = new Vector2(layout.Width, layout.Height);
        var view = new SubViewport { Size = new Vector2I(layout.Width, layout.Height), TransparentBg = true };
        AddChild(view);
        _figure = new Figure(Suits.All[0], own: false) { GivesLight = false };
        view.AddChild(_figure);
        // The world's origin at the feet: the camera's middle put so they stand where the layout says.
        float zoom = layout.Tall / (float)Rig.H;
        view.AddChild(new Camera2D
        {
            Zoom = new Vector2(zoom, zoom),
            Position = new Vector2(layout.Width / 2f - layout.Feet.X, layout.Height / 2f - layout.Feet.Y) / zoom,
            Enabled = true,
        });
        VisibilityChanged += Dress;
    }

    /// <summary>Reads what this player's hero wears and dresses the doll in it, while it shows: the items panel's read.</summary>
    public void Dress()
    {
        if (!IsVisibleInTree() || _wearer() is not var (items, players, peer)) return;
        var worn = new Worn(Suits.Of(players, peer), Suits.DressedAs(items, players, peer), players.LookOf(peer),
            items.Equipped(peer, Content.Jetpack.Item), items.Equipped(peer, Content.HelmetLamp.Item), items.Equipped(peer, Content.Helmet.Scanner));
        if (worn == _worn) return;
        _worn = worn;
        _figure.Wear(worn.Colour, worn.Dressed.Helmet, worn.Dressed.Body, worn.Look);
        _figure.Jetpack = worn.Jetpack;
        _figure.HeadLamp = worn.HeadLamp;
        _figure.Scanner = worn.Scanner;
        _figure.CapeWorn = worn.Dressed.Cape;
        _figure.EffectSkin = SkinEffects.Of(worn.Dressed.Effect);
        Unlit(_figure);
    }

    public override void _Process(double delta)
    {
        if (IsVisibleInTree()) _figure.Show(new Hero(0, 0, 0, 0, false, 1), Pose.Stand, 0, false, delta * 1000);
    }

    /// <summary>Drawn as painted: the world's light does not reach the screen.</summary>
    private static void Unlit(Node node)
    {
        if (node is CanvasItem { Material: ShaderMaterial } item) item.Material = null;
        foreach (var child in node.GetChildren()) Unlit(child);
    }
}
