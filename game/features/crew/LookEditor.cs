using System;
using Godot;
using Stillwake.Game.Render;
using Stillwake.Sim.Players;
using static Stillwake.Game.I18n.En;

namespace Stillwake.Game.Features.Crew;

/// <summary>
/// The look editor (2026-10-28-looks-are-printed): the hero bare in this player's colour, turning in a window, and a
/// picker each for skin, hair, hair colour, face and beard. Opened from the game menu at any time, and on a first run
/// before the hero is printed; closed, it hands the look on to be kept and sent. The pod prints it the next time it
/// makes the hero, which a line says.
/// </summary>
public partial class LookEditor : CanvasLayer
{
    /// <summary>The window's size in pixels, the zoom the figure is drawn at, and how long it faces each way.</summary>
    private const int ViewW = 120, ViewH = 220, Zoom = 4;
    private const double FaceS = 2.5;

    /// <summary>Each picker: its text and how many it picks among (a beard's 0 is none).</summary>
    private static readonly (string Key, int Count)[] Pickers =
    [
        ("look.skin", HeroLooks.Skins.Length),
        ("look.hair", HeroLooks.HairStyles),
        ("look.hairColour", HeroLooks.Hairs.Length),
        ("look.face", HeroLooks.Faces),
        ("look.beard", HeroLooks.Beards + 1),
    ];

    private readonly Func<int> _colour;
    private readonly Action<Look> _chosen;
    private readonly int[] _picks = new int[Pickers.Length];
    private readonly Label[] _values = new Label[Pickers.Length];
    private readonly Figure _figure;
    private double _shownS;

    /// <param name="colour">This player's suit colour, 0xRRGGBB, the jumpsuit's.</param>
    /// <param name="chosen">Keeps and sends the look, once the editor closes.</param>
    public LookEditor(Func<int> colour, Action<Look> chosen)
    {
        (_colour, _chosen) = (colour, chosen);
        Layer = ScreenLayers.Menu + 1;
        Visible = false;

        var backdrop = new ColorRect { Color = new Color(0, 0, 0, 0.5f), MouseFilter = Control.MouseFilterEnum.Stop };
        backdrop.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        AddChild(backdrop);
        var centre = new CenterContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
        centre.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        AddChild(centre);
        var panel = new PanelContainer();
        panel.AddThemeStyleboxOverride("panel", Style.Box(Style.Slate900 with { A = 0.95f }, Style.Slate700, 16));
        centre.AddChild(panel);
        var column = Style.Gap(new VBoxContainer(), 12);
        panel.AddChild(column);
        column.AddChild(Line(T("look.title"), Style.Slate300, 14));

        var row = Style.Gap(new HBoxContainer(), 16);
        column.AddChild(row);
        var window = new SubViewportContainer { CustomMinimumSize = new Vector2(ViewW, ViewH), Stretch = true };
        window.AddThemeStyleboxOverride("panel", Style.Box(Style.Slate800, Style.Slate700));
        var view = new SubViewport { Size = new Vector2I(ViewW, ViewH), TransparentBg = true, CanvasItemDefaultTextureFilter = Viewport.DefaultCanvasItemTextureFilter.Nearest };
        window.AddChild(view);
        row.AddChild(Style.Panel(Style.Slate700, window));
        _figure = new Figure(colour(), own: false);
        Unlit(_figure);
        view.AddChild(_figure);
        view.AddChild(new Camera2D { Zoom = new Vector2(Zoom, Zoom), Position = new Vector2(0, (float)(-Rig.H / 2)), Enabled = true });

        var pickers = Style.Gap(new GridContainer { Columns = 4 }, 8);
        pickers.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
        row.AddChild(pickers);
        for (int i = 0; i < Pickers.Length; i++)
        {
            int at = i;
            pickers.AddChild(new Label { Text = T(Pickers[i].Key), LabelSettings = Style.Text(Style.Mono, 12, Style.Slate300), VerticalAlignment = VerticalAlignment.Center });
            pickers.AddChild(Style.Button("<", () => Pick(at, -1), size: 14, padX: 10));
            pickers.AddChild(_values[i] = new Label { LabelSettings = Style.Text(Style.Mono, 12, Style.Slate300), HorizontalAlignment = HorizontalAlignment.Center, CustomMinimumSize = new Vector2(64, 0), VerticalAlignment = VerticalAlignment.Center });
            pickers.AddChild(Style.Button(">", () => Pick(at, 1), size: 14, padX: 10));
        }

        var next = Line(T("look.next"), Style.Slate400, 11);
        next.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        next.CustomMinimumSize = new Vector2(360, 0);
        column.AddChild(next);
        var done = Style.Button(T("look.done"), Close, size: 14, padX: 16, padY: 6);
        done.SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter;
        column.AddChild(done);
    }

    /// <summary>Whether it is open: the game menu's Esc closes it first.</summary>
    public bool Shown => Visible;

    /// <summary>Opens on <paramref name="look"/>.</summary>
    public void Open(Look look)
    {
        int[] picks = [look.Skin, look.Hair, look.HairColour, look.Face, look.Beard];
        for (int i = 0; i < Pickers.Length; i++) _picks[i] = HeroLooks.Wrap(picks[i], Pickers[i].Count);
        _shownS = 0;
        Visible = true;
        Paint();
    }

    /// <summary>Closes, handing the look on.</summary>
    public void Close()
    {
        if (!Visible) return;
        Visible = false;
        _chosen(Chosen);
    }

    private Look Chosen => new(_picks[0], _picks[1], _picks[2], _picks[3], _picks[4]);

    /// <summary>Moves <paramref name="picker"/> (skin, hair, hair colour, face, beard) on by <paramref name="by"/>, as its arrows do; also for scripted runs.</summary>
    public void Pick(int picker, int by)
    {
        _picks[picker] = HeroLooks.Wrap(_picks[picker] + by, Pickers[picker].Count);
        Paint();
    }

    private void Paint()
    {
        for (int i = 0; i < Pickers.Length; i++)
            _values[i].Text = i == 4 && _picks[i] == 0 ? T("look.none") : $"{_picks[i] + (i == 4 ? 0 : 1)} / {Pickers[i].Count - (i == 4 ? 1 : 0)}";
        _figure.Wear(_colour(), helmet: false, body: false, Chosen);
    }

    public override void _Process(double delta)
    {
        if (!Visible) return;
        _shownS += delta;
        // Facing one way then the other, as a turn would show it; its head looking a little up and down.
        int facing = (int)(_shownS / FaceS) % 2 == 0 ? 1 : -1;
        _figure.Show(new Hero(0, 0, 0, 0, false, facing), Pose.Stand, 0.2 * Math.Sin(_shownS * 1.3), false, delta * 1000);
    }

    /// <summary>Drawn as painted: the world's light does not reach the window.</summary>
    private static void Unlit(Node node)
    {
        if (node is CanvasItem { Material: ShaderMaterial } item) item.Material = null;
        foreach (var child in node.GetChildren()) Unlit(child);
    }

    private static Label Line(string text, Color colour, int size) => new()
    {
        Text = text,
        LabelSettings = Style.Text(Style.Mono, size, colour),
        HorizontalAlignment = HorizontalAlignment.Center,
        MouseFilter = Control.MouseFilterEnum.Ignore,
    };
}
