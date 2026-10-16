# 0032 — A player keeps their seat in a world

- Status: accepted
- Date: 2026-10-16

## Context

Characters belong to the world (option B): a hero and what it carries live in the world it was played
in, as in Raft, Grounded and Satisfactory, not in a file the player brings along as in Terraria. The
ship and its crew are the progress, and blueprints already belong to the world. But the sim knew a
player only by the peer id the host handed out in join order: a friend who left and came back got a
new id, their items under the old one, and a restarted host could hand someone else's inventory out.

## Decision

- **A player key per machine** (`platform/Profile`): a GUID made on the first run and kept in Godot's
  user folder, so updates, downloads and reinstalls keep it; Steam's id replaces it with Steam.
  `--player=name` sets it for a run; a window joining over ENet in development gets a key of its own.
- **Seats:** the host keeps who is who beside the world's save (`players.json`, `platform/Roster`),
  written as soon as a seat is given, never in the sim: a key has the same peer id in a world every
  time; a new one gets the next never given. A world from before has its host at seat 0 and new seats
  from 64, past its old guests.
- **Joining:** a guest says who it is first (`HelloMessage`); the host seats it then
  (`Host.Admit(channel, seat)`), or turns it away when that player is aboard already
  (`RefusedMessage`). The host's own seat is given to `LocalSession`, and its hashes are the truth.
- **Leaving and coming back:** `Leave` takes the hero out and its inventory stays; coming back, the hero
  comes out of the pod's hatch with it. A world loaded with heroes of players not here takes them out.
- The sim and the save format do not change.

## Consequences

- Another PC is another player until Steam; copying `profile.cfg` moves a player.
- A hero's place is not kept across a leave; orphaned heroes from before seats stay as they are.
- A guest that crashed and came straight back is turned away for the seconds the host takes to notice.
