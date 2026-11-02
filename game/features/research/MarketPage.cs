using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Stillwake.Content;
using Stillwake.Game.Render;
using Stillwake.Game.Shared;
using Stillwake.Sim;
using Stillwake.Sim.Research;
using static Stillwake.Game.Features.Research.StripParts;
using static Stillwake.Game.I18n.En;

namespace Stillwake.Game.Features.Research;

/// <summary>
/// The black market as the tablet's own interface hacked ([market-decrypt](../../../docs/decisions/2026-11-02-market-decrypt.md)): a row per group, its order on the left and its
/// goods as cards on the right, what is not open garbage that deciphers as its order fills (<see cref="MarketReveal"/>). A click picks; only the strip's button buys. Built once and
/// restyled, for a rebuild under the pointer would eat a click; each head and card draws itself, and repaints only when what it says has changed.
/// </summary>
public sealed class MarketPage : ITabletPage
{
    /// <summary>The one colour that does not belong to the tablet: what can be bought and the order being filled.</summary>
    public static readonly Color Poison = new("ff2d78"), PoisonDark = new("5a1030");

    private static readonly Font Plain = Style.Mono, Heavy = new FontVariation { BaseFont = Style.Mono, VariationEmbolden = 0.6f };
    private static readonly StyleBoxFlat Look = new();
    private static readonly Color Dim = new(0.55f, 0.55f, 0.6f, 0.85f), HadBefore = new(1, 1, 1, 0.75f);

    private const int NameSize = 12, PriceSize = 13, AboutSize = 11, ScrapSize = 10, HeadSize = 14, HeadLine = 11, LineTall = 15;
    /// <summary>Where a card's words start and how much of its width they leave; where its picture's middle lies and the box it fits; a head's order, small and large.</summary>
    private const int WordsAt = 56, WordsSpare = 62, PictureAt = 28, PictureWide = 44, PictureTall = 40, SmallPicture = 24, LargePicture = 40, SmallStride = 78;
    /// <summary>Below this height a met order folds to a line; above these a card has room for its words.</summary>
    private const int FoldsBelow = 90, AboutFromTall = 90, AboutFromWide = 260;
    /// <summary>The garbage standing for a picture: its cells, and how far apart they are.</summary>
    private const int BlockCells = 7, BlockStride = 6, BlockCell = 5;

    private sealed class Card(TechNode node, int row, int index, Control box, TextureRect picture)
    {
        public TechNode Node { get; } = node;
        public int Row { get; } = row;
        public int Index { get; } = index;
        public Control Box { get; } = box;
        public TextureRect Picture { get; } = picture;
        public MarketCard State { get; set; }
        public int Stamp { get; set; } = -1;
    }

    private sealed class Row(int index, TechNode? gate, Control head, TextureRect[] pictures, Card[] cards)
    {
        public int Index { get; } = index;
        public TechNode? Gate { get; } = gate;
        public Control Head { get; } = head;
        public TextureRect[] Pictures { get; } = pictures;
        public Card[] Cards { get; } = cards;
        public int Stamp { get; set; } = -1;
    }

    private readonly TechTab _tab;
    private readonly Control _view;
    private readonly StripParts _strip;
    private readonly Action<Command> _send;
    private readonly Action _readSoon;
    private readonly Func<BoardSeen> _seen;
    private readonly TechNode[] _orders;
    private readonly MarketReveal _reveal;
    private readonly Control _root = new() { MouseFilter = Control.MouseFilterEnum.Ignore, Visible = false };
    private readonly ColorRect _back = new() { Color = Style.Slate950, MouseFilter = Control.MouseFilterEnum.Ignore };
    private readonly List<Row> _rows = [];
    private IResearchQueries? _research;
    private Func<string, int> _sold = _ => 0;
    private TechNode? _picked;
    private Label? _stripPrice;
    private string? _shown;
    private bool _out;
    private int _met;
    private Vector2 _laidIn;
    private double[] _laidBy = [];

    /// <param name="tab">The tab of <c>Content.Research</c> it draws.</param>
    /// <param name="view">The tablet's view, which it draws into.</param>
    /// <param name="strip">The strip below and its words.</param>
    /// <param name="send">Sends a command to the session.</param>
    /// <param name="readSoon">Has the tablet read on its next frame.</param>
    /// <param name="seen">What this machine has seen of the orders, for the world played.</param>
    public MarketPage(TechTab tab, Control view, StripParts strip, Action<Command> send, Action readSoon, Func<BoardSeen> seen)
    {
        (_tab, _view, _strip, _send, _readSoon, _seen) = (tab, view, strip, send, readSoon, seen);
        _orders = Content.Research.Nodes.Where(n => n.Gate && n.Tab == tab.Id).ToArray();
        _reveal = new MarketReveal(tab.Rings.Length);
        _root.AddChild(_back);
        view.AddChild(_root);
        strip.Action.Pressed += Act;
    }

