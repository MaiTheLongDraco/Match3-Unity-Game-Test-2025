using System.Collections.Generic;
using UnityEngine;

public static class ResourceCache
{
    private static readonly Dictionary<string, GameObject> _prefabCache = new Dictionary<string, GameObject>();

    public static GameObject GetPrefab(string path)
    {
        if (string.IsNullOrEmpty(path)) return null;

        if (!_prefabCache.TryGetValue(path, out GameObject prefab))
        {
            prefab = Resources.Load<GameObject>(path);
            if (prefab != null)
            {
                _prefabCache[path] = prefab;
            }
        }
        return prefab;
    }

    public static void ClearCache()
    {
        _prefabCache.Clear();
    }
}
