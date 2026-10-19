# 0006. New editor features are their own modules

**Status:** Accepted

## Context
The dashboard is one `StrangeToolkitWindow` partial class spread over many files. Partial classes split files, not responsibilities: every tab can read and change every other tab's fields, so a change in one tab can break another.

## Decision
- New features go in their own folder and class (domain partitioning), owning their own state, with the window only calling `Draw(...)`. The first example is `Editor/Atmosphere/`.
- Calculations live in plain static classes with no UI or scene access, so they can be unit-tested (`AtmosphereMath`, `VideoUrlSafety`).
- Existing tabs move out the same way, one at a time, each verified in Unity before the next.

## Consequences
- More files, but each feature can change without touching the others.
- Until migration finishes, both styles exist side by side.

## How to check
- Review rule: no new fields on `StrangeToolkitWindow` for feature state.
- Each extracted module has EditMode tests for its pure logic.
