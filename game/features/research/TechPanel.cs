using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Stillwake.Content;
using Stillwake.Game.Render;
using Stillwake.Game.Shared;
using Stillwake.Sim;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Research;
using static Stillwake.Game.I18n.En;

namespace Stillwake.Game.Features.Research;

/// <summary>
/// A tech station's window, opened by the crew's E at it through <see cref="Ui.OpenTech"/>: the ship's credits, a button a tier down
/// its side, and the chosen tier's deposit and branches, each a row of blocks bought left to right (2026-10-30-trade-and-tech). A
/// block is green once bought, lit while it can be bought, dim while not, its tooltip saying what it opens and why not. E and Esc put
/// it away; the world runs on behind it.
/// </summary>
public partial class TechPanel : CanvasLayer
{
    private static readonly Color Steel = new("7f848f"), Line = new("1a1e25"), Glass = new("10161d"), Socket = new("474c57"),
        Dim = new("2a2e36"), Lit = new("6fe08a"), LitBack = new("0b1f16"), Words = new("7fc4f0"), Faint = new("5b6170"), Warm = new("f0c050");

    /// <summary>Seconds between reads of the world: a panel reads on a timer, never every frame.</summary>
    private const double ReadEvery = 0.25;
    private const int Rail = 52, Pad = 10, Icon = 34;

    private readonly Ui _ui;
    private readonly Func<Registry?> _world;
    private readonly Func<int> _peer;
    private readonly Action<Command> _send;
    private readonly Panel _frame = Boxed(Steel, 3, 4);
    private readonly VBoxContainer _rail = new() { MouseFilter = Control.MouseFilterEnum.Ignore };
    private readonly Panel _screen = Boxed(Glass, 2, 2);
    private readonly VBoxContainer _body = new();
    private readonly ScrollContainer _scroll = new() { HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled };
    private readonly TipSpots _tips;
    private string? _tier, _shown;
    private Label? _credits;
    private double _sinceRead;

    /// <param name="ui">Which station is open, set by the crew's E, and cleared here when it is put away.</param>
    /// <param name="world">The world the station stands in; null with none played.</param>
    /// <param name="peer">This player's peer: whose bag a deposit is taken from.</param>
    /// <param name="send">Sends a command to the session.</param>
    public TechPanel(Ui ui, Func<Registry?> world, Func<int> peer, Action<Command> send)
    {
        (_ui, _world, _peer, _send) = (ui, world, peer, send);
        _tips = new TipSpots(ui.Tip);
        Layer = ScreenLayers.Handbook;
        var root = new Control { MouseFilter = Control.MouseFilterEnum.Ignore, Theme = new Theme { DefaultFont = Style.Mono, DefaultFontSize = 16 } };
        root.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        AddChild(root);
        root.AddChild(_frame);
        _rail.AddThemeConstantOverride("separation", 6);
        _frame.AddChild(_rail);
        _frame.AddChild(_screen);
        _screen.ClipContents = true;
        _scroll.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        (_scroll.OffsetLeft, _scroll.OffsetTop, _scroll.OffsetRight, _scroll.OffsetBottom) = (Pad, Pad, -Pad, -Pad);
        _screen.AddChild(_scroll);
        _body.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        _body.AddThemeConstantOverride("separation", 10);
        _scroll.AddChild(_body);
        _frame.Visible = false;
        ui.OpenTech.Subscribe(() =>
        {
            // Each time it opens, on the last tier open: where the next thing to buy is.
            _tier = null;
            _shown = null;
            _sinceRead = ReadEvery;
        });
    }

    /// <summary>Whether a station is open: what Esc puts away before it opens the menu.</summary>
    public bool Shown => _ui.OpenTech.Get() != null;

    /// <summary>Puts it away: E, or Esc through the menu's <c>Escape</c>.</summary>
    public void Close() => _ui.OpenTech.Set(null);

    public override void _Process(double delta)
    {
        bool open = Shown && _world() != null;
        _frame.Visible = open;
        if (!open)
        {
            _tips.Show(null);
            return;
        }
        LayOut();
        _tips.Frame(_ui.MenuOpen.Get() ? null : _frame.GetViewport().GetMousePosition());
        _sinceRead += delta;
        if (_sinceRead < ReadEvery) return;
        _sinceRead = 0;
        Read(_world()!);
    }

