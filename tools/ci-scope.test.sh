#!/usr/bin/env bash
# The cases of ci-scope.sh. CI's scope job runs them, so a wrong rule fails a run rather than skipping a test.
cd "$(dirname "$0")" || exit 1
failed=0
expect() {
  want=$1
  shift
  got=$(printf '%s\n' "$@" | bash ci-scope.sh)
  if [ "$got" != "$want" ]; then
    echo "$* gave $got, not $want"
    failed=1
  fi
}

expect docs docs/map.md
expect docs docs/decisions/index.md .claude/commands/new-demo.md
expect docs README.md
expect docs CLAUDE.md
expect docs sim/Grid/README.md
expect docs net/Net/README.md
expect docs game/features/light/README.md
expect docs tools/ship-art/README.md

expect client game/Main.cs
expect client game/features/light/logic/Lightmap.cs
expect client game/assets/ship.png
expect client game.tests/SoundTests.cs
# Not docs: SoundTests reads the first, HeroLooksTests the second.
expect client game/audio/CREDITS.md
expect client tools/hero-art/parts.js
expect client tools/ship-art/export.mjs
expect client docs/map.md game/Main.cs
expect client game/Main.cs docs/map.md

expect all sim/Grid/Grid.cs
expect all sim/Content/Materials.cs
expect all net/Net/Host.cs
expect all sim/Grid/Notes.md
expect all .github/workflows/ci.yml
expect all tools/ci-scope.sh
expect all tools/ci-scope.test.sh
expect all global.json
expect all .editorconfig
expect all Stillwake.sln
expect all deploy/launchers/host.cmd
expect all docs/map.md sim/Grid/Grid.cs
expect all sim/Grid/Grid.cs docs/map.md
expect all game/Main.cs sim/Grid/Grid.cs
expect all sim/Grid/Grid.cs game/Main.cs
# As git writes a path with a character outside ASCII.
expect all '"docs/\321\204.md"'

# No paths at all: git failed, or the PR changes nothing.
if [ "$(bash ci-scope.sh </dev/null)" != all ]; then
  echo "no paths did not give all"
  failed=1
fi

exit $failed
