using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Stillwake.Content;
using Stillwake.Game.Render;
using Stillwake.Game.Shared;
using Stillwake.Sim;
using Stillwake.Sim.Research;
using static Stillwake.Game.I18n.En;

namespace Stillwake.Game.Features.Research;

/// <summary>
/// The black market as a darknet storefront ([market-grid](../../../docs/decisions/2026-11-03-market-grid.md)): what the mafia wants for its next level down the left,
/// tabs over a grid of every good, a picture and a price each, the rest in the strip. A price past the level is garbage, and deciphers as the level opens
/// (<see cref="MarketReveal"/>, [mafia-reputation](../../../docs/decisions/2026-11-08-mafia-reputation.md)). A click picks; only the strip's
/// button buys. Built once and restyled, for a rebuild under the pointer would eat a click; each part draws itself, and repaints only when what it says has changed.
/// </summary>
public sealed class MarketPage : ITabletPage
{
    /// <summary>The storefront's colours that the tablet paints with: behind it, its strip and edges, its accent and the words on it, a button that cannot be pressed.</summary>
    public static readonly Color Back = new("0f1113"), StripFill = new("121417"), Edge = new("23282d"), Accent = new("d9512f"), OnAccent = new("fff4ee"),
        Off = new("16191c"), OffEdge = new("2d3238"), Dim = new("7d858d"), Dear = new("7a7466");

    private static readonly Color Panel = new("15181b"), TileFill = new("1a1e22"), TileEdge = new("262b31"), Words = new("d8dce0"), Faint = new("3a4047"),
        AccentDark = new("4a1d12"), Own = new("6fbf73"), Picked = new("f2f4f6"), Gold = new("f0c050"), Plan = new("4f8fbf"), PlanWord = new("6fa3c8"),
        AskFill = new("111417"), Pill = new("1b1f23"), Badge = new("2a3036"), Empty = new("16191c"),
        Opening = new("121518"), Chip = new("22272c"), ChipEdge = new("30363c"), ChipHad = new("16191c"), ChipHadEdge = new("1f2328"),
        Had = new(0.5f, 0.52f, 0.55f, 0.75f);

    private static readonly Font Plain = Style.Clear, Strong = Style.Bold, Garbled = Style.Mono, Spaced = new FontVariation { BaseFont = Style.Bold, SpacingGlyph = 1 };
    private static readonly StyleBoxFlat Look = new();
    private static readonly Texture2D Coin = Signs.Credit();

    /// <summary>The side's inset; a wanted good's row and how far apart they are; where a tile's picture's middle lies and how tall it is.</summary>
    private const int SideIn = 12, AskTall = 40, AskStride = 46, PictureAt = 34, PictureTall = 44;
    /// <summary>Where the strip's words start; how many characters' worth of a tile's decipher its picture takes.</summary>
    private const int WordsAt = 116, PictureChars = 6;

    private sealed class Tile(MarketGood good, Control box, TextureRect picture)
    {
        public MarketGood Good { get; } = good;
        public Control Box { get; } = box;
        public TextureRect Picture { get; } = picture;
        public bool Ladder => Good.Shelf == MarketShelf.Upgrades;
        public MarketState State { get; set; }
        public MarketFresh Fresh { get; set; }
        /// <summary>The row deciphering it, its place among the tiles deciphering, and how many those are.</summary>
        public (int Row, int Index, int Count) Opening { get; set; }
        public int Stamp { get; set; } = -1;
    }

    private static readonly (MarketShelf? Shelf, string Key)[] Shelves =
        [(null, "research.market.shelf.all"), (MarketShelf.Upgrades, "research.market.shelf.upgrades"), (MarketShelf.Blueprints, "research.market.shelf.blueprints")];

    private readonly Control _view;
    private readonly StripParts _strip;
    private readonly Action<Command> _send;
    private readonly Action _readSoon;
    private readonly Func<BoardSeen> _seen;
    private readonly TechNode[] _orders;
    private readonly IReadOnlyList<MarketGood> _goods;
    private readonly MarketReveal _reveal;
    private readonly Control _root = new() { MouseFilter = Control.MouseFilterEnum.Ignore, Visible = false };
    private readonly Control _side = new() { MouseFilter = Control.MouseFilterEnum.Ignore };
    private readonly Control _tabs = new() { MouseFilter = Control.MouseFilterEnum.Stop };
    private readonly Control _slots = new() { MouseFilter = Control.MouseFilterEnum.Ignore };
    private readonly List<Tile> _tiles = [];
    private readonly List<TextureRect> _asks = [];
    private readonly List<(MarketShelf? Shelf, Rect2 At)> _tabAt = [];
    private readonly List<(float At, string Words)> _madeFrom = [];
    private List<Tile> _inView = [], _shownAll = [];
    private IResearchQueries? _research;
    private Standing _standing = new(null, [], 0, 0);
    private TechNode? _order;
    private string? _askedFor;
    private MarketShelf? _shelf;
    private Tile? _picked;
    private string? _filledFor;
    private Control? _card;
    private TextureRect? _cardPicture;
    private string? _shown;
    private bool _out;
    private int _met, _sideStamp = -1;
    private Vector2 _laidIn;

