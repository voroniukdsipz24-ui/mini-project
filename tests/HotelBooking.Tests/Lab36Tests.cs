using HotelBooking.Application.Services;
using HotelBooking.Domain.Entities;
using HotelBooking.Domain.Exceptions;
using HotelBooking.Domain.Services;
using HotelBooking.Infrastructure;
using HotelBooking.Tests.Fakes;

namespace HotelBooking.Tests;

// ═══════════════════════════════════════════════════════════════════════════
// LAB 36 — UNIT TESTS: Theory / Boundary / State Machine / Domain Services
// ═══════════════════════════════════════════════════════════════════════════

public class RoomBoundaryTests
{
    // ── [Theory] параметризовані граничні значення ─────────────────────────

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Room_NonPositiveNumber_Throws(int number) =>
        Assert.Throws<ArgumentException>(() => new Room(1, number, 1, RoomType.Standard, 100m, 2));

    [Theory]
    [InlineData(0)]
    [InlineData(-0.01)]
    [InlineData(-999)]
    public void Room_NonPositivePrice_Throws(decimal price) =>
        Assert.Throws<ArgumentException>(() => new Room(1, 101, 1, RoomType.Standard, price, 2));

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Room_NonPositiveCapacity_Throws(int capacity) =>
        Assert.Throws<ArgumentException>(() => new Room(1, 101, 1, RoomType.Standard, 100m, capacity));

    [Theory]
    [InlineData(RoomType.Standard)]
    [InlineData(RoomType.Deluxe)]
    [InlineData(RoomType.Suite)]
    [InlineData(RoomType.Penthouse)]
    public void PricingEngine_RoomType_DoesNotAffectPrice(RoomType type)
    {
        // Тип номера НЕ впливає на ціну — базова ціна за ніч уже відображає тип.
        var room     = new Room(1, 101, 1, type, 100m, 2);
        var checkIn  = new DateTime(2025, 2, 1);   // off-peak
        var checkOut = new DateTime(2025, 2, 3);   // 2 nights

        decimal result = PricingEngine.Calculate(room, checkIn, checkOut);

        // 100 × 2 nights × 1.0 season = 200 для будь-якого типу
        Assert.Equal(200m, result);
    }

    [Theory]
    [InlineData(7,  1.25)]  // July = peak
    [InlineData(8,  1.25)]  // August = peak
    [InlineData(12, 1.25)]  // December = peak
    [InlineData(4,  1.10)]  // April = shoulder
    [InlineData(9,  1.10)]  // September = shoulder
    [InlineData(11, 1.00)]  // November = off-peak
    [InlineData(1,  1.00)]  // January = off-peak
    public void PricingEngine_SeasonMultipliers_AreCorrect(int month, double expectedMultiplier)
    {
        var room     = new Room(1, 101, 1, RoomType.Standard, 100m, 2);
        var checkIn  = new DateTime(2025, month, 1);
        var checkOut = new DateTime(2025, month, 3);  // 2 nights

        decimal result = PricingEngine.Calculate(room, checkIn, checkOut);

        decimal expected = Math.Round(100m * 2 * (decimal)expectedMultiplier, 2);
        Assert.Equal(expected, result);
    }
}

public class BookingStateMachineTests
{
    private static Booking MakePending() =>
        new(1, 1, 1, DateTime.Today.AddDays(1), DateTime.Today.AddDays(4), 100m);

    // ── Всі заборонені переходи стану ─────────────────────────────────────

    [Fact]
    public void Confirm_FromCancelled_Throws()
    {
        var b = MakePending();
        b.Cancel("test");
        Assert.Throws<InvalidOperationException>(() => b.Confirm());
    }

    [Fact]
    public void CheckIn_FromPending_Throws()
    {
        var b = MakePending();
        Assert.Throws<InvalidOperationException>(() => b.CheckIn());
    }

    [Fact]
    public void CheckIn_FromCancelled_Throws()
    {
        var b = MakePending();
        b.Cancel();
        Assert.Throws<InvalidOperationException>(() => b.CheckIn());
    }

    [Fact]
    public void CheckOut_FromPending_Throws()
    {
        var b = MakePending();
        Assert.Throws<InvalidOperationException>(() => b.CheckOut());
    }

    [Fact]
    public void CheckOut_FromConfirmed_Throws()
    {
        var b = MakePending();
        b.Confirm();
        Assert.Throws<InvalidOperationException>(() => b.CheckOut());
    }

    [Fact]
    public void Cancel_FromCheckedOut_Throws()
    {
        var b = MakePending();
        b.Confirm(); b.CheckIn(); b.CheckOut();
        Assert.Throws<InvalidOperationException>(() => b.Cancel());
    }

