# 0007. Video players plug in through one adapter interface

**Status:** Accepted (migration in progress)

## Context
Six video players are supported through static classes that all have the same shape (display name, type name, installed check, prefab instantiation), but share no interface. The dashboard and the StrangeVideo inspector name each player directly, so adding a seventh means editing large shared files.

## Decision
- `IVideoPlayerAdapter` defines what the core needs from a player. Each player has a small adapter class implementing it.
- `VideoPlayerRegistry` finds every adapter automatically with Unity's `TypeCache`, so a new player is added by adding one adapter class.
- Code that only needs the common operations goes through the registry. Player-specific settings UI stays in each player's own class.
- Existing call sites move to the registry gradually, each verified in Unity.

## Consequences
- Adding a player no longer requires editing the core, once callers are migrated.
- During migration, some code still calls the static classes directly.

## How to check
- EditMode test: the registry finds exactly one adapter per supported player, with unique display names.
