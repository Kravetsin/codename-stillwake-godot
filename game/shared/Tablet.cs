using System.Collections.Generic;
using Godot;
using Stillwake.Game.Render;

namespace Stillwake.Game.Shared;

/// <summary>
/// The rugged planning tablet (0073), the build menu's and the Handbook's (0083): its screen this panel, square, edged
/// by the bezel's recess; round it the frame's pixel art at <see cref="TabletLayout.ArtScale"/> (a 9-slice whose
/// stretches tile, so any size is as crisp), the ID plate at the top and the stylus at the right; the screen's faint
/// blue wash round it and the status lamp's glow; the glass's shadow and glare over what it shows. It slides and its
/// screen comes on as <see cref="TabletMotion"/> says; where it rests is its owner's.
/// </summary>
public partial class Tablet : Panel
{
    /// <summary>The colour the screen lights what is round it with, and how strongly at the middle (the concept's).</summary>
    private static readonly Color ScreenLight = new(70 / 255f, 150 / 255f, 230 / 255f, 0.2f), LedLight = new(74 / 255f, 222 / 255f, 128 / 255f, 0.55f);
    /// <summary>How much wider and taller than the screen its wash reaches: the build menu's 1120 × 470 round 638 × 204.</summary>
    private static readonly Vector2 WashReach = new(482, 266);

    /// <summary>The tablet's text: the strips' size, and the hint's along the foot.</summary>
    public const int FontSize = 12, HintSize = 11;

    private readonly TabletMotion _motion = new();
    private readonly NinePatchRect _frame = new()
    {
        Texture = GD.Load<Texture2D>("res://art/tablet_frame.png"),
        PatchMarginLeft = 74,
        PatchMarginRight = 77,
        PatchMarginTop = 21,
        PatchMarginBottom = 14,
        AxisStretchHorizontal = NinePatchRect.AxisStretchMode.Tile,
        AxisStretchVertical = NinePatchRect.AxisStretchMode.Tile,
        DrawCenter = false,
        Scale = new Vector2(TabletLayout.ArtScale, TabletLayout.ArtScale),
        ShowBehindParent = true,
        TextureFilter = TextureFilterEnum.Nearest,
        MouseFilter = MouseFilterEnum.Stop,
    };
    private readonly TextureRect _plate = Art("res://art/tablet_plate.png"), _stylus = Art("res://art/tablet_stylus.png");
    /// <summary>The status lamp dark, over the art's lit one, while the screen is off; its glow while it is on.</summary>
    private readonly ColorRect _ledOff = new() { Color = new Color("1f3b2a"), Size = new Vector2(2, 2), MouseFilter = MouseFilterEnum.Ignore };
    private readonly TextureRect _ledGlow = ScreenGlow.Of(LedLight, 0), _wash = ScreenGlow.Of(ScreenLight, 40f / 560);
    /// <summary>The glass over the screen: the recess's shadow and a faint glare.</summary>
    private readonly Control _glass = new() { MouseFilter = MouseFilterEnum.Ignore, ClipContents = true };
    /// <summary>What the screen shows, faded in as it comes on.</summary>
    private readonly List<CanvasItem> _shown = [];
    /// <summary>The screen's size the art was last laid round.</summary>
    private Vector2 _fitted;

    public Tablet()
    {
        MouseFilter = MouseFilterEnum.Stop;
        Visible = false;
        AddThemeStyleboxOverride("panel", Style.Box(Style.Slate900, Style.Slate900, radius: 0));
        // Behind the screen: its light on what is round it, then the tablet; over the bezel, the lamp's glow.
        _wash.ShowBehindParent = true;
        AddChild(_wash);
        AddChild(_frame);
        foreach (var part in (Control[])[_plate, _stylus, _ledOff]) _frame.AddChild(part);
        AddChild(_ledGlow);
        _glass.Draw += DrawGlass;
        AddChild(_glass);
    }