    /// <param name="tab">The tab of <c>Content.Research</c> it draws.</param>
    /// <param name="view">The tablet's view, which it draws into.</param>
    /// <param name="strip">The strip below and its words.</param>
    /// <param name="send">Sends a command to the session.</param>
    /// <param name="readSoon">Has the tablet read on its next frame.</param>
    /// <param name="seen">What this machine has seen of the orders, for the world played.</param>
    public MarketPage(TechTab tab, Control view, StripParts strip, Action<Command> send, Action readSoon, Func<BoardSeen> seen)
    {
        (_view, _strip, _send, _readSoon, _seen) = (view, strip, send, readSoon, seen);
        _orders = Content.Research.Nodes.Where(n => n.Gate && n.Tab == tab.Id).ToArray();
        _goods = MarketGoods.Of(tab);
        _reveal = new MarketReveal(tab.Rings.Length);
        _side.Draw += DrawSide;
        _tabs.Draw += DrawTabs;
        _tabs.GuiInput += PickShelf;
        _slots.Draw += DrawSlots;
        foreach (var part in (Control[])[_side, _tabs, _slots]) _root.AddChild(part);
        view.AddChild(_root);
        strip.Action.Pressed += Act;
    }

    /// <summary>What lies behind the storefront, the strip's too, for the tablet to lay over its screen.</summary>
    public ColorRect Body { get; } = new() { Color = Back, MouseFilter = Control.MouseFilterEnum.Ignore, Visible = false };

    /// <summary>A tile's middle by its good's id, or a tab's by its shelf's name in lower case (<c>all</c>, <c>blueprints</c>).</summary>
    public Vector2 ScreenOf(string id)
    {
        if (_tiles.FirstOrDefault(t => t.Good.Id == id) is { } tile) return tile.Box.GetGlobalRect().GetCenter();
        var (_, at) = _tabAt.First(t => (t.Shelf?.ToString() ?? "All").Equals(id, StringComparison.OrdinalIgnoreCase));
        return _tabs.GlobalPosition + at.GetCenter();
    }

    public void Show(bool shown)
    {
        _out = _root.Visible = Body.Visible = shown;
        if (shown) _reveal.Came();
        else _reveal.Clear();
        _picked = null;
        _filledFor = null;
        _card = null;
        _shown = null;
    }

    public void Step(double delta)
    {
        if (_tiles.Count == 0) return;
        bool playing = _reveal.Playing;
        _reveal.Step(delta);
        Lay();
        if (playing || _reveal.Playing) Repaint();
    }

    public void Read(Registry m)
    {
        var research = m.Research;
        _research = research;
        if (_tiles.Count == 0) Build();
        _met = _orders.Count(research.Done);
        if (_out) _reveal.Play(_seen().SeeOrders(_met), _met);
        foreach (var tile in _tiles) tile.State = MarketGoods.StateOf(tile.Good, research.Done, research.Reached, research.BuyProblem);
        _standing = research.Standing();
        _order = _standing.Next;
        string now = string.Join('|', _tiles.Select(t => $"{t.State.Tile}{t.State.Next?.Id}{t.State.Bought}{t.State.Opened}")) + '|' + _order?.Id + '|' + _met + '|'
            + _standing.Points + '|' + string.Join(',', _standing.Wanted.Select(d => d.Sold));
        if (now == _shown) return;
        _shown = now;
        Restyle();
    }

