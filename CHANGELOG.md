# Changelog

## [2.0.0] - 2026-01-31

### Added
- **StrangeCleanup** - New UdonSharp component for resetting pickupable objects
  - Captures original positions/rotations on world load
  - Reset button returns all tracked objects to their starting positions
  - Automatically drops held pickups before reset
  - Resets Rigidbody velocities
  - Audio feedback support
  - Custom inspector with collider management and UI button creation
- **Object Auto-Cleanup Section** (World Tab)
  - Track loose objects (pickupables) for cleanup
  - List tracked objects with Sel/X buttons
  - Auto-cleanup of null references when objects are deleted
  - Reset Button status with Create button
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
