using HotelBooking.Domain.Entities;

namespace HotelBooking.Domain.Interfaces;

// ── Generic repository contract (Lab 35 вимога) ───────────────────────────

/// <summary>
/// Узагальнений контракт репозиторію — основа для спеціалізованих інтерфейсів.
/// </summary>
public interface IRepository<T, TId>
{
    Task<T?> GetByIdAsync(TId id, CancellationToken ct = default);
    Task<IReadOnlyList<T>> GetAllAsync(CancellationToken ct = default);
    Task<T> AddAsync(T entity, CancellationToken ct = default);
    Task UpdateAsync(T entity, CancellationToken ct = default);
    Task<TId> NextIdAsync(CancellationToken ct = default);
}

// ── Async persistence contract (Lab 35 вимога) ────────────────────────────

/// <summary>
/// Контракт для persistence-шару — підмінний (JSON / XML / DB).
/// </summary>
public interface IDataStore<T>
{
    Task<IReadOnlyCollection<T>> LoadAsync(CancellationToken ct = default);
    Task SaveAsync(IReadOnlyCollection<T> items, CancellationToken ct = default);
}

// ── Specialized repository contracts ─────────────────────────────────────

public interface IBookingRepository : IRepository<Booking, int>
{
    Task<IReadOnlyList<Booking>> GetByGuestIdAsync(int guestId, CancellationToken ct = default);
    Task<IReadOnlyList<Booking>> GetByRoomIdAsync(int roomId, CancellationToken ct = default);
    Task<IReadOnlyList<Booking>> GetActiveAsync(CancellationToken ct = default);
}

public interface IRoomRepository : IRepository<Room, int>
{
    Task<IReadOnlyList<Room>> GetAvailableAsync(DateTime checkIn, DateTime checkOut,
        int guestCount = 1, CancellationToken ct = default);
}

public interface IGuestRepository : IRepository<Guest, int>
{
    Task<Guest?> GetByEmailAsync(string email, CancellationToken ct = default);
}

public interface IUnitOfWork
{
    IBookingRepository Bookings { get; }
    IRoomRepository    Rooms    { get; }
    IGuestRepository   Guests   { get; }
    Task SaveAsync(CancellationToken ct = default);
}
