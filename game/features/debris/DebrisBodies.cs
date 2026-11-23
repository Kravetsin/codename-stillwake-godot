using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Stillwake.Game.Shared;
using Stillwake.Net;
using Stillwake.Sim;
using Stillwake.Sim.Debris;
using Spec = Stillwake.Content.Debris;

namespace Stillwake.Game.Features.Debris;

/// <summary>
/// The pieces as rigid bodies on the host player's client (debris-physics): a body a piece of the surface its hero is on, in a physics
/// space of their own, so they meet only each other; the ship and heroes pass through. Each eases back to its piece's drift and tumble
/// (<see cref="PieceShape"/>), and every line the sim holds pulls it by its rope (<see cref="PieceRope"/>), its holder's body middle the
/// anchor (<see cref="AnchorOf"/>). After each tick, the bodies strayed from the sim's poses, and the lines wound or let out, go to it
/// as a <see cref="Moved"/> (<see cref="BodyFeed"/>). Bodies are made and freed as pieces come and go, and made anew from the sim when
/// its world is another (a load, a resync, another surface). Nothing on a guest's client.
/// </summary>
public sealed partial class DebrisBodies : Node
{
    /// <summary>Pixels a thousandth of a cell: the physics runs in the world's pixels, as the prototype's.</summary>
    private const double Px = Camera.Cell / 1000.0;

    private static readonly double TurnsPerRadian = 65536 / (2 * Math.PI);

    private sealed class Body(Rid rid, Rid box, PieceShape shape, Piece piece)
    {
        public readonly Rid Rid = rid, Box = box;
        public readonly PieceShape Shape = shape;
        public Piece Piece = piece;
    }

    private Rid _space;
    private DebrisModule? _debris;
    private readonly Dictionary<int, Body> _bodies = [];
    private readonly Dictionary<Line, PieceRope> _ropes = new(ReferenceEqualityComparer.Instance);
    private int _tick = -1;
    private readonly HashSet<int> _seen = [], _inFlight = [];
    private readonly HashSet<(int, int)> _linesInFlight = [];
    private readonly List<int> _gone = [];
    private readonly List<Line> _dropped = [];
    private readonly List<(int Id, double Stray)> _strays = [];

    /// <summary>Where a peer's body middle is now and how fast it moves, in thousandths of a cell and of a cell a second; none unheard.</summary>
    public Func<int, (double X, double Y, double Vx, double Vy)?> AnchorOf { get; set; } = _ => null;

    /// <summary>
    /// After the session's frame: on the host player's client, the bodies of <paramref name="debris"/>'s pieces, the surface its hero is on,
    /// kept in step with them, and once a tick the feed; the sim's pieces drawn <paramref name="drift"/> ticks on.
    /// </summary>
    public void Frame(Client client, DebrisModule debris, double drift)
    {
        if (client.Id != DebrisModule.HostPlayer)
        {
            Clear();
            return;
        }
        if (!ReferenceEquals(debris, _debris))
        {
            Clear();
            _debris = debris;
        }
        if (client.World.Tick == _tick) return;
        _tick = client.World.Tick;
        Keep(debris, drift);
        Feed(client, debris, drift);
    }

    public override void _PhysicsProcess(double delta)
    {
        if (_debris is not { } debris || _bodies.Count == 0) return;
        var lines = debris.Lines;
        for (int i = 0; i < lines.Count; i++)
        {
            var line = lines[i];
            if (!_bodies.TryGetValue(line.Piece, out var body) || AnchorOf(line.By) is not { } anchor) continue;
            if (!_ropes.TryGetValue(line, out var rope)) _ropes[line] = rope = new PieceRope(line.Length);
            var (x, y, angle) = PoseOf(body);
            var (vx, vy) = VelocityOf(body);
            double spin = SpinOf(body), cos = Math.Cos(angle), sin = Math.Sin(angle);
            var hook = (x + line.HookX * cos - line.HookY * sin, y + line.HookX * sin + line.HookY * cos);
            var at = new RopeBody(x, y, vx, vy, spin, body.Shape.Mass, body.Shape.Inertia);
            if (rope.Step(delta, (anchor.X, anchor.Y), (anchor.Vx, anchor.Vy), hook, at, line.Winds) is { } pull)
                PhysicsServer2D.BodyApplyImpulse(body.Rid, Pixels(pull.X, pull.Y), Pixels(pull.AtX, pull.AtY));
        }
    }

    public override void _ExitTree()
    {
        Clear();
        if (_space.IsValid) PhysicsServer2D.FreeRid(_space);
    }

