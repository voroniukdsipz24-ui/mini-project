using HotelBooking.Domain.Entities;
using HotelBooking.Domain.Interfaces;

namespace HotelBooking.Infrastructure.Repositories;

public class JsonBookingRepository : JsonRepositoryBase<Booking>, IBookingRepository
{
    public JsonBookingRepository(string dataDir) : base(dataDir, "bookings.json") { }
    protected override int GetId(Booking item) => item.Id;

    public async Task<Booking?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        await EnsureLoadedAsync(ct);
        return _items.FirstOrDefault(b => b.Id == id);
    }
    public async Task<IReadOnlyList<Booking>> GetAllAsync(CancellationToken ct = default)
    {
        await EnsureLoadedAsync(ct);
        return _items.AsReadOnly();
    }
    public async Task<IReadOnlyList<Booking>> GetByGuestIdAsync(int guestId, CancellationToken ct = default)
    {
        await EnsureLoadedAsync(ct);
        return _items.Where(b => b.GuestId == guestId).ToList().AsReadOnly();
    }
    public async Task<IReadOnlyList<Booking>> GetByRoomIdAsync(int roomId, CancellationToken ct = default)
    {
        await EnsureLoadedAsync(ct);
        return _items.Where(b => b.RoomId == roomId).ToList().AsReadOnly();
    }
    public async Task<IReadOnlyList<Booking>> GetActiveAsync(CancellationToken ct = default)
    {
        await EnsureLoadedAsync(ct);
        return _items.Where(b => b.Status is BookingStatus.Confirmed or BookingStatus.CheckedIn)
                     .ToList().AsReadOnly();
    }
    public async Task<Booking> AddAsync(Booking booking, CancellationToken ct = default)
    {
        await EnsureLoadedAsync(ct);
        _items.Add(booking);
        return booking;
    }
    public async Task UpdateAsync(Booking booking, CancellationToken ct = default)
    {
        await EnsureLoadedAsync(ct);
        int idx = _items.FindIndex(b => b.Id == booking.Id);
        if (idx >= 0) _items[idx] = booking;
    }
}

public class JsonRoomRepository : JsonRepositoryBase<Room>, IRoomRepository
{
    public JsonRoomRepository(string dataDir) : base(dataDir, "rooms.json") { }
    protected override int GetId(Room item) => item.Id;

    public async Task<Room?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        await EnsureLoadedAsync(ct);
        return _items.FirstOrDefault(r => r.Id == id);
    }
    public async Task<IReadOnlyList<Room>> GetAllAsync(CancellationToken ct = default)
    {
        await EnsureLoadedAsync(ct);
        return _items.AsReadOnly();
    }
    public async Task<IReadOnlyList<Room>> GetAvailableAsync(DateTime checkIn, DateTime checkOut,
        int guestCount = 1, CancellationToken ct = default)
    {
        await EnsureLoadedAsync(ct);
        return _items.Where(r => r.IsAvailable() && r.Capacity >= guestCount)
                     .ToList().AsReadOnly();
    }
    public async Task<Room> AddAsync(Room room, CancellationToken ct = default)
    {
        await EnsureLoadedAsync(ct);
        _items.Add(room);
        return room;
    }
    public async Task UpdateAsync(Room room, CancellationToken ct = default)
    {
        await EnsureLoadedAsync(ct);
        int idx = _items.FindIndex(r => r.Id == room.Id);
        if (idx >= 0) _items[idx] = room;
    }
}

public class JsonGuestRepository : JsonRepositoryBase<Guest>, IGuestRepository
{
    public JsonGuestRepository(string dataDir) : base(dataDir, "guests.json") { }
    protected override int GetId(Guest item) => item.Id;

    public async Task<Guest?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        await EnsureLoadedAsync(ct);
        return _items.FirstOrDefault(g => g.Id == id);
    }
    public async Task<Guest?> GetByEmailAsync(string email, CancellationToken ct = default)
    {
        await EnsureLoadedAsync(ct);
        return _items.FirstOrDefault(g => g.Email == email.ToLowerInvariant());
    }
    public async Task<IReadOnlyList<Guest>> GetAllAsync(CancellationToken ct = default)
    {
        await EnsureLoadedAsync(ct);
        return _items.AsReadOnly();
    }
    public async Task<Guest> AddAsync(Guest guest, CancellationToken ct = default)
    {
        await EnsureLoadedAsync(ct);
        _items.Add(guest);
        return guest;
    }
    public async Task UpdateAsync(Guest guest, CancellationToken ct = default)
    {
        await EnsureLoadedAsync(ct);
        int idx = _items.FindIndex(g => g.Id == guest.Id);
        if (idx >= 0) _items[idx] = guest;
    }
}
