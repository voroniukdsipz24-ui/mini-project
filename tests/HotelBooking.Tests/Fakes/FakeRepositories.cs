using HotelBooking.Domain.Entities;
using HotelBooking.Domain.Interfaces;

namespace HotelBooking.Tests.Fakes;

// ── In-memory fakes — no file I/O ─────────────────────────────────────────

public class FakeBookingRepository : IBookingRepository
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

public class FakeRoomRepository : IRoomRepository
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

public class FakeGuestRepository : IGuestRepository
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

public class FakeUnitOfWork : IUnitOfWork
{
    public FakeBookingRepository BookingRepo { get; } = new();
    public FakeRoomRepository    RoomRepo    { get; } = new();
    public FakeGuestRepository   GuestRepo   { get; } = new();

    public IBookingRepository Bookings => BookingRepo;
    public IRoomRepository    Rooms    => RoomRepo;
    public IGuestRepository   Guests   => GuestRepo;

    public Task SaveAsync(CancellationToken ct = default) => Task.CompletedTask;
}