    // ── Дозволені переходи ────────────────────────────────────────────────

    [Fact]
    public void FullLifecycle_Pending_Confirmed_CheckedIn_CheckedOut()
    {
        var b = MakePending();
        Assert.Equal(BookingStatus.Pending, b.Status);

        b.Confirm();
        Assert.Equal(BookingStatus.Confirmed, b.Status);

        b.CheckIn();
        Assert.Equal(BookingStatus.CheckedIn, b.Status);

        b.CheckOut();
        Assert.Equal(BookingStatus.CheckedOut, b.Status);
        Assert.Equal(PaymentStatus.Paid, b.PaymentStatus);
    }

    [Fact]
    public void Cancel_FromPending_SetsCancelled()
    {
        var b = MakePending();
        b.Cancel("guest request");
        Assert.Equal(BookingStatus.Cancelled, b.Status);
        Assert.Contains("guest request", b.Notes);
    }

    [Fact]
    public void Cancel_FromConfirmed_SetsCancelled()
    {
        var b = MakePending();
        b.Confirm();
        b.Cancel("changed mind");
        Assert.Equal(BookingStatus.Cancelled, b.Status);
    }

    // ── Граничні значення дат ─────────────────────────────────────────────

    [Fact]
    public void Booking_CheckInToday_IsAllowed()
    {
        // today is valid (not in the past)
        var b = new Booking(1, 1, 1, DateTime.Today, DateTime.Today.AddDays(1), 100m);
        Assert.Equal(BookingStatus.Pending, b.Status);
    }

    [Fact]
    public void Booking_OneNight_IsMinimalValid()
    {
        var b = new Booking(1, 1, 1,
            DateTime.Today.AddDays(1),
            DateTime.Today.AddDays(2),
            100m);
        Assert.Equal(1, b.Nights);
    }

    [Fact]
    public void Booking_NightsCalculation_MultipleNights()
    {
        var b = new Booking(1, 1, 1,
            DateTime.Today.AddDays(1),
            DateTime.Today.AddDays(8),
            100m);
        Assert.Equal(7, b.Nights);
    }
}

public class GuestBoundaryTests
{
    [Theory]
    [InlineData("notanemail")]
    [InlineData("missing-at-sign")]
    [InlineData("")]
    [InlineData("   ")]
    public void Guest_InvalidEmail_Throws(string email) =>
        Assert.Throws<ArgumentException>(() =>
            new Guest(1, "A", "B", email, "", "", DateTime.Today.AddYears(-20)));

    [Theory]
    [InlineData("", "Lastname")]
    [InlineData("   ", "Lastname")]
    public void Guest_EmptyFirstName_Throws(string firstName, string lastName) =>
        Assert.Throws<ArgumentException>(() =>
            new Guest(1, firstName, lastName, "a@b.com", "", "", DateTime.Today.AddYears(-20)));

    [Theory]
    [InlineData("Firstname", "")]
    [InlineData("Firstname", "   ")]
    public void Guest_EmptyLastName_Throws(string firstName, string lastName) =>
        Assert.Throws<ArgumentException>(() =>
            new Guest(1, firstName, lastName, "a@b.com", "", "", DateTime.Today.AddYears(-20)));

    [Fact]
    public void Guest_Email_AlwaysLowercased()
    {
        var g = new Guest(1, "A", "B", "UPPER@DOMAIN.COM", "", "", DateTime.Today.AddYears(-20));
        Assert.Equal("upper@domain.com", g.Email);
    }

    [Fact]
    public void Guest_FullName_CombinesFirstAndLast()
    {
        var g = new Guest(1, "Тарас", "Шевченко", "t@s.com", "", "", new DateTime(1814, 3, 9));
        Assert.Equal("Тарас Шевченко", g.FullName);
    }

    [Fact]
    public void Guest_Age_CalculatedCorrectly()
    {
        var dob = DateTime.Today.AddYears(-30);
        var g = new Guest(1, "A", "B", "a@b.com", "", "", dob);
        Assert.Equal(30, g.Age);
    }
}

public class HotelEntityTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(6)]
    [InlineData(10)]
    public void Hotel_InvalidStarRating_Throws(int stars) =>
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new Hotel(1, "Test", "Addr", stars, 5));

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(5)]
    public void Hotel_ValidStarRating_CreatesHotel(int stars)
    {
        var h = new Hotel(1, "Grand", "Kyiv", stars, 10);
        Assert.Equal(stars, h.StarRating);
    }

    [Fact]
    public void Hotel_EmptyName_Throws() =>
        Assert.Throws<ArgumentException>(() => new Hotel(1, "", "Addr", 4, 5));

    [Fact]
    public void Hotel_Display_ContainsNameAndStars()
    {
        var h = new Hotel(1, "Grand Palais", "Kyiv", 5, 10);
        Assert.Contains("Grand Palais", h.Display());
        Assert.Contains("5★", h.Display());
    }
}

