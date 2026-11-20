using System.Collections.Generic;
using Godot;
using Stillwake.Game.Render;
using Stillwake.Game.Shared;
using Stillwake.Net;
using Stillwake.Sim;

namespace Stillwake.Game.Features.Crew;

/// <summary>
/// The drone's first meeting with its hero (drone-meeting), played here as each hero pairs: this one at its E on the core, held
/// kneeling and its hands still till it ends; another as it is seen turning paired here, the core not lost, never one already
/// paired when first seen. The sim paired it at the press; the meeting is the client's alone.
/// </summary>
public partial class HeroView
{
    /// <summary>A meeting playing: when it began on <c>_clockMs</c>, the core where it lay, the side its hero kneels on, its view, and this frame's state.</summary>
    private sealed class Met(double startMs, Vector2 core, int side, MeetingView view)
    {
        public readonly double StartMs = startMs;
        public readonly Vector2 Core = core;
        public readonly int Side = side;
        public readonly MeetingView View = view;
        public double U, ClockS;
        public Vector2 Drone;
        public float Angle;
    }

    private readonly Dictionary<int, Met> _meetings = [];
    private readonly MeetingWatch _watch = new();
    /// <summary>Whether this hero is held by its own meeting this frame: no keys, hands, use or clicks.</summary>
    private bool _locked;

    /// <summary>A dev run's meeting (<c>--meeting</c>): played on this hero as it is placed, that many seconds in, without pairing.</summary>
    public double? MeetAt { get; set; }

    /// <summary>How far into the last breath this hero's own meeting swims the screen, 0 without one (<see cref="Meeting.Dizzy"/>).</summary>
    public double Dizzy { get; private set; }

    /// <summary>Starts this hero's meeting <paramref name="atS"/> seconds in, turning it to face the core.</summary>
    private void MeetHere(Client client, Registry m, double atS = 0)
    {
        if (!Meet(client.Id, _current, m, atS)) return;
        int faces = -_meetings[client.Id].Side;
        (_previous, _current) = (_previous with { Facing = faces }, _current with { Facing = faces });
        _ui.CoreTaken = true;
    }

    /// <summary>Starts <paramref name="id"/>'s meeting at the core nearest its <paramref name="hero"/>; whether there is one.</summary>
    private bool Meet(int id, Hero hero, Registry m, double atS = 0)
    {
        if (_meetings.ContainsKey(id)) return true;
        var feet = FeetOf(hero);
        Vector2? core = null;
        foreach (var machine in m.Grid.MachinesWith(Content.MachineTrait.Wakes))
        {
            var middle = new Vector2(machine.Cell % m.Grid.Width, machine.Cell / m.Grid.Width) * ShipCamera.Cell
                + new Vector2((float)Meeting.InPicture.X, (float)Meeting.InPicture.Y);
            if (core is not { } best || middle.DistanceSquaredTo(feet) < best.DistanceSquaredTo(feet)) core = middle;
        }
        if (core is not { } at) return false;
        var view = new MeetingView();
        AddChild(view);
        _meetings[id] = new Met(_clockMs - atS * 1000, at, feet.X >= at.X ? 1 : -1, view);
        return true;
    }

    private void EndMeeting(int id)
    {
        if (!_meetings.Remove(id, out var met)) return;
        met.View.QueueFree();
    }

    /// <summary>
    /// <paramref name="figure"/> as its hero's meeting, if any, bends it this frame, before it is shown: kneeling where it stands, the near
    /// hand to the core, the head after it and the drone; its drone hidden while the meeting draws it, then flown to its place. Lying
    /// or afloat it only watches. The pose and look to show it in.
    /// </summary>
    private (Pose Pose, double Look) InMeeting(int id, Figure figure, Hero hero, Pose pose, double look)
    {
        if (_meetings.TryGetValue(id, out var met) && (_clockMs - met.StartMs) / 1000 >= Meeting.End) EndMeeting(id);
        if (!_meetings.TryGetValue(id, out met))
        {
            (figure.Kneel, figure.Reach, figure.DroneShown, figure.DroneTo) = (null, null, null, null);
            return (pose, look);
        }
        double u = met.U = (_clockMs - met.StartMs) / 1000;
        met.ClockS = Time.GetTicksMsec() / 1000.0;
        var helm = figure.HeadAt;
        var idle = helm + new Vector2(-hero.Facing * (float)DroneFloat.Behind, -(float)DroneFloat.Above);
        var drone = Meeting.DroneAt(u, new Vec(met.Core.X, met.Core.Y), met.Side, new Vec(idle.X, idle.Y), met.ClockS);
        met.Drone = new Vector2((float)drone.X, (float)drone.Y);
        met.Angle = (float)Meeting.AngleOf(u, drone, new Vec(helm.X, helm.Y), figure.HeadLampToward.Angle());
        bool kneels = pose is not (Pose.Lie or Pose.Tumble or Pose.Float);
        figure.Kneel = new Kneeling(Meeting.Kneel(u), Meeting.Lean(u));
        figure.Reach = kneels ? (met.Core, Meeting.Reach(u)) : null;
        figure.DroneShown = !Meeting.DrawsDrone(u);
        figure.DroneTo = Meeting.DrawsDrone(u) ? null : (met.Drone, Vector2.FromAngle(met.Angle));
        double toDrone = Poses.HeadTurn((met.Drone.X - helm.X) * hero.Facing, met.Drone.Y - helm.Y);
        return kneels ? (Pose.Kneel, Meeting.Look(u, toDrone)) : (pose, look);
    }

    /// <summary>Draws <paramref name="id"/>'s meeting after its figure is shown, from its palm as drawn.</summary>
    private void ShowMeeting(int id, Figure figure, Registry m)
    {
        if (!_meetings.TryGetValue(id, out var met)) return;
        bool pit = m.Crew.CoreLost() || m.Players.HasDrone(_client!.Id) || _ui.CoreTaken;
        met.View.Show(new MeetingFrame(met.U, met.ClockS, figure.ClockMs / 1000, met.Core, met.Side, figure.Palm, met.Drone, met.Angle,
            figure.DroneLensColour, pit));
    }

    /// <summary>What the meetings tell the rest of the client this frame: the screen's swim, and how long the core still lies in one.</summary>
    private void HoldCore(int own)
    {
        Dizzy = _meetings.TryGetValue(own, out var mine) ? Meeting.Dizzy(mine.U) : 0;
        double? lies = null;
        foreach (var met in _meetings.Values)
            if (Meeting.CoreLies(met.U) && (lies == null || met.U < lies)) lies = met.U;
        _ui.CoreMeeting = lies;
    }

    private static Vector2 FeetOf(Hero hero) => new((float)((double)hero.X / Hero.Unit * Rig.Cell), (float)((double)hero.Y / Hero.Unit * Rig.Cell));
}
