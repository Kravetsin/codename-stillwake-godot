using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Stillwake.Game.Features.Crew;
using Stillwake.Game.Render;

namespace Stillwake.Game.Dev;

/// <summary>
/// <c>-- --suit-cards=&lt;art folder&gt;</c>: every suit's card, and every suit skin's, cut from the hero wearing it (hero-skins): standing
/// headless on nothing, from its collar down to just under its hips, fitted to the card. A suit's is written as
/// <c>items/&lt;id&gt;.png</c>, a skin's as <c>skins/&lt;id&gt;.png</c>; then the game quits. A suit drawn in new parts gets its card
/// from the rig that wears it, never by hand.
/// </summary>
public partial class SuitCards(string folder) : Node
{
    /// <summary>The card's side and the room left round the suit on it, in hd pixels.</summary>
    private const int Card = ItemIcons.Pixels * HiRes.Scale, Room = 2;
    private const int View = 224;
    /// <summary>How far under the hip joint the card's foot is cut, a share of the hero's height: the hanging forearm and the top of the thighs show.</summary>
    private const double UnderHip = 0.07;

    private readonly List<(string Body, string Path)> _cards = [];
    private SubViewport _view = null!;
    private Node2D _world = null!;
    private Figure? _figure;
    private int _card = -1, _frames;

    public override void _Ready()
    {
        foreach (var item in Content.Items.All.Where(i => i.Equip == Stillwake.Content.Suit.Slot)) _cards.Add((item.Id, $"items/{item.Id}.png"));
        foreach (var skin in Skins.All.Where(s => s.Kind == Skins.Suit)) _cards.Add((skin.Id, $"skins/{skin.Id}.png"));
        _view = new SubViewport
        {
            Size = new Vector2I(View, View),
            TransparentBg = true,
            RenderTargetUpdateMode = SubViewport.UpdateMode.Always,
            CanvasItemDefaultTextureFilter = Viewport.DefaultCanvasItemTextureFilter.Nearest,
        };
        AddChild(_view);
        _world = new Node2D();
        _view.AddChild(_world);
        _view.AddChild(new Camera2D { Zoom = new Vector2(HiRes.Scale, HiRes.Scale), Position = new Vector2(0, (float)(-Rig.H / 2)), Enabled = true });
        Lit.Ambient((1, 1, 1));
        Next();
    }

    private void Next()
    {
        _card++;
        _frames = 0;
        _figure?.QueueFree();
        _figure = null;
        if (_card >= _cards.Count)
        {
            GD.Print($"suit cards: {_cards.Count} written into {folder}");
            GetTree().Quit();
            return;
        }
        int colour = Suits.All[0];
        _figure = new Figure(colour, own: false) { Tracked = false, Headless = true };
        _figure.Wear(colour, null, _cards[_card].Body);
        _world.AddChild(_figure);
        for (int i = 0; i < 40; i++) _figure.Show(new Hero(0, 0, 0, 0, false, 1), Pose.Stand, 0, false, 1000.0 / 60);
        _figure.Position = Vector2.Zero;
    }

    public override void _Process(double delta)
    {
        if (_card >= _cards.Count || ++_frames < 3) return;
        var shot = _view.GetTexture().GetImage();
        shot.Convert(Image.Format.Rgba8);
        var card = CardOf(shot);
        string path = $"{folder}/{_cards[_card].Path}";
        DirAccess.MakeDirRecursiveAbsolute(path.GetBaseDir());
        card.SavePng(path);
        Next();
    }

    // The suit from its top down to the cut under the hips, its colours as drawn (the view's are over nothing, so weighed by their
    // cover), fitted into the card with room round it, centred.
    private static Image CardOf(Image shot)
    {
        int feet = (int)Math.Round(View / 2 + Rig.H / 2 * HiRes.Scale);
        int cut = feet - (int)Math.Round((Rig.Hip - Rig.H * UnderHip) * HiRes.Scale);
        int x0 = View, x1 = -1, y0 = View;
        for (int y = 0; y < cut; y++)
            for (int x = 0; x < View; x++)
                if (shot.GetPixel(x, y).A > 0)
                    (x0, x1, y0) = (Math.Min(x0, x), Math.Max(x1, x), Math.Min(y0, y));
        var suit = shot.GetRegion(new Rect2I(x0, y0, x1 - x0 + 1, cut - y0));
        for (int y = 0; y < suit.GetHeight(); y++)
            for (int x = 0; x < suit.GetWidth(); x++)
            {
                var c = suit.GetPixel(x, y);
                if (c.A > 0) suit.SetPixel(x, y, new Color(Math.Min(1, c.R / c.A), Math.Min(1, c.G / c.A), Math.Min(1, c.B / c.A), c.A));
            }
        double k = Math.Min((Card - 2.0 * Room) / suit.GetWidth(), (Card - 2.0 * Room) / suit.GetHeight());
        int w = (int)Math.Round(suit.GetWidth() * k), h = (int)Math.Round(suit.GetHeight() * k);
        suit.Resize(w, h, Image.Interpolation.Lanczos);
        var card = Image.CreateEmpty(Card, Card, false, Image.Format.Rgba8);
        card.BlitRect(suit, new Rect2I(0, 0, w, h), new Vector2I((Card - w) / 2, (Card - h) / 2));
        return card;
    }
}
