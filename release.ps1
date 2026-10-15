# Releases what is on origin/main (docs/releasing.md): the next version after the latest tag, a patch
# unless told minor, major or an exact vX.Y.Z; refused while main's CI is not green. Run by release.cmd.
param([string]$Bump = "patch", [switch]$Force)
$ErrorActionPreference = "Stop"
Set-Location $PSScriptRoot

git fetch origin main --tags --quiet
if ($LASTEXITCODE -ne 0) { throw "Cannot fetch from origin." }
$sha = (git rev-parse origin/main).Trim()
$subject = (git log -1 --format=%s origin/main).Trim()

# The latest release by its numbers, not its tag's text: v0.0.10 comes after v0.0.9.
$tags = @(git tag -l "v*" | Where-Object { $_ -match '^v\d+\.\d+\.\d+$' })
$latest = $tags | Sort-Object { [version]($_.TrimStart('v')) } | Select-Object -Last 1
$from = if ($latest) { [version]($latest.TrimStart('v')) } else { [version]"0.0.0" }
$next = switch -Regex ($Bump) {
    '^v?\d+\.\d+\.\d+$' { $Bump.TrimStart('v') }
    '^major$' { "$($from.Major + 1).0.0" }
    '^minor$' { "$($from.Major).$($from.Minor + 1).0" }
    '^patch$' { "$($from.Major).$($from.Minor).$($from.Build + 1)" }
    default { throw "Release what: patch, minor, major or vX.Y.Z, not '$Bump'." }
}
$tag = "v$next"
if ($tags -contains $tag) { throw "$tag is out already." }
if ($latest -and [version]$next -le $from) { throw "$tag is not after $latest." }

# Green CI on that very commit, when the GitHub CLI can say; -Force releases without asking.
if (-not $Force -and (Get-Command gh -ErrorAction SilentlyContinue)) {
    # Held first, then piped: Windows PowerShell hands a JSON array on as one object otherwise.
    $runs = gh run list --repo Kravetsin/codename-stillwake-godot --workflow CI --branch main --limit 10 --json headSha,conclusion,status 2>$null | Out-String | ConvertFrom-Json
    $run = $runs | Where-Object { $_.headSha -eq $sha } | Select-Object -First 1
    if (-not $run) { throw "No CI run for main's $($sha.Substring(0, 7)) yet: wait for it, or -Force." }
    if ($run.status -ne "completed") { throw "CI is still running on main's $($sha.Substring(0, 7)): wait for it." }
    if ($run.conclusion -ne "success") { throw "CI failed on main's $($sha.Substring(0, 7)): fix main first." }
}

Write-Host "Release $tag (after $(if ($latest) { $latest } else { 'nothing' })) on $($sha.Substring(0, 7)) '$subject'?"
if ((Read-Host "y/N") -notmatch '^(y|yes)$') { Write-Host "Not released."; exit 1 }
git tag $tag $sha
git push origin $tag
if ($LASTEXITCODE -ne 0) {
    git tag -d $tag | Out-Null
    throw "$tag did not go up: someone may have released it first. Run again for the next."
}
Write-Host "Tagged $tag. CI builds it in a few minutes: https://github.com/Kravetsin/codename-stillwake-godot/releases/tag/$tag"