    private void Build()
    {
        _strip.Learn(_research!);
        foreach (var good in _goods)
        {
            var box = new Control { MouseFilter = Control.MouseFilterEnum.Stop, Visible = false };
            var picture = PictureOf(good.Steps[0].Id, PictureTall);
            box.AddChild(picture);
            _slots.AddChild(box);
            var tile = new Tile(good, box, picture);
            box.Draw += () => DrawTile(tile);
            box.GuiInput += e =>
            {
                if (e is not InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left }) return;
                box.AcceptEvent();
                _reveal.ClearCard(good.Id);
                Pick(tile);
            };
            _strip.Tips.Add(box, () => Cleared(tile).Playing ? null : TipOf(good.Steps[0].Id));
            _tiles.Add(tile);
        }
        Lay();
    }

    private TextureRect PictureOf(string id, int size)
    {
        var picture = _strip.Picture(id, size);
        picture.CustomMinimumSize = Vector2.Zero;
        return picture;
    }

    /// <summary>The order, the tabs and the grid where they lie in the view now: only when its size has moved.</summary>
    private void Lay()
    {
        var size = _view.Size;
        if (size == _laidIn) return;
        _laidIn = size;
        var (wide, tall) = MarketGrid.GridIn(size.X, size.Y);
        float left = MarketGrid.SideWide + MarketGrid.SideGap;
        _side.Size = new Vector2(MarketGrid.SideWide, size.Y);
        _tabs.Position = new Vector2(left, 0);
        _tabs.Size = new Vector2(wide, MarketGrid.TabsTall);
        _slots.Position = new Vector2(left, MarketGrid.TabsTall);
        _slots.Size = new Vector2(wide, tall);
        LayTiles();
        _side.QueueRedraw();
        _tabs.QueueRedraw();
    }

    /// <summary>The tiles of the shelf shown in order, as many as the grid holds; the rest hidden.</summary>
    private void LayTiles()
    {
        var (columns, rows, wide, tall) = MarketGrid.Of(_slots.Size.X, _slots.Size.Y);
        foreach (var tile in _tiles) tile.Box.Visible = false;
        for (int i = 0; i < _inView.Count && i < columns * rows; i++)
        {
            var at = Slot(i, columns, wide, tall);
            var tile = _inView[i];
            tile.Box.Position = at.Position;
            tile.Box.Size = at.Size;
            tile.Box.Visible = true;
            tile.Picture.Position = new Vector2(10, PictureAt - PictureTall / 2);
            tile.Picture.Size = new Vector2(at.Size.X - 20, PictureTall);
        }
        _slots.QueueRedraw();
    }

    private static Rect2 Slot(int i, int columns, float wide, float tall)
    {
        float x = i % columns * (wide + MarketGrid.Gap), y = i / columns * (tall + MarketGrid.Gap);
        var from = new Vector2(Mathf.Round(x), Mathf.Round(y));
        return new Rect2(from, new Vector2(Mathf.Round(x + wide), Mathf.Round(y + tall)) - from);
    }

    /// <summary>What the sim says now, everywhere: the tiles shown and their order, what deciphers, the order's pictures, the strip and its button.</summary>
    private void Restyle()
    {
        if (_research is not { } research) return;
        _shownAll = _tiles;
        _inView = [.. MarketGoods.Sorted(_goods, research.Reached, _shelf).Select(g => _tiles.First(t => t.Good == g))];
        int fresh = 0;
        foreach (var tile in _inView)
        {
            tile.Fresh = MarketGoods.FreshOf(tile.Good, research.Reached, _reveal.Plays);
            if (tile.Fresh != MarketFresh.None) fresh++;
        }
        int index = 0;
        foreach (var tile in _inView.Where(t => t.Fresh != MarketFresh.None))
            tile.Opening = (tile.Good.Steps.Where(s => research.Reached(s) && _reveal.Plays(s.Ring)).Max(s => s.Ring), index++, fresh);
        LayTiles();
        foreach (var tile in _tiles)
        {
            tile.Stamp = -1;
            tile.Box.QueueRedraw();
            tile.Picture.Visible = Cleared(tile).Picture;
            tile.Picture.Modulate = OwnedPlan(tile) ? Had : Colors.White;
        }
        AskPictures();
        _sideStamp = -1;
        _side.QueueRedraw();
        _tabs.QueueRedraw();
        if (!_out) return;
        if (Current?.Good.Id != _filledFor) Fill();
        if (_cardPicture != null && Current is { } picked) _cardPicture.Modulate = OwnedPlan(picked) ? Had : Colors.White;
        _card?.QueueRedraw();
        ShowAction();
    }

    private static bool OwnedPlan(Tile tile) => !tile.Ladder && tile.State.Tile == MarketTile.Owned;

    /// <summary>The pictures of what the order asks for: made afresh for a new order, hidden while its head is garbage, dim once enough is sold.</summary>
    private void AskPictures()
    {
        if (_order?.Id != _askedFor)
        {
            _askedFor = _order?.Id;
            foreach (var old in _asks) old.QueueFree();
            _asks.Clear();
            foreach (var (item, _) in _order?.Order ?? [])
            {
                var picture = PictureOf(item, AskTall);
                picture.Size = new Vector2(32, 30);
                _side.AddChild(picture);
                _strip.Tips.Add(picture, () => TipOf(item));
                _asks.Add(picture);
            }
        }
        if (_order is not { } order) return;
        bool garbage = _reveal.HeadDeciphering(order.Ring);
        for (int i = 0; i < _asks.Count; i++)
        {
            var (item, want) = order.Order[i];
            _asks[i].Position = new Vector2(SideIn + 21 - 16, 74 + i * AskStride + 5);
            _asks[i].Visible = !garbage;
            _asks[i].Modulate = SoldOf(item) >= want ? Had : Colors.White;
        }
    }

    /// <summary>What a decipher has moved on: a tile or the order that says something new this frame, a picture once it shows.</summary>
    private void Repaint()
    {
        foreach (var tile in _tiles)
        {
            var cleared = Cleared(tile);
            int now = cleared.Playing ? cleared.Step * 1000 + cleared.Price * 30 + cleared.Status + (cleared.Picture ? 500 : 0) : -1;
            if (now == tile.Stamp) continue;
            tile.Stamp = now;
            tile.Box.QueueRedraw();
            tile.Picture.Visible = cleared.Picture;
        }
        int said = _order is { } order && _reveal.HeadDeciphering(order.Ring) ? _reveal.HeadStepOf(order.Ring) * 1000 + _reveal.HeadShown(order.Ring, 999) : -1;
        if (said == _sideStamp) return;
        _sideStamp = said;
        _side.QueueRedraw();
        AskPictures();
    }

    public void Pointer(InputEvent e) => Zoom(e, _view);

    public void LetGo() { }

    /// <summary>Nothing zooms here; the wheel is only kept from reaching the hotbar.</summary>
    public void Zoom(InputEvent e, Control over)
    {
        if (e is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.WheelUp or MouseButton.WheelDown }) over.AcceptEvent();
    }

    private void PickShelf(InputEvent e)
    {
        if (e is not InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left } press) return;
        _tabs.AcceptEvent();
        foreach (var (shelf, at) in _tabAt)
        {
            if (!at.HasPoint(press.Position) || shelf == _shelf) continue;
            _shelf = shelf;
            Restyle();
        }
    }

    private void Pick(Tile tile)
    {
        _picked = tile;
        _readSoon();
        Restyle();
    }

    /// <summary>The good the strip is about: the one picked, or the first on the shelf shown, or the first at all.</summary>
    private Tile? Current => _picked != null && _shownAll.Contains(_picked) ? _picked : _inView.FirstOrDefault() ?? _shownAll.FirstOrDefault();

    public void Fill()
    {
        _strip.Clear();
        _card = null;
        _cardPicture = null;
        _madeFrom.Clear();
        if (Current is not { } tile)
        {
            _filledFor = null;
            return;
        }
        _filledFor = tile.Good.Id;
        string id = tile.Good.Steps[0].Id;
        var card = new Control { MouseFilter = Control.MouseFilterEnum.Ignore, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, SizeFlagsVertical = Control.SizeFlags.ExpandFill };
        card.Draw += () => DrawCard(card, tile);
        var picture = PictureOf(id, 80);
        picture.Position = new Vector2(7, 10);
        picture.Size = new Vector2(86, 80);
        _strip.Tips.Add(picture, () => TipOf(id));
        card.AddChild(picture);
        if (!tile.Ladder)
        {
            float x = WordsAt + Width(Spaced, T("research.takes").ToUpperInvariant(), 10) + 14;
            foreach (var (item, count) in TechLinks.Of(id).Takes)
            {
                var one = PictureOf(item, 20);
                one.Position = new Vector2(x + 2, 75);
                one.Size = new Vector2(20, 20);
                _strip.Tips.Add(one, () => TipOf(item));
                card.AddChild(one);
                string words = T("research.taken", ("n", count), ("what", _strip.NameOf(item)));
                _madeFrom.Add((x, words));
                x += 30 + Width(Plain, words, 12) + 14;
            }
        }
        _strip.Facts.AddChild(card);
        (_card, _cardPicture) = (card, picture);
        picture.Modulate = OwnedPlan(tile) ? Had : Colors.White;
    }

    /// <summary>What the button says now and whether it can be pressed: the price to buy at, or why not.</summary>
    private void ShowAction()
    {
        var action = _strip.Action;
        if (Current is not { } tile || _research is not { } research)
        {
            action.Visible = false;
            return;
        }
        action.Visible = true;
        var state = tile.State;
        bool can = state.Tile == MarketTile.Affordable;
        action.Disabled = !can;
        action.Text = "";
        var word = _strip.ActionWord;
        word.Text = state.Tile switch
        {
            MarketTile.Affordable => T("research.buy"),
            MarketTile.Dear => T(research.BuyProblem(state.Next!) ?? "research.noCredits"),
            MarketTile.Locked => T("research.reputation"),
            _ => T(tile.Ladder ? "research.market.boughtOut" : "research.bought"),
        };
        word.LabelSettings.Font = can ? Strong : Plain;
        word.LabelSettings.FontSize = can ? 20 : 12;
        word.LabelSettings.FontColor = can ? OnAccent : Dim;
        _strip.ActionPrice.Visible = state.Next != null && state.Tile != MarketTile.Locked;
        if (state.Next is not { } next || state.Tile == MarketTile.Locked) return;
        _strip.ActionPrice.Set(Prices.Of(next));
        _strip.ActionPrice.Tint(can ? OnAccent : Dear);
    }

    /// <summary>The strip's button: buys the step the good picked offers. A page off the screen has nothing picked.</summary>
    private void Act()
    {
        if (_research is not { } research || Current?.State.Next is not { } next) return;
        if (research.BuyProblem(next) == null) _send(new BuyBlock(next.Id));
        _readSoon();
    }

    /// <summary>The words about a good: an upgrade's, a blueprint's or an item's kind; none where nothing says.</summary>
    private static string AboutOf(string id) =>
        Content.Upgrades.Of(id) is { } upgrade ? T($"upgrade.{upgrade.Kind}.about")
        : Table.ContainsKey($"tool.{id}.about") ? T($"tool.{id}.about")
        : Table.ContainsKey($"item.{id}.kind") ? T($"item.{id}.kind") : "";

    private Tip TipOf(string id)
    {
        if (Content.Upgrades.Of(id) is { } upgrade) return new Tip(_strip.NameOf(id), [new TipLine(T($"upgrade.{upgrade.Kind}.about"), Tone.Dim)]);
        if (_strip.PlacementOf(id) is not { } placement) return new Tip(_strip.NameOf(id)) { Card = ItemCards.Of(id) };
        var lines = new List<TipLine>();
        if (Table.ContainsKey($"tool.{id}.about")) lines.Add(new TipLine(T($"tool.{id}.about"), Tone.Dim));
        if (Content.Research.Node(id) is { } node && _research?.BuyProblem(node) is { } why && why != "research.bought") lines.Add(new TipLine(T(why), Tone.Unknown));
        return new Tip(_strip.NameOf(id), lines) { Picture = placement };
    }

    /// <summary>The words a tile says under its picture: the price of its next step, or why there is none.</summary>
    private static string PriceOf(Tile tile) => tile.State.Tile switch
    {
        MarketTile.Owned => T(tile.Ladder ? "research.market.allBought" : "research.market.owned"),
        _ => Prices.Of(tile.State.Next!).ToString(),
    };

    /// <summary>A kind of upgrade's count: how many of those opened are bought.</summary>
    private static string CountOf(Tile tile) => tile.Ladder ? $"{tile.State.Bought}/{tile.State.Opened}" : "";

    /// <summary>What of a tile has cleared: whether it deciphers and on which step, its picture, and how many characters of its price and its count.</summary>
    private (bool Playing, int Step, bool Picture, int Price, int Status) Cleared(Tile tile)
    {
        string price = PriceOf(tile), count = CountOf(tile);
        var (row, index, all) = tile.Opening;
        if (tile.Fresh == MarketFresh.None || !_reveal.Deciphering(tile.Good.Id, row, index, all)) return (false, 0, true, price.Length, count.Length);
        bool more = tile.Fresh == MarketFresh.More;
        int picture = more ? 0 : PictureChars, shown = _reveal.CardShown(tile.Good.Id, row, index, all, picture + price.Length + count.Length), rest = shown - picture;
        return (true, _reveal.StepOf(row), more || shown >= picture, Math.Clamp(rest, 0, price.Length), Math.Max(0, rest - price.Length));
    }

    private void DrawTile(Tile tile)
    {
        var box = tile.Box;
        float wide = box.Size.X, tall = box.Size.Y, middle = wide / 2, under = tall - 12;
        var cleared = Cleared(tile);
        uint place = Cipher.Place(tile.Good.Id);
        bool picked = Current == tile && !cleared.Playing;
        Frame(box, new Rect2(0, 0, wide, tall), cleared.Playing ? Opening : TileFill, picked ? Picked : cleared.Playing ? Accent : TileEdge, picked ? 2 : 1);
        if (!cleared.Picture) Block(box, place, new Vector2(Mathf.Round(middle - 18), PictureAt - 18), 36, 6, cleared.Step, AccentDark);
        string price = PriceOf(tile), count = CountOf(tile);
        bool locked = tile.State.Tile == MarketTile.Locked, garbled = locked || cleared.Price < price.Length;
        string said = garbled ? Cipher.Of(price, place + 7, locked ? 0 : cleared.Price, cleared.Step) : price;
        if (OwnedPlan(tile))
        {
            SayMiddle(box, Strong, said, middle, under, 11, Own);
            Check(box, new Vector2(wide - 15, 6), 9, Own);
        }
        else if (tile.State.Tile == MarketTile.Owned) SayMiddle(box, garbled ? Garbled : Plain, said, middle, under, 10, Dim);
        else Credits(box, said, middle, under, garbled ? 11 : 12, garbled ? Garbled : Strong, garbled ? Dim : tile.State.Tile == MarketTile.Affordable ? Gold : Dear, garbled ? 0.3f : 1, true);
        if (tile.Ladder)
        {
            bool hidden = cleared.Status < count.Length;
            Tag(box, hidden ? Cipher.Of(count, place + 13, cleared.Status, cleared.Step) : count, wide - 4, 4, hidden ? AccentDark : Badge);
        }
        else if (!OwnedPlan(tile) && cleared.Picture) box.DrawRect(new Rect2(5, 6, 3, 10), Plan);
    }

    /// <summary>Outlines where no good is yet.</summary>
    private void DrawSlots()
    {
        var (columns, rows, wide, tall) = MarketGrid.Of(_slots.Size.X, _slots.Size.Y);
        for (int i = _inView.Count; i < columns * rows; i++) Frame(_slots, Slot(i, columns, wide, tall), Colors.Transparent, Empty, 1);
    }

    private void DrawTabs()
    {
        _tabAt.Clear();
        float x = 0;
        foreach (var (shelf, key) in Shelves)
        {
            bool on = shelf == _shelf;
            string word = T(key), n = _shownAll.Count(t => shelf == null || t.Good.Shelf == shelf).ToString();
            var font = on ? Strong : Plain;
            float words = Width(font, word, 13), number = Width(Plain, n, 11), wide = words + number + 26;
            Say(_tabs, font, word, x + 8, 18, 13, on ? Words : Dim);
            Frame(_tabs, new Rect2(x + 12 + words, 7, number + 8, 14), on ? AccentDark : Pill, Colors.Transparent, 0, 7);
            Say(_tabs, Plain, n, x + 16 + words, 18, 11, on ? Words : Dim);
            if (on) _tabs.DrawRect(new Rect2(x + 4, 25, wide - 8, 2), Accent);
            _tabAt.Add((shelf, new Rect2(x, 0, wide, 26)));
            x += wide + 6;
        }
        _tabs.DrawRect(new Rect2(0, 27, _tabs.Size.X, 1), Edge);
    }

    /// <summary>How many of <paramref name="item"/> are sold toward the next level: since the level before it opened.</summary>
    private int SoldOf(string item) => _standing.Wanted.FirstOrDefault(d => d.Item == item)?.Sold ?? 0;

    /// <summary>
    /// What the mafia wants for its next level: each good and how many are sold of it since the level before, garbage while that level deciphers, then
    /// the level's bar, which anything else sold moves at a seventh.
    /// </summary>
    private void DrawSide()
    {
        float tall = _side.Size.Y, wide = MarketGrid.SideWide - 2 * SideIn;
        Frame(_side, new Rect2(0, 0, MarketGrid.SideWide, tall), Panel, Edge, 1);
        Caps(_side, T("research.market.wanted"), SideIn, 20, Dim);
        if (_order is not { } order || _research is null)
        {
            if (_research != null) Say(_side, Plain, Fitted(T("research.market.allFilled"), Plain, 12, wide), SideIn, 42, 12, Dim);
            return;
        }
        bool garbage = _reveal.HeadDeciphering(order.Ring);
        int step = _reveal.HeadStepOf(order.Ring);
        uint place = Cipher.Place(order.Id);
        string Clear(string text, uint at) => garbage ? Cipher.Of(text, at, _reveal.HeadShown(order.Ring, text.Length), step) : text;
        string name = T($"research.gate.{order.Id}");
        if (garbage) Say(_side, Garbled, Clear(name, place), SideIn, 42, 15, Dim);
        else Say(_side, Strong, Fitted(name, Strong, 17, wide), SideIn, 42, 17, Words);
        Say(_side, Plain, Fitted(Clear(T("research.market.sell"), place + 1), Plain, 11, wide), SideIn, 60, 11, Dim);
        float y = 74;
        for (int i = 0; i < order.Order.Count; i++)
        {
            var (item, want) = order.Order[i];
            int have = Math.Min(want, SoldOf(item));
            bool enough = have >= want;
            Frame(_side, new Rect2(SideIn, y, wide, AskTall), AskFill, Colors.Transparent, 0);
            if (garbage) Block(_side, Cipher.Place(item), new Vector2(SideIn + 6, y + 5), 30, 5, step, Faint);
            string words = Fitted(_strip.NameOf(item), Plain, 12, wide - 52 - 40);
            Say(_side, Plain, Clear(words, place + 10 + (uint)i), SideIn + 44, y + 17, 12, garbage ? Dim : Words);
            SayRight(_side, Strong, Clear($"{have}/{want}", place + 20 + (uint)i), SideIn + wide - 8, y + 17, 12, enough ? Own : Words);
            var bar = new Rect2(SideIn + 44, y + 27, wide - 52, 4);
            _side.DrawRect(bar, Faint);
            _side.DrawRect(bar with { Size = new Vector2(bar.Size.X * have / want, bar.Size.Y) }, enough ? Own : Accent);
            y += AskStride;
        }
        y += 14;
        float part = _standing.Target > 0 ? Math.Min(1f, (float)_standing.Points / _standing.Target) : 0;
        Caps(_side, T("research.market.level"), SideIn, y, Dim);
        CapsRight(_side, Clear($"{(int)(part * 100)}%", place + 30), SideIn + wide, y, Words);
        y += 8;
        var level = new Rect2(SideIn, y, wide, 6);
        _side.DrawRect(level, Faint);
        _side.DrawRect(level with { Size = new Vector2(level.Size.X * part, level.Size.Y) }, Accent);
        y += 22;
        Say(_side, Plain, Fitted(Clear(T("research.market.other"), place + 31), Plain, 11, wide), SideIn, y, 11, Dim);
    }

    /// <summary>The strip: the picture large, what kind of good it is, its name and what it is, then its prices bought and not, or what it is made from.</summary>
    private void DrawCard(Control card, Tile tile)
    {
        if (_research is not { } research) return;
        float wide = card.Size.X, room = wide - WordsAt - 12;
        string id = tile.Good.Steps[0].Id;
        Frame(card, new Rect2(0, 0, 100, 100), TileFill, TileEdge, 1);
        Caps(card, T(tile.Ladder ? "research.market.kind.upgrade" : "research.market.kind.blueprint"), WordsAt, 14, tile.Ladder ? Dim : PlanWord);
        Say(card, Strong, Fitted(_strip.NameOf(id), Strong, 18, room), WordsAt, 36, 18, Words);
        Say(card, Plain, Fitted(AboutOf(id), Plain, 12, room), WordsAt, 55, 12, Dim);
        if (tile.Ladder)
        {
            float x = WordsAt + Caps(card, T("research.market.boughtOf", ("n", tile.State.Bought), ("m", tile.State.Opened)), WordsAt, 88, Dim) + 14;
            foreach (var step in tile.Good.Steps.Where(research.Reached))
            {
                bool had = research.Done(step);
                string price = Prices.Of(step).ToString();
                float chip = CreditLine.CoinOf(11) + 3 + Width(Strong, price, 11) + 12;
                if (x + chip > wide) break;
                Frame(card, new Rect2(x, 75, chip, 20), had ? ChipHad : Chip, had ? ChipHadEdge : ChipEdge, 1, 10);
                Credits(card, price, x + 6, 89, 11, Strong, had ? Faint : Gold, had ? 0.4f : 1, false);
                x += chip + 5;
            }
            return;
        }
        float after = WordsAt + Caps(card, T("research.takes"), WordsAt, 88, Dim) + 14;
        if (_madeFrom.Count == 0) Say(card, Plain, T("research.market.nothing"), after, 89, 12, Dim);
        foreach (var (at, words) in _madeFrom)
        {
            Frame(card, new Rect2(at, 73, 24, 24), TileFill, TileEdge, 1);
            Say(card, Plain, words, at + 30, 89, 12, Words);
        }
    }

    /// <summary>Garbage where a picture will be: <paramref name="cells"/> by <paramref name="cells"/> filling <paramref name="side"/>, the same at the same place.</summary>
    private static void Block(CanvasItem on, uint place, Vector2 at, float side, int cells, int step, Color colour)
    {
        float cell = side / cells;
        for (int j = 0; j < cells; j++)
            for (int i = 0; i < cells; i++)
                if (Cipher.Garble(place, i + j * cells, step) % 3 == 0) on.DrawRect(new Rect2(at.X + i * cell, at.Y + j * cell, Math.Max(1, cell - 1), Math.Max(1, cell - 1)), colour);
    }

    /// <summary>A flat box with a thin edge; the one <see cref="StyleBoxFlat"/> is set afresh for each, for drawing it copies what it says.</summary>
    private static void Frame(CanvasItem on, Rect2 rect, Color fill, Color edge, int width, int radius = 3)
    {
        Look.BgColor = fill;
        Look.BorderColor = edge;
        Look.SetBorderWidthAll(width);
        Look.SetCornerRadiusAll(radius);
        Look.AntiAliasing = radius > 3;
        on.DrawStyleBox(Look, rect);
    }

    /// <summary>An amount as <see cref="CreditLine"/> writes it, the coin before the number, from <paramref name="x"/> or about it as its middle.</summary>
    private static void Credits(CanvasItem on, string amount, float x, float y, int size, Font font, Color colour, float coin, bool centred)
    {
        int side = CreditLine.CoinOf(size);
        float wide = side + 3 + Width(font, amount, size), left = Mathf.Round(centred ? x - wide / 2 : x);
        on.DrawTextureRect(Coin, new Rect2(left, Mathf.Round(y - size * 0.36f - side / 2f), side, side), false, Colors.White with { A = coin });
        Say(on, font, amount, left + side + 3, y, size, colour);
    }

    /// <summary>A small count in a box, its right edge at <paramref name="right"/>.</summary>
    private static void Tag(CanvasItem on, string text, float right, float top, Color fill)
    {
        float wide = Width(Strong, text, 10) + 8;
        Frame(on, new Rect2(right - wide, top, wide, 14), fill, Colors.Transparent, 0);
        Say(on, Strong, text, right - wide + 4, top + 10.5f, 10, Words);
    }

    private static void Check(CanvasItem on, Vector2 at, float side, Color colour) =>
        on.DrawPolyline([at + new Vector2(0, side * 0.5f), at + new Vector2(side * 0.38f, side * 0.88f), at + new Vector2(side, side * 0.1f)], colour, 2, true);

    /// <summary>A heading in spaced capitals; how wide it came out.</summary>
    private static float Caps(CanvasItem on, string text, float x, float y, Color colour)
    {
        string caps = text.ToUpperInvariant();
        Say(on, Spaced, caps, x, y, 10, colour);
        return Width(Spaced, caps, 10);
    }

    private static void CapsRight(CanvasItem on, string text, float right, float y, Color colour) =>
        Caps(on, text, right - Width(Spaced, text.ToUpperInvariant(), 10), y, colour);

    private static float Width(Font font, string text, int size) => font.GetStringSize(text, HorizontalAlignment.Left, -1, size).X;

    /// <summary><paramref name="text"/> cut with an ellipsis to fit <paramref name="wide"/>.</summary>
    private static string Fitted(string text, Font font, int size, float wide)
    {
        if (Width(font, text, size) <= wide) return text;
        int n = text.Length;
        while (n > 1 && Width(font, text[..(n - 1)] + "…", size) > wide) n--;
        return text[..(n - 1)] + "…";
    }

    private static void Say(CanvasItem on, Font font, string text, float x, float y, int size, Color colour) =>
        on.DrawString(font, new Vector2(x, y), text, HorizontalAlignment.Left, -1, size, colour);

    private static void SayRight(CanvasItem on, Font font, string text, float right, float y, int size, Color colour) =>
        Say(on, font, text, right - Width(font, text, size), y, size, colour);

    private static void SayMiddle(CanvasItem on, Font font, string text, float middle, float y, int size, Color colour) =>
        Say(on, font, text, middle - Width(font, text, size) / 2, y, size, colour);
}
