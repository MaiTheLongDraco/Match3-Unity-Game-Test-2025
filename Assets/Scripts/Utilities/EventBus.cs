using System;
using System.Collections.Generic;

/// <summary>
/// Generic EventBus - cho phép publish/subscribe các sự kiện toàn cục trong game
/// mà không cần tham chiếu trực tiếp giữa các class.
/// </summary>
public static class EventBus
{
    private static readonly Dictionary<Type, List<Delegate>> _handlers = new Dictionary<Type, List<Delegate>>();

    /// <summary>Đăng ký lắng nghe một loại event.</summary>
    public static void Subscribe<T>(Action<T> handler) where T : struct
    {
        Type type = typeof(T);
        if (!_handlers.ContainsKey(type))
        {
            _handlers[type] = new List<Delegate>();
        }
        _handlers[type].Add(handler);
    }

    /// <summary>Hủy đăng ký lắng nghe. Phải gọi trong OnDestroy() để tránh memory leak.</summary>
    public static void Unsubscribe<T>(Action<T> handler) where T : struct
    {
        Type type = typeof(T);
        if (_handlers.ContainsKey(type))
        {
            _handlers[type].Remove(handler);
        }
    }

    /// <summary>Phát một event tới tất cả subscriber đang lắng nghe.</summary>
    public static void Publish<T>(T evt) where T : struct
    {
        Type type = typeof(T);
        if (!_handlers.ContainsKey(type)) return;

        List<Delegate> list = _handlers[type];
        for (int i = 0; i < list.Count; i++)
        {
            (list[i] as Action<T>)?.Invoke(evt);
        }
    }

    /// <summary>Xóa toàn bộ handler — nên gọi khi load scene mới.</summary>
    public static void Clear()
    {
        _handlers.Clear();
    }
}