    /// <summary>The hack's mark over the tablet's screen, for the tablet to lay there.</summary>
    public MarketFaults Faults { get; } = new();

    public Vector2 ScreenOf(string id) => _rows.SelectMany(r => r.Cards).First(c => c.Node.Id == id).Box.GetGlobalRect().GetCenter();

    public void Show(bool shown)
    {
        _out = _root.Visible = Faults.Visible = shown;
        if (shown)
        {
            _reveal.Came();
        }
        else
        {
            _picked = null;
            _reveal.Clear();
        }
        _shown = null;
    }

    public void Step(double delta)
    {
        if (_rows.Count == 0) return;
        bool playing = _reveal.Playing;
        _reveal.Step(delta);
        Lay();
        if (playing || _reveal.Playing) Repaint();
    }

    public void Read(Registry m)
    {
        var research = m.Research;
        _research = research;
        _sold = m.Knowledge.SoldOf;
        if (_rows.Count == 0) Build();
        _met = _orders.Count(research.Done);
        if (_out) _reveal.Play(_seen().SeeOrders(_met), _met);
        var cards = _rows.SelectMany(r => r.Cards);
        string now = (_picked?.Id ?? "") + '|' + string.Join('|', cards.Select(c => research.BuyProblem(c.Node) ?? "")) + '|' + string.Join(',', _orders.SelectMany(o => o.Order).Select(d => _sold(d.Item)));
        if (now == _shown) return;
        _shown = now;
        foreach (var row in _rows)
        {
            foreach (var card in row.Cards) card.State = MarketRows.StateOf(research.Done(card.Node), research.Reached(card.Node), research.BuyProblem(card.Node));
            Restyle(row);
        }
        if (_out) ShowAction(research);
    }

    private void Build()
    {
        _strip.Learn(_research!);
        for (int r = 0; r < _tab.Rings.Length; r++)
        {
            var ring = _tab.Rings[r];
            var head = new Control { MouseFilter = Control.MouseFilterEnum.Pass, ClipContents = true };
            var pictures = ring.Order.Select(d => PictureOf(d.Item)).ToArray();
            foreach (var picture in pictures) head.AddChild(picture);
            _root.AddChild(head);
            var row = new Row(r, ring.Gate is { } gate ? Content.Research.Node(gate) : null, head, pictures, ring.Blocks.Select((b, i) => CardOf(Content.Research.Node(b.Id)!, r, i)).ToArray());
            head.Draw += () => DrawHead(row);
            _strip.Tips.Add(head, () => HeadTip(row));
            for (int i = 0; i < pictures.Length; i++)
            {
                string item = ring.Order[i].Item;
                _strip.Tips.Add(pictures[i], () => TipOf(item));
            }
            _rows.Add(row);
        }
        Lay();
        if (_out) Fill();
    }

