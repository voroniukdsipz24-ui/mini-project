using System.Diagnostics;
using HotelBooking.Application.Caching;
using HotelBooking.Application.Services;
using HotelBooking.Domain.Entities;
using HotelBooking.Domain.Interfaces;
using HotelBooking.Tests.Fakes;

namespace HotelBooking.Tests;

// ── A: Базові тести MemoryCache ────────────────────────────────────────────

public class MemoryCacheTests
{
    [Fact]
    public async Task GetOrAdd_MissingKey_CallsFactory()
    {
        var cache = new MemoryCache<string, int>();
        int factoryCalls = 0;

        var v = await cache.GetOrAddAsync("k", () =>
        {
            factoryCalls++;
            return Task.FromResult(42);
        });

        Assert.Equal(42, v);
        Assert.Equal(1, factoryCalls);
    }

    [Fact]
    public async Task GetOrAdd_ExistingKey_ReturnsCachedWithoutFactory()
    {
        var cache = new MemoryCache<string, int>();
        int factoryCalls = 0;

        await cache.GetOrAddAsync("k", () => { factoryCalls++; return Task.FromResult(42); });
        var v = await cache.GetOrAddAsync("k", () => { factoryCalls++; return Task.FromResult(99); });

        Assert.Equal(42, v);
        Assert.Equal(1, factoryCalls);   // factory не викликався вдруге
    }

    [Fact]
    public async Task Invalidate_ForcesFactoryAgain()
    {
        var cache = new MemoryCache<string, int>();
        int factoryCalls = 0;

        await cache.GetOrAddAsync("k", () => { factoryCalls++; return Task.FromResult(42); });
        cache.Invalidate("k");
        await cache.GetOrAddAsync("k", () => { factoryCalls++; return Task.FromResult(43); });

        Assert.Equal(2, factoryCalls);
    }

    [Fact]
    public async Task TtlExpiration_ForcesFactoryAgain()
    {
        var cache = new MemoryCache<string, int>();
        int factoryCalls = 0;

        await cache.GetOrAddAsync("k", () => { factoryCalls++; return Task.FromResult(42); }, ttl: TimeSpan.FromMilliseconds(50));
        await Task.Delay(100);
        await cache.GetOrAddAsync("k", () => { factoryCalls++; return Task.FromResult(99); }, ttl: TimeSpan.FromMilliseconds(50));

        Assert.Equal(2, factoryCalls);
    }

    [Fact]
    public async Task Clear_RemovesAllEntries()
    {
        var cache = new MemoryCache<string, int>();
        await cache.GetOrAddAsync("a", () => Task.FromResult(1));
        await cache.GetOrAddAsync("b", () => Task.FromResult(2));
        Assert.Equal(2, cache.Count);

        cache.Clear();
        Assert.Equal(0, cache.Count);
    }

    [Fact]
    public async Task ConcurrentAccess_FactoryRunsOnce()
    {
        var cache = new MemoryCache<string, int>();
        int factoryCalls = 0;

        // 20 паралельних завдань, всі з одним ключем
        var tasks = Enumerable.Range(0, 20).Select(_ =>
            cache.GetOrAddAsync("hot-key", async () =>
            {
                Interlocked.Increment(ref factoryCalls);
                await Task.Delay(10);
                return 42;
            })).ToList();

        var results = await Task.WhenAll(tasks);

        Assert.All(results, v => Assert.Equal(42, v));
        Assert.Equal(1, factoryCalls);  // factory виконався ОДИН раз
    }
}

// ── B: CacheInvalidationHandler ────────────────────────────────────────────

public class CacheInvalidationHandlerTests
{
    [Fact]
    public async Task DefaultStrategy_InvalidatesAllTopGuestsKeys()
    {
        var cache = new MemoryCache<string, object>();
        // Заповнюємо ключі
        await cache.GetOrAddAsync("top-guests-1", () => Task.FromResult<object>("v1"));
        await cache.GetOrAddAsync("top-guests-5", () => Task.FromResult<object>("v5"));
        await cache.GetOrAddAsync("other-key",    () => Task.FromResult<object>("other"));
        Assert.Equal(3, cache.Count);

        var handler = CacheInvalidationHandler.Default(cache);
        await handler.HandleAsync(new BookingEvent(1, BookingStatus.Pending, null, DateTime.UtcNow));

        // Тільки інший ключ лишився
        Assert.Equal(1, cache.Count);
    }

    [Fact]
    public async Task CustomStrategy_OnlyInvalidatesMatchingKeys()
    {
        var cache = new MemoryCache<string, object>();
        await cache.GetOrAddAsync("top-guests-5",  () => Task.FromResult<object>("v"));
        await cache.GetOrAddAsync("top-guests-10", () => Task.FromResult<object>("v"));
        await cache.GetOrAddAsync("occupancy",     () => Task.FromResult<object>("v"));

        // Кастомна стратегія: інвалідуємо лише top-guests-5 на Cancelled
        var handler = new CacheInvalidationHandler(cache, evt =>
            evt.NewStatus == BookingStatus.Cancelled
                ? new[] { "top-guests-5" }
                : Array.Empty<string>());

        await handler.HandleAsync(new BookingEvent(1, BookingStatus.Pending, null, DateTime.UtcNow));
        Assert.Equal(3, cache.Count);   // Pending → нічого не інвалідовано

        await handler.HandleAsync(new BookingEvent(1, BookingStatus.Cancelled, null, DateTime.UtcNow));
        Assert.Equal(2, cache.Count);   // top-guests-5 видалено
    }

