# Changelog

## [2.1.0] - 2026-07-02

### Added
- **StrangeVideo** - UdonSharp component for video player management
  - Supports 6 video player systems: ProTV, iwaSync3, USharpVideo, VizVid, YamaPlayer, VideoTXL
  - Unified inspector with inline settings for each player type
  - Built-in sync for Unity video players (master-driven timestamp sync)
  - AudioLink integration with automatic wiring
  - Custom inspector with Dashboard quick-access button
  - In-editor video playback via AVPro shim (play mode URL resolution with yt-dlp)
- **Video Workflow Section** (Visuals Tab)
  - Video player dropdown with one-click prefab instantiation
  - Smart status panel with progressive status rows and auto-wiring
  - Per-player inline settings (autoplay, sync, security, media options, optional components, etc.)
  - Debug overrides panel for testing without packages installed
  - StrangeHub video timestamp sync (synced field for video state)
- **Smarter Material Manager / Shader Swapper**
  - Texture transfer now recognizes a much wider range of shader/studio naming conventions (not just standard names like `_MainTex`), so albedo, normal, and height maps carry over correctly on far more third-party shaders, including ShaderGraph-based asset packs
  - Packed ORM (Occlusion/Roughness/Metallic) textures are now detected and can optionally be transferred into the Metallic slot via a new toggle - off by default since channel packing may not match the target shader, and flagged with a console warning when detected
  - Alpha cutout and two-sided rendering settings now carry over automatically when swapping into Standard or Bakery/Standard, so cutout foliage and double-sided cards no longer render as solid blocky rectangles after a swap

### Changed
- Material Manager's isolate mode ("Whitelist") is now the default. Previously, dragging objects/materials into the target list *protected* them and swapped everything else in the scene instead of isolating just those - the opposite of what most people expected

### Fixed
- Materials on disabled/inactive objects were silently skipped during a shader swap (both isolate and "apply to all" modes) - toggle systems and hidden props are now included correctly
- Leftover shader keywords from the original shader no longer linger on a material after swapping
- Fixed a texture-matching collision where a color/albedo texture could occasionally get pulled into the Metallic slot instead of its own
- If the world's instance master left, built-in video sync would silently and permanently stop working for everyone else for the rest of the instance - it now correctly resumes for whoever becomes the new master
- In-editor video URL testing could fire a duplicate, contradictory result after a failed resolve (or for non-HTTPS sources), sometimes trying to load an error message as if it were a video URL

## [2.0.0] - 2026-01-31

### Added
- **StrangeCleanup** - UdonSharp component for resetting pickupable objects
  - **Multiple cleanup groups** - Create separate reset buttons for different object sets
  - Each cleanup group manages its own tracked objects independently
  - Captures original positions/rotations on world load
  - Reset button returns all tracked objects to their starting positions
  - Automatically drops held pickups before reset
  - Resets Rigidbody velocities
  - Audio feedback support
  - Custom inspector with tracked objects management, collider controls, and UI button creation
  - **Auto Respawn** - Automatically reset objects after configurable idle time (1-60 minutes)
    - Timer starts when object is dropped (not when it stops moving)
    - Objects respawn even if still in motion
    - Interval-based checking (0.5s) for performance optimization
    - Cached pickup references to avoid per-check GetComponent calls
- **Object Auto-Cleanup Section** (World Tab)
  - Lists all cleanup groups in scene with object counts
  - Shows Global Sync status indicator per group
  - Create new cleanup groups with one click
- **Linked Cleanup Groups** (StrangeHub Inspector)
  - Shows all cleanup groups in scene alongside Smart Toggles
  - Displays object count and sync status per group
  - Clickable names to select cleanup buttons
- **Global Sync** for StrangeToggle and StrangeCleanup
  - Toggle state syncs to all players including late joiners
  - Reset action syncs to all players
  - Mutually exclusive with persistence (per-player) mode
  - Manual sync mode with ownership transfer
  - Automatic VRCObjectSync component management on tracked cleanup objects
