<#
Writes docs/decisions/index.md from the ADRs' title and status lines. The generator is DocsGuardTests.AdrIndex in
game.tests, so the guard that checks the index and this script that writes it never disagree.
#>
$root = Split-Path $PSScriptRoot -Parent
$env:STILLWAKE_WRITE_ADR_INDEX = "1"
try {
    & dotnet test (Join-Path $root "game.tests") --nologo -v q --filter "FullyQualifiedName~DocsGuardTests.AdrIndexMatchesTheDecisions" |
        Where-Object { "$_" -match ': error |^Failed!' }
    if ($LASTEXITCODE -eq 0) { "docs/decisions/index.md written" }
}
finally { Remove-Item Env:STILLWAKE_WRITE_ADR_INDEX }
exit $LASTEXITCODE
