using HotelBooking.Application.Extensions;
using HotelBooking.Domain.Entities;
using HotelBooking.Infrastructure;

namespace HotelBooking.Tests;

// ── BookingExtensions (custom LINQ) tests ─────────────────────────────────

public class BookingExtensionsTests
{
    private static Booking MakeBooking(int id, int roomId, int guestId,
        BookingStatus status, int daysFromNow = 1, int nights = 3)
    {
        var b = new Booking(id, roomId, guestId,
            DateTime.Today.AddDays(daysFromNow),
            DateTime.Today.AddDays(daysFromNow + nights),
            100m);

        if (status == BookingStatus.Confirmed || status == BookingStatus.CheckedIn ||
            status == BookingStatus.CheckedOut || status == BookingStatus.Cancelled)
        {
            b.Confirm();
        }
        if (status == BookingStatus.CheckedIn || status == BookingStatus.CheckedOut)
            b.CheckIn();
        if (status == BookingStatus.CheckedOut)
            b.CheckOut();
        if (status == BookingStatus.Cancelled)
            b.Cancel("test");

        return b;
    }

    [Fact]
    public void Active_ReturnsOnlyConfirmedAndCheckedIn()
    {
        var bookings = new[]
        {
            MakeBooking(1, 1, 1, BookingStatus.Pending),
            MakeBooking(2, 2, 1, BookingStatus.Confirmed),
            MakeBooking(3, 3, 1, BookingStatus.CheckedIn),
            MakeBooking(4, 4, 1, BookingStatus.Cancelled),
        };

        var active = bookings.Active().ToList();

        Assert.Equal(2, active.Count);
        Assert.All(active, b =>
            Assert.True(b.Status is BookingStatus.Confirmed or BookingStatus.CheckedIn));
    }

    [Fact]
    public void ForRoom_FiltersCorrectly()
    {
        var bookings = new[]
        {
            MakeBooking(1, 10, 1, BookingStatus.Confirmed),
            MakeBooking(2, 20, 1, BookingStatus.Confirmed),
            MakeBooking(3, 10, 2, BookingStatus.Pending),
        };

        var forRoom10 = bookings.ForRoom(10).ToList();

        Assert.Equal(2, forRoom10.Count);
        Assert.All(forRoom10, b => Assert.Equal(10, b.RoomId));
    }

    [Fact]
    public void ForGuest_FiltersCorrectly()
    {
        var bookings = new[]
        {
            MakeBooking(1, 1, 100, BookingStatus.Confirmed),
            MakeBooking(2, 2, 200, BookingStatus.Confirmed),
            MakeBooking(3, 3, 100, BookingStatus.Pending),
        };

        var forGuest100 = bookings.ForGuest(100).ToList();

        Assert.Equal(2, forGuest100.Count);
    }

    [Fact]
    public void TotalRevenue_ExcludesCancelled()
    {
        var bookings = new[]
        {
            MakeBooking(1, 1, 1, BookingStatus.Confirmed,  nights: 2), // 200
            MakeBooking(2, 2, 1, BookingStatus.Cancelled,  nights: 3), // excluded
            MakeBooking(3, 3, 1, BookingStatus.CheckedOut, nights: 4), // 400 (after checkout price=400)
        };

        decimal revenue = bookings.TotalRevenue();

        Assert.True(revenue > 0);
        // cancelled booking excluded
        Assert.Equal(bookings.Where(b => b.Status != BookingStatus.Cancelled).Sum(b => b.TotalPrice), revenue);
    }

    [Fact]
    public void TotalNights_ExcludesCancelled()
    {
        var bookings = new[]
        {
            MakeBooking(1, 1, 1, BookingStatus.Confirmed,  nights: 3),
            MakeBooking(2, 2, 1, BookingStatus.Cancelled,  nights: 5),
        };

        int nights = bookings.TotalNights();

        Assert.Equal(3, nights);
    }

    [Fact]
    public void HasConflict_WhenOverlap_ReturnsTrue()
    {
        var existing = new Booking(1, 10, 1,
            DateTime.Today.AddDays(5),
            DateTime.Today.AddDays(10),
            100m);
        existing.Confirm();

        var bookings = new[] { existing };

        bool conflict = bookings.HasConflict(10,
            DateTime.Today.AddDays(7),
            DateTime.Today.AddDays(12));

        Assert.True(conflict);
    }

    [Fact]
    public void HasConflict_WhenNoOverlap_ReturnsFalse()
    {
        var existing = new Booking(1, 10, 1,
            DateTime.Today.AddDays(1),
            DateTime.Today.AddDays(4),
            100m);

        var bookings = new[] { existing };

        bool conflict = bookings.HasConflict(10,
            DateTime.Today.AddDays(4),
            DateTime.Today.AddDays(7));

        Assert.False(conflict);
    }

