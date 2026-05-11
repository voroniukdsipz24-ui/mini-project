using HotelBooking.Domain.Interfaces;
using HotelBooking.Infrastructure.Repositories;

namespace HotelBooking.Infrastructure;

/// <summary>
/// Unit of Work — координує збереження всіх репозиторіїв одночасно.
/// Патерн: Unit of Work + Facade.
/// </summary>
public class JsonUnitOfWork : IUnitOfWork
{
    private readonly JsonBookingRepository _bookings;
    private readonly JsonRoomRepository    _rooms;
    private readonly JsonGuestRepository   _guests;

    public IBookingRepository Bookings => _bookings;
    public IRoomRepository    Rooms    => _rooms;
    public IGuestRepository   Guests   => _guests;

    public JsonUnitOfWork(string dataDirectory)
    {
        _bookings = new JsonBookingRepository(dataDirectory);
        _rooms    = new JsonRoomRepository(dataDirectory);
        _guests   = new JsonGuestRepository(dataDirectory);
    }

    public async Task SaveAsync(CancellationToken ct = default)
    {
        await _bookings.PersistAsync(ct);
        await _rooms.PersistAsync(ct);
        await _guests.PersistAsync(ct);
    }
}
