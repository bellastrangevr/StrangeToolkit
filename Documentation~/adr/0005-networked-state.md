# 0005. Networked and per-player state

**Status:** Accepted

## Context
VRChat instances are a distributed system: messages arrive late or not at all, players join late, and owners leave mid-action. Two problems were found:
- On a global cleanup reset, every player took ownership of every prop at once and moved it, so they fought over the same objects.
- "Persistent" toggles were only kept in memory, so they reset every visit.

## Decision
- **One mover per action.** For synced objects (VRCObjectSync), only the player who caused the action takes ownership and moves them, teleporting with `FlagDiscontinuity()`. Other players only change things that aren't synced. Auto-respawn of synced props is done by the master only.
- **Per-player state uses PlayerData.** Keys are prefixed `StrangeToggle/`, and scripts wait for `OnPlayerRestored` for the local player before reading or writing, as VRChat's docs require.
- PlayerData keys can't be deleted, so a toggle's `persistenceID` should never be changed casually: the old key stays in the player's storage.

## Consequences
- Less network traffic and no jitter on reset.
- Toggles flipped before data finishes loading keep their state until the saved value arrives.

## How to check
- Manual, two clients: reset while the other player holds a prop; join late after a reset; leave and rejoin to see toggles restored.
