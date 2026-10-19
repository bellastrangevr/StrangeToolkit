# 0004. Untrusted input to external programs

**Status:** Accepted

## Context
In-editor video playback passes video URLs to yt-dlp. Those URLs come from scene data, prefabs and shared playlists, so they come from other people. yt-dlp has options that run programs (`--exec`), so a URL that could inject options could run code on the creator's PC. The command also disabled TLS certificate checks.

## Decision
- Only `https` URLs are sent to yt-dlp, and only after `VideoUrlSafety.TryGetSafeHttpsUrl` accepts them: no quotes, whitespace, backslashes or shell-special characters, and they must parse as an absolute https URI. The normalised URI is what gets passed.
- The URL is passed after `--`, so yt-dlp can never read it as an option.
- TLS certificate checking stays on.
- Any future feature that starts a process or downloads files follows the same rule: validate, normalise, then pass as data.

## Consequences
- A few unusual but legitimate URLs (with raw spaces or quotes) are refused in the editor. They still work in VRChat itself; only the editor preview is affected.

## How to check
- EditMode tests: `Tests/Editor/VideoUrlSafetyTests.cs` (injection attempts must be rejected).
