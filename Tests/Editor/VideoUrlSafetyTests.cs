using NUnit.Framework;

namespace StrangeToolkit.Tests
{
    // ADR 0004: URLs reach yt-dlp only when they can't add command-line options
    public class VideoUrlSafetyTests
    {
        [TestCase("https://www.youtube.com/watch?v=dQw4w9WgXcQ")]
        [TestCase("https://example.com/video.mp4")]
        [TestCase("  https://example.com/a?b=c&d=e  ")]
        public void AcceptsNormalHttpsUrls(string url)
        {
            Assert.IsTrue(VideoUrlSafety.TryGetSafeHttpsUrl(url, out string safe));
            StringAssert.StartsWith("https://", safe);
        }

        [TestCase("https://example.com/\" --exec \"calc.exe")]
        [TestCase("https://example.com/ --exec calc.exe")]
        [TestCase("https://example.com/'x'")]
        [TestCase("https://example.com/a`whoami`")]
        [TestCase("https://example.com/a\\b")]
        [TestCase("https://example.com/a|b")]
        [TestCase("https://example.com/\nmore")]
        public void RejectsUrlsThatCouldInjectOptions(string url)
        {
            Assert.IsFalse(VideoUrlSafety.TryGetSafeHttpsUrl(url, out string safe));
            Assert.IsNull(safe);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("http://example.com/video.mp4")]
        [TestCase("rtsp://example.com/stream")]
        [TestCase("--exec calc.exe")]
        [TestCase("https://")]
        public void RejectsNonHttpsOrMalformed(string url)
        {
            Assert.IsFalse(VideoUrlSafety.TryGetSafeHttpsUrl(url, out _));
        }

        [Test]
        public void OnlyHttpsGoesToYtdlp()
        {
            Assert.IsTrue(VideoUrlSafety.NeedsResolve("https://example.com"));
            Assert.IsTrue(VideoUrlSafety.NeedsResolve("HTTPS://example.com"));
            Assert.IsFalse(VideoUrlSafety.NeedsResolve("http://example.com"));
            Assert.IsFalse(VideoUrlSafety.NeedsResolve("rtspt://example.com"));
            Assert.IsFalse(VideoUrlSafety.NeedsResolve(null));
        }
    }
}
