using Godot;
using Stillwake.Game.Shared;
using S = Stillwake.Game.Sound;

namespace Stillwake.Game.Features.Landing;

/// <summary>
/// The ship's flight on every peer's screen, wherever its hero is (solar-system): the call when a course is set with the crew
/// away, an amber light along the screen's edge and a beep; the burn as a course sets off, a white-hot one and a blast; a thud on
/// arrival. Wordless. Stand-ins until the user's picks, every one a one-shot.
/// </summary>
public partial class JumpCall : CanvasLayer
{
    private const double CallS = 1.4, BurnS = 2.2;
    private const float Band = 64;
    private static readonly Color CallColour = new("f5b041"), BurnColour = new("ffe9c4");

    private readonly AudioStreamPlayer _beep = Cues.Player(S.Alarm, S.Ui), _blast = Cues.Player(S.PodFlash, S.Ui), _thud = Cues.Player(S.PodTouchdown, S.Ui);
    private readonly Edge _edge = new();
    private double _left, _lasts = 1;

    public JumpCall()
    {
        Layer = 90;
        AddChild(_edge);
        AddChild(_beep);
        AddChild(_blast);
        AddChild(_thud);
    }

    /// <summary>Someone set a course with the crew away: come home. One showing already is let run, so a catch-up's many refusals call once.</summary>
    public void Call()
    {
        if (_left > 0) return;
        Show(CallColour, CallS);
        _beep.Play();
    }

    /// <summary>A course set off: the planet below is left.</summary>
    public void Burn()
    {
        Show(BurnColour, BurnS);
        _blast.Play();
    }

    /// <summary>The ship arrived at its planet.</summary>
    public void Arrive() => _thud.Play();

    private void Show(Color colour, double seconds)
    {
        (_edge.Colour, _left, _lasts) = (colour, seconds, seconds);
        _edge.QueueRedraw();
    }

    public override void _Process(double delta)
    {
        if (_left <= 0) return;
        _left -= delta;
        // Two pulses for a call, fading out; the burn one swell.
        double along = 1 - _left / _lasts;
        _edge.Strength = _left <= 0 ? 0 : (float)((1 - along) * (0.55 + 0.45 * Mathf.Cos(along * Mathf.Tau * 2)));
        _edge.QueueRedraw();
    }

    private sealed partial class Edge : Control
    {
        public Color Colour;
        public float Strength;

        public Edge()
        {
            SetAnchorsPreset(LayoutPreset.FullRect);
            MouseFilter = MouseFilterEnum.Ignore;
        }

        public override void _Draw()
        {
            if (Strength <= 0) return;
            var size = GetViewportRect().Size;
            Color on = Colour with { A = 0.7f * Strength }, off = Colour with { A = 0 };
            Fade(new Rect2(0, 0, size.X, Band), on, on, off, off);
            Fade(new Rect2(0, size.Y - Band, size.X, Band), off, off, on, on);
            Fade(new Rect2(0, 0, Band, size.Y), on, off, off, on);
            Fade(new Rect2(size.X - Band, 0, Band, size.Y), off, on, on, off);
        }

        /// <summary>A rectangle coloured by its corners: top-left, top-right, bottom-right, bottom-left.</summary>
        private void Fade(Rect2 r, Color tl, Color tr, Color br, Color bl) =>
            DrawPolygon([r.Position, new(r.End.X, r.Position.Y), r.End, new(r.Position.X, r.End.Y)], [tl, tr, br, bl]);
    }
}