    /// <summary>Bodies made for pieces come and freed for pieces gone; ropes of lines the sim no longer holds dropped.</summary>
    private void Keep(DebrisModule debris, double drift)
    {
        if (!_space.IsValid)
        {
            _space = PhysicsServer2D.SpaceCreate();
            PhysicsServer2D.SpaceSetActive(_space, true);
        }
        _seen.Clear();
        var pieces = debris.Pieces;
        for (int i = 0; i < pieces.Count; i++)
        {
            var piece = pieces[i];
            _seen.Add(piece.Id);
            if (_bodies.TryGetValue(piece.Id, out var body)) body.Piece = piece;
            else _bodies[piece.Id] = Make(piece, drift);
        }
        _gone.Clear();
        foreach (int id in _bodies.Keys)
            if (!_seen.Contains(id)) _gone.Add(id);
        foreach (int id in _gone)
        {
            Free(_bodies[id]);
            _bodies.Remove(id);
        }
        _dropped.Clear();
        foreach (var line in _ropes.Keys)
            if (!debris.Lines.Contains(line)) _dropped.Add(line);
        foreach (var line in _dropped) _ropes.Remove(line);
    }

    /// <summary>A body for <paramref name="piece"/>, where it is drawn now and moving as the sim moves it, eased back to its drift and its tumble as first seen.</summary>
    private Body Make(Piece piece, double drift)
    {
        var shape = PieceShape.Of(piece.Item);
        var box = PhysicsServer2D.RectangleShapeCreate();
        PhysicsServer2D.ShapeSetData(box, Pixels(shape.HalfW, shape.HalfH));
        var rid = PhysicsServer2D.BodyCreate();
        PhysicsServer2D.BodySetMode(rid, PhysicsServer2D.BodyMode.Rigid);
        PhysicsServer2D.BodySetSpace(rid, _space);
        PhysicsServer2D.BodyAddShape(rid, box);
        PhysicsServer2D.BodySetCollisionLayer(rid, 1);
        PhysicsServer2D.BodySetCollisionMask(rid, 1);
        double inertia = shape.Inertia * Px * Px;
        PhysicsServer2D.BodySetParam(rid, PhysicsServer2D.BodyParameter.Mass, shape.Mass);
        PhysicsServer2D.BodySetParam(rid, PhysicsServer2D.BodyParameter.Inertia, inertia);
        PhysicsServer2D.BodySetParam(rid, PhysicsServer2D.BodyParameter.GravityScale, 0);
        PhysicsServer2D.BodySetParam(rid, PhysicsServer2D.BodyParameter.LinearDampMode, (int)PhysicsServer2D.BodyDampMode.Replace);
        PhysicsServer2D.BodySetParam(rid, PhysicsServer2D.BodyParameter.AngularDampMode, (int)PhysicsServer2D.BodyDampMode.Replace);
        PhysicsServer2D.BodySetParam(rid, PhysicsServer2D.BodyParameter.LinearDamp, PieceShape.DragPerSecond);
        PhysicsServer2D.BodySetParam(rid, PhysicsServer2D.BodyParameter.AngularDamp, PieceShape.DragPerSecond);
        PhysicsServer2D.BodySetState(rid, PhysicsServer2D.BodyState.CanSleep, false);
        // Its own drift and tumble: what the damping and the steady push settle it back to.
        double driftX = -piece.Speed * (double)World.TicksPerSecond, spin = piece.Spin * World.TicksPerSecond / TurnsPerRadian;
        PhysicsServer2D.BodySetConstantForce(rid, Pixels(PieceShape.Push(shape.Mass, driftX), 0));
        PhysicsServer2D.BodySetConstantTorque(rid, (float)PieceShape.Push(inertia, spin));
        var body = new Body(rid, box, shape, piece);
        Set(body, PieceDrawn.Pose(piece, drift), piece.Vx, piece.Vy, piece.Spin);
        return body;
    }

    /// <summary><paramref name="body"/> put at <paramref name="pose"/>, moving <paramref name="vx"/>, <paramref name="vy"/> and turning <paramref name="spin"/> a tick, as the sim keeps them.</summary>
    private static void Set(Body body, PiecePose pose, double vx, double vy, double spin)
    {
        PhysicsServer2D.BodySetState(body.Rid, PhysicsServer2D.BodyState.Transform, new Transform2D((float)pose.Radians, Pixels(pose.X, pose.Y)));
        PhysicsServer2D.BodySetState(body.Rid, PhysicsServer2D.BodyState.LinearVelocity, Pixels(vx * World.TicksPerSecond, vy * World.TicksPerSecond));
        PhysicsServer2D.BodySetState(body.Rid, PhysicsServer2D.BodyState.AngularVelocity, (float)(spin * World.TicksPerSecond / TurnsPerRadian));
    }

