# Changelog

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
