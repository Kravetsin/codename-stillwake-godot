# Briefing an agent

A brief is the only context an agent gets. Fill every section; a missing one is how past tasks went wrong (rules
guessed in code, checks skipped, files read whole). Copy the block below and replace the angle brackets.

```
Goal: <one sentence: what the player can do after this, or what bug is gone>.

Rules (confirmed with the user; the agent does not invent rules):
- <rule, as the player meets it>
- <what does NOT change>

Start here:
- docs/map.md → <domain / module line>; docs/glossary.md → <game word>.
- Files: <the 2–6 files to open first>. Read READMEs by section, ADRs only by link.

Worktree: git -C <repo> fetch; worktree add ..\<name> -b <type>/<slug> origin/<base>.

Player sequences to walk in the game (each one a frame or a recording, looked at):
1. <action → what is seen>
2. <the broken case from the bug report, now right>
Compare against: <a named reference: main's frame of X, the inventory's sort button, the old 16 px art>.

Tests: a scenario test per rule and per refusal; a failing test first for a bug, shown failing once.
Run: tools/test.ps1 <filter> while working; tools/test.ps1 once at the end.
Frames: tools/snap.ps1 <out.png> -- <game args>; tools/snap.ps1 <out.png> -Movie <s> -- <args> for motion.

Done when: suite green; dotnet format Stillwake.sln --verify-no-changes clean; docs guard green;
README sections and map lines of touched modules current; an ADR if a rule changed.
Deliver: <PR to main | commits to branch X>; commits end with the Co-Authored-By line; no CI waiting.
Report in under <N> lines: what changed, deviations from the rules, frame paths, open issues.
```

## Notes

- **Rules come first.** If the user's request leaves a rule open, write the options and the player sequences
  for the user before launching; a guessed rule costs a round of rework.
- **One concept, one owner.** If a change needs the same `if` in several modules, it is a missing trait
  (`Structures.cs`, `MachineSpec`, `Material`) or a missing query; add that instead (0112).
- **Name the reference** for anything visual. "Like the inventory's sort button" with its file beats a
  description.
- **Keep it narrow.** One agent, one goal. Parallel agents on the same files merge badly.
