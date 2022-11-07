using System.Collections.Concurrent;
using System.Collections.Generic;
namespace Super.RPC;

public class ObjectIdDictionary<TKeyId, TKeyObj, TValue>
    where TKeyId : notnull
    where TKeyObj : notnull
{
    public record Entry(TKeyId id, TKeyObj obj, TValue value);

    public ConcurrentDictionary<TKeyId, Entry> ById = new ConcurrentDictionary<TKeyId, Entry>();
    public ConcurrentDictionary<TKeyObj, Entry> ByObj = new ConcurrentDictionary<TKeyObj, Entry>();

    public void Add(TKeyId id, TKeyObj obj, TValue value)
    {
        lock (ById)
        {
            var entry = new Entry(id, obj, value);
            ById[id] = entry;
            ByObj[obj] = entry;
        }
    }

    public void RemoveById(TKeyId id)
    {
        lock (ById) if (ById.Remove(id, out var entry))
            {
                ByObj.Remove(entry.obj, out var entry2);
            }
    }
}
