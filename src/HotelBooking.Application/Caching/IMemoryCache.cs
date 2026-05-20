namespace HotelBooking.Application.Caching;

/// <summary>
/// Generic in-memory кеш з TTL і ключовою інвалідацією.
/// Призначений для memoization expensive-обчислень (звітів, агрегацій).
/// Thread-safe.
/// </summary>
/// <typeparam name="TKey">Тип ключа (повинен бути придатний для Dictionary).</typeparam>
/// <typeparam name="TValue">Тип значення, що кешується.</typeparam>
public interface IMemoryCache<TKey, TValue> where TKey : notnull
{
    /// <summary>
    /// Повертає значення з кешу або обчислює його через <paramref name="factory"/>,
    /// зберігає на <paramref name="ttl"/> часу та повертає. Атомарно — паралельні
    /// виклики з тим самим ключем чекають один factory-виклик.
    /// </summary>
    Task<TValue> GetOrAddAsync(TKey key, Func<Task<TValue>> factory, TimeSpan? ttl = null);

    /// <summary>Видаляє конкретний ключ з кешу.</summary>
    void Invalidate(TKey key);

    /// <summary>Очищує весь кеш.</summary>
    void Clear();

    /// <summary>Кількість записів у кеші (для діагностики/тестів).</summary>
    int Count { get; }
}
