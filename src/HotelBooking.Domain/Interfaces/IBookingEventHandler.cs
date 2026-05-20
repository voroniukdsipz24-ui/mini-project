using HotelBooking.Domain.Entities;

namespace HotelBooking.Domain.Interfaces;

/// <summary>
/// Подія зміни статусу бронювання. Випускається BookingService після кожної
/// успішної зміни стану (Created, Confirmed, CheckedIn, CheckedOut, Cancelled).
/// </summary>
public record BookingEvent(
    int BookingId,
    BookingStatus NewStatus,
    BookingStatus? PreviousStatus,
    DateTime At,
    string? Note = null);

/// <summary>
/// Observer-інтерфейс: підписник на події бронювань.
/// Реалізації плагіняться через DI (декілька handlers за потребою).
/// Приклади: AuditLogHandler, EmailNotificationHandler, AnalyticsHandler.
/// </summary>
public interface IBookingEventHandler
{
    Task HandleAsync(BookingEvent evt, CancellationToken ct = default);
}
