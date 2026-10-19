# 0002. Atmospheres apply over the scene's own settings

**Status:** Accepted

## Context
Atmosphere presets change sky, fog and camera clipping at runtime. Before this decision, each preset changed only the values it set and left everything else as the previous preset had it, so switching presets stacked (fog left on, old skybox kept). The dashboard also couldn't show the presets at all, so the game and the editor quietly disagreed.

VRChat controls the player camera. Clipping comes from the scene descriptor's **Reference Camera** at load, and can only be changed at runtime through `VRCCameraSettings.ScreenCamera` (near clamped to 0.001-0.05, far at least near + 0.1). Sources: VRChat creator docs (VRC Camera Settings, VRC Scene Descriptor) and ClientSim's `ClientSimSceneManager`.

## Decision
- StrangeHub captures the scene's own sky, fog and clipping once (its *baseline*), and every preset is applied as baseline + that preset's overrides. Nothing carries over between presets.
- Fog mode `0` means "keep the scene's mode"; linear distances only apply when end > start; clipping only when the preset turns it on.
- The editor edits the Reference Camera for the world's default clipping. Presets use `VRCCameraSettings` at runtime.
- The dashboard warns when the default preset will look different in-game from the editor, and when far clip and fog distance don't line up.

## Consequences
- What you see in the editor matches the game unless a preset deliberately overrides it, and the dashboard says when it does.
- UdonSharp can't call editor helpers, so the rules exist twice: `StrangeHub.ApplyAtmosphere` (runtime) and `AtmosphereMath` (editor warnings). Each points to the other; they must change together.
- Runtime clipping changes can't be tested in ClientSim (no VRCCameraSettings there), only in a real client.

## How to check
- EditMode tests cover `AtmosphereMath` (`Tests/Editor/AtmosphereMathTests.cs`).
- Manual: switch presets several times in a Build & Test client; fog and clipping must return to the scene's values for presets that don't control them.
