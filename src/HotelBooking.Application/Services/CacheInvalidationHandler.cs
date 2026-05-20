using HotelBooking.Application.Caching;
using HotelBooking.Domain.Interfaces;

namespace HotelBooking.Application.Services;

/// <summary>
/// Observer-handler, що інвалідує ключі кешу на події бронювань.
///
/// Стратегія інвалідації передається як делегат — це дозволяє підключити різні
/// політики без зміни самого Handler. Наприклад:
///  - "інвалідувати ТОП-N гостей на будь-яку подію" (default)
///  - "інвалідувати лише на Cancelled/Delivered" (для аналітики)
///  - "інвалідувати конкретні ключі по бізнес-правилу"
///
/// Це класичний use case Strategy pattern через делегат.
/// </summary>
public class CacheInvalidationHandler : IBookingEventHandler
{
    private readonly IMemoryCache<string, object> _cache;
    private readonly Func<BookingEvent, IEnumerable<string>> _strategy;

    /// <summary>
    /// Створює handler з кастомною стратегією: <c>BookingEvent → ключі для інвалідації</c>.
    /// </summary>
    public CacheInvalidationHandler(
        IMemoryCache<string, object> cache,
        Func<BookingEvent, IEnumerable<string>> strategy)
    {
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _strategy = strategy ?? throw new ArgumentNullException(nameof(strategy));
    }

    /// <summary>
    /// Створює handler зі стратегією за замовчуванням: будь-яка подія
    /// інвалідує всі звіти ТОП-гостей (top-guests-1..50).
    /// </summary>
    public static CacheInvalidationHandler Default(IMemoryCache<string, object> cache) =>
        new(cache, DefaultStrategy);

    /// <summary>
    /// Default-стратегія: інвалідує всі загальні аналітичні ключі при будь-якій події.
    /// Це консервативна політика — простіше і безпечніше за тонке таргетування.
    /// </summary>
    public static IEnumerable<string> DefaultStrategy(BookingEvent _)
    {
        // ТОП-N гостей для популярних N (1..10) і дашборду (top=5 за замовчуванням)
        for (int n = 1; n <= 10; n++)
            yield return $"top-guests-{n}";
    }

    public Task HandleAsync(BookingEvent evt, CancellationToken ct = default)
    {
        foreach (var key in _strategy(evt))
            _cache.Invalidate(key);
        return Task.CompletedTask;
    }
}
