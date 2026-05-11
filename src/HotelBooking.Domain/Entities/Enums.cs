namespace HotelBooking.Domain.Entities;

public enum RoomType
{
    Standard,
    Deluxe,
    Suite,
    Penthouse
}

public enum RoomStatus
{
    Available,
    Occupied,
    Maintenance,
    Reserved
}

public enum BookingStatus
{
    Pending,
    Confirmed,
    CheckedIn,
    CheckedOut,
    Cancelled
}

public enum PaymentStatus
{
    Unpaid,
    PartiallyPaid,
    Paid,
    Refunded
}
