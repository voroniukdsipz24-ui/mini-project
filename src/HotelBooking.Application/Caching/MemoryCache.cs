using System.Collections.Concurrent;

namespace HotelBooking.Application.Caching;

/// <summary>
/// Реалізація <see cref="IMemoryCache{TKey, TValue}"/> на <see cref="ConcurrentDictionary{TKey, TValue}"/>.
/// Використовує <see cref="Lazy{T}"/> для атомарного «одного factory-виклику» при race condition.
/// TTL перевіряється «лінно» — при кожному GetOrAdd, прострочені записи замінюються.
/// </summary>
public class MemoryCache<TKey, TValue> : IMemoryCache<TKey, TValue> where TKey : notnull
{
    private readonly ConcurrentDictionary<TKey, CacheEntry> _store = new();
    private readonly TimeSpan _defaultTtl;

    public MemoryCache(TimeSpan? defaultTtl = null)
    {
        _defaultTtl = defaultTtl ?? TimeSpan.FromMinutes(5);
    }

    /// <inheritdoc/>
    public int Count => _store.Count;

    /// <inheritdoc/>
    public async Task<TValue> GetOrAddAsync(TKey key, Func<Task<TValue>> factory, TimeSpan? ttl = null)
    {
        if (factory is null) throw new ArgumentNullException(nameof(factory));

        var effectiveTtl = ttl ?? _defaultTtl;

        // Якщо є валідний запис — повертаємо одразу
        if (_store.TryGetValue(key, out var existing) && !existing.IsExpired())
            return await existing.Value;

        // Інакше — створюємо новий Lazy<Task<TValue>>, який атомарно виконається лише раз
        var newEntry = new CacheEntry(
            new Lazy<Task<TValue>>(factory, LazyThreadSafetyMode.ExecutionAndPublication),
            DateTime.UtcNow + effectiveTtl);

        // AddOrUpdate забезпечує: якщо ключа немає → додаємо;
        // якщо є але expired → замінюємо; якщо є і валідний → беремо існуючий.
        var actual = _store.AddOrUpdate(
            key,
            newEntry,
            (_, current) => current.IsExpired() ? newEntry : current);

        return await actual.Value;
    }

    /// <inheritdoc/>
    public void Invalidate(TKey key) => _store.TryRemove(key, out _);

    /// <inheritdoc/>
    public void Clear() => _store.Clear();

    private sealed class CacheEntry
    {
        public Lazy<Task<TValue>> Value { get; }
        public DateTime ExpiresAt { get; }

        public CacheEntry(Lazy<Task<TValue>> value, DateTime expiresAt)
        {
            Value = value;
            ExpiresAt = expiresAt;
        }

        public bool IsExpired() => DateTime.UtcNow >= ExpiresAt;
    }
}
