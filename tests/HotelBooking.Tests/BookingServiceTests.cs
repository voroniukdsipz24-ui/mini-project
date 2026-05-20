using HotelBooking.Application.Services;
using HotelBooking.Domain.Entities;
using HotelBooking.Domain.Exceptions;
using HotelBooking.Tests.Fakes;

namespace HotelBooking.Tests;

public class BookingServiceTests
{
    private static (BookingService svc, FakeUnitOfWork uow) BuildSut(
        bool seedRoom = true, bool seedGuest = true)
    {
        var uow = new FakeUnitOfWork();

        if (seedRoom)
            uow.RoomRepo.Seed(new[]
            {
                new Room(1, 101, 1, RoomType.Standard, 100m, 2, "Test room")
            });

        if (seedGuest)
            uow.GuestRepo.Seed(new[]
            {
                new Guest(1, "Тест", "Гість", "test@test.com", "", "AA001", new DateTime(1990, 1, 1))
            });

        return (new BookingService(uow), uow);
    }

    [Fact]
    public async Task CreateBooking_ValidInput_ReturnsBooking()
    {
        var (svc, _) = BuildSut();
        var checkIn  = DateTime.Today.AddDays(5);
        var checkOut = DateTime.Today.AddDays(8);

        var booking = await svc.CreateBookingAsync(1, 1, checkIn, checkOut);

        Assert.Equal(BookingStatus.Pending, booking.Status);
        Assert.Equal(1, booking.RoomId);
        Assert.Equal(1, booking.GuestId);
        Assert.True(booking.TotalPrice > 0);
    }

    [Fact]
    public async Task CreateBooking_GuestNotFound_ThrowsGuestNotFoundException()
    {
        var (svc, _) = BuildSut(seedGuest: false);

        await Assert.ThrowsAsync<GuestNotFoundException>(() =>
            svc.CreateBookingAsync(99, 1, DateTime.Today.AddDays(1), DateTime.Today.AddDays(3)));
    }

    [Fact]
    public async Task CreateBooking_RoomNotFound_ThrowsRoomNotFoundException()
    {
        var (svc, _) = BuildSut(seedRoom: false);

        await Assert.ThrowsAsync<RoomNotFoundException>(() =>
            svc.CreateBookingAsync(1, 99, DateTime.Today.AddDays(1), DateTime.Today.AddDays(3)));
    }

    [Fact]
    public async Task CreateBooking_OverlappingDates_ThrowsRoomNotAvailable()
    {
        var (svc, _) = BuildSut();

        var checkIn  = DateTime.Today.AddDays(5);
        var checkOut = DateTime.Today.AddDays(8);

        // First booking
        await svc.CreateBookingAsync(1, 1, checkIn, checkOut);

        // Room is now Reserved — second attempt should fail
        await Assert.ThrowsAsync<RoomNotAvailableException>(() =>
            svc.CreateBookingAsync(1, 1, checkIn.AddDays(1), checkOut.AddDays(2)));
    }

    [Fact]
    public async Task ConfirmBooking_Pending_SetsConfirmed()
    {
        var (svc, _) = BuildSut();
        var b = await svc.CreateBookingAsync(1, 1, DateTime.Today.AddDays(2), DateTime.Today.AddDays(4));

        var confirmed = await svc.ConfirmBookingAsync(b.Id);
        Assert.Equal(BookingStatus.Confirmed, confirmed.Status);
    }

    [Fact]
    public async Task CheckIn_ConfirmedBooking_SetsCheckedIn()
    {
        var (svc, uow) = BuildSut();
        var b = await svc.CreateBookingAsync(1, 1, DateTime.Today.AddDays(1), DateTime.Today.AddDays(3));
        await svc.ConfirmBookingAsync(b.Id);

        var checkedIn = await svc.CheckInAsync(b.Id);

        Assert.Equal(BookingStatus.CheckedIn, checkedIn.Status);
        var room = await uow.Rooms.GetByIdAsync(1);
        Assert.Equal(RoomStatus.Occupied, room!.Status);
    }

    [Fact]
    public async Task CheckOut_CheckedIn_SetsCheckedOutAndRoomAvailable()
    {
        var (svc, uow) = BuildSut();
        var b = await svc.CreateBookingAsync(1, 1, DateTime.Today.AddDays(1), DateTime.Today.AddDays(3));
        await svc.ConfirmBookingAsync(b.Id);
        await svc.CheckInAsync(b.Id);

        var checkedOut = await svc.CheckOutAsync(b.Id);

        Assert.Equal(BookingStatus.CheckedOut, checkedOut.Status);
        Assert.Equal(PaymentStatus.Paid, checkedOut.PaymentStatus);
        var room = await uow.Rooms.GetByIdAsync(1);
        Assert.Equal(RoomStatus.Available, room!.Status);
    }

    [Fact]
    public async Task CancelBooking_Pending_SetsRoomAvailable()
    {
        var (svc, uow) = BuildSut();
        var b = await svc.CreateBookingAsync(1, 1, DateTime.Today.AddDays(5), DateTime.Today.AddDays(8));

        var cancelled = await svc.CancelBookingAsync(b.Id, "Тест скасування");

        Assert.Equal(BookingStatus.Cancelled, cancelled.Status);
        var room = await uow.Rooms.GetByIdAsync(1);
        Assert.Equal(RoomStatus.Available, room!.Status);
    }

    [Fact]
    public async Task GetBooking_NotFound_ReturnsNull()
    {
        var (svc, _) = BuildSut();
        var result = await svc.GetBookingAsync(999);
        Assert.Null(result);
    }
}

// ── GuestService tests ────────────────────────────────────────────────────

public class GuestServiceTests
{
    private static (GuestService svc, FakeUnitOfWork uow) BuildSut()
    {
        var uow = new FakeUnitOfWork();
        return (new GuestService(uow), uow);
    }

    [Fact]
    public async Task RegisterGuest_ValidData_ReturnsGuest()
    {
        var (svc, _) = BuildSut();
        var guest = await svc.RegisterGuestAsync(
            "Іван", "Франко", "ivan@test.com", "+380", "UA001", new DateTime(1980, 8, 27));

        Assert.Equal("Іван Франко", guest.FullName);
        Assert.Equal("ivan@test.com", guest.Email);
    }

    [Fact]
    public async Task RegisterGuest_DuplicateEmail_Throws()
    {
        var (svc, _) = BuildSut();
        await svc.RegisterGuestAsync("A", "B", "dup@test.com", "", "P1", DateTime.Today.AddYears(-20));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            svc.RegisterGuestAsync("C", "D", "dup@test.com", "", "P2", DateTime.Today.AddYears(-20)));
    }
}

// ── ReportService tests ───────────────────────────────────────────────────

public class ReportServiceTests
{
    [Fact]
    public async Task GetOccupancyReport_NoBookings_ReturnsZero()
    {
        var uow = new FakeUnitOfWork();
        uow.RoomRepo.Seed(new[] { new Room(1, 101, 1, RoomType.Standard, 100m, 2) });
        var svc = new ReportService(uow);

        var report = await svc.GetOccupancyReportAsync(DateTime.Today, DateTime.Today.AddDays(30));

        Assert.Equal(0m, report.TotalRevenue);
        Assert.Equal(0, report.OccupiedDays);
    }
}
