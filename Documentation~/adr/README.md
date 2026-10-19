# Architecture Decision Records

Short records of design decisions in Strange Toolkit and why they were made, so they don't get re-argued or accidentally undone.

Each record has: **Status**, **Context** (the forces at play), **Decision**, **Consequences** (good and bad), and **How to check** (a test, CI step or review rule that keeps it true).

| # | Decision | Status |
|---|---|---|
| [0001](0001-parallel-arrays-for-udon-data.md) | Store per-item runtime data as parallel arrays on Udon behaviours | Accepted |
| [0002](0002-atmosphere-baseline.md) | Atmospheres apply over the scene's own settings; clipping via Reference Camera and VRCCameraSettings | Accepted |
| [0003](0003-optional-integrations.md) | Optional third-party packages are detected, never hard-referenced | Accepted |
| [0004](0004-untrusted-input-to-processes.md) | Treat URLs and scene data as untrusted before handing them to external programs | Accepted |
| [0005](0005-networked-state.md) | One player moves synced objects; per-player state lives in PlayerData | Accepted |
| [0006](0006-domain-modules.md) | New editor features are their own modules, not more of the dashboard partial class | Accepted |
| [0007](0007-video-player-adapters.md) | Video players plug in through one adapter interface | Accepted |

New record: copy the format of an existing one, take the next number, and add it to this table.
