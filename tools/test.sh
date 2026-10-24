#!/usr/bin/env bash
# Runs the tests quietly: build errors, each failed test with its message and our stack lines, the docs guard's
# warnings, and a line of totals per test project.
#
#   tools/test.sh                         # every test in Stillwake.sln
#   tools/test.sh Logistics               # tests whose full name contains "Logistics"
#   tools/test.sh DocsGuard game.tests    # in one project: sim/Tests, net/Tests or game.tests
set -o pipefail
root="$(cd "$(dirname "$0")/.." && pwd)"
filter=()
[ -n "$1" ] && filter=(--filter "FullyQualifiedName~$1")
dotnet test "$root/${2:-Stillwake.sln}" --nologo -v q --logger "console;verbosity=detailed" "${filter[@]}" 2>&1 | awk '
  { sub(/\r$/, "") }
  /^[ \t]*(Passed|Failed|Skipped) [^ ]/ { if (match($2, /^[^ ]*\.Tests\./)) project = substr($2, 1, RLENGTH - 1) }
  /^[ \t]*(Passed|Skipped) [^ ]/ || /^Test Run (Failed|Successful)/ { failing = 0; next }
  /^Total tests: / { total = $3; next }
  /^[ \t]+(Passed|Failed|Skipped): [0-9]/ { s = $1; sub(/:/, "", s); counts[s] = $2; next }
  /^ Total time: / {
    printf "%s: %d tests, %d passed, %d failed, %d skipped (%s %s)\n", project, total, counts["Passed"], counts["Failed"], counts["Skipped"], $3, tolower($4)
    delete counts; next
  }
  /^[ \t]*Failed [^ ]/ { failing = 1 }
  /^[ \t]*WARN / || /: (error|warning) [A-Z]+[0-9]+/ { print; next }
  failing && (!/^[ \t]+at / || /\.cs:line [0-9]+/) { print }
'