- **UI Button/Toggle Creation**
  - "Add UI Toggle" button creates world-space VRChat UI with checkmark
  - "Add UI Button" for cleanup reset buttons
  - "Remove UI Toggle/Button" to clean up created UI
  - Automatic VRCUiShape and TextMeshPro integration
  - Checkmark auto-added to toggle objects array
- **Lighting Workflow** (Visuals Tab)
  - Streamlined 3-step workflow: Setup → Scene Volumes → Baking
  - PC/Quest mode detection based on build target
  - "Apply Recommended PC Settings" - ProgressiveGPU, Shadowmask, Directional, 20 texels
  - "Apply Recommended Quest Settings" - ProgressiveGPU, Subtractive, Non-Directional, 12 texels
  - "Apply Bakery PC Settings" - Shadowmask, Dominant, 5 bounces, 16 samples
  - "Apply Bakery Quest Settings" - Subtractive, 2 bounces, 8 samples
  - Lighting Presets system - Save/Load complete lighting configurations
  - Auto-Generate Scene Volumes with configurable max size
- **GPU Instancing Overhaul** (Visuals Tab)
  - Hierarchical grouping: Ready for Instancing vs Material Consolidation Candidates
  - Per-object selection within instance groups
  - Material thumbnail previews in consolidation view
  - Master material selection for consolidation workflow
  - Static batching integration with mutual exclusivity toggle
  - "Switch to Instancing" / "Switch to Static" buttons for quick conversion
  - Minimum instance count threshold (default 5+ objects)
- **Material Manager Improvements**
  - Material thumbnail preview (32x32) in list
  - "Sel" button to select material in Project window
- **External Tools Status** (World Tab)
  - Bakery GPU Lightmapper detection with Asset Store link
  - RedSim Light Volumes detection with VPM listing link
- **Interactables Tab Improvements**
  - Reorganized Smart Toggle expanded settings into 4 sections (Action, Feedback, Brain, Trigger)
  - Add/Remove UI Toggle buttons
  - Prefab instance handling with "Unpack Prefab" option
- **Static Batching Tooltips** (Auditor Tab)
  - Tooltip warning when switching from static to dynamic about baked lighting implications

### Changed
- **Major Editor Reorganization** - Complete restructure into subfolders:
  - `Editor/Auditors/` - All auditor partial classes (Audio, Particles, Physics, etc.)
  - `Editor/Converters/` - Quest conversion utilities
  - `Editor/Inspectors/` - Custom inspectors (StrangeHub, StrangeToggle, StrangeCleanup)
  - `Editor/Resources/` - Editor resources (logo)
  - `Editor/Tabs/` - Tab partial classes (World, Visuals, Interactables, Auditor, Quest, Expansions)
  - `Editor/Utilities/` - Helper classes (Logger, BuildDataReader, TextureVRAMCalculator, LightingPreset)
- **Visuals Tab Overhaul**
  - Lighting Workflow section with PC/Quest mode indicator
  - GPU Instancing Tools section
  - Material Manager section
  - Removed Auditor redirect (consolidated into Auditor Tab)
- **Expansions Tab**
  - Auto-scan on tab draw and project changes
  - Improved package root detection (finds Expansions folder first, then package.json)
- **Release Workflow**
  - Added `.gitkeep` exclusion for VPM zip (keeps folder structure without placeholder files)

