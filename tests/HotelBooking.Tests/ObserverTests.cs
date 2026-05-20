using HotelBooking.Application.Services;
using HotelBooking.Domain.Entities;
using HotelBooking.Domain.Interfaces;
using HotelBooking.Infrastructure;
using HotelBooking.Tests.Fakes;

namespace HotelBooking.Tests;

/// <summary>
/// Тести Observer pattern: BookingService нотифікує IBookingEventHandler-ів
/// на ключові життєві події бронювання.
/// </summary>
public class ObserverTests
{
    private static (BookingService svc, InMemoryAuditLogHandler audit) BuildSutWithAudit()
    {
        var uow = new FakeUnitOfWork();
        uow.RoomRepo.Seed(new[] { new Room(1, 101, 1, RoomType.Standard, 100m, 2, "Test") });
        uow.GuestRepo.Seed(new[] { new Guest(1, "T", "G", "t@t.com", "", "AA001", new DateTime(1990, 1, 1)) });

        var audit = new InMemoryAuditLogHandler();
        var svc = new BookingService(uow, new IBookingEventHandler[] { audit });
        return (svc, audit);
    }

    [Fact]
    public async Task CreateBooking_PublishesEvent()
    {
        var (svc, audit) = BuildSutWithAudit();
        await svc.CreateBookingAsync(1, 1, DateTime.Today.AddDays(2), DateTime.Today.AddDays(4));

        Assert.Single(audit.Events);
        Assert.Equal(BookingStatus.Pending, audit.Events[0].NewStatus);
        Assert.Null(audit.Events[0].PreviousStatus);
    }

    [Fact]
    public async Task FullLifecycle_PublishesAllEvents()
    {
        var (svc, audit) = BuildSutWithAudit();
        var b = await svc.CreateBookingAsync(1, 1, DateTime.Today.AddDays(2), DateTime.Today.AddDays(4));
        await svc.ConfirmBookingAsync(b.Id);
        await svc.CheckInAsync(b.Id);
        await svc.CheckOutAsync(b.Id);

        Assert.Equal(4, audit.Events.Count);
        Assert.Equal(BookingStatus.Pending,    audit.Events[0].NewStatus);
        Assert.Equal(BookingStatus.Confirmed,  audit.Events[1].NewStatus);
        Assert.Equal(BookingStatus.CheckedIn,  audit.Events[2].NewStatus);
        Assert.Equal(BookingStatus.CheckedOut, audit.Events[3].NewStatus);

        // Перевіряємо, що PreviousStatus коректний у переходах
        Assert.Equal(BookingStatus.Pending,   audit.Events[1].PreviousStatus);
        Assert.Equal(BookingStatus.Confirmed, audit.Events[2].PreviousStatus);
        Assert.Equal(BookingStatus.CheckedIn, audit.Events[3].PreviousStatus);
    }

    [Fact]
    public async Task Cancel_PublishesEventWithReason()
    {
        var (svc, audit) = BuildSutWithAudit();
        var b = await svc.CreateBookingAsync(1, 1, DateTime.Today.AddDays(2), DateTime.Today.AddDays(4));
        await svc.CancelBookingAsync(b.Id, "guest changed plans");

        var cancelEvt = audit.Events.Last();
        Assert.Equal(BookingStatus.Cancelled, cancelEvt.NewStatus);
        Assert.Equal("guest changed plans", cancelEvt.Note);
    }

    [Fact]
    public async Task FailingHandler_DoesNotBreakOperation()
    {
        var uow = new FakeUnitOfWork();
        uow.RoomRepo.Seed(new[] { new Room(1, 101, 1, RoomType.Standard, 100m, 2, "Test") });
        uow.GuestRepo.Seed(new[] { new Guest(1, "T", "G", "t@t.com", "", "AA001", new DateTime(1990, 1, 1)) });

        var failingHandler = new FailingHandler();
        var goodHandler    = new InMemoryAuditLogHandler();
        var svc = new BookingService(uow, new IBookingEventHandler[] { failingHandler, goodHandler });

        // Має не кинути виняток, навіть якщо handler падає
        var b = await svc.CreateBookingAsync(1, 1, DateTime.Today.AddDays(2), DateTime.Today.AddDays(4));

        Assert.NotNull(b);
        Assert.Single(goodHandler.Events);   // Інший handler виконався успішно
    }

    [Fact]
    public async Task NoHandlers_OperationStillWorks()
    {
        var uow = new FakeUnitOfWork();
        uow.RoomRepo.Seed(new[] { new Room(1, 101, 1, RoomType.Standard, 100m, 2, "Test") });
        uow.GuestRepo.Seed(new[] { new Guest(1, "T", "G", "t@t.com", "", "AA001", new DateTime(1990, 1, 1)) });

        var svc = new BookingService(uow);   // без handlers
        var b = await svc.CreateBookingAsync(1, 1, DateTime.Today.AddDays(2), DateTime.Today.AddDays(4));

        Assert.NotNull(b);
    }

    private class FailingHandler : IBookingEventHandler
    {
        public Task HandleAsync(BookingEvent evt, CancellationToken ct = default)
            => throw new InvalidOperationException("Simulated handler failure");
    }
}

/// <summary>
/// Тест файлового аудит-логу (інтеграційний — пише на диск, чистить після).
/// </summary>
public class FileAuditLogTests : IDisposable
{
    private readonly string _dir;

    public FileAuditLogTests()
    {
        _dir = Path.Combine(Path.GetTempPath(), $"HotelAudit_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_dir);
    }

    public void Dispose() { if (Directory.Exists(_dir)) Directory.Delete(_dir, true); }

    [Fact]
    public async Task FileAudit_WritesLineToDisk()
    {
        var handler = new FileAuditLogHandler(_dir);
        var evt = new BookingEvent(42, BookingStatus.Confirmed, BookingStatus.Pending, DateTime.UtcNow, "test");
        await handler.HandleAsync(evt);

        var path = Path.Combine(_dir, "audit.log");
        Assert.True(File.Exists(path));
        var content = await File.ReadAllTextAsync(path);
        Assert.Contains("booking #42", content);
        Assert.Contains("Pending → Confirmed", content);
        Assert.Contains("test", content);
    }

    [Fact]
    public async Task FileAudit_AppendsMultipleEvents()
    {
        var handler = new FileAuditLogHandler(_dir);
        await handler.HandleAsync(new BookingEvent(1, BookingStatus.Pending, null, DateTime.UtcNow));
        await handler.HandleAsync(new BookingEvent(1, BookingStatus.Confirmed, BookingStatus.Pending, DateTime.UtcNow));
        await handler.HandleAsync(new BookingEvent(1, BookingStatus.CheckedIn, BookingStatus.Confirmed, DateTime.UtcNow));

        var lines = await File.ReadAllLinesAsync(Path.Combine(_dir, "audit.log"));
        Assert.Equal(3, lines.Length);
    }
}