    /// <summary>The bezel round the screen: it takes the clicks and the wheel as the screen does, its owner listening to both.</summary>
    public Control Bezel => _frame;

    /// <summary>Where the tablet lies on the screen, its frame and stylus: what the crew's markers keep out of.</summary>
    public Rect2 Covers => _frame.GetGlobalRect();

    /// <summary>How far up it is (<see cref="TabletMotion.Up"/>): 0 below the screen's edge, 1 at rest.</summary>
    public double Up => _motion.Up;

    /// <summary>Whether any of it is on the screen: out, or still sliding away.</summary>
    public bool Shown => _motion.Shown;

    /// <summary>Puts <paramref name="part"/> on the screen, under the glass, faded in with it as it comes on.</summary>
    public void OnScreen(Control part)
    {
        AddChild(part);
        _shown.Add(part);
        MoveChild(_glass, -1);
    }

    /// <summary>
    /// One frame of <paramref name="seconds"/>, the tablet out or not: slid and its screen lit (<see cref="TabletMotion"/>).
    /// Put away, it takes no more input at once, though it shows while it slides down.
    /// </summary>
    public void Step(bool open, double seconds)
    {
        _motion.Step(open, seconds);
        if (Size != _fitted) Fit();
        Visible = _motion.Shown;
        var mouse = open ? MouseBehaviorRecursiveEnum.Inherited : MouseBehaviorRecursiveEnum.Disabled;
        if (MouseBehaviorRecursive != mouse) MouseBehaviorRecursive = mouse;
        float power = (float)_motion.Power;
        foreach (var part in _shown) part.Modulate = Colors.White with { A = power };
        _ledOff.Modulate = Colors.White with { A = 1 - power };
        _ledGlow.Modulate = Colors.White with { A = power };
        _wash.Modulate = Colors.White with { A = power * (float)_motion.Up };
    }

    /// <summary>How far below its rest it is drawn now, its screen's top at <paramref name="restTop"/>: past the bottom edge once put away.</summary>
    public int Drop(float restTop) =>
        (int)Mathf.Round((1 - (float)_motion.Up) * (GetViewportRect().Size.Y - restTop + TabletLayout.Lift));

    /// <summary>The tablet round the screen as large as it is now, in its art's pixels.</summary>
    private void Fit()
    {
        _fitted = Size;
        const int s = TabletLayout.ArtScale;
        var art = new Vector2(Size.X / s + TabletLayout.ArtAround, Size.Y / s + TabletLayout.ArtAround);
        _frame.Position = new Vector2(-TabletLayout.ScreenLeft * s, -TabletLayout.ScreenTop * s);
        _frame.Size = art;
        _plate.Position = new Vector2(2 + ((int)art.X - 12 - 33) / 2, 9);
        _stylus.Position = new Vector2(art.X - 13, 0);
        _ledOff.Position = new Vector2(art.X - 43, 11);
        _ledGlow.Size = new Vector2(18, 18);
        _ledGlow.Position = new Vector2(Size.X - 59, -17);
        _wash.Size = Size + WashReach;
        _wash.Position = (Size - _wash.Size) / 2;
        _glass.Size = Size;
        _glass.QueueRedraw();
    }

    /// <summary>The recess's shadow on the screen's top and left edges, and the glass's glare: faint, over whatever it shows.</summary>
    private void DrawGlass()
    {
        float w = _glass.Size.X, h = _glass.Size.Y;
        for (int y = 0; y < 6; y++) _glass.DrawRect(new Rect2(0, y, w, 1), Colors.Black with { A = 0.55f * (1 - (y + 0.5f) / 6) });
        for (int x = 0; x < 4; x++) _glass.DrawRect(new Rect2(x, 6, 1, h - 6), Colors.Black with { A = 0.4f * (1 - (x + 0.5f) / 4) });
        var glare = new Color(200 / 255f, 225 / 255f, 1);
        _glass.DrawColoredPolygon([new(70, 0), new(190, 0), new(60, h), new(-60, h)], glare with { A = 0.035f });
        _glass.DrawColoredPolygon([new(206, 0), new(222, 0), new(92, h), new(76, h)], glare with { A = 0.03f });
    }