### Fixed
- Toggle deletion on prefab instances (properly removes backing UdonBehaviour)
- UI Toggle text alignment in CreateUIToggleVisuals
- Expansions folder detection when Editor files reorganized into subfolders
- Obsolete `Lightmapping` API calls for Unity 2022+ compatibility
- `LightingSettings` null reference exceptions
- Bakery window integration improvements
- Quest Converter transform synchronization for objects with duplicate names (Queue-based matching)
- Duplicate method definitions in `StrangeToolkitWindow`
- Potential null reference in `StrangeCleanup.InitializeAutoRespawn()`
- Missing Undo support for post-processing removal in Quest Converter
- Deprecated `FindObjectsOfType` calls updated to `FindObjectsByType`
- Exception logging added for material blacklist loading failures
- Reflection caching in PhysBones auditor (performance optimization)
- Magic numbers replaced with named constants in PhysBones auditor (thresholds, UI sizing)
- Magic numbers replaced with named constants in Quest Converter
- Empty catch blocks now explicitly catch `System.Exception`
- Removed unused imports in Quest Converter

## [1.2.1] - 2026-01-30

### Fixed
- Fixed UdonSharp assembly definitions not being recognized in VPM packages
  - Added missing UdonSharp references to asmdef files
  - Added UdonSharpAssemblyDefinition assets for Scripts and Expansions folders

## [1.2.0] - 2026-01-28

### Added
- **PhysBone Auditor** - Monitors VRCPhysBone performance metrics
  - Component count, transform count, unique collider count
  - Collision check calculation (bones × colliders per component)
  - Color-coded warnings for high collision checks (yellow >64, red >128)
  - Per-component breakdown sorted by performance impact
- **StrangeToolkitLogger** - Reusable colored console logging utility
  - Prefixed logs with `[Strange Toolkit]`
  - Color-coded output: green (success), yellow (warning), red (error), cyan (action), magenta (detection)

### Changed
- **Avatar Components Auditor** renamed to **Missing Scripts Auditor**
  - Now scans for any missing/broken script references (not just avatar components)
  - PhysBones, PhysBone Colliders, and Contacts now supported in World SDK
  - Removal buttons with colored console logging

## [1.1.0] - 2026-01-28

### Added
- **Quest Tab** - New tab for Quest world conversion
  - Scene duplication wizard (creates `_Quest` version with cloned materials)
  - Build target detection and auto-switch to Android
  - Transform sync from PC scene
- **Extended Auditor** - Granular performance analysis tools
  - Audio Auditor - Detects stereo clips, uncompressed audio, with auto-optimization
  - Particle Auditor - Flags high particle counts, collision, transparent materials
  - Physics Auditor - Detects expensive MeshColliders on Rigidbodies
  - Shadow Auditor - Finds small objects casting unnecessary shadows
  - Shader Auditor - Identifies non-mobile shaders (Quest mode)
  - Texture Auditor - Detects missing Android compression overrides
  - Avatar Components Auditor - Finds leftover VRCAvatarDescriptor/PhysBones in worlds
  - Post-Processing Auditor - Flags expensive volumes on Quest
- **QuestConverter** - Utility class for Quest optimization operations
  - Shader swapping to mobile-friendly variants
  - Audio optimization (force mono, compression)
  - Particle count reduction
  - Rigidbody optimization
  - Shadow caster removal
  - Post-processing removal
- Audit Profile selector (PC/Quest) for context-aware scanning

### Changed
- Refactored Auditor tab into partial classes for better maintainability
- Scene Weight Inspector now shows compressed size from build log when available

## [1.0.0] - 2026-01-28

### Added
- Initial release
- StrangeHub - Central hub component for managing Strange Toolkit features
- StrangeToggle - Toggle component for enabling/disabling features
- StrangeAtmosphereSwitch - Component for switching atmosphere settings
- Broken Static Objects detection - auditor detects objects that ARE static but SHOULD NOT be (e.g., pickups/animated objects accidentally set to static)
- "Fix All (Set to Dynamic)" button to quickly fix all broken static objects
- Editor tools
  - StrangeToolkitWindow - Main editor window (split into partial classes for maintainability)
  - StrangeHubEditor - Custom inspector for StrangeHub
  - StrangeToggleEditor - Custom inspector for StrangeToggle
- VPM repository hosting via GitHub Pages
- Expansion system for modular add-ons
