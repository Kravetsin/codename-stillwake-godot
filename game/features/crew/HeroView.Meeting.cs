using System.Collections.Generic;
using Godot;
using Stillwake.Game.Render;
using Stillwake.Game.Shared;
using Stillwake.Net;
using Stillwake.Sim;

namespace Stillwake.Game.Features.Crew;

/// <summary>
/// The drone's first meeting with its hero (drone-meeting). This player's own plays at its E on the core as the vision it picked: held
/// still till it ends, the HUD hidden and the scene graded, its figure moved onto the vision while it lasts. A friend seen turning
/// paired here, the core not lost (never one already paired when first seen), just stands where it pressed E for the scene's length,
/// its drone hidden, then fading in by its head as the scene ends there; the core keeps lying for this player. The sim paired it at
/// the press and spares it meanwhile; the meeting is the client's alone.
/// </summary>
public partial class HeroView
{
    /// <summary>A meeting playing: when it began on <c>_clockMs</c>; for this player's own, its scene and view; and how far in it is this frame.</summary>
    private sealed class Met(double startMs, MeetingScene? scene, MeetingView? view)
    {
        public readonly double StartMs = startMs;
        public readonly MeetingScene? Scene = scene;
        public readonly MeetingView? View = view;
        public double U;
    }

    /// <summary>How long a friend's new drone takes to fade in by its head once its meeting ends there, seconds.</summary>
    private const double FadeInS = 0.4;

    private readonly Dictionary<int, Met> _meetings = [];
    private readonly MeetingWatch _watch = new();
    /// <summary>Whether this hero is held by its own meeting this frame: no keys, hands, use or clicks.</summary>
    private bool _locked;
    /// <summary>Where this hero's figure stands among this view's children, to go back to from the vision.</summary>
    private int _figureAt = -1;

    /// <summary>A dev run's meeting (<c>--meeting</c>): played on this hero as it is placed, that many seconds in, without pairing.</summary>
    public double? MeetAt { get; set; }

    /// <summary>How far into the last breath this hero's own meeting swims the screen, 0 without one (<see cref="Meeting.Dizzy"/>).</summary>
    public double Dizzy { get; private set; }

    /// <summary>This player's cutscene: its grade, 0..1, and how much of the HUD shows, 0..1; (0, 1) without one.</summary>
    public (double Grade, double Hud) Cutscene { get; private set; } = (0, 1);

    /// <summary>Starts this hero's own meeting at the core nearest it, <paramref name="atS"/> seconds in (E's moment by default), turning it to face the core.</summary>
    private void MeetHere(Client client, Registry m, double? atS = null)
    {
        if (_meetings.ContainsKey(client.Id)) return;
        var feet = FeetOf(_current);
        if (CoreNear(m, feet) is not { } core) return;
        int side = feet.X >= core.X ? 1 : -1;
        double u = atS ?? Meeting.Times.Press;
        var scene = new MeetingScene(new Vec(core.X, core.Y), new Vec(feet.X, feet.Y), side, _figure.ClockMs / 1000 - u);
        var view = new MeetingView(scene);
        AddChild(view);
        _meetings[client.Id] = new Met(_clockMs - u * 1000, scene, view);
        (_previous, _current) = (_previous with { Facing = -side }, _current with { Facing = -side });
        _ui.CoreTaken = true;
    }

    /// <summary>Starts friend <paramref name="id"/>'s meeting as seen here: E's moment now.</summary>
    private void MeetFriend(int id)
    {
        if (!_meetings.ContainsKey(id)) _meetings[id] = new Met(_clockMs - Meeting.Times.Press * 1000, null, null);
    }

    /// <summary>The middle of the alien core nearest <paramref name="feet"/> as it lies, world pixels; none on a map without one.</summary>
    private static Vector2? CoreNear(Registry m, Vector2 feet)
    {
        Vector2? core = null;
        foreach (var machine in m.Grid.MachinesWith(Content.MachineTrait.Wakes))
        {
            var middle = new Vector2(machine.Cell % m.Grid.Width, machine.Cell / m.Grid.Width) * ShipCamera.Cell
                + new Vector2((float)Meeting.InPicture.X, (float)Meeting.InPicture.Y);
            if (core is not { } best || middle.DistanceSquaredTo(feet) < best.DistanceSquaredTo(feet)) core = middle;
        }
        return core;
    }

    private void EndMeeting(int id, Figure? figure = null)
    {
        if (!_meetings.Remove(id, out var met) || met.View is not { } view) return;
        if (figure != null) Place(figure, this);
        view.QueueFree();
    }