    private static TextureRect Art(string path) => new()
    {
        Texture = GD.Load<Texture2D>(path),
        TextureFilter = TextureFilterEnum.Nearest,
        MouseFilter = MouseFilterEnum.Ignore,
    };

    /// <summary>A line of the tablet's text: monospaced, <paramref name="size"/> px, centred down its line.</summary>
    public static Label Text(string text, Color colour, int size = FontSize) => new()
    {
        Text = text,
        LabelSettings = new LabelSettings { Font = Style.Mono, FontSize = size, FontColor = colour },
        MouseFilter = MouseFilterEnum.Ignore,
        VerticalAlignment = VerticalAlignment.Center,
    };

    /// <summary>A tab of the strip at <paramref name="x"/>: its name and, when it has one, its count; painted by <see cref="PaintTab"/>.</summary>
    public static PanelContainer Tab(string name, string? count, float x)
    {
        var row = Style.Gap(new HBoxContainer { MouseFilter = MouseFilterEnum.Ignore }, 6);
        row.AddChild(Text(name, Style.Slate300));
        float width = 6 + Style.Mono.GetStringSize(name, fontSize: FontSize).X + 6;
        if (count != null)
        {
            row.AddChild(Text(count, Style.Slate500));
            width += Style.Mono.GetStringSize(count, fontSize: FontSize).X + 6;
        }
        var box = new PanelContainer { MouseFilter = MouseFilterEnum.Pass, Position = new Vector2(x, 0) };
        box.AddChild(row);
        box.CustomMinimumSize = new Vector2(Mathf.Ceil(width), TabletLayout.TabHeight);
        return box;
    }

    /// <summary>A tab lit sky when <paramref name="on"/>, a shade lighter under the pointer, its name white and its count pale sky when lit.</summary>
    public static void PaintTab(PanelContainer tab, bool on, bool hovered)
    {
        var style = Style.Box(on ? Style.Sky700 : hovered ? Style.Slate700 : Style.Slate800, on ? Style.Sky700 : Style.Slate800);
        (style.ContentMarginLeft, style.ContentMarginRight) = (6, 6);
        tab.AddThemeStyleboxOverride("panel", style);
        var labels = tab.GetChild(0).GetChildren();
        ((Label)labels[0]).LabelSettings.FontColor = on ? Colors.White : Style.Slate300;
        if (labels.Count > 1) ((Label)labels[1]).LabelSettings.FontColor = on ? Style.Sky200 : Style.Slate500;
    }

    /// <summary>A scrollbar's track and its thumb in it, the thumb shown only while there is more to scroll to.</summary>
    public static (Panel Track, Panel Thumb) ScrollBar()
    {
        var track = new Panel { MouseFilter = MouseFilterEnum.Ignore };
        var thumb = new Panel { MouseFilter = MouseFilterEnum.Ignore };
        track.AddThemeStyleboxOverride("panel", Style.Box(Style.Slate800, Style.Slate800, radius: 3));
        thumb.AddThemeStyleboxOverride("panel", Style.Box(Style.Slate600, Style.Slate600, radius: 3));
        track.AddChild(thumb);
        return (track, thumb);
    }

    /// <summary>Lays <paramref name="thumb"/> at <paramref name="at"/> down its track (<see cref="TabletLayout.Thumb"/>), or hides it with none.</summary>
    public static void PlaceThumb(Panel thumb, (float Top, float Length)? at)
    {
        thumb.Visible = at != null;
        if (at is not { } bar) return;
        thumb.Position = new Vector2(0, Mathf.Round(bar.Top));
        thumb.Size = new Vector2(TabletLayout.BarWidth, Mathf.Round(bar.Length));
    }
}
