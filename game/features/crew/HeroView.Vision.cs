using System;
using Godot;
using Stillwake.Game.Shared;
using Stillwake.Net;
using Stillwake.Sim;

namespace Stillwake.Game.Features.Crew;

/// <summary>
/// A form's vision (form-vision): as the research key's lesson reaches it, this player's own hero is held where it is, floating, the HUD gone, while
/// the drone shows it what the form does (<see cref="FormVisionView"/>). The sim spares the hero meanwhile, told once by <c>Envision</c> as it
/// begins. Its own drone by the head is hidden while the vision's drone flies; its figure is moved onto the vision's layer. Friends see nothing.
/// </summary>
public partial class HeroView
{
    /// <summary>A vision playing: the form, its view, the hero as it was held, and where its drone hung by the head as it began, world px.</summary>
    private sealed class Envisioned(string form, VisionForm kind, FormVisionView view, Hero held, Vector2 home)
    {
        public readonly string Form = form;
        public readonly VisionForm Kind = kind;
        public readonly FormVisionView View = view;
        public readonly Hero Held = held;
        public readonly Vector2 Home = home;
        public double U;
    }

    private Envisioned? _vision;

    /// <summary>How much brighter the hero's figure is drawn in the void than its pictures.</summary>
    private const float Lifted = 1.25f;

    /// <summary>The form whose vision this player's lesson is at, and how many seconds into it (form-vision, from the research key); null for none.</summary>
    public (string Form, double Seconds)? DroneVision { private get; set; }

    /// <summary>A dev run's visions (<c>--form-vision</c>), given the ms since the last frame: read in place of <see cref="DroneVision"/> when set.</summary>
    public Func<double, (string Form, double Seconds)?>? VisionScript { private get; set; }

    /// <summary>
    /// Whether this player's hero can have a form's vision now: one that is there and not passed out, not in its drone's meeting. A lesson whose
    /// study ends while it cannot skips its vision.
    /// </summary>
    public bool CanEnvision => _m != null && _client != null && _m.Crew.EnvisionProblem(_client.Id) == null && !_meetings.ContainsKey(_client.Id);

    /// <summary>Whether this player is in a form's vision this frame.</summary>
    public bool Envisions => _vision != null;

    /// <summary>How this player's vision's layer is zoomed over the screen this frame, the world to the screen; none outside one. The scanner's gauge follows it.</summary>
    public Transform2D? VisionLayer => _vision?.View.Vision.Transform;

    /// <summary>
    /// What the scanner reads in a scanner's vision this frame: an id of its own for each piece, over the middle of its top in cells, its hit points
    /// left of the most; none outside one.
    /// </summary>
    public (int Id, double X, double Y, int Left, int Max)? VisionRead => _vision?.View.Read;

    /// <summary>
    /// Starts, plays on or ends this player's vision as its lesson says, before the hero steps: a vision begins held where the hero is, its speed
    /// gone, and tells the sim once; one of another form starts over; none, ended, or the hero gone, it ends.
    /// </summary>
    private void Envision(Client client, Registry m, Sim.Crew.Hero? known, double elapsedMs)
    {
        var wanted = VisionScript != null ? VisionScript(elapsedMs) : DroneVision;
        if (_vision is { } playing && (wanted is not { } now || now.Form != playing.Form || !FormVision.Playing(now.Seconds) || known == null))
            EndVision();
        if (wanted is not { } shown || !FormVision.Playing(shown.Seconds) || known == null || FormVision.Of(shown.Form) is not { } kind) return;
        if (_vision == null)
        {
            if (_meetings.ContainsKey(client.Id) || known.Out > 0) return;
            var view = new FormVisionView();
            AddChild(view);
            _vision = new Envisioned(shown.Form, kind, view, _current with { Vx = 0, Vy = 0 }, _figure.DroneAnchor);
            if (m.Crew.EnvisionProblem(client.Id) == null) client.Send(new Sim.Crew.Envision());
        }
        _vision.U = shown.Seconds;
    }

    private void EndVision()
    {
        if (_vision is not { } ended) return;
        _vision = null;
        Place(_figure, this);
        _figure.Modulate = Colors.White;
        ended.View.QueueFree();
    }

    /// <summary>The hero's figure as the vision holds it, before it is shown: kneeling a little and leaning as it floats, its head turned to where the drone looks, its own drone hidden.</summary>
    private (Pose Pose, double Look) InVision(Figure figure, Hero hero, Pose pose, double look)
    {
        if (_vision is not { } vision) return (pose, look);
        var frame = FormVision.At(vision.Kind, vision.U);
        var (_, kneel, lean) = FormVision.Float(vision.U);
        figure.Kneel = new Kneeling(kneel, lean);
        figure.DroneShown = false;
        var aim = FormVisionView.AimOf(frame, FeetOf(hero), hero.Facing);
        var neck = FeetOf(hero) - new Vector2(0, (float)(Rig.Neck + FormVision.Float(vision.U).Lift));
        return (Pose.Kneel, frame.Veil * Poses.HeadTurn((aim.X - neck.X) * hero.Facing, aim.Y - neck.Y));
    }

    /// <summary>Draws this player's vision after its figure is shown: the figure moved onto the vision and risen as it floats, the HUD as the vision has it.</summary>
    private void ShowVision(Figure figure, Hero hero, double elapsedMs)
    {
        if (_vision is not { } vision) return;
        var frame = FormVision.At(vision.Kind, vision.U);
        Place(figure, vision.View.Vision);
        figure.Transform = new Transform2D(0, new Vector2(hero.Facing, 1), 0, FeetOf(hero) - new Vector2(0, (float)FormVision.Float(vision.U).Lift));
        vision.View.Show(frame, FeetOf(hero), hero.Facing, vision.Home, elapsedMs);
        Cutscene = (0, frame.Hud);
        // Off the world's lights his dark suit would sink into the void: brightened a little as it comes.
        figure.Modulate = Colors.White.Lerp(new Color(Lifted, Lifted, Lifted), (float)frame.Veil);
    }
}
