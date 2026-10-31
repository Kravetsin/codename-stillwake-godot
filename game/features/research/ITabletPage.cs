using Godot;
using Stillwake.Sim;

namespace Stillwake.Game.Features.Research;

/// <summary>A tab of the research tablet: what <see cref="ResearchTablet"/> asks of whatever it shows there.</summary>
public interface ITabletPage
{
    /// <summary>Puts it on the screen, read afresh, or takes it off, nothing left picked.</summary>
    void Show(bool shown);

    /// <summary>A frame while it is the page on the screen.</summary>
    void Step(double delta);

    /// <summary>A read of the world, on the tablet's timer, on the screen or not.</summary>
    void Read(Registry m);

    /// <summary>The pointer over the view: what the page makes of a press, a drag and the wheel.</summary>
    void Pointer(InputEvent e);

    /// <summary>The wheel over <paramref name="over"/>, the view or the bezel; anything else is ignored.</summary>
    void Zoom(InputEvent e, Control over);

    /// <summary>Fills the strip below for what is picked.</summary>
    void Fill();

    /// <summary>Forgets a press under way, as the tablet goes away: its release comes to nothing.</summary>
    void LetGo();
}
