using HotelBooking.Domain.Entities;

namespace HotelBooking.Application.Extensions;

/// <summary>
/// Custom LINQ extensions for Booking collections.
/// Демонструє тему "custom extensions для LINQ" курсу.
/// </summary>
public static class BookingExtensions
{
    /// <summary>Повертає лише активні бронювання (Confirmed або CheckedIn).</summary>
    public static IEnumerable<Booking> Active(this IEnumerable<Booking> bookings) =>
        bookings.Where(b => b.Status is BookingStatus.Confirmed or BookingStatus.CheckedIn);

    /// <summary>Повертає бронювання у вказаному часовому проміжку.</summary>
    public static IEnumerable<Booking> ForPeriod(
        this IEnumerable<Booking> bookings, DateTime from, DateTime to) =>
        bookings.Where(b => b.CheckInDate < to && b.CheckOutDate > from);

    /// <summary>Сума доходу по незасупованих бронюваннях.</summary>
    public static decimal TotalRevenue(this IEnumerable<Booking> bookings) =>
        bookings.Where(b => b.Status is not BookingStatus.Cancelled)
                .Sum(b => b.TotalPrice);

    /// <summary>Фільтр за кімнатою.</summary>
    public static IEnumerable<Booking> ForRoom(
        this IEnumerable<Booking> bookings, int roomId) =>
        bookings.Where(b => b.RoomId == roomId);

    /// <summary>Фільтр за гостем.</summary>
    public static IEnumerable<Booking> ForGuest(
        this IEnumerable<Booking> bookings, int guestId) =>
        bookings.Where(b => b.GuestId == guestId);

    /// <summary>Перевірка конфліктів для конкретного номера та дат.</summary>
    public static bool HasConflict(
        this IEnumerable<Booking> bookings, int roomId, DateTime checkIn, DateTime checkOut) =>
        bookings
            .ForRoom(roomId)
            .Where(b => b.Status is not BookingStatus.Cancelled and not BookingStatus.CheckedOut)
            .Any(b => b.OverlapsWith(checkIn, checkOut));

    /// <summary>Повертає бронювання відсортовані за датою заїзду.</summary>
    public static IEnumerable<Booking> ByCheckIn(this IEnumerable<Booking> bookings) =>
        bookings.OrderBy(b => b.CheckInDate);

    /// <summary>Підрахунок ночей за колекцією бронювань.</summary>
    public static int TotalNights(this IEnumerable<Booking> bookings) =>
        bookings.Where(b => b.Status is not BookingStatus.Cancelled)
                .Sum(b => b.Nights);
}
