using Godot;

namespace Stillwake.Game.Shared;

/// <summary>
/// A screen of the wrist computer (0074): a panel whose background is the screen (<see cref="WristArt.DrawScreen"/>),
/// framed by its piece of metal, which takes the clicks on the bezel; with <c>lit</c>, a faint wash of the screen's blue
/// round it and its lamp's glow, as the build tablet's (0073). Its padding and what it holds are as any panel's.
/// </summary>
public partial class WristScreen : PanelContainer
{
    private static readonly Color Light = new(70 / 255f, 150 / 255f, 230 / 255f, 0.12f), LedLight = new(74 / 255f, 222 / 255f, 128 / 255f, 0.35f);
    /// <summary>How much wider and taller than the screen its light reaches, in screen pixels.</summary>
    private static readonly Vector2 Reach = new(320, 200);

    private readonly NinePatchRect _art;
    private readonly Color _screen;
    private readonly TextureRect? _light, _led;
    private WristPiece _piece;
    private float _power = 1;

    /// <param name="piece">Its metal.</param>
    /// <param name="screen">Its screen's colour.</param>
    /// <param name="padding">Between the screen's edge and what it shows, in screen pixels.</param>
    /// <param name="lit">Whether it lights what is round it, and its lamp glows.</param>
    public WristScreen(WristPiece piece, Color screen, int padding, bool lit = false)
    {
        _piece = piece;
        _screen = screen;
        MouseFilter = MouseFilterEnum.Stop;
        var box = new StyleBoxEmpty();
        box.SetContentMarginAll(padding);
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

    /// <summary>Its metal: the forearm display's bottom edge changes as a machine opens under it.</summary>
    public WristPiece Piece
    {
        get => _piece;
        set
        {
            if (value == _piece) return;
            _piece = value;
            WristArt.Use(_art, value);
            Place();
        }
    }

    /// <summary>Where the screen and its metal lie on the screen: what the crew's markers keep out of.</summary>
    public Rect2 Outer => _art.GetGlobalRect();

    /// <summary>What the screen shows, and its light, at <paramref name="power"/> of lit: 0 dark, 1 on (<see cref="TabletMotion.Power"/>).</summary>
    public void Lit(float power)
    {
        if (power == _power) return;
        _power = power;
        var shown = Colors.White with { A = power };
        foreach (var child in GetChildren())
            if (child is CanvasItem item && child != _art) item.Modulate = shown;
    }

    private void Place()
    {
        WristArt.Fit(_art, _piece, Size);
        if (_light != null)
        {
            _light.Size = Size + Reach;
            _light.Position = -Reach / 2;
        }
        if (_led != null && _piece.Led is var (fromRight, down))
        {
            int s = WristPiece.Scale;
            _led.Size = new Vector2(18, 18);
            _led.Position = new Vector2(Size.X + (_piece.Window.Right - fromRight) * s, (down - _piece.Window.Top) * s) - _led.Size / 2;
        }
    }

    public override void _Draw() => WristArt.DrawScreen(this, Size, _screen);
}
