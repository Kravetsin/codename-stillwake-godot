using System;
using System.Collections.Generic;
using Godot;

namespace Stillwake.Game.Shared;

/// <summary>
/// A screen of the wrist computer (0074): a panel whose background is the screen (<see cref="WristArt.DrawScreen"/>),
/// framed by its piece of metal, which takes the clicks on the bezel; with <c>lit</c>, a faint wash of the screen's blue
/// round it and its lamp's glow, as the build tablet's (0073). Its padding and what it holds are as any panel's; hardware
/// mounted on its metal (<see cref="Mount"/>, the bag's sort key, 0081) stands outside the screen and is never dimmed with it.
/// </summary>
public partial class WristScreen : PanelContainer
{
    private static readonly Color Light = new(70 / 255f, 150 / 255f, 230 / 255f, 0.12f), LedLight = new(74 / 255f, 222 / 255f, 128 / 255f, 0.35f);
    /// <summary>How much wider and taller than the screen its light reaches, in screen pixels.</summary>
    private static readonly Vector2 Reach = new(320, 200);

    private readonly NinePatchRect _art;
    private readonly Color _ownScreen;
    private Color _screen;
    private readonly TextureRect? _light, _led;
    /// <summary>The lamp's own colour over the art's green while tinted (<see cref="Tint"/>).</summary>
    private Control? _lamp;
    private readonly List<(Control Part, Func<Vector2, Vector2> At)> _mounts = [];
    private readonly WristPiece _piece;
    private float _power = 1;
    private double _blink;

    /// <param name="piece">Its metal.</param>
    /// <param name="screen">Its screen's colour.</param>
    /// <param name="padding">Between the screen's edge and what it shows, in screen pixels.</param>
    /// <param name="lit">Whether it lights what is round it, and its lamp glows.</param>
    /// <param name="across">Its left and right padding, when not <paramref name="padding"/>.</param>
    public WristScreen(WristPiece piece, Color screen, int padding, bool lit = false, int? across = null)
    {
        _piece = piece;
        _screen = _ownScreen = screen;
        MouseFilter = MouseFilterEnum.Stop;
        var box = new StyleBoxEmpty();
        box.SetContentMarginAll(padding);
        box.ContentMarginLeft = box.ContentMarginRight = across ?? padding;
        AddThemeStyleboxOverride("panel", box);
        if (lit)
        {
            _light = ScreenGlow.Of(Light, 0.05f);
            _light.ShowBehindParent = true;
            AddChild(_light);
        }
        AddChild(_art = WristArt.Frame(piece));
        if (lit && piece.Led != null) AddChild(_led = ScreenGlow.Of(LedLight, 0));
        // After the container has laid its children out over the screen: the art and the lights go round it instead.
        SortChildren += Place;
    }

    // Godot turns processing on at ready for any node with a _Process: only a flaring lamp needs it.
    public override void _Ready() => SetProcess(_blink > 0);

    /// <summary>Where the screen, its metal and what is mounted on it lie on the screen: what the crew's markers keep out of.</summary>
    public Rect2 Outer
    {
        get
        {
            var outer = _art.GetGlobalRect();
            foreach (var (part, _) in _mounts) outer = outer.Merge(part.GetGlobalRect());
            return outer;
        }
    }

    /// <summary>
    /// Mounts <paramref name="part"/> on the metal, its own size, its top-left where <paramref name="at"/> puts it from the
    /// screen's size: outside the screen, never dimmed with it.
    /// </summary>
    public void Mount(Control part, Func<Vector2, Vector2> at)
    {
        _mounts.Add((part, at));
        AddChild(part);
    }

    /// <summary>Flares the lamp for <see cref="WristLook.LampBlinkS"/>: the bag sorted.</summary>
    public void Blink()
    {
        if (_led == null) return;
        _blink = WristLook.LampBlinkS;
        SetProcess(true);
    }

    public override void _Process(double delta)
    {
        if (_led == null) return;
        _blink = Math.Max(0, _blink - delta);
        float flare = (float)(_blink / WristLook.LampBlinkS);
        // From the lamp's 0.35 to about 0.95, and a third wider, as the mockup's.
        _led.SelfModulate = new Color(1 + 1.7f * flare, 1 + 1.7f * flare, 1 + 1.7f * flare);
        _led.Scale = Vector2.One * (1 + 0.45f * flare);
        if (_blink <= 0) SetProcess(false);
    }

    /// <summary>
    /// Tints the screen <paramref name="screen"/>, its light and lamp <paramref name="glow"/>; none, back to its own: the
    /// equipment's turns warm amber on its Appearance tab (gear-tabs).
    /// </summary>
    public void Tint(Color? screen, Color? glow)
    {
        _screen = screen ?? _ownScreen;
        QueueRedraw();
        if (_light != null) ScreenGlow.Recolour(_light, glow is { } light ? light with { A = Light.A } : Light);
        if (_led == null) return;
        ScreenGlow.Recolour(_led, glow is { } led ? led with { A = LedLight.A * 1.4f } : LedLight);
        if (glow is { } lamp)
        {
            var dot = new StyleBoxFlat { BgColor = lamp.Lightened(0.2f), ShadowColor = lamp with { A = 0.6f }, ShadowSize = 2 };
            dot.SetCornerRadiusAll(3);
            if (_lamp == null) AddChild(_lamp = new Panel { MouseFilter = MouseFilterEnum.Ignore });
            _lamp.AddThemeStyleboxOverride("panel", dot);
            QueueSort();
        }
        if (_lamp != null) _lamp.Visible = glow != null;
    }

    /// <summary>What the screen shows, and its light, at <paramref name="power"/> of lit: 0 dark, 1 on (<see cref="TabletMotion.Power"/>).</summary>
    public void Lit(float power)
    {
        if (power == _power) return;
        _power = power;
        var shown = Colors.White with { A = power };
        foreach (var child in GetChildren())
            if (child is CanvasItem item && child != _art && !_mounts.Exists(m => m.Part == child)) item.Modulate = shown;
    }

    private void Place()
    {
        WristArt.Fit(_art, _piece, Size);
        // The container stretched them over the screen as it does every child: each back to its own size and place.
        foreach (var (part, at) in _mounts)
        {
            part.Size = part.CustomMinimumSize;
            part.Position = at(Size);
        }
        if (_light != null)
        {
            _light.Size = Size + Reach;
            _light.Position = -Reach / 2;
        }
        if (_led != null && _piece.Led is var (fromRight, down))
        {
            int s = WristPiece.Scale;
            _led.Size = new Vector2(18, 18);
            _led.PivotOffset = _led.Size / 2;
            _led.Position = new Vector2(Size.X + (_piece.Window.Right - fromRight) * s, (down - _piece.Window.Top) * s) - _led.Size / 2;
            if (_lamp != null)
            {
                _lamp.Size = new Vector2(5, 5);
                _lamp.Position = _led.Position + (_led.Size - _lamp.Size) / 2;
            }
        }
    }

    public override void _Draw() => WristArt.DrawScreen(this, Size, _screen);
}
