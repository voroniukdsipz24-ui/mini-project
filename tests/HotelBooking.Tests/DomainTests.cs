using HotelBooking.Domain.Entities;
using HotelBooking.Domain.Services;

namespace HotelBooking.Tests;

// ── Room entity tests ─────────────────────────────────────────────────────

public class RoomTests
{
    [Fact]
    public void Constructor_ValidArgs_CreatesAvailableRoom()
    {
        var room = new Room(1, 101, 1, RoomType.Standard, 100m, 2);
        Assert.Equal(RoomStatus.Available, room.Status);
        Assert.Equal(101, room.Number);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Constructor_InvalidNumber_Throws(int number)
    {
        Assert.Throws<ArgumentException>(() => new Room(1, number, 1, RoomType.Standard, 100m, 2));
    }

    [Fact]
    public void Constructor_ZeroPrice_Throws()
    {
        Assert.Throws<ArgumentException>(() => new Room(1, 101, 1, RoomType.Standard, 0m, 2));
    }

    [Fact]
    public void SetStatus_ChangesStatus()
    {
        var room = new Room(1, 101, 1, RoomType.Standard, 100m, 2);
        room.SetStatus(RoomStatus.Maintenance);
        Assert.Equal(RoomStatus.Maintenance, room.Status);
        Assert.False(room.IsAvailable());
    }

    [Fact]
    public void UpdatePrice_Negative_Throws()
    {
        var room = new Room(1, 101, 1, RoomType.Standard, 100m, 2);
        Assert.Throws<ArgumentException>(() => room.UpdatePrice(-10m));
    }
}

// ── Guest entity tests ────────────────────────────────────────────────────

public class GuestTests
{
    private static Guest Valid() =>
        new(1, "Олена", "Шевченко", "olena@test.com", "+380501234567", "AA123456", new DateTime(1990, 1, 1));

    [Fact]
    public void Constructor_Valid_SetsFullName()
    {
        var g = Valid();
        Assert.Equal("Олена Шевченко", g.FullName);
    }

    [Fact]
    public void Constructor_InvalidEmail_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            new Guest(1, "Олена", "Шевченко", "not-an-email", "", "", DateTime.Today));
    }

    [Fact]
    public void Constructor_EmptyFirstName_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            new Guest(1, "", "Шевченко", "a@b.com", "", "", DateTime.Today));
    }

    [Fact]
    public void Email_IsLowercased()
    {
        var g = new Guest(1, "A", "B", "UPPER@TEST.COM", "", "", DateTime.Today);
        Assert.Equal("upper@test.com", g.Email);
    }
}

// ── Booking entity tests ──────────────────────────────────────────────────

public class BookingTests
{
    private static Booking Valid(int nights = 3) =>
        new(1, roomId: 1, guestId: 1,
            checkIn: DateTime.Today.AddDays(1),
            checkOut: DateTime.Today.AddDays(1 + nights),
            pricePerNight: 100m);

    [Fact]
    public void Constructor_Valid_IsPending()
    {
        var b = Valid();
        Assert.Equal(BookingStatus.Pending, b.Status);
        Assert.Equal(PaymentStatus.Unpaid, b.PaymentStatus);
    }

    [Fact]
    public void Nights_CalculatedCorrectly()
    {
        var b = Valid(5);
        Assert.Equal(5, b.Nights);
    }

    [Fact]
    public void Constructor_PastCheckIn_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            new Booking(1, 1, 1, DateTime.Today.AddDays(-1), DateTime.Today.AddDays(2), 100m));
    }

    [Fact]
    public void Constructor_CheckOutBeforeCheckIn_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            new Booking(1, 1, 1, DateTime.Today.AddDays(3), DateTime.Today.AddDays(1), 100m));
    }

    [Fact]
    public void Confirm_FromPending_SetsConfirmed()
    {
        var b = Valid();
        b.Confirm();
        Assert.Equal(BookingStatus.Confirmed, b.Status);
    }

    [Fact]
    public void Confirm_AlreadyConfirmed_Throws()
    {
        var b = Valid();
        b.Confirm();
        Assert.Throws<InvalidOperationException>(() => b.Confirm());
    }

    [Fact]
    public void CheckIn_WithoutConfirm_Throws()
    {
        var b = Valid();
        Assert.Throws<InvalidOperationException>(() => b.CheckIn());
    }

    [Fact]
    public void CheckOut_AfterCheckIn_SetsCheckedOut()
    {
        var b = Valid();
        b.Confirm();
        b.CheckIn();
        b.CheckOut();
        Assert.Equal(BookingStatus.CheckedOut, b.Status);
    }

    [Fact]
    public void Cancel_WhenCheckedIn_Throws()
    {
        var b = Valid();
        b.Confirm();
        b.CheckIn();
        Assert.Throws<InvalidOperationException>(() => b.Cancel());
    }

    [Fact]
    public void OverlapsWith_Overlapping_ReturnsTrue()
    {
        var b = Valid(5); // Day+1 to Day+6
        Assert.True(b.OverlapsWith(
            DateTime.Today.AddDays(3),
            DateTime.Today.AddDays(8)));
    }

    [Fact]
    public void OverlapsWith_Adjacent_ReturnsFalse()
    {
        var b = Valid(3); // Day+1 to Day+4
        Assert.False(b.OverlapsWith(
            DateTime.Today.AddDays(4),
            DateTime.Today.AddDays(7)));
    }
}

// ── PricingEngine tests ───────────────────────────────────────────────────

public class PricingEngineTests
{
    private static Room MakeRoom(RoomType type, decimal price = 100m) =>
        new(1, 101, 1, type, price, 2);

    [Fact]
    public void Calculate_OffSeason_BasePriceTimesNights()
    {
        var room = MakeRoom(RoomType.Standard, 100m);
        var checkIn  = new DateTime(2025, 2, 10); // off-peak
        var checkOut = new DateTime(2025, 2, 13);
        decimal result = PricingEngine.Calculate(room, checkIn, checkOut);
        // 100 * 3 nights * 1.0 season = 300
        Assert.Equal(300m, result);
    }

    [Fact]
    public void Calculate_RoomTypeDoesNotAffectPrice()
    {
        // Type-multiplier видалений: ціна Suite = базова ціна × ночей × сезон
        var room = MakeRoom(RoomType.Suite, 100m);
        var checkIn  = new DateTime(2025, 2, 1);
        var checkOut = new DateTime(2025, 2, 3);
        decimal result = PricingEngine.Calculate(room, checkIn, checkOut);
        // 100 * 2 nights * 1.0 season = 200
        Assert.Equal(200m, result);
    }

    [Fact]
    public void Calculate_PeakSeason_AppliesMultiplier()
    {
        var room = MakeRoom(RoomType.Standard, 100m);
        var checkIn  = new DateTime(2025, 7, 1);
        var checkOut = new DateTime(2025, 7, 3);
        decimal result = PricingEngine.Calculate(room, checkIn, checkOut);
        // 100 * 2 nights * 1.25 season = 250
        Assert.Equal(250m, result);
    }

    [Fact]
    public void Calculate_InvalidDates_Throws()
    {
        var room = MakeRoom(RoomType.Standard);
        Assert.Throws<ArgumentException>(() =>
            PricingEngine.Calculate(room, new DateTime(2025, 5, 5), new DateTime(2025, 5, 3)));
    }
}
