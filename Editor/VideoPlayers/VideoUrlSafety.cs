using System;

namespace StrangeToolkit
{
    /// <summary>
    /// Checks video URLs before they are handed to yt-dlp for in-editor playback.
    /// URLs come from scene data, prefabs and shared playlists, so they are untrusted input
    /// and must never be able to add command-line options (e.g. --exec).
    /// </summary>
    public static class VideoUrlSafety
    {
        /// <summary>True if the URL should go through yt-dlp at all (only https links are resolved).</summary>
        public static bool NeedsResolve(string rawUrl)
        {
            return rawUrl != null && rawUrl.TrimStart().StartsWith("https://", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Returns true and a normalized URL that is safe to pass as a single quoted argument,
        /// or false if the URL contains anything that could break out of the argument.
        /// </summary>
        public static bool TryGetSafeHttpsUrl(string rawUrl, out string safeUrl)
        {
            safeUrl = null;
            if (string.IsNullOrWhiteSpace(rawUrl)) return false;

            string trimmed = rawUrl.Trim();
            foreach (char c in trimmed)
            {
                if (char.IsWhiteSpace(c) || char.IsControl(c)) return false;
                switch (c)
                {
                    case '"': case '\'': case '`': case '\\':
                    case '<': case '>': case '|': case '^':
                        return false;
                }
            }

            if (!Uri.TryCreate(trimmed, UriKind.Absolute, out Uri uri)) return false;
            if (uri.Scheme != Uri.UriSchemeHttps) return false;
            if (string.IsNullOrEmpty(uri.Host)) return false;

            safeUrl = uri.AbsoluteUri;
            return true;
        }
    }
}
