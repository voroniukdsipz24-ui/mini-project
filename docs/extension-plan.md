# Extension Plan — Самостійна 29

## Мета розширень
Закрити три виявлені прогалини курсу через осмислені розширення, а не випадкові "додаткові фічі".

---

## Розширення 1: Custom LINQ Extensions

**Проблема**: Фільтрація бронювань повторюється у кількох місцях.  
**Рішення**: `BookingExtensions.cs` — методи розширення для `IEnumerable<Booking>`.

```csharp
// src/HotelBooking.Application/Extensions/BookingExtensions.cs
public static class BookingExtensions
{
    public static IEnumerable<Booking> Active(this IEnumerable<Booking> bookings) =>
        bookings.Where(b => b.Status is BookingStatus.Confirmed or BookingStatus.CheckedIn);

    public static IEnumerable<Booking> ForPeriod(
        this IEnumerable<Booking> bookings, DateTime from, DateTime to) =>
        bookings.Where(b => b.CheckInDate < to && b.CheckOutDate > from);

    public static decimal TotalRevenue(this IEnumerable<Booking> bookings) =>
        bookings.Where(b => b.Status is not BookingStatus.Cancelled)
                .Sum(b => b.TotalPrice);

    public static IEnumerable<Booking> ForRoom(
        this IEnumerable<Booking> bookings, int roomId) =>
        bookings.Where(b => b.RoomId == roomId);
}
```

**Де використовується**: ReportService (спрощення запитів), BookingService (перевірка конфліктів).

---

## Розширення 2: Observer — Booking Events

**Проблема**: Немає механізму реакції на зміну статусу бронювання.  
**Рішення**: `IBookingEventHandler` + `BookingEventDispatcher`.

```csharp
// src/HotelBooking.Domain/Events/
public record BookingStatusChanged(int BookingId, BookingStatus OldStatus, BookingStatus NewStatus);

public interface IBookingEventHandler
{
    Task HandleAsync(BookingStatusChanged evt);
}

// src/HotelBooking.Application/Services/
public class ConsoleAuditHandler : IBookingEventHandler
{
    public Task HandleAsync(BookingStatusChanged evt)
    {
        Console.WriteLine($"[AUDIT] Booking #{evt.BookingId}: {evt.OldStatus} → {evt.NewStatus}");
        return Task.CompletedTask;
    }
}
```

**Де підключається**: BookingService отримує `IEnumerable<IBookingEventHandler>` і диспетчеризує після зміни статусу.

---

## Розширення 3: Decorator — Logging Service

**Проблема**: Логування розкидане по коду.  
**Рішення**: `LoggingBookingService` — Decorator навколо `BookingService`.

```csharp
public class LoggingBookingService : BookingService
{
    private readonly BookingService _inner;
    private readonly TextWriter _log;

    public LoggingBookingService(BookingService inner, TextWriter log)
        : base(null!) { _inner = inner; _log = log; }

    public new async Task<Booking> CreateBookingAsync(
        int guestId, int roomId, DateTime checkIn, DateTime checkOut)
    {
        await _log.WriteLineAsync($"[{DateTime.UtcNow:O}] CreateBooking: G{guestId} R{roomId}");
        var result = await _inner.CreateBookingAsync(guestId, roomId, checkIn, checkOut);
        await _log.WriteLineAsync($"[{DateTime.UtcNow:O}] Created #{result.Id}");
        return result;
    }
}
```
