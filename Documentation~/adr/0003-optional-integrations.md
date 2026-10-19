# 0003. Optional packages are detected, never hard-referenced

**Status:** Accepted

## Context
The toolkit supports many packages that a world may or may not have: Bakery, RedSim, AudioLink, AVPro, ProTV, VideoTXL, YamaPlayer, iwaSync3, USharpVideo, VizVid. A hard compile-time reference to any of them breaks the whole toolkit for everyone who doesn't have it. A compile error anywhere also stops every other tool from loading.

## Decision
- Optional packages are found at load time by type name (`TypeLookup.FindInAllAssemblies`) and their prefabs by path with a by-name fallback (`PrefabLocator`).
- The only compile-time optional dependency is AVPro, used for in-editor video playback. It is gated by the `AVPRO_IMPORTED` define, which `AVProImportHandler` sets identically for Standalone, Android and iOS, so switching platform never changes what compiles.
- `STRANGE_DISABLE_AVPRO` (added by hand in Player Settings) switches off all AVPro code, as an escape hatch if an AVPro or VRChat SDK version mismatch stops compilation.

## Consequences
- The toolkit loads with any combination of optional packages.
- Type and member names in strings aren't checked by the compiler; an upstream rename silently disables the integration. Adapters should log clearly when a package is present but an expected type is missing.

## How to check
- CI goal (not yet in place): compile the package in a clean project with **no** optional packages installed.
- Review rule: no `using` of an optional package's namespace outside an `#if` guarded block.
