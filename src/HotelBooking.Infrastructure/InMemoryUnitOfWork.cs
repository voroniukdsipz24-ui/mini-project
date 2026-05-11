using HotelBooking.Domain.Entities;
using HotelBooking.Domain.Interfaces;

namespace HotelBooking.Infrastructure;

// ── In-memory repository implementations ─────────────────────────────────

internal class InMemoryBookingRepository : IBookingRepository
{
    private readonly List<Booking> _items = new();

    public Task<Booking?> GetByIdAsync(int id, CancellationToken ct = default) =>
        Task.FromResult(_items.FirstOrDefault(b => b.Id == id));

    public Task<IReadOnlyList<Booking>> GetAllAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<Booking>>(_items.AsReadOnly());

    public Task<IReadOnlyList<Booking>> GetByGuestIdAsync(int guestId, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<Booking>>(_items.Where(b => b.GuestId == guestId).ToList().AsReadOnly());

    public Task<IReadOnlyList<Booking>> GetByRoomIdAsync(int roomId, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<Booking>>(_items.Where(b => b.RoomId == roomId).ToList().AsReadOnly());

    public Task<IReadOnlyList<Booking>> GetActiveAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<Booking>>(
            _items.Where(b => b.Status is BookingStatus.Confirmed or BookingStatus.CheckedIn)
                  .ToList().AsReadOnly());

    public Task<Booking> AddAsync(Booking booking, CancellationToken ct = default)
    {
        _items.Add(booking);
        return Task.FromResult(booking);
    }

    public Task UpdateAsync(Booking booking, CancellationToken ct = default)
    {
        int idx = _items.FindIndex(b => b.Id == booking.Id);
        if (idx >= 0) _items[idx] = booking;
        return Task.CompletedTask;
    }

    public Task<int> NextIdAsync(CancellationToken ct = default) =>
        Task.FromResult(_items.Count == 0 ? 1 : _items.Max(b => b.Id) + 1);
}

internal class InMemoryRoomRepository : IRoomRepository
{
    private readonly List<Room> _items = new();

    public void Seed(IEnumerable<Room> rooms) => _items.AddRange(rooms);

    public Task<Room?> GetByIdAsync(int id, CancellationToken ct = default) =>
        Task.FromResult(_items.FirstOrDefault(r => r.Id == id));

    public Task<IReadOnlyList<Room>> GetAllAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<Room>>(_items.AsReadOnly());

    public Task<IReadOnlyList<Room>> GetAvailableAsync(
        DateTime checkIn, DateTime checkOut, int guestCount = 1, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<Room>>(
            _items.Where(r => r.IsAvailable() && r.Capacity >= guestCount).ToList().AsReadOnly());

    public Task<Room> AddAsync(Room room, CancellationToken ct = default)
    {
        _items.Add(room);
        return Task.FromResult(room);
    }

    public Task UpdateAsync(Room room, CancellationToken ct = default)
    {
        int idx = _items.FindIndex(r => r.Id == room.Id);
        if (idx >= 0) _items[idx] = room;
        return Task.CompletedTask;
    }

    public Task<int> NextIdAsync(CancellationToken ct = default) =>
        Task.FromResult(_items.Count == 0 ? 1 : _items.Max(r => r.Id) + 1);
}

internal class InMemoryGuestRepository : IGuestRepository
{
    private readonly List<Guest> _items = new();

    public void Seed(IEnumerable<Guest> guests) => _items.AddRange(guests);

    public Task<Guest?> GetByIdAsync(int id, CancellationToken ct = default) =>
        Task.FromResult(_items.FirstOrDefault(g => g.Id == id));

    public Task<Guest?> GetByEmailAsync(string email, CancellationToken ct = default) =>
        Task.FromResult(_items.FirstOrDefault(g => g.Email == email.ToLowerInvariant()));

    public Task<IReadOnlyList<Guest>> GetAllAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<Guest>>(_items.AsReadOnly());

    public Task<Guest> AddAsync(Guest guest, CancellationToken ct = default)
    {
        _items.Add(guest);
        return Task.FromResult(guest);
    }

    public Task UpdateAsync(Guest guest, CancellationToken ct = default)
    {
        int idx = _items.FindIndex(g => g.Id == guest.Id);
        if (idx >= 0) _items[idx] = guest;
        return Task.CompletedTask;
    }

    public Task<int> NextIdAsync(CancellationToken ct = default) =>
        Task.FromResult(_items.Count == 0 ? 1 : _items.Max(g => g.Id) + 1);
}

// ── Unit of Work ──────────────────────────────────────────────────────────

/// <summary>
/// In-memory Unit of Work — для Lab 34 (перший вертикальний зріз) і тестів.
/// Не зберігає дані між запусками; на Lab 35+ замінюється на JsonUnitOfWork.
/// </summary>
public class InMemoryUnitOfWork : IUnitOfWork
{
    private readonly InMemoryBookingRepository _bookings = new();
    private readonly InMemoryRoomRepository    _rooms    = new();
    private readonly InMemoryGuestRepository   _guests   = new();

    public IBookingRepository Bookings => _bookings;
    public IRoomRepository    Rooms    => _rooms;
    public IGuestRepository   Guests   => _guests;

    public Task SaveAsync(CancellationToken ct = default) => Task.CompletedTask;

    public async Task SeedDemoDataAsync()
    {
        await _rooms.AddAsync(new Room(1, 101, 1, RoomType.Standard,  80m, 2, "Стандартний номер"));
        await _rooms.AddAsync(new Room(2, 201, 2, RoomType.Deluxe,   120m, 2, "Делюкс з балконом"));
        await _rooms.AddAsync(new Room(3, 301, 3, RoomType.Suite,    200m, 2, "Люкс з джакузі"));
        await _rooms.AddAsync(new Room(4, 102, 1, RoomType.Standard,  80m, 1, "Одномісний стандарт"));
        await _rooms.AddAsync(new Room(5, 202, 2, RoomType.Deluxe,   130m, 3, "Делюкс сімейний"));

        await _guests.AddAsync(new Guest(1, "Олена", "Шевченко", "olena@example.com",
            "+380501234567", "AA123456", new DateTime(1990, 3, 15)));
        await _guests.AddAsync(new Guest(2, "Михайло", "Коваль", "mykhailo@example.com",
            "+380671234567", "BB654321", new DateTime(1985, 7, 22)));
    }
}
