#!/usr/bin/env bash
# What a PR can break, from its changed paths on stdin, one a line: docs, client or all. What it does not know is all.
# Why each path goes where: docs/decisions/2026-10-31-ci-scope.md. Its cases: tools/ci-scope.test.sh.
scope=
while IFS= read -r path; do
  case "$path" in
    tools/ci-scope*) echo all; exit ;;
    docs/* | .claude/* | tools/* | README.md | CLAUDE.md | */README.md) scope=${scope:-docs} ;;
    game/* | game.tests/*) scope=client ;;
    *) echo all; exit ;;
  esac
done
echo "${scope:-all}"
