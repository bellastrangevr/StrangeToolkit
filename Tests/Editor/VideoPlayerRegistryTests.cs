using System.Linq;
using NUnit.Framework;

namespace StrangeToolkit.Tests
{
    // ADR 0007: every supported player is found automatically, once, in the dashboard's priority order
    public class VideoPlayerRegistryTests
    {
        [Test]
        public void FindsAllSixPlayersInPriorityOrder()
        {
            var ids = VideoPlayerRegistry.All.Select(a => a.Id).ToArray();
            CollectionAssert.AreEqual(new[] { "protv", "iwasync3", "usharpvideo", "vizvid", "yamaplayer", "videotxl" }, ids);
        }

        [Test]
        public void IdsAndNamesAreUnique()
        {
            var all = VideoPlayerRegistry.All;
            Assert.AreEqual(all.Count, all.Select(a => a.Id).Distinct().Count());
            Assert.AreEqual(all.Count, all.Select(a => a.DisplayName).Distinct().Count());
        }

        [Test]
        public void FindReturnsNullForBuiltInUnityPlayer()
        {
            Assert.IsNull(VideoPlayerRegistry.Find("unity"));
            Assert.IsNotNull(VideoPlayerRegistry.Find("protv"));
        }
    }
}
