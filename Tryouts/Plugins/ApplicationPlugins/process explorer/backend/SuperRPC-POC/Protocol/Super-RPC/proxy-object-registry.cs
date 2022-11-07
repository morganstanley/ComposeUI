using System;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using System.Runtime;

namespace Super.RPC;

public class ProxyObjectRegistry
{

    // https://www.nimaara.com/per-object-garbage-collection-notification-in-net/
    private sealed record NotifyWhenDisposed(Action disposed)
    {
        ~NotifyWhenDisposed() { disposed(); }
    }

    private readonly Dictionary<string, DependentHandle> byId = new Dictionary<string, DependentHandle>();
    private readonly ConditionalWeakTable<object, string> byObj = new ConditionalWeakTable<object, string>();

    public void Register(string objId, object obj, Action? dispose = null)
    {
        lock (byId)
        {
            byId.Add(objId, new DependentHandle(obj, new NotifyWhenDisposed(() => {
                byId.Remove(objId);
                dispose?.Invoke();
            })));
            byObj.Add(obj, objId);
        }
    }

    public string? GetId(object obj)
    {
        lock (byId) return byObj.TryGetValue(obj, out var objId) ? objId : null;
    }

    public object? Get(string objId)
    {
        lock (byId)
        {
            if (byId.TryGetValue(objId, out var handle))
            {
                var obj = handle.Target;
                if (obj is not null)
                {
                    return obj;
                }
                else
                {
                    byId.Remove(objId);
                }
            }
            return null;
        }
    }

}
