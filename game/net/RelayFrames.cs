using System;
using System.Text.Json.Nodes;

namespace Stillwake.Game.Net;

/// <summary>
/// The playtest relay's frames (ADR 0012, packages/net/src/relay.ts of the TypeScript game): JSON
/// <c>{ event, data }</c> over a WebSocket. The relay routes them without reading what they carry, so
/// this game's Protocol messages ride inside as JSON text. Plain .NET, tested without Godot.
/// </summary>
public static class RelayFrames
{
    public const string Host = "stillwake:host", Hosted = "stillwake:hosted", Join = "stillwake:join",
        Connected = "stillwake:connected", Refused = "stillwake:refused", Joined = "stillwake:joined",
        Left = "stillwake:left", HostLeft = "stillwake:host-left", ToHost = "stillwake:to-host",
        FromPeer = "stillwake:from-peer", ToPeer = "stillwake:to-peer", FromHost = "stillwake:from-host";

    /// <summary>Where the playtest server's relay answers.</summary>
    public const string PlaytestUrl = "wss://stillwake.toss-it.org/relay";

    /// <summary>A frame received: its event, the peer it names if any, and the message it carries if any.</summary>
    public readonly record struct Frame(string Event, int? Peer, string? Message, string? Session);

    /// <summary>Opens a session named <paramref name="name"/>, or numbered by the relay when it is taken or none.</summary>
    public static string HostFrame(string? name) => Of(Host, name == null ? new JsonObject() : new JsonObject { ["name"] = name });

    /// <summary>Joins session <paramref name="session"/>; the relay answers with the token it was sent.</summary>
    public static string JoinFrame(string session) => Of(Join, new JsonObject { ["session"] = session, ["token"] = 1 });

    /// <summary>A guest's message to its host.</summary>
    public static string ToHostFrame(string message) => Of(ToHost, new JsonObject { ["message"] = message });

    /// <summary>A host's message to its guest <paramref name="peer"/>.</summary>
    public static string ToPeerFrame(int peer, string message) => Of(ToPeer, new JsonObject { ["peer"] = peer, ["message"] = message });

    /// <summary>What a frame from the relay says; null for one that is not JSON or names no event.</summary>
    public static Frame? Read(string text)
    {
        JsonNode? root;
        try
        {
            root = JsonNode.Parse(text);
        }
        catch (System.Text.Json.JsonException)
        {
            return null;
        }
        if (root?["event"]?.GetValue<string>() is not { } ev) return null;
        var data = root["data"];
        int? peer = data?["peer"] is JsonValue p && p.TryGetValue(out int id) ? id : null;
        string? message = data?["message"] is JsonValue m && m.TryGetValue(out string? carried) ? carried : null;
        string? session = data?["session"] is JsonValue s && s.TryGetValue(out string? name) ? name : null;
        return new Frame(ev, peer, message, session);
    }

    /// <summary>The Authorization header the playtest server asks of every socket: any user, the password.</summary>
    public static string Authorization(string password) =>
        "Authorization: Basic " + Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(":" + password));

    private static string Of(string ev, JsonObject data) => new JsonObject { ["event"] = ev, ["data"] = data }.ToJsonString();
}