    private Card CardOf(TechNode node, int row, int index)
    {
        var box = new Control { MouseFilter = Control.MouseFilterEnum.Stop };
        var picture = PictureOf(node.Id);
        box.AddChild(picture);
        _root.AddChild(box);
        var card = new Card(node, row, index, box, picture);
        box.Draw += () => DrawCard(card);
        box.GuiInput += e =>
        {
            if (e is not InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left }) return;
            box.AcceptEvent();
            if (card.State == MarketCard.Sealed) return;
            _reveal.ClearCard(row, index);
            Pick(node);
        };
        _strip.Tips.Add(box, () => card.State == MarketCard.Sealed ? new Tip(T("research.locked")) : TipOf(node.Id));
        return card;
    }

    private TextureRect PictureOf(string id)
    {
        var picture = _strip.Picture(id, PictureTall);
        picture.CustomMinimumSize = Vector2.Zero;
        return picture;
    }

    /// <summary>The rows where they lie in the view now: only when its size or the tall row has moved.</summary>
    private void Lay()
    {
        var size = _view.Size;
        var weights = _reveal.Weights(_met);
        if (size == _laidIn && weights.SequenceEqual(_laidBy)) return;
        (_laidIn, _laidBy) = (size, weights);
        _back.Size = size;
        var rows = MarketRows.Of(size.Y, weights);
        float cardsFrom = MarketRows.HeadWide + MarketRows.Gap, cardsWide = size.X - cardsFrom;
        foreach (var row in _rows)
        {
            var (at, tall) = rows[row.Index];
            float top = Mathf.Round(at), high = Mathf.Round(at + tall) - top;
            row.Head.Position = new Vector2(0, top);
            row.Head.Size = new Vector2(MarketRows.HeadWide, high);
            LayHead(row);
            foreach (var card in row.Cards)
            {
                var (left, wide) = MarketRows.Card(cardsWide, row.Cards.Length, card.Index);
                float from = Mathf.Round(cardsFrom + left);
                card.Box.Position = new Vector2(from, top);
                card.Box.Size = new Vector2(Mathf.Round(cardsFrom + left + wide) - from, high);
                card.Picture.Size = new Vector2(PictureWide, PictureTall);
                card.Picture.Position = new Vector2(PictureAt - PictureWide / 2, Mathf.Round((high - PictureTall) / 2));
            }
        }
    }

    /// <summary>A head's pictures of what it orders: hidden while it is garbage, small in a line once met, large with what is sold under them.</summary>
    private void LayHead(Row row)
    {
        if (row.Gate is not { } gate || _research is not { } research) return;
        bool closed = !research.Reached(gate) || _reveal.HeadDeciphering(row.Index), fold = research.Done(gate) && row.Head.Size.Y < FoldsBelow;
        float column = (MarketRows.HeadWide - 16f) / row.Pictures.Length;
        for (int i = 0; i < row.Pictures.Length; i++)
        {
            var picture = row.Pictures[i];
            picture.Visible = !closed;
            int side = fold ? SmallPicture : LargePicture;
            var middle = fold ? new Vector2(22 + i * SmallStride, row.Head.Size.Y - 20) : new Vector2(8 + i * column + column / 2, 48);
            picture.Size = new Vector2(side, side);
            picture.Position = (middle - picture.Size / 2).Round();
            picture.Modulate = research.Done(gate) || _sold(gate.Order[i].Item) >= gate.Order[i].Count ? Dim : Colors.White;
        }
    }

    private void Restyle(Row row)
    {
        row.Stamp = -1;
        row.Head.QueueRedraw();
        LayHead(row);
        foreach (var card in row.Cards)
        {
            card.Stamp = -1;
            card.Box.QueueRedraw();
            card.Picture.Visible = card.State != MarketCard.Sealed && !_reveal.Deciphering(row.Index, card.Index);
            card.Picture.Modulate = card.State == MarketCard.Dear ? Dim : card.State == MarketCard.Bought ? HadBefore : Colors.White;
        }
    }

    /// <summary>What a decipher has moved on: a head or a card that says something new this frame, its picture once it is done.</summary>
    private void Repaint()
    {
        foreach (var row in _rows)
        {
            int said = _reveal.HeadDeciphering(row.Index) ? _reveal.HeadStepOf(row.Index) * 1000 + _reveal.HeadShown(row.Index, 999) : -1;
            if (said != row.Stamp)
            {
                row.Stamp = said;
                row.Head.QueueRedraw();
                LayHead(row);
            }
            foreach (var card in row.Cards)
            {
                bool playing = _reveal.Deciphering(row.Index, card.Index);
                int now = playing ? _reveal.StepOf(row.Index) * 1000 + _reveal.CardShown(row.Index, card.Index, 999) : -1;
                if (now == card.Stamp) continue;
                card.Stamp = now;
                card.Box.QueueRedraw();
                card.Picture.Visible = card.State != MarketCard.Sealed && !playing;
            }
        }
    }

    public void Pointer(InputEvent e)
    {
        if (e is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left }) Pick(null);
        else Zoom(e, _view);
    }

    public void LetGo() { }

    /// <summary>Nothing zooms here; the wheel is only kept from reaching the hotbar.</summary>
    public void Zoom(InputEvent e, Control over)
    {
        if (e is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.WheelUp or MouseButton.WheelDown }) over.AcceptEvent();
    }

    private void Pick(TechNode? node)
    {
        _picked = node;
        _shown = null;
        _readSoon();
        Fill();
    }

    public void Fill()
    {
        _strip.Clear();
        _stripPrice = null;
        var facts = _strip.Facts;
        if (_picked is not { } node)
        {
            var hint = Text(T("research.pickHint"), Style.Slate500);
            hint.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
            facts.AddChild(hint);
            return;
        }
        string id = node.Id;
        _stripPrice = Text(Prices.Of(node).ToString(), Style.Slate500, PriceSize + 3);
        _stripPrice.LabelSettings.Font = Heavy;
        var title = _strip.Title(_strip.NameOf(id), AboutOf(id), id, TipOf, under: _stripPrice);
        title.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        facts.AddChild(title);
        var link = TechLinks.Of(id);
        if (link.Takes.Count > 0)
        {
            var takes = Flow(10);
            foreach (var (item, count) in link.Takes) takes.AddChild(_strip.Named(item, T("research.taken", ("n", count), ("what", _strip.NameOf(item))), Style.Slate100));
            facts.AddChild(Fact(T("research.takes"), Style.Slate400, takes, 3));
        }
        if (link.MadeAt.Count > 0 || _strip.PlacementOf(id) != null)
        {
            var at = Flow(10);
            foreach (string where in link.MadeAt) at.AddChild(_strip.Named(where, _strip.NameOf(where), Style.Slate100));
            if (link.MadeAt.Count == 0) at.AddChild(Text(T("research.builtInPlace"), Style.Slate500));
            facts.AddChild(Fact(T("research.madeAt"), Style.Slate400, at, 2));
        }
        if (link.UsedIn.Count > 0) facts.AddChild(Fact(T("research.usedIn"), Style.Slate400, _strip.Pictures(link.UsedIn, TipOf), 5));
    }

    /// <summary>What the button says now and whether it can be pressed; none with nothing picked.</summary>
    private void ShowAction(IResearchQueries research)
    {
        var action = _strip.Action;
        if (_picked is not { } node)
        {
            action.Visible = false;
            return;
        }
        action.Visible = true;
        string? why = research.BuyProblem(node);
        action.Disabled = why != null;
        _strip.ActionLine.Visible = why == null;
        _strip.ActionPrice.Set(Prices.Of(node));
        _strip.ActionPrice.Tint(Style.Slate100);
        action.Text = why == null ? "" : T(why);
        if (_stripPrice != null) _stripPrice.LabelSettings.FontColor = why == null ? Poison : Style.Slate500;
    }

    /// <summary>The strip's button: buys what is picked. A page off the screen has nothing picked.</summary>
    private void Act()
    {
        if (_research is not { } research || _picked is not { } node) return;
        if (research.BuyProblem(node) == null) _send(new BuyBlock(node.Id));
        _readSoon();
    }

    /// <summary>The words about a good: an upgrade's, a blueprint's or an item's kind; none where nothing says.</summary>
    private static string AboutOf(string id) =>
        Content.Upgrades.Of(id) is { } upgrade ? T($"upgrade.{upgrade.Kind}.about")
        : Table.ContainsKey($"tool.{id}.about") ? T($"tool.{id}.about")
        : Table.ContainsKey($"item.{id}.kind") ? T($"item.{id}.kind") : "";

    private Tip? HeadTip(Row row)
    {
        if (row.Gate is not { } gate || _research is not { } research) return null;
        return !research.Reached(gate) ? new Tip(T("research.locked")) : research.Done(gate) ? null : new Tip(T("research.orderAbout"));
    }

    private Tip TipOf(string id)
    {
        if (Content.Upgrades.Of(id) is { } upgrade) return new Tip(_strip.NameOf(id), [new TipLine(T($"upgrade.{upgrade.Kind}.about"), Tone.Dim)]);
        if (_strip.PlacementOf(id) is not { } placement) return new Tip(_strip.NameOf(id)) { Card = ItemCards.Of(id) };
        var lines = new List<TipLine>();
        if (Table.ContainsKey($"tool.{id}.about")) lines.Add(new TipLine(T($"tool.{id}.about"), Tone.Dim));
        if (Content.Research.Node(id) is { } node && _research?.BuyProblem(node) is { } why && why != "research.bought") lines.Add(new TipLine(T(why), Tone.Unknown));
        return new Tip(_strip.NameOf(id), lines) { Picture = placement };
    }

    private void DrawCard(Card card)
    {
        var box = card.Box;
        float wide = box.Size.X, tall = box.Size.Y;
        string id = card.Node.Id, name = _strip.NameOf(id), price = Prices.Of(card.Node).ToString();
        uint place = Cipher.Place(id);
        bool playing = _reveal.Deciphering(card.Row, card.Index), closed = card.State == MarketCard.Sealed || playing, picked = _picked == card.Node && !closed;
        int step = playing ? _reveal.StepOf(card.Row) : 0, shown = !closed ? int.MaxValue : playing ? _reveal.CardShown(card.Row, card.Index, name.Length + price.Length) : 0;
        var (fill, edge, width) =
            closed ? (Style.Slate950, playing ? Poison : Style.Slate800, 1)
            : picked ? (Style.Slate700, Colors.White, 2)
            : card.State == MarketCard.Affordable ? (Style.Slate800, Poison, 2)
            : card.State == MarketCard.Bought ? (Style.Slate900, Style.Slate800, 1)
            : (Style.Slate900, Style.Slate700, 1);
        Frame(box, new Rect2(0, 0, wide, tall), fill, edge, width);
        if (closed) DrawBlock(box, place, tall, step, playing ? PoisonDark : Style.Slate800);
        float cell = Cell(Plain, NameSize), least = LineTall * 2;
        var lines = Cipher.Wrap(name, Math.Max(8, (int)((wide - WordsSpare) / cell)), 2);
        var about = tall > AboutFromTall && wide > AboutFromWide ? Cipher.Wrap(AboutOf(id), (int)((wide - WordsSpare - 4) / Cell(Plain, AboutSize)), 2) : [];
        float block = lines.Count * LineTall + 18 + about.Count * 14, top = Mathf.Round((tall - Math.Max(block, least)) / 2) + 11;
        int at = 0;
        bool cursor = playing;
        for (int i = 0; i < lines.Count; i++)
        {
            string line = lines[i];
            int clear = Math.Clamp(shown - at, 0, line.Length);
            float y = top + i * LineTall;
            var colour = closed ? (playing ? Style.Slate300 : Style.Slate500) : card.State == MarketCard.Bought ? Style.Slate500 : card.State == MarketCard.Dear ? Style.Slate400 : Style.Slate100;
            Say(box, Plain, closed ? Cipher.Of(line, place + (uint)i * 5, clear, step) : line, WordsAt, y, NameSize, colour);
            if (cursor && clear < line.Length)
            {
                cursor = false;
                box.DrawRect(new Rect2(WordsAt + clear * cell, y - 10, cell, 12), Poison);
                Say(box, Plain, line[clear] == ' ' ? " " : Cipher.Garble(place, clear, step).ToString(), WordsAt + clear * cell, y, NameSize, Colors.White);
            }
            at += line.Length;
        }
        float under = top + lines.Count * LineTall + 3;
        if (closed) Say(box, Heavy, Cipher.Of(price, place + 77, Math.Clamp(shown - at, 0, price.Length), step), WordsAt, under, PriceSize, playing ? Poison : Style.Slate600);
        else if (card.State == MarketCard.Bought) Say(box, Plain, T("research.bought"), WordsAt, under, NameSize, Bought);
        else Say(box, Heavy, price, WordsAt, under, PriceSize, card.State == MarketCard.Affordable ? Poison : Style.Slate500);
        for (int i = 0; i < about.Count; i++)
            Say(box, Plain, closed ? Cipher.Of(about[i], place + 41 + (uint)i, 0) : about[i], WordsAt, under + 16 + i * 14, AboutSize, closed ? Style.Slate700 : Style.Slate500);
        if (closed) return;
        // What the cipher left on a card it let go: two characters that never cleared.
        string left = $"{Cipher.Garble(place, 1)}{Cipher.Garble(place, 2)}";
        Say(box, Plain, left, wide - 6 - 2 * Cell(Plain, ScrapSize), tall - 6, ScrapSize, Style.Slate600);
    }

    /// <summary>Garbage where a picture will be: a block of cells, the same at the same place.</summary>
    private static void DrawBlock(Control box, uint place, float tall, int step, Color colour)
    {
        float left = PictureAt - BlockCells * BlockStride / 2f, top = Mathf.Round((tall - BlockCells * BlockStride) / 2);
        for (int j = 0; j < BlockCells; j++)
            for (int i = 0; i < BlockCells; i++)
                if (Cipher.Garble(place, i + j * BlockCells, step) % 3 == 0) box.DrawRect(new Rect2(left + i * BlockStride, top + j * BlockStride, BlockCell, BlockCell), colour);
    }

    private void DrawHead(Row row)
    {
        var head = row.Head;
        float wide = head.Size.X, tall = head.Size.Y;
        const int Left = 10;
        if (row.Gate is not { } gate || _research is not { } research)
        {
            Frame(head, new Rect2(0, 0, wide, tall), Style.Slate900, Style.Slate800, 1);
            Say(head, Heavy, T("research.market.forSale"), Left, 26, HeadSize, Style.Slate100);
            Say(head, Plain, Fitted(T("research.market.fromStart"), wide - 2 * Left, HeadLine), Left, 46, HeadLine, Style.Slate500);
            return;
        }
        bool opening = _reveal.HeadDeciphering(row.Index), done = research.Done(gate), active = research.Reached(gate) && !done;
        Frame(head, new Rect2(0, 0, wide, tall), active ? Style.Slate800 : Style.Slate900, active ? Poison : Style.Slate800, active ? 2 : 1);
        string name = T($"research.gate.{gate.Id}");
        if (!research.Reached(gate) || opening)
        {
            uint place = Cipher.Place(gate.Id);
            int step = _reveal.HeadStepOf(row.Index);
            Say(head, Heavy, Cipher.Of(name, place, opening ? _reveal.HeadShown(row.Index, name.Length) : 0, step), Left, 26, HeadSize, opening ? Poison : Style.Slate600);
            string asks = string.Join(' ', gate.Order.Select(d => $"{_strip.NameOf(d.Item)} {d.Count}"));
            Say(head, Plain, Fitted(Cipher.Of(asks, place + 3, opening ? _reveal.HeadShown(row.Index, asks.Length) : 0, step), wide - 2 * Left, HeadLine), Left, 46, HeadLine, Style.Slate700);
            return;
        }
        if (done && tall < FoldsBelow)
        {
            Say(head, Heavy, name, Left, 22, HeadSize, Style.Slate400);
            SayRight(head, T("research.market.filled"), wide - Left, 22, HeadLine, Bought);
            for (int i = 0; i < gate.Order.Count; i++) Say(head, Plain, gate.Order[i].Count.ToString(), 22 + i * SmallStride + 16, tall - 15, HeadLine, Style.Slate500);
            return;
        }
        Say(head, Heavy, name, Left, 20, HeadSize, done ? Style.Slate400 : Style.Slate100);
        SayRight(head, T("research.market.soFar"), wide - Left, 20, ScrapSize, Style.Slate500);
        float column = (wide - 16) / gate.Order.Count;
        for (int i = 0; i < gate.Order.Count; i++)
        {
            var (item, want) = gate.Order[i];
            int have = Math.Min(want, _sold(item));
            float middle = 8 + i * column + column / 2;
            SayMiddle(head, Fitted(_strip.NameOf(item), column - 4, HeadLine), middle, 82, HeadLine, Style.Slate400);
            SayMiddle(head, $"{have}/{want}", middle, 97, NameSize, have >= want ? Bought : Style.Slate100);
            var bar = new Rect2(middle - column / 2 + 5, 104, column - 10, 4);
            head.DrawRect(bar, Style.Slate700);
            head.DrawRect(bar with { Size = new Vector2(bar.Size.X * have / want, bar.Size.Y) }, have >= want ? Bought : Poison);
        }
    }

    /// <summary>A flat box with a thin edge; the one <see cref="StyleBoxFlat"/> is set afresh for each, for drawing it copies what it says.</summary>
    private static void Frame(CanvasItem on, Rect2 rect, Color fill, Color edge, int width)
    {
        Look.BgColor = fill;
        Look.BorderColor = edge;
        Look.SetBorderWidthAll(width);
        Look.SetCornerRadiusAll(4);
        Look.AntiAliasing = false;
        on.DrawStyleBox(Look, rect);
    }

    /// <summary>The width of a character: the grid garbage and clear words stand on, measured, for the font is the machine's.</summary>
    private static float Cell(Font font, int size) => font.GetCharSize('0', size).X;

    private static string Fitted(string text, float wide, int size) => Cipher.Trim(text, (int)(wide / Cell(Plain, size)));

    private static void Say(CanvasItem on, Font font, string text, float x, float y, int size, Color colour) =>
        on.DrawString(font, new Vector2(x, y), text, HorizontalAlignment.Left, -1, size, colour);

    private static void SayRight(CanvasItem on, string text, float right, float y, int size, Color colour) =>
        Say(on, Plain, text, right - text.Length * Cell(Plain, size), y, size, colour);

    private static void SayMiddle(CanvasItem on, string text, float middle, float y, int size, Color colour) =>
        Say(on, Plain, text, middle - text.Length * Cell(Plain, size) / 2, y, size, colour);
}