public class EntityBasePolymorphismTests
{
    [Fact]
    public void Room_Display_ContainsRoomInfo()
    {
        var r = new Room(1, 101, 2, RoomType.Deluxe, 120m, 2, "test");
        var display = r.Display();
        Assert.Contains("101", display);
        Assert.Contains("Deluxe", display);
    }

    [Fact]
    public void Guest_Display_ContainsFullName()
    {
        var g = new Guest(1, "Іван", "Франко", "i@f.com", "", "", new DateTime(1856, 8, 27));
        Assert.Contains("Іван Франко", g.Display());
    }

    [Fact]
    public void Booking_Display_ContainsStatus()
    {
        var b = new Booking(1, 1, 1, DateTime.Today.AddDays(1), DateTime.Today.AddDays(3), 100m);
        Assert.Contains("Pending", b.Display());
    }

    [Fact]
    public void EntityBase_Equals_SameIdSameType_AreEqual()
    {
        var r1 = new Room(1, 101, 1, RoomType.Standard, 100m, 2);
        var r2 = new Room(1, 202, 2, RoomType.Deluxe,   200m, 3);
        Assert.Equal(r1, r2); // Same Id, same type → equal
    }

    [Fact]
    public void EntityBase_Equals_DifferentId_NotEqual()
    {
        var r1 = new Room(1, 101, 1, RoomType.Standard, 100m, 2);
        var r2 = new Room(2, 101, 1, RoomType.Standard, 100m, 2);
        Assert.NotEqual(r1, r2);
    }
}


// ═══════════════════════════════════════════════════════════════════════════
// LAB 36 — INTEGRATION TESTS: Full cycle + fault handling
// ═══════════════════════════════════════════════════════════════════════════

