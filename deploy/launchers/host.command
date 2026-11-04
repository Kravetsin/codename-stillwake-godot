#!/bin/sh
# Hosts a co-op session on the playtest server: friends join it by its name with join.command.
cd "$(dirname "$0")" || exit 1
printf 'Session name (letters, digits, dashes): '; read -r NAME
if [ -z "$STILLWAKE_PASSWORD" ]; then printf 'Playtest password: '; read -r STILLWAKE_PASSWORD; export STILLWAKE_PASSWORD; fi
exec ./Stillwake.app/Contents/MacOS/Stillwake -- --relay --host="$NAME"
