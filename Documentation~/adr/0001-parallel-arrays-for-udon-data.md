# 0001. Parallel arrays for per-item runtime data

**Status:** Accepted

## Context
Runtime scripts are UdonSharp. Udon can't reliably hold custom classes or structs as data, can't read ScriptableObjects at runtime, and can't call ordinary C# helper classes. StrangeHub needs a list of atmosphere presets, and StrangeCleanup a list of props with their start positions.

## Decision
Per-item data is stored as parallel arrays on the behaviour (`atmoNames[i]`, `atmoFogColors[i]`, ...), one array per setting, all indexed the same way.

The editor never resizes or deletes entries array-by-array by hand. It goes through one list of array names (`Editor/Atmosphere/AtmosphereFields.All`), so every array always changes together.

## Consequences
- Works within Udon's limits and serializes with the scene.
- Index alignment is a hidden rule: one array of a different length shifts every preset after it. The single field list contains that risk.
- Adding a setting touches three places: the array on the Hub, its name in `AtmosphereFields`, and its default in `AtmosphereSection.FillNewEntry`. Older Hubs have shorter arrays, so runtime code must bounds-check every read and fall back to the old behaviour.
- The `AtmospherePreset` ScriptableObject is retired for this reason (kept only so old assets don't break).

## How to check
- Review rule: any new `atmo*` array on StrangeHub must appear in `AtmosphereFields.All`.
- Runtime reads of these arrays always check `index < array.Length`.
