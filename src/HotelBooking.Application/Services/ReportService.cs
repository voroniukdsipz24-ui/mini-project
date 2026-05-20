using HotelBooking.Application.Caching;
using HotelBooking.Domain.Entities;
using HotelBooking.Domain.Interfaces;

namespace HotelBooking.Application.Services;

public record OccupancyReport(
    DateTime From, DateTime To,
    int TotalRooms, int OccupiedDays, double OccupancyRate,
    decimal TotalRevenue, decimal AverageNightlyRate);

public record RoomTypeReport(RoomType Type, int Bookings, decimal Revenue, double AvgNights);

/// <summary>
/// Сервіс аналітичних звітів. Підтримує опціональний кеш через <see cref="IMemoryCache{TKey, TValue}"/>:
/// якщо інжектовано — найгарячіший запит (TopGuests) memoize-ться за ключем.
/// Без кешу — fallback на пряме обчислення (default behaviour, не ламає старі тести).
/// </summary>
public class ReportService
{
    private readonly IUnitOfWork _uow;
    private readonly IMemoryCache<string, object>? _cache;

    public ReportService(IUnitOfWork uow, IMemoryCache<string, object>? cache = null)
    {
        _uow = uow;
        _cache = cache;
    }

    public async Task<OccupancyReport> GetOccupancyReportAsync(DateTime from, DateTime to)
    {
        var bookings = await _uow.Bookings.GetAllAsync();
        var rooms    = await _uow.Rooms.GetAllAsync();

        var relevant = bookings
            .Where(b => b.Status is not BookingStatus.Cancelled)
            .Where(b => b.CheckInDate < to && b.CheckOutDate > from)
            .ToList();

        int totalRooms     = rooms.Count;
        int periodDays     = (to - from).Days;
        int totalRoomDays  = totalRooms * periodDays;
        int occupiedDays   = relevant.Sum(b => b.Nights);
        double occupancy   = totalRoomDays > 0 ? (double)occupiedDays / totalRoomDays : 0;
        decimal revenue    = relevant.Sum(b => b.TotalPrice);
        decimal avgNightly = occupiedDays > 0 ? revenue / occupiedDays : 0;

        return new OccupancyReport(from, to, totalRooms, occupiedDays,
            Math.Round(occupancy * 100, 2), revenue, avgNightly);
    }

    public async Task<IReadOnlyList<RoomTypeReport>> GetRoomTypeReportAsync()
    {
        var bookings = await _uow.Bookings.GetAllAsync();
        var rooms    = await _uow.Rooms.GetAllAsync();

        var roomDict = rooms.ToDictionary(r => r.Id);

        return bookings
            .Where(b => b.Status is not BookingStatus.Cancelled)
            .Where(b => roomDict.ContainsKey(b.RoomId))
            .GroupBy(b => roomDict[b.RoomId].Type)
            .Select(g => new RoomTypeReport(
                g.Key,
                g.Count(),
                g.Sum(b => b.TotalPrice),
                g.Average(b => b.Nights)))
            .OrderByDescending(r => r.Revenue)
            .ToList()
            .AsReadOnly();
    }

    /// <summary>
    /// ТОП-N гостей за витратами. Кешується якщо <see cref="IMemoryCache{TKey, TValue}"/>
    /// інжектовано. Ключ: $"top-guests-{top}". Інвалідація — через
    /// CacheInvalidationHandler на події IBookingEventHandler.
    /// </summary>
    public async Task<IReadOnlyList<(Guest Guest, int Bookings, decimal Spent)>> GetTopGuestsAsync(int top = 5)
    {
        string cacheKey = $"top-guests-{top}";

        if (_cache != null)
        {
            var cached = await _cache.GetOrAddAsync(cacheKey,
                async () => (object) await ComputeTopGuestsAsync(top));
            return (IReadOnlyList<(Guest, int, decimal)>) cached;
        }

        return await ComputeTopGuestsAsync(top);
    }

    private async Task<IReadOnlyList<(Guest Guest, int Bookings, decimal Spent)>> ComputeTopGuestsAsync(int top)
    {
        var bookings = await _uow.Bookings.GetAllAsync();
        var guests   = await _uow.Guests.GetAllAsync();

        var guestDict = guests.ToDictionary(g => g.Id);

        return bookings
            .Where(b => b.Status is not BookingStatus.Cancelled)
            .Where(b => guestDict.ContainsKey(b.GuestId))
            .GroupBy(b => b.GuestId)
            .Select(g => (
                Guest: guestDict[g.Key],
                Bookings: g.Count(),
                Spent: g.Sum(b => b.TotalPrice)))
            .OrderByDescending(x => x.Spent)
            .Take(top)
            .ToList()
            .AsReadOnly();
    }
}
