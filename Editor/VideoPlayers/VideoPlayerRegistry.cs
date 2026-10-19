using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;

namespace StrangeToolkit
{
    /// <summary>
    /// Finds every IVideoPlayerAdapter in the project (via Unity's TypeCache) so the core never has to list players by hand.
    /// </summary>
    public static class VideoPlayerRegistry
    {
        private static List<IVideoPlayerAdapter> _all;

        /// <summary>All known players, in priority order.</summary>
        public static IReadOnlyList<IVideoPlayerAdapter> All
        {
            get
            {
                if (_all == null)
                {
                    _all = TypeCache.GetTypesDerivedFrom<IVideoPlayerAdapter>()
                        .Where(t => !t.IsAbstract && !t.IsInterface && t.GetConstructor(Type.EmptyTypes) != null)
                        .Select(t => (IVideoPlayerAdapter)Activator.CreateInstance(t))
                        .OrderBy(a => a.Priority)
                        .ToList();
                }
                return _all;
            }
        }

        public static bool AnyInstalled => All.Any(a => a.IsInstalled);

        /// <summary>The highest-priority installed player, or null if none is installed.</summary>
        public static IVideoPlayerAdapter FirstInstalled => All.FirstOrDefault(a => a.IsInstalled);

        public static IVideoPlayerAdapter Find(string id) => All.FirstOrDefault(a => a.Id == id);
    }
}
