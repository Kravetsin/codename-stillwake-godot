using System;
using System.Collections.Generic;
using Godot;
using Stillwake.Game.Platform;
using Stillwake.Game.Render;
using static Stillwake.Game.I18n.En;

namespace Stillwake.Game.Menu;

/// <summary>
/// What the game opens on (0031, the approved mockup), loading nothing until the player picks: the name
/// and a column of buttons on the left, centred — Continue the last world played (New game on a first
/// run), Worlds, Play together, Quit — over the stars streaming by and a few pieces of wreckage tumbling
/// past. Worlds and Play together open a panel opposite the button, closed by pressing it again. The
/// version at the foot, and the update once one is out.
/// </summary>
public partial class TitleScreen : CanvasLayer
{
    /// <summary>The panels' left edge, and how close to the screen's top and bottom they may come.</summary>
    private const float PanelLeft = 340, Edge = 16;

    private readonly Func<WorldEntry?> _continue;
    private readonly Func<string?> _updateTo, _updating;
    private readonly Button _continueButton, _update;
    private readonly Label _updateLine, _problem;
    private readonly PanelContainer _panel;
    private readonly Dictionary<string, (Button Button, Control Page)> _pages = [];
    private string? _open;

    /// <summary>The Worlds panel: what it has picked is what Play together hosts.</summary>
    public WorldsPanel Worlds { get; }

    /// <param name="continueWorld">The world Continue plays, or none on a first run.</param>
    /// <param name="play">Continue: plays that world, or a new one.</param>
    public TitleScreen(Func<WorldEntry?> continueWorld, Action play, WorldsPanel worlds, ITogether together, Action quit,
        string version, Func<string?> updateTo, Action update, Func<string?> updating)
    {
        (_continue, _updateTo, _updating) = (continueWorld, updateTo, updating);
        Worlds = worlds;
        Layer = ScreenLayers.Menu;
        var root = new Control { MouseFilter = Control.MouseFilterEnum.Ignore };
        root.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        AddChild(root);
        root.AddChild(new Wreckage());

        var column = Style.Gap(new VBoxContainer { CustomMinimumSize = new Vector2(260, 0) }, 8);
        (column.AnchorTop, column.AnchorBottom) = (0.5f, 0.5f);
        column.GrowVertical = Control.GrowDirection.Both;
        column.OffsetLeft = 56;
        root.AddChild(column);
        column.AddChild(new Label
        {
            Text = "STILLWAKE",
            LabelSettings = Style.Text(Style.Mono, 40, Style.Sky200, shadow: true),
        });
        column.AddChild(_continueButton = Style.Button("", play, size: 15, padX: 16, padY: 10));
        _continueButton.Alignment = HorizontalAlignment.Left;
        foreach (var (key, page) in new (string, Control)[] { ("title.worlds", worlds), ("title.together", new TogetherPage(together, () => Open(null))) })
        {
            var button = Style.Button(T(key), () => Open(_open == key ? null : key), size: 14, padX: 16, padY: 7);
            button.Alignment = HorizontalAlignment.Left;
            column.AddChild(button);
            _pages[key] = (button, page);
        }
        var quitButton = Style.Button(T("title.quit"), quit, size: 14, padX: 16, padY: 7);
        quitButton.Alignment = HorizontalAlignment.Left;
        column.AddChild(quitButton);
        column.AddChild(_problem = new Label { LabelSettings = Style.Text(Style.Mono, 12, Style.Red300), AutowrapMode = TextServer.AutowrapMode.WordSmart });
        var foot = Style.Gap(new HBoxContainer(), 10);
        foot.AddChild(new Label { Text = version, LabelSettings = Style.Text(Style.Mono, 11, Style.Slate400) });
        foot.AddChild(_update = Style.Button("", update, size: 11, padX: 8, padY: 3));
        foot.AddChild(_updateLine = new Label { LabelSettings = Style.Text(Style.Mono, 11, Style.Sky200) });
        column.AddChild(foot);

        _panel = new PanelContainer { Visible = false };
        _panel.AddThemeStyleboxOverride("panel", Style.Box(Style.Slate900 with { A = 0.94f }, Style.Slate700, 14));
        root.AddChild(_panel);
        foreach (var (_, page) in _pages.Values)
        {
            page.Visible = false;
            _panel.AddChild(page);
        }
    }

    /// <summary>Says what went wrong under the buttons: a world that would not load, a host that left.</summary>
    public void Say(string text) => _problem.Text = text;

    /// <summary>Opens the panel of <paramref name="key"/>'s button, or closes the one open.</summary>
    public void Open(string? key)
    {
        _open = key;
        _panel.Visible = key != null;
        foreach (var (k, (button, page)) in _pages)
        {
            page.Visible = k == key;
            button.Modulate = k == key ? new Color(0.75f, 0.9f, 1.15f) : Colors.White;
        }
        if (key == "title.worlds") Worlds.Refresh();
        if (key == "title.together" && _pages[key].Page is TogetherPage together) together.Opened();
    }