public class FullCycleIntegrationTests : IDisposable
{
    private readonly string _dir;
    public FullCycleIntegrationTests()
    {
        _dir = Path.Combine(Path.GetTempPath(), $"HBTest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_dir);
    }
    public void Dispose() { if (Directory.Exists(_dir)) Directory.Delete(_dir, true); }

    private static (BookingService bs, RoomSearchService rs, GuestService gs) BuildServices(IUnitOfWork uow)
    {
        return (new BookingService(uow), new RoomSearchService(uow), new GuestService(uow));
    }

    // ── Integration: save → reload → business operation ───────────────────

    [Fact]
    public async Task CreateBooking_SaveReload_BookingPersistedAndUsable()
    {
        // Session 1: create data
        var uow1 = new JsonUnitOfWork(_dir);
        var (bs1, rs1, gs1) = BuildServices(uow1);
        await rs1.AddRoomAsync(101, 1, RoomType.Standard, 100m, 2);
        await gs1.RegisterGuestAsync("Олена", "Шевченко", "o@s.com", "+380", "AA1", new DateTime(1990, 1, 1));
        var booking = await bs1.CreateBookingAsync(1, 1, DateTime.Today.AddDays(1), DateTime.Today.AddDays(3));
        await uow1.SaveAsync();

        // Session 2: reload and operate
        var uow2 = new JsonUnitOfWork(_dir);
        var (bs2, _, _) = BuildServices(uow2);
        var confirmed = await bs2.ConfirmBookingAsync(booking.Id);
        await uow2.SaveAsync();

        // Session 3: verify
        var uow3 = new JsonUnitOfWork(_dir);
        var reloaded = await uow3.Bookings.GetByIdAsync(booking.Id);
        Assert.NotNull(reloaded);
        Assert.Equal(BookingStatus.Confirmed, reloaded!.Status);
    }

    [Fact]
    public async Task FullLifecycle_CreateConfirmCheckinCheckout_AllPersistedCorrectly()
    {
        var uow = new JsonUnitOfWork(_dir);
        var (bs, rs, gs) = BuildServices(uow);

        await rs.AddRoomAsync(101, 1, RoomType.Standard, 100m, 2);
        await gs.RegisterGuestAsync("Тест", "Гість", "t@g.com", "", "P1", new DateTime(1990, 1, 1));

        var b = await bs.CreateBookingAsync(1, 1, DateTime.Today.AddDays(1), DateTime.Today.AddDays(3));
        await bs.ConfirmBookingAsync(b.Id);
        await bs.CheckInAsync(b.Id);
        await bs.CheckOutAsync(b.Id);
        await uow.SaveAsync();

        // Reload and verify final state
        var uow2 = new JsonUnitOfWork(_dir);
        var reloaded = await uow2.Bookings.GetByIdAsync(b.Id);
        var room = await uow2.Rooms.GetByIdAsync(1);

        Assert.Equal(BookingStatus.CheckedOut, reloaded!.Status);
        Assert.Equal(PaymentStatus.Paid, reloaded.PaymentStatus);
        Assert.Equal(RoomStatus.Available, room!.Status);
    }

    [Fact]
    public async Task CancelBooking_SaveReload_RoomIsAvailableAfterReload()
    {
        var uow = new JsonUnitOfWork(_dir);
        var (bs, rs, gs) = BuildServices(uow);

        await rs.AddRoomAsync(201, 2, RoomType.Deluxe, 120m, 2);
        await gs.RegisterGuestAsync("A", "B", "a@b.com", "", "P2", new DateTime(1985, 5, 5));

        var b = await bs.CreateBookingAsync(1, 1, DateTime.Today.AddDays(5), DateTime.Today.AddDays(8));
        await bs.CancelBookingAsync(b.Id, "test reason");
        await uow.SaveAsync();

        var uow2 = new JsonUnitOfWork(_dir);
        var room = await uow2.Rooms.GetByIdAsync(1);
        var cancelled = await uow2.Bookings.GetByIdAsync(b.Id);

        Assert.Equal(RoomStatus.Available, room!.Status);
        Assert.Equal(BookingStatus.Cancelled, cancelled!.Status);
    }

    [Fact]
    public async Task MultipleBookings_DifferentRooms_AllPersistedCorrectly()
    {
        var uow = new JsonUnitOfWork(_dir);
        var (bs, rs, gs) = BuildServices(uow);

        await rs.AddRoomAsync(101, 1, RoomType.Standard, 100m, 2);
        await rs.AddRoomAsync(201, 2, RoomType.Deluxe,   150m, 2);
        await gs.RegisterGuestAsync("G1", "L1", "g1@t.com", "", "P1", new DateTime(1990, 1, 1));
        await gs.RegisterGuestAsync("G2", "L2", "g2@t.com", "", "P2", new DateTime(1991, 1, 1));

        var b1 = await bs.CreateBookingAsync(1, 1, DateTime.Today.AddDays(1), DateTime.Today.AddDays(3));
        var b2 = await bs.CreateBookingAsync(2, 2, DateTime.Today.AddDays(2), DateTime.Today.AddDays(5));
        await uow.SaveAsync();

        var uow2 = new JsonUnitOfWork(_dir);
        var all = await uow2.Bookings.GetAllAsync();

        Assert.Equal(2, all.Count);
        Assert.Contains(all, b => b.Id == b1.Id);
        Assert.Contains(all, b => b.Id == b2.Id);
    }

    // ── Integration: fault handling ───────────────────────────────────────

    [Fact]
    public async Task CorruptedBookingsFile_GracefulRecovery_StartsEmpty()
    {
        await File.WriteAllTextAsync(
            Path.Combine(_dir, "bookings.json"),
            "<<< THIS IS NOT JSON >>>");

        var uow = new JsonUnitOfWork(_dir);
        var bookings = await uow.Bookings.GetAllAsync(); // must NOT throw

        Assert.Empty(bookings);
    }

    [Fact]
    public async Task EmptyJsonArray_LoadsAsEmptyCollection()
    {
        await File.WriteAllTextAsync(Path.Combine(_dir, "rooms.json"), "[]");

        var uow = new JsonUnitOfWork(_dir);
        var rooms = await uow.Rooms.GetAllAsync();

        Assert.Empty(rooms);
    }

    [Fact]
    public async Task ConflictDetection_AfterReload_StillPreventsDoubleBooking()
    {
        // Create first booking and save
        var uow1 = new JsonUnitOfWork(_dir);
        var (bs1, rs1, gs1) = BuildServices(uow1);
        await rs1.AddRoomAsync(101, 1, RoomType.Standard, 100m, 2);
        await gs1.RegisterGuestAsync("G1", "L1", "g1@t.com", "", "P1", new DateTime(1990, 1, 1));
        await bs1.CreateBookingAsync(1, 1, DateTime.Today.AddDays(5), DateTime.Today.AddDays(8));
        await uow1.SaveAsync();

        // Reload — room should be Reserved, conflict should be detected
        var uow2 = new JsonUnitOfWork(_dir);
        var (bs2, _, _) = BuildServices(uow2);

        // Same room, overlapping dates → must throw
        await Assert.ThrowsAsync<RoomNotAvailableException>(() =>
            bs2.CreateBookingAsync(1, 1, DateTime.Today.AddDays(6), DateTime.Today.AddDays(10)));
    }

    [Fact]
    public async Task NextId_AfterMultipleSaveReloads_IsAlwaysUnique()
    {
        var uow1 = new JsonUnitOfWork(_dir);
        await uow1.Rooms.AddAsync(new Room(1, 101, 1, RoomType.Standard, 100m, 2));
        await uow1.Rooms.AddAsync(new Room(2, 102, 1, RoomType.Standard, 100m, 2));
        await uow1.SaveAsync();

        var uow2 = new JsonUnitOfWork(_dir);
        int nextId = await uow2.Rooms.NextIdAsync();

        Assert.Equal(3, nextId);

        await uow2.Rooms.AddAsync(new Room(nextId, 103, 1, RoomType.Standard, 100m, 2));
        await uow2.SaveAsync();

        var uow3 = new JsonUnitOfWork(_dir);
        int nextId2 = await uow3.Rooms.NextIdAsync();
        Assert.Equal(4, nextId2);
    }
}

// ═══════════════════════════════════════════════════════════════════════════
// LAB 36 — FAULT HANDLING: доменні винятки, негативні сценарії
// ═══════════════════════════════════════════════════════════════════════════

public class FaultHandlingTests
{
    private static (BookingService svc, FakeUnitOfWork uow) BuildSut()
    {
        var uow = new FakeUnitOfWork();
        uow.RoomRepo.Seed(new[] { new Room(1, 101, 1, RoomType.Standard, 100m, 2) });
        uow.GuestRepo.Seed(new[] { new Guest(1, "Test", "Guest", "t@g.com", "", "P1", new DateTime(1990, 1, 1)) });
        return (new BookingService(uow), uow);
    }

