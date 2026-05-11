using HotelBooking.Domain.Entities;
using HotelBooking.Domain.Exceptions;
using HotelBooking.Domain.Interfaces;
using HotelBooking.Domain.Services;

namespace HotelBooking.Application.Services;

/// <summary>
/// Orchestrates booking use-cases. Depends only on domain interfaces (DIP).
/// </summary>
public class BookingService
{
    private readonly IUnitOfWork _uow;

    public BookingService(IUnitOfWork uow) => _uow = uow;

    // USE CASE 1: Create booking
    public async Task<Booking> CreateBookingAsync(
        int guestId, int roomId, DateTime checkIn, DateTime checkOut)
    {
        var guest = await _uow.Guests.GetByIdAsync(guestId)
            ?? throw new GuestNotFoundException(guestId);

        var room = await _uow.Rooms.GetByIdAsync(roomId)
            ?? throw new RoomNotFoundException(roomId);

        if (!room.IsAvailable())
            throw new RoomNotAvailableException(roomId, checkIn, checkOut);

        // Check for date conflicts with existing bookings
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

        return booking;
    }

    // USE CASE 2: Confirm booking
    public async Task<Booking> ConfirmBookingAsync(int bookingId)
    {
        var booking = await GetOrThrowAsync(bookingId);
        booking.Confirm();
        await _uow.Bookings.UpdateAsync(booking);
        await _uow.SaveAsync();
        return booking;
    }

    // USE CASE 3: Check-in
    public async Task<Booking> CheckInAsync(int bookingId)
    {
        var booking = await GetOrThrowAsync(bookingId);
        booking.CheckIn();

        var room = await _uow.Rooms.GetByIdAsync(booking.RoomId);
        if (room != null)
        {
            room.SetStatus(RoomStatus.Occupied);
            await _uow.Rooms.UpdateAsync(room);
        }

        await _uow.Bookings.UpdateAsync(booking);
        await _uow.SaveAsync();
        return booking;
    }

    // USE CASE 4: Check-out
    public async Task<Booking> CheckOutAsync(int bookingId)
    {
        var booking = await GetOrThrowAsync(bookingId);
        booking.CheckOut();
        booking.MarkPaid();

        var room = await _uow.Rooms.GetByIdAsync(booking.RoomId);
        if (room != null)
        {
            room.SetStatus(RoomStatus.Available);
            await _uow.Rooms.UpdateAsync(room);
        }

        await _uow.Bookings.UpdateAsync(booking);
        await _uow.SaveAsync();
        return booking;
    }

    // USE CASE 5: Cancel booking
    public async Task<Booking> CancelBookingAsync(int bookingId, string reason = "")
    {
        var booking = await GetOrThrowAsync(bookingId);
        booking.Cancel(reason);

        var room = await _uow.Rooms.GetByIdAsync(booking.RoomId);
        if (room != null && room.Status == RoomStatus.Reserved)
        {
            room.SetStatus(RoomStatus.Available);
            await _uow.Rooms.UpdateAsync(room);
        }

        await _uow.Bookings.UpdateAsync(booking);
        await _uow.SaveAsync();
        return booking;
    }

    public async Task<IReadOnlyList<Booking>> GetAllBookingsAsync() =>
        await _uow.Bookings.GetAllAsync();

    public async Task<IReadOnlyList<Booking>> GetGuestBookingsAsync(int guestId) =>
        await _uow.Bookings.GetByGuestIdAsync(guestId);

    public async Task<Booking?> GetBookingAsync(int id) =>
        await _uow.Bookings.GetByIdAsync(id);

    private async Task<Booking> GetOrThrowAsync(int id) =>
        await _uow.Bookings.GetByIdAsync(id) ?? throw new BookingNotFoundException(id);
}
