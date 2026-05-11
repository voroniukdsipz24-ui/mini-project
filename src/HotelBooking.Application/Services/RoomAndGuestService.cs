using HotelBooking.Domain.Entities;
using HotelBooking.Domain.Exceptions;
using HotelBooking.Domain.Interfaces;

namespace HotelBooking.Application.Services;

public class RoomSearchService
{
    private readonly IUnitOfWork _uow;

    public RoomSearchService(IUnitOfWork uow) => _uow = uow;

    public async Task<IReadOnlyList<Room>> SearchAvailableAsync(
        DateTime checkIn, DateTime checkOut,
        RoomType? type = null,
        int guestCount = 1,
        decimal? maxPrice = null)
    {
        var rooms = await _uow.Rooms.GetAvailableAsync(checkIn, checkOut, guestCount);

        return rooms
            .Where(r => type == null || r.Type == type)
            .Where(r => maxPrice == null || r.PricePerNight <= maxPrice)
            .OrderBy(r => r.PricePerNight)
            .ToList()
            .AsReadOnly();
    }

    public async Task<IReadOnlyList<Room>> GetAllRoomsAsync() =>
        await _uow.Rooms.GetAllAsync();

    public async Task<Room?> GetRoomAsync(int id) =>
        await _uow.Rooms.GetByIdAsync(id);

    public async Task<Room> AddRoomAsync(int number, int floor, RoomType type,
        decimal pricePerNight, int capacity, string description = "")
    {
        var all = await _uow.Rooms.GetAllAsync();
        if (all.Any(r => r.Number == number))
            throw new InvalidOperationException($"Room number {number} already exists.");

        int id = await _uow.Rooms.NextIdAsync();
        var room = new Room(id, number, floor, type, pricePerNight, capacity, description);
        await _uow.Rooms.AddAsync(room);
        await _uow.SaveAsync();
        return room;
    }

    public async Task SetRoomStatusAsync(int roomId, RoomStatus status)
    {
        var room = await _uow.Rooms.GetByIdAsync(roomId)
            ?? throw new RoomNotFoundException(roomId);
        room.SetStatus(status);
        await _uow.Rooms.UpdateAsync(room);
        await _uow.SaveAsync();
    }
}

public class GuestService
{
    private readonly IUnitOfWork _uow;

    public GuestService(IUnitOfWork uow) => _uow = uow;

    public async Task<Guest> RegisterGuestAsync(
        string firstName, string lastName, string email,
        string phone, string passport, DateTime dob)
    {
        var existing = await _uow.Guests.GetByEmailAsync(email);
        if (existing != null)
            throw new InvalidOperationException($"Guest with email '{email}' already exists.");

        int id = await _uow.Guests.NextIdAsync();
        var guest = new Guest(id, firstName, lastName, email, phone, passport, dob);
        await _uow.Guests.AddAsync(guest);
        await _uow.SaveAsync();
        return guest;
    }

    public async Task<IReadOnlyList<Guest>> GetAllGuestsAsync() =>
        await _uow.Guests.GetAllAsync();

    public async Task<Guest?> FindGuestByEmailAsync(string email) =>
        await _uow.Guests.GetByEmailAsync(email);

    public async Task<Guest?> GetGuestAsync(int id) =>
        await _uow.Guests.GetByIdAsync(id);
}
