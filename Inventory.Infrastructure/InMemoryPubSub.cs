// Inventory.Infrastructure/InMemoryPubSub.cs
using System.Collections.Concurrent;
using Inventory.Domain;

namespace Inventory.Domain; // keep same as IPubSub

public class InMemoryPubSub : IPubSub
{
    private readonly ConcurrentDictionary<Type, List<Func<object, Task>>> _map = new();

    public Task PublishAsync<T>(T message, CancellationToken ct = default)
    {
        if (_map.TryGetValue(typeof(T), out var hs))
            return Task.WhenAll(hs.Select(h => h(message!)));
        return Task.CompletedTask;
    }

    public void Subscribe<T>(Func<T, Task> handler)
    {
        var list = _map.GetOrAdd(typeof(T), _ => new());
        list.Add(o => handler((T)o));
    }
}
