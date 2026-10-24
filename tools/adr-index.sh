#!/usr/bin/env bash
# Writes docs/decisions/index.md from the ADRs' title and status lines. The generator is DocsGuardTests.AdrIndex in
# game.tests, so the guard that checks the index and this script that writes it never disagree.
set -o pipefail
root="$(cd "$(dirname "$0")/.." && pwd)"
STILLWAKE_WRITE_ADR_INDEX=1 dotnet test "$root/game.tests" --nologo -v q \
  --filter "FullyQualifiedName~DocsGuardTests.AdrIndexMatchesTheDecisions" | grep -E ': error |^Failed!'
status=${PIPESTATUS[0]}
[ "$status" -eq 0 ] && echo "docs/decisions/index.md written"
exit "$status"