    private void LayOut()
    {
        var view = _frame.GetViewportRect().Size;
        float width = Mathf.Clamp(view.X * 0.62f, 560, 1200), height = Mathf.Clamp(view.Y * 0.72f, 380, 800);
        _frame.Size = new Vector2(width, height);
        _frame.Position = ((view - _frame.Size) / 2).Round();
        _rail.Position = new Vector2(Pad, Pad);
        _rail.Size = new Vector2(Rail, height - 2 * Pad);
        _screen.Position = new Vector2(Pad * 2 + Rail, Pad);
        _screen.Size = new Vector2(width - Pad * 3 - Rail, height - 2 * Pad);
    }

    /// <summary>Builds the window again when what it shows has changed: a deposit, a block, what the bag holds. Credits only retype their line.</summary>
    private void Read(Registry m)
    {
        var research = m.Research;
        // Where there is something to do: the first tier open with a block to buy, else the next shut, else the last.
        _tier ??= (Content.Research.Tiers.FirstOrDefault(t => research.Open(t) && t.Branches.Any(b => b.Blocks.Any(k => !research.Bought(k))))
            ?? Content.Research.Tiers.FirstOrDefault(t => !research.Open(t)) ?? Content.Research.Tiers[^1]).Id;
        if (!Content.Research.TryTier(_tier, out var tier)) return;
        var carried = m.Items.Totals(_peer());
        int Carried(string item) => carried.Where(t => t.Item == item).Sum(t => t.Count);
        string now = string.Join('|', [
            _tier,
            .. Content.Research.Tiers.Select(t => research.Open(t) ? "1" : "0"),
            .. tier.Deposit.Select(d => $"{research.Deposited(tier, d.Item)}/{Carried(d.Item)}"),
            .. tier.Branches.SelectMany(b => b.Blocks).Select(b => research.BuyProblem(b) ?? ""),
        ]);
        // Sales raise the credits twice a second: a rebuild for each would eat a click on the Deposit key between press and release.
        if (now == _shown)
        {
            _credits!.Text = T("research.credits", ("n", research.Credits));
            return;
        }
        _shown = now;
        _tips.Clear();
        Empty(_rail);
        Empty(_body);
        foreach (var each in Content.Research.Tiers) _rail.AddChild(TierButton(each, research));
        _body.AddChild(_credits = Words2(T("research.credits", ("n", research.Credits)), Warm));
        _body.AddChild(Words2(T($"research.tier.{tier.Id}"), Words));
        if (!research.Open(tier)) _body.AddChild(Deposit(tier, research, Carried));
        var recipes = research.Recipes();
        foreach (var branch in tier.Branches) _body.AddChild(Branch(branch, research, recipes));
    }

    private static void Empty(Control box)
    {
        foreach (var old in box.GetChildren())
        {
            box.RemoveChild(old);
            old.QueueFree();
        }
    }

