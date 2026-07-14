using System;
using System.Collections.Generic;

public static class ServiceLocator
{
    private static readonly Dictionary<Type, object> _services = new Dictionary<Type, object>();

    public static void Register<T>(T service)
    {
        Type type = typeof(T);
        if (!_services.ContainsKey(type))
        {
            _services.Add(type, service);
        }
        else
        {
            _services[type] = service;
        }
    }

    public static T Resolve<T>()
    {
        Type type = typeof(T);
        if (_services.TryGetValue(type, out object service))
        {
            return (T)service;
        }
        throw new Exception($"Service {type} is not registered in ServiceLocator.");
    }

    public static void Unregister<T>()
    {
        Type type = typeof(T);
        if (_services.ContainsKey(type))
        {
            _services.Remove(type);
        }
    }
}