    [Fact]
    public async Task GuestNotFoundException_IsTyped_DomainException()
    {
        var (svc, _) = BuildSut();
        var ex = await Assert.ThrowsAsync<GuestNotFoundException>(() =>
            svc.CreateBookingAsync(999, 1, DateTime.Today.AddDays(1), DateTime.Today.AddDays(3)));
        Assert.IsAssignableFrom<DomainException>(ex);
        Assert.Contains("999", ex.Message);
    }

    [Fact]
    public async Task RoomNotFoundException_IsTyped_DomainException()
    {
        var (svc, _) = BuildSut();
        var ex = await Assert.ThrowsAsync<RoomNotFoundException>(() =>
            svc.CreateBookingAsync(1, 999, DateTime.Today.AddDays(1), DateTime.Today.AddDays(3)));
        Assert.IsAssignableFrom<DomainException>(ex);
        Assert.Contains("999", ex.Message);
    }

    [Fact]
    public async Task RoomNotAvailableException_ContainsRoomIdAndDates()
    {
        var (svc, _) = BuildSut();
        // First booking claims the room
        await svc.CreateBookingAsync(1, 1, DateTime.Today.AddDays(5), DateTime.Today.AddDays(8));

        var ex = await Assert.ThrowsAsync<RoomNotAvailableException>(() =>
            svc.CreateBookingAsync(1, 1, DateTime.Today.AddDays(6), DateTime.Today.AddDays(9)));

        Assert.Equal(1, ex.RoomId);
        Assert.IsAssignableFrom<DomainException>(ex);
    }

    [Fact]
    public async Task BookingNotFoundException_ThrownOnMissingId()
    {
        var (svc, _) = BuildSut();
        var ex = await Assert.ThrowsAsync<BookingNotFoundException>(() =>
            svc.ConfirmBookingAsync(9999));
        Assert.IsAssignableFrom<DomainException>(ex);
    }

    [Fact]
    public async Task CheckIn_AfterCancellation_ThrowsInvalidOperation()
    {
        var (svc, _) = BuildSut();
        var b = await svc.CreateBookingAsync(1, 1, DateTime.Today.AddDays(1), DateTime.Today.AddDays(3));
        await svc.CancelBookingAsync(b.Id, "test");

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            svc.ConfirmBookingAsync(b.Id));
    }

    [Fact]
    public async Task RegisterGuest_DuplicateEmail_ThrowsInvalidOperation()
    {
        var uow = new FakeUnitOfWork();
        var gs = new GuestService(uow);
        await gs.RegisterGuestAsync("A", "B", "dup@test.com", "", "P1", new DateTime(1990, 1, 1));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            gs.RegisterGuestAsync("C", "D", "dup@test.com", "", "P2", new DateTime(1990, 1, 1)));
    }
}
