# Changelog

## [1.1.0] - 2026-01-28

### Added
- Broken Static Objects detection - auditor now detects objects that ARE static but SHOULD NOT be (e.g., pickups/animated objects accidentally set to static)
- "Fix All (Set to Dynamic)" button to quickly fix all broken static objects

### Fixed
- Static candidate detection now checks parent hierarchy for Rigidbody and VRCPickup components
- Child meshes of pickups are now correctly excluded from static candidates
- Objects with parent Rigidbody/VRCPickup now show reason as "Has Rigidbody (self or parent)" or "Is Pickup (self or parent)"

## [1.0.0] - 2026-01-27

### Added
- Initial release
- StrangeHub - Central hub component for managing Strange Toolkit features
- StrangeToggle - Toggle component for enabling/disabling features
- StrangeAtmosphereSwitch - Component for switching atmosphere settings
- Editor tools
  - StrangeToolkitWindow - Main editor window
  - StrangeHubEditor - Custom inspector for StrangeHub
  - StrangeToggleEditor - Custom inspector for StrangeToggle
- Internal release instructions and git configuration
