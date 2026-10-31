using System;
using System.Collections.Generic;
using UnityEngine;

namespace StrangeToolkit
{
    internal static class VideoPlayerContext
    {
        private static Transform _root;
        private static bool _scoped;
        private static bool _allowLookups = true;
        private static Dictionary<Type, Component> _cache;

        public static IDisposable Enter(Transform root, bool allowLookups = true) => new Scope(root, allowLookups);

        public static Component Find(Type type)
        {
            if (type == null) return null;
            if (!_scoped) return UnityEngine.Object.FindFirstObjectByType(type, FindObjectsInactive.Include) as Component;
            if (_root == null || !_allowLookups) return null;
            if (_cache.TryGetValue(type, out var cached)) return cached;
            var matches = _root.GetComponentsInChildren(type, true);
            var match = matches.Length == 1 ? matches[0] : null;
            _cache[type] = match;
            return match;
        }

        public static UnityEngine.Object[] FindAll(Type type)
        {
            if (type == null) return Array.Empty<UnityEngine.Object>();
            if (!_scoped) return UnityEngine.Object.FindObjectsByType(type, FindObjectsInactive.Include, FindObjectsSortMode.None);
            return _root != null && _allowLookups ? _root.GetComponentsInChildren(type, true) : Array.Empty<Component>();
        }

        private sealed class Scope : IDisposable
        {
            private readonly Transform _previousRoot;
            private readonly bool _previousScoped;
            private readonly bool _previousAllow;
            private readonly Dictionary<Type, Component> _previousCache;
            public Scope(Transform root, bool allowLookups)
            {
                _previousRoot = _root;
                _previousScoped = _scoped;
                _previousAllow = _allowLookups;
                _previousCache = _cache;
                _root = root;
                _scoped = true;
                _allowLookups = allowLookups;
                _cache = new Dictionary<Type, Component>();
            }
            public void Dispose()
            {
                _root = _previousRoot;
                _scoped = _previousScoped;
                _allowLookups = _previousAllow;
                _cache = _previousCache;
            }
        }
    }
}
