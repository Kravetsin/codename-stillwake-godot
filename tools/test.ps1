<#
Runs the tests quietly: build errors, each failed test with its message and our stack lines, the docs guard's
warnings, and a line of totals per test project.

  tools/test.ps1                          # every test in Stillwake.sln
  tools/test.ps1 Logistics                # tests whose full name contains "Logistics"
  tools/test.ps1 DocsGuard game.tests     # in one project: sim/Tests, net/Tests or game.tests
#>
param([string]$Filter = "", [string]$Project = "Stillwake.sln")

$root = Split-Path $PSScriptRoot -Parent
$test = @("test", (Join-Path $root $Project), "--nologo", "-v", "q", "--logger", "console;verbosity=detailed")
if ($Filter) { $test += @("--filter", "FullyQualifiedName~$Filter") }

$failing = $false
$project = ""
$total = 0
$counts = @{}
& dotnet @test 2>&1 | ForEach-Object {
    $line = "$_".TrimEnd("`r")
    if ($line -match '^\s*(Passed|Failed|Skipped) (\S*?\.Tests)\.') { $project = $Matches[2] }
    if ($line -match '^\s*(Passed|Skipped) \S' -or $line -match '^Test Run (Failed|Successful)') { $failing = $false; return }
    if ($line -match '^Total tests: (\d+)') { $total = $Matches[1]; $counts = @{}; return }
    if ($line -match '^\s+(Passed|Failed|Skipped): (\d+)') { $counts[$Matches[1]] = $Matches[2]; return }
    if ($line -match '^ Total time: (\S+) (\w+)') {
        $n = { param($k) if ($counts[$k]) { $counts[$k] } else { 0 } }
        "${project}: $total tests, $(& $n Passed) passed, $(& $n Failed) failed, $(& $n Skipped) skipped ($($Matches[1]) $($Matches[2].ToLower()))"
        return
    }
    if ($line -match '^\s*Failed \S') { $failing = $true }
    if ($line -match '^\s*WARN ' -or $line -match ': (error|warning) [A-Z]+\d+') { $line; return }
    if ($failing -and ($line -notmatch '^\s+at ' -or $line -match '\.cs:line \d+')) { $line }
}
exit $LASTEXITCODE
