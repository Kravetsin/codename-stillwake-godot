#!/bin/sh
# Joins a friend's co-op session on the playtest server, by the name they hosted it as.
cd "$(dirname "$0")" || exit 1
printf 'Session name: '; read -r NAME
if [ -z "$STILLWAKE_PASSWORD" ]; then printf 'Playtest password: '; read -r STILLWAKE_PASSWORD; export STILLWAKE_PASSWORD; fi
exec ./Stillwake.app/Contents/MacOS/Stillwake -- --relay --join="$NAME"
