using HotelBooking.Domain.Entities;
using HotelBooking.Domain.Exceptions;
using HotelBooking.Domain.Interfaces;
using HotelBooking.Domain.Services;

namespace HotelBooking.Application.Services;

/// <summary>
/// Сервіс бізнес-логіки бронювань: оркеструє use cases і нотифікує спостерігачів.
/// Залежить лише від інтерфейсів Domain (DIP) — InMemory чи JSON UoW взаємозамінні.
/// Observer pattern: випускає <see cref="BookingEvent"/> через колекцію <see cref="IBookingEventHandler"/>.
/// </summary>
public class BookingService
{
    private readonly IUnitOfWork _uow;
    private readonly IEnumerable<IBookingEventHandler> _handlers;

    /// <summary>
    /// Конструктор. Handlers необов'язкові — якщо порожні, події просто не публікуються.
    /// </summary>
    public BookingService(IUnitOfWork uow, IEnumerable<IBookingEventHandler>? handlers = null)
    {
        _uow = uow;
        _handlers = handlers ?? Array.Empty<IBookingEventHandler>();
    }

    /// <summary>
    /// Створює нове бронювання з перевірками: існування гостя/номера,
    /// доступність номера, відсутність конфліктів дат. Розраховує ціну
    /// через <see cref="PricingEngine"/> і резервує номер.
    /// </summary>
    /// <exception cref="GuestNotFoundException">Гостя з таким Id не існує.</exception>
    /// <exception cref="RoomNotFoundException">Номера з таким Id не існує.</exception>
    /// <exception cref="RoomNotAvailableException">Номер недоступний або є конфлікт дат.</exception>
    public async Task<Booking> CreateBookingAsync(
        int guestId, int roomId, DateTime checkIn, DateTime checkOut)
    {
        var guest = await _uow.Guests.GetByIdAsync(guestId)
            ?? throw new GuestNotFoundException(guestId);

        var room = await _uow.Rooms.GetByIdAsync(roomId)
            ?? throw new RoomNotFoundException(roomId);

        if (!room.IsAvailable())
            throw new RoomNotAvailableException(roomId, checkIn, checkOut);

        var conflicts = await _uow.Bookings.GetByRoomIdAsync(roomId);
        bool hasConflict = conflicts
            .Where(b => b.Status is not BookingStatus.Cancelled and not BookingStatus.CheckedOut)
            .Any(b => b.OverlapsWith(checkIn, checkOut));

        if (hasConflict)
            throw new RoomNotAvailableException(roomId, checkIn, checkOut);

        decimal totalPrice = PricingEngine.Calculate(room, checkIn, checkOut);
        int id = await _uow.Bookings.NextIdAsync();

        var booking = new Booking(id, roomId, guestId, checkIn, checkOut,
                                  room.PricePerNight, notes: "", totalPrice: totalPrice);
        booking.Room  = room;
        booking.Guest = guest;

        await _uow.Bookings.AddAsync(booking);
        room.SetStatus(RoomStatus.Reserved);
        await _uow.Rooms.UpdateAsync(room);
        await _uow.SaveAsync();

        await NotifyAsync(new BookingEvent(booking.Id, BookingStatus.Pending, null, DateTime.UtcNow, "Booking created"));
        return booking;
    }

    /// <summary>Підтверджує бронювання (Pending → Confirmed).</summary>
    public async Task<Booking> ConfirmBookingAsync(int id)
    {
        var b = await GetOrThrow(id);
        var prev = b.Status;
        b.Confirm();
        await _uow.Bookings.UpdateAsync(b);
        await _uow.SaveAsync();
        await NotifyAsync(new BookingEvent(b.Id, b.Status, prev, DateTime.UtcNow));
        return b;
    }

    /// <summary>Заселяє гостя (Confirmed → CheckedIn), номер → Occupied.</summary>
    public async Task<Booking> CheckInAsync(int id)
    {
        var b = await GetOrThrow(id);
        var prev = b.Status;
        b.CheckIn();
        var room = await _uow.Rooms.GetByIdAsync(b.RoomId);
        if (room != null)
        {
            room.SetStatus(RoomStatus.Occupied);
            await _uow.Rooms.UpdateAsync(room);
        }
        await _uow.Bookings.UpdateAsync(b);
        await _uow.SaveAsync();
        await NotifyAsync(new BookingEvent(b.Id, b.Status, prev, DateTime.UtcNow));
        return b;
    }

    /// <summary>Виселяє гостя (CheckedIn → CheckedOut, оплата → Paid), номер → Available.</summary>
    public async Task<Booking> CheckOutAsync(int id)
    {
        var b = await GetOrThrow(id);
        var prev = b.Status;
        b.CheckOut();
        var room = await _uow.Rooms.GetByIdAsync(b.RoomId);
        if (room != null)
        {
            room.SetStatus(RoomStatus.Available);
            await _uow.Rooms.UpdateAsync(room);
        }
        await _uow.Bookings.UpdateAsync(b);
        await _uow.SaveAsync();
        await NotifyAsync(new BookingEvent(b.Id, b.Status, prev, DateTime.UtcNow));
        return b;
    }

    /// <summary>Скасовує бронювання (Pending/Confirmed → Cancelled), номер → Available.</summary>
    public async Task<Booking> CancelBookingAsync(int id, string reason = "")
    {
        var b = await GetOrThrow(id);
        var prev = b.Status;
        b.Cancel(reason);
        var room = await _uow.Rooms.GetByIdAsync(b.RoomId);
        if (room != null && room.Status == RoomStatus.Reserved)
        {
            room.SetStatus(RoomStatus.Available);
            await _uow.Rooms.UpdateAsync(room);
        }
        await _uow.Bookings.UpdateAsync(b);
        await _uow.SaveAsync();
        await NotifyAsync(new BookingEvent(b.Id, b.Status, prev, DateTime.UtcNow, reason));
        return b;
    }

    /// <summary>Повертає всі бронювання (read-only).</summary>
    public async Task<IReadOnlyList<Booking>> GetAllBookingsAsync() =>
        await _uow.Bookings.GetAllAsync();

    /// <summary>Повертає бронювання за Id або null, якщо не знайдено.</summary>
    public async Task<Booking?> GetBookingAsync(int id) =>
        await _uow.Bookings.GetByIdAsync(id);

    private async Task<Booking> GetOrThrow(int id) =>
        await _uow.Bookings.GetByIdAsync(id) ?? throw new BookingNotFoundException(id);

    /// <summary>
    /// Публікує подію всім зареєстрованим observer-ам. Помилки в handlers
    /// не перериваюсь основну операцію — лише логуються в stderr.
    /// </summary>
    private async Task NotifyAsync(BookingEvent evt)
    {
        foreach (var h in _handlers)
        {
            try { await h.HandleAsync(evt); }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[WARN] Event handler failed: {ex.Message}");
            }
        }
    }
}
