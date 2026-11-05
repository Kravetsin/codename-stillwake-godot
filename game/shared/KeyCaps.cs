using System.Collections.Generic;
using Godot;
using Stillwake.Game.I18n;
using Stillwake.Game.Render;

namespace Stillwake.Game.Shared;

/// <summary>How a hint line is lit (build-hints): as the rest, the line to act on now, or short of something.</summary>
public enum HintTone
{
    Plain,
    Main,
    Short,
}

/// <summary>
/// Hint lines drawn as picked (build-hints, the wing's column and the buttons alone): a key as a lit cap with its key dark on
/// it, a mouse button as the top of a mouse with the pressed part lit, a drag's arrow pulsing after it, a hold's ring
/// filling, an item by its icon; then the words. Signs from <c>tools/anime-art</c> (<c>signs</c>), in logical pixels.
/// </summary>
public static class KeyCaps
{
    /// <summary>A row's height; a cap's, the tallest piece.</summary>
    public const float Row = 13, CapHeight = 16;
    private const float Item = 13, CapSlice = 5, CapPad = 8, CapLeast = 15;
    /// <summary>The words' size, a cap's key's, in the game's mono.</summary>
    public const int WordSize = 10;
    private const int KeySize = 9, LongKeySize = 8;
    /// <summary>A drag's arrow pulses once in this, a hold's ring fills in <see cref="HoldFills"/> of every <see cref="HoldEvery"/>.</summary>
    private const float DragPulse = 0.9f, HoldFills = 1.2f, HoldEvery = 1.6f;
    private const int HoldSteps = 12;
    private static readonly Color Words = new("6f8499"), Strong = new("b9cad8"), ShortWords = new("f06a60"), Letter = new("06101a"), Cross = new("ec3f36");
    private static Font Mono => Style.Mono;

    /// <summary>The colour of a line's words in <paramref name="tone"/>.</summary>
    public static Color WordsOf(HintTone tone) => tone switch { HintTone.Main => Strong, HintTone.Short => ShortWords, _ => Words };

    /// <summary><paramref name="line"/> laid in rows of <paramref name="width"/>, the rows after the first <paramref name="indent"/> in.</summary>
    public static List<HintPlaced> Lay(KeyHint line, float width, float indent) =>
        HintFlow.Flow(HintFlow.PiecesOf(line, En.T(line.Says), WidthOf, w => Mono.GetStringSize(w, fontSize: WordSize).X), width, indent);

    /// <summary>How many rows <paramref name="placed"/> takes.</summary>
    public static int RowsOf(List<HintPlaced> placed) => placed.Count == 0 ? 0 : placed[^1].Row + 1;

    /// <summary>
    /// <paramref name="placed"/> drawn on <paramref name="on"/> from <paramref name="at"/>, the middle of its first row, at
    /// <paramref name="time"/> seconds for the signs that move; <paramref name="flash"/> from 1 to 0 lights the words up.
    /// </summary>
    public static void Draw(CanvasItem on, List<HintPlaced> placed, Vector2 at, HintTone tone, double time, float flash = 0)
    {
        var words = WordsOf(tone).Lerp(Colors.White, flash * 0.6f);
        float ascent = Mono.GetAscent(WordSize), descent = Mono.GetDescent(WordSize);
        foreach (var (piece, row, x) in placed)
        {
            var spot = at + new Vector2(x, row * Row);
            if (piece.Press is { } press) DrawPress(on, press, spot, tone, time);
            else on.DrawString(Mono, new Vector2(spot.X, spot.Y + (ascent - descent) / 2), piece.Word!, HorizontalAlignment.Left, -1, WordSize, words);
        }
    }

    /// <summary>How wide <paramref name="press"/> is drawn.</summary>
    public static float WidthOf(Press press) => press switch
    {
        KeyPress key => CapWidth(Binds.NameOf(key.Act)),
        CapPress cap => CapWidth(cap.Name),
        MousePress { Button: Mouse.LeftDrag or Mouse.RightDrag } => Size("sign_mouseLeft").X + 1.5f + Size("sign_drag").X,
        MousePress { Button: Mouse.LeftHold } => Size("sign_mouseLeft").X + 0.5f + Size("sign_holdTrack").X,
        MousePress => Size("sign_mouseLeft").X,
        _ => Item,
    };