    private Control TierButton(TechTier tier, IResearchQueries research)
    {
        bool open = research.Open(tier), chosen = tier.Id == _tier;
        var box = Boxed(chosen ? LitBack : open ? Socket : Dim, 2, 2);
        Restyle(box, chosen ? LitBack : open ? Socket : Dim, chosen ? Lit : Line);
        box.CustomMinimumSize = new Vector2(Rail, Rail);
        var number = Words2((Content.Research.Tiers.ToList().IndexOf(tier) + 1).ToString(), open ? Colors.White : Faint);
        number.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        number.HorizontalAlignment = HorizontalAlignment.Center;
        number.VerticalAlignment = VerticalAlignment.Center;
        box.AddChild(number);
        _tips.Add(box, () => new Tip(T($"research.tier.{tier.Id}")));
        box.GuiInput += e =>
        {
            if (e is not InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left }) return;
            _tier = tier.Id;
            _shown = null;
            _sinceRead = ReadEvery;
            box.AcceptEvent();
        };
        return box;
    }

    /// <summary>What a tier still shut asks for, each with its picture, what is in and what is asked, and the key putting in what the bag holds.</summary>
    private Control Deposit(TechTier tier, IResearchQueries research, Func<string, int> carried)
    {
        var row = new HBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
        row.AddThemeConstantOverride("separation", 12);
        foreach (var (item, count) in tier.Deposit)
        {
            var one = new HBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
            one.AddThemeConstantOverride("separation", 4);
            one.AddChild(Picture(item, null));
            int had = research.Deposited(tier, item);
            one.AddChild(Words2($"{had}/{count}", had >= count ? Lit : carried(item) > 0 ? Warm : Faint));
            row.AddChild(one);
            _tips.Add(one, () => new Tip(T($"item.{item}"), new TipLine(T("research.inTheBag", ("n", carried(item))), Tone.Good)));
        }
        var key = new Button { Text = T("research.deposit"), FocusMode = Control.FocusModeEnum.None };
        key.Pressed += () => { if (_ui.OpenTech.Get() is int station) _send(new Deposit(station, tier.Id)); };
        row.AddChild(key);
        return row;
    }

    /// <summary>A branch's name and its blocks in a row, an arrow between them.</summary>
    private Control Branch(TechBranch branch, IResearchQueries research, IReadOnlyList<Recipe> recipes)
    {
        var column = new VBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
        column.AddChild(Words2(T($"research.branch.{branch.Id}"), Faint));
        var row = new HFlowContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
        row.AddThemeConstantOverride("h_separation", 6);
        row.AddThemeConstantOverride("v_separation", 6);
        for (int i = 0; i < branch.Blocks.Length; i++)
        {
            if (i > 0)
            {
                var arrow = Words2("›", Faint);
                arrow.VerticalAlignment = VerticalAlignment.Center;
                row.AddChild(arrow);
            }
            row.AddChild(Block(branch.Blocks[i], research, recipes));
        }
        column.AddChild(row);
        return column;
    }

    private Control Block(TechBlock block, IResearchQueries research, IReadOnlyList<Recipe> recipes)
    {
        string? why = research.BuyProblem(block);
        bool bought = why == "research.bought", can = why == null;
        var box = Boxed(bought ? LitBack : can ? Socket : Dim, 2, 2);
        Restyle(box, bought ? LitBack : can ? Socket : Dim, bought ? Lit : can ? Warm : Line);
        var column = new VBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
        column.Position = new Vector2(4, 4);
        var icons = new HBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
        icons.AddThemeConstantOverride("separation", 2);
        var names = new List<string>();
        foreach (string id in block.Opens)
        {
            var recipe = recipes.First(r => r.Id == id);
            icons.AddChild(Picture(id, recipe.Placement, dim: !bought && !can));
            names.Add(ItemCards.NameOf(id, recipe.Placement));
        }
        column.AddChild(icons);
        column.AddChild(Words2(bought ? "✓" : block.Price.ToString(), bought ? Lit : can ? Warm : Faint));
        box.AddChild(column);
        box.CustomMinimumSize = new Vector2(block.Opens.Length * (Icon + 2) + 8, Icon + 34);
        _tips.Add(box, () => TipOf(names, why));
        box.GuiInput += e =>
        {
            if (e is not InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left }) return;
            if (research.BuyProblem(block) == null && _ui.OpenTech.Get() is int station) _send(new BuyBlock(station, block.Id));
            box.AcceptEvent();
        };
        return box;
    }

    private static Tip TipOf(IReadOnlyList<string> names, string? why)
    {
        var lines = names.Skip(1).Select(n => new TipLine(n, Tone.Good)).ToList();
        if (why != null && why != "research.bought") lines.Add(new TipLine(T(why), Tone.Unknown));
        return new Tip(names[0], lines);
    }

    /// <summary>A blueprint as the build menu pictures it, a craft or a material as its item.</summary>
    private static TextureRect Picture(string id, Placement? placement, bool dim = false) => new()
    {
        Texture = placement is { } p ? BlueprintArt.Thumbnail(p) : ItemIcons.Icon(id, Icon),
        ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
        StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
        TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
        CustomMinimumSize = new Vector2(Icon, Icon),
        MouseFilter = Control.MouseFilterEnum.Ignore,
        Modulate = dim ? new Color(0.45f, 0.45f, 0.5f, 0.7f) : Colors.White,
    };

    private static Label Words2(string text, Color colour)
    {
        var label = new Label { Text = text, MouseFilter = Control.MouseFilterEnum.Ignore };
        label.AddThemeColorOverride("font_color", colour);
        return label;
    }

    public override void _Input(InputEvent e)
    {
        // E puts it away and is spent, so the crew's E does not open it again at once.
        if (e is not InputEventKey { Pressed: true, Echo: false, PhysicalKeycode: Key.E } || !Shown) return;
        Close();
        GetViewport().SetInputAsHandled();
    }

    private static Panel Boxed(Color fill, int border, int corner)
    {
        var box = new Panel { MouseFilter = Control.MouseFilterEnum.Stop };
        Restyle(box, fill, Line, border, corner);
        return box;
    }

    private static void Restyle(Panel box, Color fill, Color edge, int border = 2, int corner = 2)
    {
        var look = new StyleBoxFlat { BgColor = fill, BorderColor = edge };
        look.SetBorderWidthAll(border);
        look.SetCornerRadiusAll(corner);
        box.AddThemeStyleboxOverride("panel", look);
    }
}