    public override void _Process(double delta)
    {
        var world = _continue();
        _continueButton.Text = world != null
            ? $"{T("title.continue")}\n{world.Name} · {WorldsPanel.Played(world.PlaySeconds)}"
            : T("title.newGame");
        string? tag = _updateTo(), how = _updating();
        _update.Visible = tag != null && how == null;
        if (tag != null) _update.Text = T("menu.update", ("version", tag));
        _updateLine.Text = how ?? "";
        if (_open is not { } key) return;
        // Opposite the button pressed, kept on screen.
        var button = _pages[key].Button.GetGlobalRect();
        float height = _panel.GetCombinedMinimumSize().Y, screen = _panel.GetViewportRect().Size.Y;
        float top = Mathf.Clamp(button.GetCenter().Y - height / 2, Edge, Math.Max(Edge, screen - height - Edge));
        _panel.Position = new Vector2(PanelLeft, top);
        _panel.Size = _panel.GetCombinedMinimumSize();
    }

    /// <summary>
    /// Space, drawn whole so nothing of the game behind shows: stars streaming slowly by, the nearer
    /// brighter and quicker, and a few pieces of wreckage tumbling past.
    /// </summary>
    private sealed partial class Wreckage : Control
    {
        private const int Pieces = 6, StarCount = 260;
        private static readonly Color Space = new("04060a"), Near = new("cfe0ff"), Mid = new("7d8fb0"), Far = new("3a4560");
        private readonly (Vector2 At, float Depth)[] _stars = new (Vector2, float)[StarCount];
        private readonly (Vector2 At, float Turn, Vector2 Size, float Speed, float Spin)[] _pieces = new (Vector2, float, Vector2, float, float)[Pieces];
        private static readonly Color Plate = new("3d4659"), Lit = new("566078"), Dark = new("252b37");

        public Wreckage()
        {
            MouseFilter = MouseFilterEnum.Ignore;
            SetAnchorsPreset(LayoutPreset.FullRect);
            var rng = new Random(11);
            for (int i = 0; i < StarCount; i++) _stars[i] = (new Vector2(rng.Next(0, 2560), rng.Next(0, 1440)), 0.2f + 0.8f * rng.NextSingle());
            for (int i = 0; i < Pieces; i++)
                _pieces[i] = (new Vector2(rng.Next(0, 1600), rng.Next(60, 900)), rng.NextSingle() * 6,
                    new Vector2(8 + rng.Next(0, 26), 6 + rng.Next(0, 14)), 6 + rng.NextSingle() * 12, (rng.NextSingle() - 0.5f) * 0.3f);
        }

        public override void _Process(double delta)
        {
            float width = GetViewportRect().Size.X;
            for (int i = 0; i < StarCount; i++)
            {
                var (at, depth) = _stars[i];
                at.X -= 6 * depth * (float)delta;
                if (at.X < 0) at.X += 2560;
                _stars[i] = (at, depth);
            }
            for (int i = 0; i < Pieces; i++)
            {
                var p = _pieces[i];
                p.At.X -= p.Speed * (float)delta;
                p.Turn += p.Spin * (float)delta;
                if (p.At.X < -60) p.At.X = width + 60;
                _pieces[i] = p;
            }
            QueueRedraw();
        }

        public override void _Draw()
        {
            var screen = GetViewportRect().Size;
            DrawRect(new Rect2(Vector2.Zero, screen), Space);
            foreach (var (at, depth) in _stars)
                if (at.X < screen.X && at.Y < screen.Y)
                    Star(at, depth > 0.85f ? 2 : 1, depth > 0.8f ? Near : depth > 0.5f ? Mid : Far);
            foreach (var (at, turn, size, _, _) in _pieces)
            {
                DrawSetTransform(at, turn, Vector2.One * 2);
                var box = new Rect2(-size / 2, size);
                DrawRect(box, Plate);
                DrawRect(new Rect2(box.Position, new Vector2(box.Size.X, 1)), Lit);
                DrawRect(new Rect2(box.Position + new Vector2(0, box.Size.Y - 1), new Vector2(box.Size.X, 1)), Dark);
                DrawRect(new Rect2(box.Position + new Vector2(2, 2), Vector2.One), Dark);
            }
            DrawSetTransform(Vector2.Zero, 0, Vector2.One);
        }

        /// <summary>
        /// A star a fraction of a pixel along, shared between the pixels it lies across by how much of it is
        /// in each: drawn whole on one pixel, a slow star would stand still and then jump.
        /// </summary>
        private void Star(Vector2 at, int size, Color colour)
        {
            var whole = at.Floor();
            float fx = at.X - whole.X, fy = at.Y - whole.Y;
            for (int dy = 0; dy < 2; dy++)
                for (int dx = 0; dx < 2; dx++)
                {
                    float share = (dx == 0 ? 1 - fx : fx) * (dy == 0 ? 1 - fy : fy);
                    if (share > 0.01f) DrawRect(new Rect2(whole + new Vector2(dx, dy), Vector2.One * size), colour with { A = share });
                }
        }
    }
}