    [Fact]
    public void ByCheckIn_SortsAscending()
    {
        var b1 = MakeBooking(1, 1, 1, BookingStatus.Pending, daysFromNow: 10);
        var b2 = MakeBooking(2, 2, 1, BookingStatus.Pending, daysFromNow: 2);
        var b3 = MakeBooking(3, 3, 1, BookingStatus.Pending, daysFromNow: 5);

        var sorted = new[] { b1, b2, b3 }.ByCheckIn().ToList();

        Assert.Equal(b2.Id, sorted[0].Id);
        Assert.Equal(b3.Id, sorted[1].Id);
        Assert.Equal(b1.Id, sorted[2].Id);
    }

    [Fact]
    public void ForPeriod_IncludesOverlappingBookings()
    {
        // Booking: Day+2 to Day+5
        var b = MakeBooking(1, 1, 1, BookingStatus.Pending, daysFromNow: 2, nights: 3);

        // Period: Day+3 to Day+7 — overlaps
        var result = new[] { b }
            .ForPeriod(DateTime.Today.AddDays(3), DateTime.Today.AddDays(7))
            .ToList();

        Assert.Single(result);
    }
}

// ── Persistence Integration Tests ─────────────────────────────────────────

public class PersistenceTests : IDisposable
{
    private readonly string _tempDir;

    public PersistenceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"HotelBookingTest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    [Fact]
    public async Task JsonUnitOfWork_SaveAndReload_PreservesRooms()
    {
        // Arrange — save with first UoW instance
        var uow1 = new JsonUnitOfWork(_tempDir);
        var room = new Room(1, 101, 1, RoomType.Standard, 100m, 2, "Test");
        await uow1.Rooms.AddAsync(room);
        await uow1.SaveAsync();

        // Act — reload with second UoW instance (simulates app restart)
        var uow2 = new JsonUnitOfWork(_tempDir);
        var loaded = await uow2.Rooms.GetAllAsync();

        // Assert
        Assert.Single(loaded);
        Assert.Equal(101, loaded[0].Number);
        Assert.Equal(100m, loaded[0].PricePerNight);
        Assert.Equal(RoomType.Standard, loaded[0].Type);
    }

    [Fact]
    public async Task JsonUnitOfWork_SaveAndReload_PreservesGuests()
    {
        var uow1 = new JsonUnitOfWork(_tempDir);
        var guest = new Guest(1, "Іван", "Франко", "ivan@test.com", "+380", "UA001",
            new DateTime(1956, 8, 27));
        await uow1.Guests.AddAsync(guest);
        await uow1.SaveAsync();

        var uow2 = new JsonUnitOfWork(_tempDir);
        var loaded = await uow2.Guests.GetAllAsync();

        Assert.Single(loaded);
        Assert.Equal("ivan@test.com", loaded[0].Email);
        Assert.Equal("Іван Франко", loaded[0].FullName);
    }

    [Fact]
    public async Task JsonUnitOfWork_SaveAndReload_PreservesBookingStatus()
    {
        // Save a Confirmed booking
        var uow1 = new JsonUnitOfWork(_tempDir);
        var room = new Room(1, 101, 1, RoomType.Standard, 100m, 2);
        await uow1.Rooms.AddAsync(room);

        var booking = new Booking(1, 1, 1,
            DateTime.Today.AddDays(1), DateTime.Today.AddDays(3), 100m);
        booking.Confirm();
        await uow1.Bookings.AddAsync(booking);
        await uow1.SaveAsync();

        // Reload
        var uow2 = new JsonUnitOfWork(_tempDir);
        var loaded = await uow2.Bookings.GetAllAsync();

        Assert.Single(loaded);
        Assert.Equal(BookingStatus.Confirmed, loaded[0].Status);
        Assert.Equal(1, loaded[0].RoomId);
    }

    [Fact]
    public async Task EnsureLoadedAsync_MissingFile_StartsEmpty()
    {
        // No files exist in _tempDir
        var uow = new JsonUnitOfWork(_tempDir);
        var rooms = await uow.Rooms.GetAllAsync();
        Assert.Empty(rooms);
    }

    [Fact]
    public async Task EnsureLoadedAsync_CorruptedJson_StartsEmptyWithWarning()
    {
        // Write corrupted JSON
        await File.WriteAllTextAsync(
            Path.Combine(_tempDir, "rooms.json"),
            "{ this is not valid JSON !!!");

        var uow = new JsonUnitOfWork(_tempDir);

        // Should NOT throw — should recover gracefully
        var rooms = await uow.Rooms.GetAllAsync();
        Assert.Empty(rooms);
    }

    [Fact]
    public async Task NextIdAsync_AfterReload_ContinuesFromLastId()
    {
        var uow1 = new JsonUnitOfWork(_tempDir);
        await uow1.Rooms.AddAsync(new Room(1, 101, 1, RoomType.Standard, 100m, 2));
        await uow1.Rooms.AddAsync(new Room(2, 102, 1, RoomType.Deluxe,   150m, 2));
        await uow1.SaveAsync();

        var uow2 = new JsonUnitOfWork(_tempDir);
        int nextId = await uow2.Rooms.NextIdAsync();

        Assert.Equal(3, nextId);
    }
}
