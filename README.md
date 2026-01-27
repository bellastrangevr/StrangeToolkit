# Strange Toolkit

[![GitHub release](https://img.shields.io/github/v/release/bellastrangevr/StrangeToolkit?style=flat-square)](https://github.com/bellastrangevr/StrangeToolkit/releases/latest)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg?style=flat-square)](LICENSE)
[![VPM Package](https://img.shields.io/badge/VPM-Package-blue?style=flat-square)](https://github.com/bellastrangevr/StrangeToolkit/releases/latest/download/vpm.json)
[![Discord](https://img.shields.io/badge/Discord-Join-5865F2?style=flat-square&logo=discord&logoColor=white)](https://discord.gg/ZV5nK5vNFP)
[![VRChat Group](https://img.shields.io/badge/VRChat-Group-1DB954?style=flat-square&logo=vrchat&logoColor=white)](https://vrchat.com/home/group/grp_c2d5a5a6-2bf7-4eac-b92a-dd1f2363006b)

A Unity editor toolkit for VRChat world creators. Provides atmosphere presets, smart toggles with persistence, world auditing, and optimization tools.

## Features

### Strange Hub
Central manager for your world that handles:
- **Atmosphere Presets** - Define multiple skybox/fog/lighting configurations and switch between them at runtime
- **Toggle Persistence** - Save and restore toggle states across sessions
- **Object Cleanup** - Track loose objects and reset them to original positions

### Smart Toggles
Interactive toggles that can:
- Toggle GameObjects on/off
- Control animator parameters
- Change material emission colors
- Play audio feedback
- Persist state via the Hub

### Editor Dashboard
Access via `Strange Toolkit > Open Dashboard`:
- **World Tab** - Manage atmosphere presets and object cleanup
- **Visuals Tab** - Lighting tools, LPPV creation, mass shader swapping
- **Interactables Tab** - Quickly add smart toggles to objects
- **Auditor Tab** - Performance scanning, occlusion culling checks, asset weight analysis

### Expansions (Beta)
- **DJ Mode** - Stage lighting control via unified animator
- **Game Mode** - RPG-style vitals and loot systems

## Installation

Download the latest release and import into your project. Use the in-editor button to add to VPM.

## License

[MIT](LICENSE)