    /// <summary>
    /// The feed, once a tick: each body back at the tick's time (<paramref name="drift"/> ticks ago, along its motion) against the sim's
    /// pose, the strays picked (<see cref="BodyFeed.Picked"/>), and each line whose rope is wound away from the sim's length by more than
    /// <c>Debris.FeedSlack</c>; none of them while a feed of it is still on its way.
    /// </summary>
    private void Feed(Client client, DebrisModule debris, double drift)
    {
        _inFlight.Clear();
        _linesInFlight.Clear();
        var flying = client.InFlight;
        for (int i = 0; i < flying.Count; i++)
            if (flying[i] is Moved moved)
            {
                foreach (var b in moved.Bodies) _inFlight.Add(b.Id);
                foreach (var l in moved.Lines) _linesInFlight.Add((l.By, l.Piece));
            }
        _strays.Clear();
        foreach (var (id, body) in _bodies)
        {
            if (_inFlight.Contains(id)) continue;
            var piece = body.Piece;
            _strays.Add((id, BodyFeed.Stray(Back(body, drift), new PiecePose(piece.X, piece.Y, piece.Angle), body.Shape.HalfW, body.Shape.HalfH)));
        }
        var picked = BodyFeed.Picked(_strays);
        var lengths = new List<LineLength>();
        var lines = debris.Lines;
        for (int i = 0; i < lines.Count; i++)
            if (_ropes.TryGetValue(lines[i], out var rope) && !_linesInFlight.Contains((lines[i].By, lines[i].Piece))
                && Math.Abs(rope.Wound - lines[i].Length) > Spec.FeedSlack)
                lengths.Add(new LineLength(lines[i].By, lines[i].Piece, (int)Math.Round(rope.Wound)));
        if (picked.Count == 0 && lengths.Count == 0) return;
        var bodies = new List<Sim.Debris.Body>(picked.Count);
        foreach (int id in picked) bodies.Add(Fed(_bodies[id], drift));
        client.Send(new Moved(debris.Clock, bodies, lengths));
    }

    /// <summary><paramref name="body"/>'s pose <paramref name="drift"/> ticks back along its motion and spin: where it was at the tick's time.</summary>
    private static PiecePose Back(Body body, double drift)
    {
        var (x, y, angle) = PoseOf(body);
        var (vx, vy) = VelocityOf(body);
        double seconds = drift / World.TicksPerSecond;
        return new PiecePose(x - vx * seconds, y - vy * seconds, (angle - SpinOf(body) * seconds) * TurnsPerRadian);
    }

    /// <summary><paramref name="body"/> as the sim keeps a piece, at the tick's time: whole thousandths of a cell, of a cell a tick, 65536ths of a turn and a tick.</summary>
    private static Sim.Debris.Body Fed(Body body, double drift)
    {
        var pose = Back(body, drift);
        var (vx, vy) = VelocityOf(body);
        double perTick = 1.0 / World.TicksPerSecond;
        return new Sim.Debris.Body(body.Piece.Id, (int)Math.Round(pose.X), (int)Math.Round(pose.Y), (int)Math.Round(vx * perTick), (int)Math.Round(vy * perTick),
            (int)Math.Round(pose.Angle) & 0xFFFF, (int)Math.Round(SpinOf(body) * TurnsPerRadian * perTick));
    }

    /// <summary>Where <paramref name="body"/>'s middle is, in thousandths of a cell, and its angle in radians.</summary>
    private static (double X, double Y, double Angle) PoseOf(Body body)
    {
        var t = PhysicsServer2D.BodyGetState(body.Rid, PhysicsServer2D.BodyState.Transform).AsTransform2D();
        return (t.Origin.X / Px, t.Origin.Y / Px, t.Rotation);
    }

    /// <summary>How fast <paramref name="body"/> moves, in thousandths of a cell a second.</summary>
    private static (double X, double Y) VelocityOf(Body body)
    {
        var v = PhysicsServer2D.BodyGetState(body.Rid, PhysicsServer2D.BodyState.LinearVelocity).AsVector2();
        return (v.X / Px, v.Y / Px);
    }

    /// <summary>How fast <paramref name="body"/> turns, in radians a second, clockwise as the screen shows it.</summary>
    private static double SpinOf(Body body) => PhysicsServer2D.BodyGetState(body.Rid, PhysicsServer2D.BodyState.AngularVelocity).AsDouble();

    private static Vector2 Pixels(double x, double y) => new((float)(x * Px), (float)(y * Px));

    private void Free(Body body)
    {
        PhysicsServer2D.FreeRid(body.Rid);
        PhysicsServer2D.FreeRid(body.Box);
    }

    /// <summary>Every body and rope gone: not the host player, or another world.</summary>
    private void Clear()
    {
        foreach (var body in _bodies.Values) Free(body);
        _bodies.Clear();
        _ropes.Clear();
        (_debris, _tick) = (null, -1);
    }
}
