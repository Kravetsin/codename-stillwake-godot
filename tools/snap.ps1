<#
Builds the game and saves what it shows, so a check of the client needs no Godot command line by hand.

  tools/snap.ps1 out.png -- --planet --zoom 3              # one frame, 4 s into the world
  tools/snap.ps1 out.png -At 8 -- --testbed --lit          # later
  tools/snap.ps1 run.png -Movie 3 -- --hills --autopilot   # 3 s recorded at 60 fps, every 12th frame on one sheet

Everything after -- goes to the game (dev arguments: game/dev/logic/DevModes.cs). The frame or sheet path is printed last.
#>
param(
    [Parameter(Mandatory = $true, Position = 0)][string]$Out,
    [int]$At = 4,
    [double]$Movie = 0,
    [int]$Every = 12,
    [Parameter(ValueFromRemainingArguments = $true)][string[]]$GameArgs
)

$root = Split-Path $PSScriptRoot -Parent
$godot = if ($env:STILLWAKE_GODOT) { $env:STILLWAKE_GODOT } else { "$env:USERPROFILE\Tools\godot-4.7.2\Godot_v4.7.2-stable_mono_win64\Godot_v4.7.2-stable_mono_win64.exe" }
$console = $godot -replace '\.exe$', '_console.exe'
if (-not (Test-Path $console)) { $console = $godot }
$env:MSBUILDDISABLENODEREUSE = "1"; $env:DOTNET_CLI_USE_MSBUILD_SERVER = "0"; $env:UseSharedCompilation = "false"
$Out = [System.IO.Path]::GetFullPath($Out)
New-Item -ItemType Directory -Force (Split-Path $Out -Parent) | Out-Null
$GameArgs = @($GameArgs | Where-Object { $_ -ne "--" })

$build = & dotnet build (Join-Path $root "Stillwake.sln") -v q -nologo 2>&1
if ($LASTEXITCODE -ne 0) { $build | Select-String -Pattern "error" | Select-Object -First 20; exit 1 }
& $console --headless --path (Join-Path $root "game") --import *> $null

if ($Movie -le 0) {
    & $console --path (Join-Path $root "game") -- @GameArgs --snap $Out --snap-at $At *> $null
    if (-not (Test-Path $Out)) { "no frame was saved"; exit 1 }
    $Out
    exit 0
}

# A recording: frames into a folder beside $Out, then every $Every-th frame laid out on one sheet.
$dir = [System.IO.Path]::ChangeExtension($Out, $null).TrimEnd('.') + "-frames"
New-Item -ItemType Directory -Force $dir | Out-Null
Get-ChildItem $dir -Filter *.png | Remove-Item
$frames = [int]($Movie * 60)
& $console --path (Join-Path $root "game") --write-movie (Join-Path $dir "f.png") --fixed-fps 60 --quit-after $frames -- @GameArgs *> $null
$files = @(Get-ChildItem $dir -Filter *.png | Sort-Object Name)
if ($files.Count -eq 0) { "no frames were recorded"; exit 1 }
Add-Type -AssemblyName System.Drawing
$picked = @(for ($i = 0; $i -lt $files.Count; $i += $Every) { $files[$i] })
$first = [System.Drawing.Image]::FromFile($picked[0].FullName)
$w = [int]($first.Width / 2); $h = [int]($first.Height / 2); $first.Dispose()
$cols = [Math]::Min(4, $picked.Count); $rows = [Math]::Ceiling($picked.Count / $cols)
$sheet = New-Object System.Drawing.Bitmap ($w * $cols), ($h * $rows)
$g = [System.Drawing.Graphics]::FromImage($sheet)
$g.InterpolationMode = "NearestNeighbor"
for ($i = 0; $i -lt $picked.Count; $i++) {
    $img = [System.Drawing.Image]::FromFile($picked[$i].FullName)
    $g.DrawImage($img, ($i % $cols) * $w, [Math]::Floor($i / $cols) * $h, $w, $h)
    $img.Dispose()
}
$g.Dispose(); $sheet.Save($Out); $sheet.Dispose()
"$($files.Count) frames in $dir"
$Out
