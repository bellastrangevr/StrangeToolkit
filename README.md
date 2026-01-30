# Strange Toolkit

[![GitHub release](https://img.shields.io/github/v/release/bellastrangevr/StrangeToolkit?style=flat-square)](https://github.com/bellastrangevr/StrangeToolkit/releases/latest)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg?style=flat-square)](LICENSE)
[![VPM Package](https://img.shields.io/badge/VPM-Package-blue?style=flat-square)](https://github.com/bellastrangevr/StrangeToolkit/releases/latest/download/vpm.json)
[![Discord](https://img.shields.io/badge/Discord-Join-5865F2?style=flat-square&logo=discord&logoColor=white)](https://discord.gg/ZV5nK5vNFP)
[![VRChat Group](https://img.shields.io/badge/VRChat-Group-1DB954?style=flat-square&logo=vrchat&logoColor=white)](https://vrchat.com/home/group/grp_c2d5a5a6-2bf7-4eac-b92a-dd1f2363006b)

A Unity editor toolkit for VRChat world creators. Provides atmosphere presets, smart toggles with persistence, world auditing, and optimization tools.

> **Warning**: This toolkit is still a work in progress and may cause unexpected issues. Please back up your project before use.

## Features

### Strange Hub
Central manager for your world that handles:
- **Atmosphere Presets** - Define multiple skybox/fog/lighting configurations and switch between them at runtime
- **Toggle Persistence** - Save and restore toggle states across sessions

### Object Cleanup (StrangeCleanup)
Track and reset pickupable objects:
- Captures original positions/rotations on world load
- Reset button returns all tracked objects to starting positions
- Automatically drops held pickups before reset
- World-space UI button creation
- Global sync option to reset for all players

### Smart Toggles
Interactive toggles with world-space UI support:
- Toggle GameObjects on/off
- Control animator parameters
- Change material emission colors
- Play audio feedback
- Global sync to all players or per-player persistence
- Create VRChat UI toggles with checkmark indicators

### Editor Dashboard
Access via `Strange Toolkit > Open Dashboard`:
- **World Tab** - Manage atmosphere presets, object cleanup, external tool status
- **Visuals Tab** - Lighting workflow with presets, GPU instancing tools, material manager
- **Interactables Tab** - Smart toggles with world-space UI creation
- **Auditor Tab** - Performance scanning, occlusion culling checks, asset weight analysis
- **Quest Tab** - Quest conversion workflow with transform sync
- **Expansions Tab** - Modular add-on management

## Installation

Download the latest release and import into your project. Use the in-editor button to add to VPM, or [add via the website](https://bellastrangevr.github.io/StrangeToolkit/).

## License

[MIT](LICENSE)