    private static float CapWidth(string name) =>
        Mathf.Max(CapLeast, Mathf.Ceil(Mono.GetStringSize(name, fontSize: name.Length > 1 ? LongKeySize : KeySize).X + CapPad));

    private static void DrawPress(CanvasItem on, Press press, Vector2 at, HintTone tone, double time)
    {
        switch (press)
        {
            case KeyPress key:
                Cap(on, Binds.NameOf(key.Act), at);
                break;
            case CapPress cap:
                Cap(on, cap.Name, at);
                break;
            case MousePress mouse:
                DrawMouse(on, mouse.Button, at, time);
                break;
            case ItemPress item:
                var rect = new Rect2(at.X, at.Y - Item / 2, Item, Item);
                // The builder not carried: its icon faint and struck through in red.
                on.DrawTextureRect(ItemIcons.Icon(item.Item), rect, false, tone == HintTone.Short ? new Color(1, 1, 1, 0.35f) : Colors.White);
                if (tone == HintTone.Short) on.DrawLine(rect.Position + new Vector2(1, Item - 1), rect.Position + new Vector2(Item - 1, 1), Cross, 1.4f, true);
                break;
        }
    }

    /// <summary>A cap's face stretched across its key's name, its ends kept (a 3-slice), the key written dark on it.</summary>
    private static void Cap(CanvasItem on, string name, Vector2 at)
    {
        var face = PixelArt.Picture("sign_cap");
        float w = CapWidth(name), top = at.Y - CapHeight / 2, end = CapSlice;
        float src = face.GetWidth(), tall = face.GetHeight();
        on.DrawTextureRectRegion(face, new Rect2(at.X, top, CapSlice, CapHeight), new Rect2(0, 0, end, tall));
        on.DrawTextureRectRegion(face, new Rect2(at.X + CapSlice, top, w - 2 * CapSlice, CapHeight), new Rect2(end, 0, src - 2 * end, tall));
        on.DrawTextureRectRegion(face, new Rect2(at.X + w - CapSlice, top, CapSlice, CapHeight), new Rect2(src - end, 0, end, tall));
        int size = name.Length > 1 ? LongKeySize : KeySize;
        float text = Mono.GetStringSize(name, fontSize: size).X, ascent = Mono.GetAscent(size), descent = Mono.GetDescent(size);
        // Its face's middle sits a little above the cap's, over the lip.
        on.DrawString(Mono, new Vector2(at.X + (w - text) / 2, at.Y - 0.7f + (ascent - descent) / 2), name, HorizontalAlignment.Left, -1, size, Letter);
    }

    private static void DrawMouse(CanvasItem on, Mouse button, Vector2 at, double time)
    {
        string sign = button switch { Mouse.Right or Mouse.RightDrag => "sign_mouseRight", Mouse.Middle => "sign_mouseMiddle", _ => "sign_mouseLeft" };
        var size = Size(sign);
        on.DrawTextureRect(PixelArt.Picture(sign), new Rect2(at.X, at.Y - size.Y / 2, size), false);
        float after = at.X + size.X;
        if (button is Mouse.LeftDrag or Mouse.RightDrag)
        {
            float phase = (float)(time % DragPulse / DragPulse);
            var arrow = Size("sign_drag");
            on.DrawTextureRect(PixelArt.Picture("sign_drag"), new Rect2(after + 1.5f + phase * 1.5f, at.Y - arrow.Y / 2, arrow), false,
                new Color(1, 1, 1, 0.35f + 0.65f * (1 - phase)));
        }
        else if (button == Mouse.LeftHold)
        {
            var ring = Size("sign_holdTrack");
            var rect = new Rect2(after + 0.5f, at.Y - ring.Y / 2, ring);
            on.DrawTextureRect(PixelArt.Picture("sign_holdTrack"), rect, false);
            int step = Mathf.Min(HoldSteps, (int)(time % HoldEvery / HoldFills * HoldSteps));
            if (step > 0) on.DrawTextureRect(PixelArt.Picture($"sign_hold_{step}"), rect, false);
        }
    }

    /// <summary>A sign's logical size, as its texture reports it.</summary>
    private static Vector2 Size(string sign) => PixelArt.Picture(sign).GetSize();
}
