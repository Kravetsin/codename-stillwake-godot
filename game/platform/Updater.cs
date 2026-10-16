using System;
using System.IO;
using Godot;

namespace Stillwake.Game.Platform;

/// <summary>
/// Asks GitHub once at the start whether a newer release is out (<see cref="Updates"/>), and on
/// <see cref="Update"/> fetches its zip, unpacks it beside the user's saves, and hands over to a script
/// that waits for the game to close, puts the new build over this one and starts it. Only an exported
/// release looks: the editor and desk builds have no folder of their own to replace.
/// </summary>
public partial class Updater : Node
{
    private readonly Updates.Version _own;
    private readonly HttpRequest _http = new() { Timeout = 30 };
    private Updates.Release? _latest;

    public Updater(Updates.Version own) => _own = own;

    /// <summary>A newer release to update to, once GitHub has said so.</summary>
    public Updates.Release? Available { get; private set; }

    /// <summary>How the update stands, in a few words, while one runs; none otherwise.</summary>
    public string? Status { get; private set; }

    public override void _Ready()
    {
        AddChild(_http);
        if (OS.HasFeature("editor") || _own.Release == null) return;
        _http.RequestCompleted += Checked;
        _http.Request(Updates.LatestUrl, ["User-Agent: Stillwake", "Accept: application/vnd.github+json"]);
    }

    private void Checked(long result, long code, string[] headers, byte[] body)
    {
        _http.RequestCompleted -= Checked;
        if (result != (long)HttpRequest.Result.Success || code != 200) return;
        _latest = Updates.Read(System.Text.Encoding.UTF8.GetString(body));
        if (_latest is { } r && Updates.Newer(r.Tag, _own.Release)) Available = r;
    }

    /// <summary>Fetches the newer release and restarts into it.</summary>
    public void Update()
    {
        if (Available is not { } release || Status != null) return;
        Status = $"downloading {release.Tag}…";
        string folder = ProjectSettings.GlobalizePath("user://update");
        if (Directory.Exists(folder)) Directory.Delete(folder, recursive: true);
        Directory.CreateDirectory(folder);
        _http.DownloadFile = Path.Combine(folder, "Stillwake.zip");
        _http.RequestCompleted += Downloaded;
        // Large releases take a while: no timeout on the download itself.
        _http.Timeout = 0;
        _http.Request(release.Zip, ["User-Agent: Stillwake"]);
    }

    /// <summary>Run before the game quits to restart into the update: the world saved first.</summary>
    public Action? BeforeRestart { get; set; }

    private void Downloaded(long result, long code, string[] headers, byte[] body)
    {
        _http.RequestCompleted -= Downloaded;
        if (result != (long)HttpRequest.Result.Success || code != 200)
        {
            Status = $"the download failed ({code})";
            return;
        }
        try
        {
            string folder = ProjectSettings.GlobalizePath("user://update");
            string files = Path.Combine(folder, "files");
            Unpack(Path.Combine(folder, "Stillwake.zip"), files);
            string exe = OS.GetExecutablePath();
            string script = Path.Combine(folder, "update.cmd");
            File.WriteAllText(script, Updates.Script(OS.GetProcessId(), files, Path.GetDirectoryName(exe)!, exe));
            OS.CreateProcess("cmd.exe", ["/c", script]);
            BeforeRestart?.Invoke();
            GetTree().Quit();
        }
        catch (Exception e)
        {
            Status = $"the update failed: {e.Message}";
        }
    }

    /// <summary>Every file of the zip at <paramref name="zip"/> into <paramref name="into"/>, folders and all.</summary>
    private static void Unpack(string zip, string into)
    {
        using var reader = new ZipReader();
        if (reader.Open(zip) != Error.Ok) throw new IOException("the zip will not open");
        foreach (string name in reader.GetFiles())
        {
            string path = Path.Combine(into, name);
            if (name.EndsWith('/'))
            {
                Directory.CreateDirectory(path);
                continue;
            }
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllBytes(path, reader.ReadFile(name));
        }
    }
}
