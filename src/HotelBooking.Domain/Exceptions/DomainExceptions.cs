namespace HotelBooking.Domain.Exceptions;

public class DomainException : Exception
{
    public DomainException(string message) : base(message) { }
    public DomainException(string message, Exception inner) : base(message, inner) { }
}

public class RoomNotAvailableException : DomainException
{
    public int RoomId { get; }
    public RoomNotAvailableException(int roomId, DateTime checkIn, DateTime checkOut)
        : base($"Room {roomId} is not available from {checkIn:d} to {checkOut:d}.") => RoomId = roomId;
}

public class BookingNotFoundException : DomainException
{
    public BookingNotFoundException(int id) : base($"Booking #{id} not found.") { }
}

public class GuestNotFoundException : DomainException
{
    public GuestNotFoundException(int id) : base($"Guest #{id} not found.") { }
}

public class RoomNotFoundException : DomainException
{
    public RoomNotFoundException(int id) : base($"Room #{id} not found.") { }
}

public class InvalidBookingOperationException : DomainException
{
    public InvalidBookingOperationException(string message) : base(message) { }
}
