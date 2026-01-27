# Changelog

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