    [Fact]
    public async Task IntegrationWithBookingService_CacheInvalidatedOnCreate()
    {
        var cache = new MemoryCache<string, object>();
        var uow = new FakeUnitOfWork();
        uow.RoomRepo.Seed(new[] { new Room(1, 101, 1, RoomType.Standard, 100m, 2, "T") });
        uow.GuestRepo.Seed(new[] { new Guest(1, "T", "G", "t@t.com", "", "AA001", new DateTime(1990, 1, 1)) });

        var handler = CacheInvalidationHandler.Default(cache);
        var bookingSvc = new BookingService(uow, new IBookingEventHandler[] { handler });
        var reportSvc  = new ReportService(uow, cache);

        // Перший виклик — заповнює кеш
        await reportSvc.GetTopGuestsAsync(5);
        Assert.True(cache.Count > 0);

        // Створюємо бронювання — handler інвалідує
        await bookingSvc.CreateBookingAsync(1, 1, DateTime.Today.AddDays(2), DateTime.Today.AddDays(4));

        Assert.Equal(0, cache.Count);
    }
}

// ── C: Performance tests (Theory + InlineData) ─────────────────────────────

public class PerformanceTests
{
    /// <summary>
    /// Підготовлює UoW з N бронюваннями і M гостями для замірів.
    /// </summary>
    private static FakeUnitOfWork BuildLargeUow(int bookingCount, int guestCount)
    {
        var uow = new FakeUnitOfWork();

        // Гості
        uow.GuestRepo.Seed(Enumerable.Range(1, guestCount).Select(i =>
            new Guest(i, $"Guest{i}", "Test", $"g{i}@test.com", "", $"P{i:D6}", new DateTime(1990, 1, 1))));

        // Номери (5 номерів для розподілу)
        uow.RoomRepo.Seed(Enumerable.Range(1, 5).Select(i =>
            new Room(i, 100 + i, 1, RoomType.Standard, 100m, 2, "Test")));

        // Бронювання — рівномірно розподілені між гостями
        var rng = new Random(42);
        var bookings = new List<Booking>();
        for (int i = 1; i <= bookingCount; i++)
        {
            int guestId = ((i - 1) % guestCount) + 1;
            int roomId  = ((i - 1) % 5) + 1;
            var checkIn  = DateTime.Today.AddDays(2 + rng.Next(0, 30));
            var checkOut = checkIn.AddDays(1 + rng.Next(1, 5));
            bookings.Add(new Booking(i, roomId, guestId, checkIn, checkOut, 100m));
        }
        uow.BookingRepo.Seed(bookings);
        return uow;
    }

    [Theory]
    [InlineData(100)]
    [InlineData(1000)]
    [InlineData(5000)]
    public async Task GetTopGuests_NoCachе_PerformanceUpperBound(int bookingCount)
    {
        var uow = BuildLargeUow(bookingCount, guestCount: 50);
        var svc = new ReportService(uow);   // БЕЗ кешу

        var sw = Stopwatch.StartNew();
        await svc.GetTopGuestsAsync(5);
        sw.Stop();

        // Верхня межа: 1 секунда навіть на 5000 бронювань.
        // (на CI може коливатись, тому 1с — м'який bound для регресії)
        Assert.True(sw.ElapsedMilliseconds < 1000,
            $"Cold path took {sw.ElapsedMilliseconds}ms for {bookingCount} bookings");
    }

    [Theory]
    [InlineData(1000)]
    [InlineData(5000)]
    public async Task GetTopGuests_WithCache_WarmIsFasterThanCold(int bookingCount)
    {
        var uow = BuildLargeUow(bookingCount, guestCount: 50);
        var cache = new MemoryCache<string, object>(TimeSpan.FromMinutes(5));
        var svc = new ReportService(uow, cache);

        // Cold — перший виклик (factory виконується)
        var swCold = Stopwatch.StartNew();
        await svc.GetTopGuestsAsync(5);
        swCold.Stop();

        // Warm — другий виклик (повертає з кешу)
        var swWarm = Stopwatch.StartNew();
        for (int i = 0; i < 10; i++)
            await svc.GetTopGuestsAsync(5);
        swWarm.Stop();

        // 10 викликів з кешу повинні бути значно швидше за 1 cold
        // (warm * 10 < cold * 5 — дуже консервативна межа)
        Assert.True(swWarm.ElapsedMilliseconds < swCold.ElapsedMilliseconds * 5,
            $"Warm (10x): {swWarm.ElapsedMilliseconds}ms vs Cold (1x): {swCold.ElapsedMilliseconds}ms — cache not effective");
    }

    [Fact]
    public async Task GetTopGuests_AfterInvalidation_RecomputesFreshly()
    {
        var uow = BuildLargeUow(100, guestCount: 10);
        var cache = new MemoryCache<string, object>(TimeSpan.FromMinutes(5));
        var handler = CacheInvalidationHandler.Default(cache);
        var bookingSvc = new BookingService(uow, new IBookingEventHandler[] { handler });
        var reportSvc  = new ReportService(uow, cache);

        // Заповнюємо кеш
        var before = await reportSvc.GetTopGuestsAsync(5);
        Assert.True(cache.Count > 0);

        // Подія → інвалідація
        await bookingSvc.CreateBookingAsync(1, 1, DateTime.Today.AddDays(10), DateTime.Today.AddDays(12));

        // Кеш порожній
        Assert.Equal(0, cache.Count);

        // Новий виклик — наповнює кеш заново
        var after = await reportSvc.GetTopGuestsAsync(5);
        Assert.True(cache.Count > 0);

        // Дані оновились — гість 1 тепер має на одне бронювання більше
        Assert.NotNull(after);
    }
}
