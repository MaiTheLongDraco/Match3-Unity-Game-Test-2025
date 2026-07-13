using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;


public static class ViewPool
{
    // Mỗi prefabName có một Queue riêng chứa các object đang chờ tái dụng
    private static readonly Dictionary<string, Queue<GameObject>> _pools
        = new Dictionary<string, Queue<GameObject>>();

    // Root ẩn chứa tất cả pooled object — tránh làm bẩn scene hierarchy
    private static Transform _poolRoot;

#if UNITY_EDITOR
    // Thống kê chỉ dùng trong Editor để profiling
    private static int _statGet;
    private static int _statReturn;
    private static int _statNewInstantiate;
#endif


   
    public static void SetPoolRoot(Transform root)
    {
        _poolRoot = root;
    }

   /// <summary>
   /// Khởi tạo object trong pool với số lượng tương ứng
   /// </summary>
   /// <param name="prefabName"></param>
   /// <param name="count"></param>
    public static void Prewarm(string prefabName, int count)
    {
        if (string.IsNullOrEmpty(prefabName) || count <= 0) return;

        for (int i = 0; i < count; i++)
        {
            GameObject go = CreateNew(prefabName);
            // lúc prewarm thì tắt nó đi
            go.SetActive(false);
            if (go == null) return; // Prefab không tồn tại, dừng sớm
            ReturnToQueue(prefabName, go);
        }
    }

  
    public static GameObject Get(string prefabName)
    {
        if (string.IsNullOrEmpty(prefabName)) return null;

#if UNITY_EDITOR
        _statGet++;
#endif

        if (_pools.TryGetValue(prefabName, out Queue<GameObject> queue))
        {
            while (queue.Count > 0)
            {
                GameObject pooled = queue.Dequeue();

                // Drain stale null entries (object bị Destroy bởi code ngoài pool)
                if (pooled == null) continue;

                pooled.transform.SetParent(null);
                pooled.SetActive(true);
                return pooled;
            }
        }

        // Auto-expand: pool rỗng → tạo mới, ghi log để phát hiện nếu prewarm chưa đủ
        return CreateNew(prefabName);
    }

    public static void Return(string prefabName, GameObject go)
    {
        if (go == null) return;

        // Safeguard: nếu không có key → fallback Destroy để tránh memory leak
        if (string.IsNullOrEmpty(prefabName))
        {
            Debug.LogError("[ViewPool] Return() gọi với prefabName rỗng. Object sẽ bị Destroy.");
            GameObject.Destroy(go);
            return;
        }

#if UNITY_EDITOR
        _statReturn++;
#endif

        // Kill toàn bộ DOTween đang chạy trên object (không complete để tránh OnComplete callback chạy lại)
        go.transform.DOKill(false);

        // Reset về trạng thái clean cho lần Get tiếp theo
        go.transform.localScale    = Vector3.one;
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity;
        go.transform.SetParent(_poolRoot);
        go.SetActive(false);

        ReturnToQueue(prefabName, go);
    }

    /// <summary>
    /// Destroy toàn bộ pooled objects và xóa sạch pool.
    /// Gọi khi app quit hoặc chuyển scene lớn.
    /// KHÔNG cần gọi giữa các màn chơi — pool giữ nguyên để tái sử dụng cho màn tiếp theo.
    /// </summary>
    public static void Clear()
    {
        foreach (var kvp in _pools)
        {
            foreach (GameObject go in kvp.Value)
            {
                if (go != null) GameObject.Destroy(go);
            }
        }
        _pools.Clear();

#if UNITY_EDITOR
        _statGet           = 0;
        _statReturn        = 0;
        _statNewInstantiate = 0;
#endif
    }

    // ─── Private helpers ───────────────────────────────────────────────────────

    private static GameObject CreateNew(string prefabName)
    {
        GameObject prefab = ResourceCache.GetPrefab(prefabName);
        if (prefab == null)
        {
            Debug.LogError($"[ViewPool] Không tìm thấy prefab: '{prefabName}'. Kiểm tra Constants và thư mục Resources/prefabs.");
            return null;
        }

#if UNITY_EDITOR
        _statNewInstantiate++;
#endif

        GameObject go = GameObject.Instantiate(prefab);
        go.name = prefab.name; // Bỏ suffix "(Clone)" để scene hierarchy dễ đọc hơn
        return go;
    }

    private static void ReturnToQueue(string prefabName, GameObject go)
    {
        if (!_pools.TryGetValue(prefabName, out Queue<GameObject> queue))
        {
            queue = new Queue<GameObject>();
            _pools[prefabName] = queue;
        }
        queue.Enqueue(go);
    }

#if UNITY_EDITOR

    /// <summary>
    /// Trả về chuỗi thống kê pool hiện tại.
    /// Gọi từ custom inspector hoặc chạy trong console khi debug.
    /// </summary>
    public static string GetDebugStats()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"[ViewPool] Get={_statGet} | Return={_statReturn} | NewInstantiate={_statNewInstantiate}");
        foreach (var kvp in _pools)
        {
            sb.AppendLine($"  Pool '{kvp.Key}': {kvp.Value.Count} available");
        }
        return sb.ToString();
    }
#endif
}