    /// <summary>
    /// <paramref name="figure"/> as its hero's meeting, if any, holds it this frame, before it is shown: this player's own standing, then
    /// floating in the vision with its palm held out to the core, its head turned to it, its drone hidden while the meeting draws it; a
    /// friend's as it is, its drone hidden till the scene ends there, then faded in. Lying or tumbling it only watches. The pose and look
    /// to show it in.
    /// </summary>
    private (Pose Pose, double Look) InMeeting(int id, Figure figure, Hero hero, Pose pose, double look)
    {
        if (_meetings.TryGetValue(id, out var met))
        {
            met.U = (_clockMs - met.StartMs) / 1000;
            if (met.U >= Meeting.End + (met.Scene == null ? FadeInS : 0)) EndMeeting(id, figure);
        }
        if (!_meetings.TryGetValue(id, out met))
        {
            (figure.Kneel, figure.Reach, figure.DroneShown, figure.DroneTo, figure.DroneFade) = (null, null, null, null, 1);
            return (pose, look);
        }
        double u = met.U;
        if (met.Scene is not { } scene)
        {
            figure.DroneShown = u < Meeting.End ? false : null;
            figure.DroneFade = (float)Meeting.Ss(u, Meeting.End, Meeting.End + FadeInS);
            return (pose, look);
        }
        var held = scene.HeroAt(u);
        // Shown standing first: the vision's turn is put back on after (ShowMeeting).
        figure.Rotation = 0;
        var idle = Idle(figure, hero.Facing);
        var core = scene.CoreAt(u, new Vec(idle.X, idle.Y), figure.HeadLampToward.Angle());
        // The arm and head are bent in the figure's own standing frame; in the vision it is then moved, scaled and turned whole.
        var toStanding = Standing(hero) * At(held, hero.Facing).AffineInverse();
        var to = toStanding * new Vector2((float)core.At.X, (float)core.At.Y);
        var helm = FeetOf(hero) + new Vector2(0, -(float)Rig.Neck);
        bool stands = pose is not (Pose.Lie or Pose.Tumble);
        figure.Kneel = new Kneeling(held.Kneel, held.Lean);
        figure.Reach = stands && held.Reach > 0 ? (to, held.Reach) : null;
        figure.DroneShown = Meeting.DrawsDrone(u) ? false : null;
        (figure.DroneTo, figure.DroneFade) = (null, 1);
        return stands ? (Pose.Kneel, Poses.HeadTurn((to.X - helm.X) * hero.Facing, to.Y - helm.Y)) : (pose, look);
    }

    /// <summary>Draws this player's meeting after its figure is shown: the figure moved onto the vision while it is up, the meeting from its palm as drawn.</summary>
    private void ShowMeeting(int id, Figure figure, Registry m)
    {
        if (!_meetings.TryGetValue(id, out var met) || met.Scene is not { } scene || met.View is not { } view) return;
        double u = met.U;
        var shown = figure.GlobalTransform;
        var palm = figure.Palm;
        int facing = -scene.Side;
        if (Meeting.PhaseOf(u) is MeetingPhase.Into or MeetingPhase.Vision or MeetingPhase.Back)
        {
            Place(figure, view.Vision);
            figure.Transform = At(scene.HeroAt(u), facing);
            palm = figure.GlobalTransform * (shown.AffineInverse() * palm);
        }
        else Place(figure, this);
        bool pit = m.Crew.CoreLost() || m.Players.HasDrone(_client!.Id) || _ui.CoreTaken;
        view.Show(new MeetingFrame(u, Time.GetTicksMsec() / 1000.0, figure.ClockMs / 1000, palm, Idle(figure, facing), figure.HeadLampToward.Angle(),
            figure.DroneLensColour, pit));
    }

    /// <summary>What this player's meeting tells the rest of the client this frame: the screen's swim, the cutscene, how long the core still lies.</summary>
    private void HoldCore(int own)
    {
        bool meets = _meetings.TryGetValue(own, out var mine) && mine.Scene != null;
        double u = meets ? mine!.U : 0;
        Dizzy = meets ? Meeting.Dizzy(u) : 0;
        Cutscene = meets ? (Meeting.Grade(u), Meeting.Hud(u)) : (0, 1);
        _ui.CoreMeeting = meets && Meeting.CoreLies(u) ? u : null;
    }

    /// <summary>Moves this hero's <paramref name="figure"/> onto <paramref name="parent"/>, back to its place among this view's children when that is this view.</summary>
    private void Place(Figure figure, Node parent)
    {
        if (figure != _figure || figure.GetParent() == parent) return;
        if (figure.GetParent() == this) _figureAt = figure.GetIndex();
        figure.GetParent().RemoveChild(figure);
        parent.AddChild(figure);
        if (parent == this && _figureAt >= 0) MoveChild(figure, _figureAt);
        if (parent == this) figure.Rotation = 0;
    }

    /// <summary>Where the drone floats by the head of <paramref name="figure"/> facing <paramref name="facing"/>, as drawn last.</summary>
    private static Vector2 Idle(Figure figure, int facing) => figure.HeadAt + new Vector2(-facing * (float)DroneFloat.Behind, -(float)DroneFloat.Above);

    /// <summary>The figure's frame standing at <paramref name="hero"/>'s feet, as <see cref="Figure.Show"/> places it.</summary>
    private static Transform2D Standing(Hero hero) => new(0, new Vector2(hero.Facing, 1), 0, FeetOf(hero));

    /// <summary>The figure's frame as the meeting holds it: at its feet, its size and turn, facing <paramref name="facing"/>.</summary>
    private static Transform2D At(MeetingHero held, int facing) =>
        new((float)held.Turn, new Vector2(facing * (float)held.Scale, (float)held.Scale), 0, new Vector2((float)held.Feet.X, (float)held.Feet.Y));

    private static Vector2 FeetOf(Hero hero) => new((float)((double)hero.X / Hero.Unit * Rig.Cell), (float)((double)hero.Y / Hero.Unit * Rig.Cell));
}
