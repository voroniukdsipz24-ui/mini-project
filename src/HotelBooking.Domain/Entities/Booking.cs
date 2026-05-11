namespace HotelBooking.Domain.Entities;

using System.Text.Json.Serialization;

/// <summary>
/// Бронювання — aggregate з власним state machine.
/// Наслідує EntityBase для поліморфного Display() та ідентифікації.
/// </summary>
public class Booking : EntityBase
{
    public int RoomId          { get; private set; }
    public int GuestId         { get; private set; }
    public DateTime CheckInDate  { get; private set; }
    public DateTime CheckOutDate { get; private set; }
    public BookingStatus Status  { get; private set; }
    public PaymentStatus PaymentStatus { get; private set; }
    public decimal TotalPrice  { get; private set; }
    public string Notes        { get; private set; }
    public DateTime CreatedAt  { get; private set; }

    // Navigation (populated by application layer)
    public Room?  Room  { get; set; }
    public Guest? Guest { get; set; }

    /// <summary>
    /// Constructor for JSON deserialization — параметри точно відповідають іменам властивостей.
    /// </summary>
    [JsonConstructor]
    public Booking(int id, int roomId, int guestId,
                   DateTime checkInDate, DateTime checkOutDate,
                   BookingStatus status, PaymentStatus paymentStatus,
                   decimal totalPrice, string notes, DateTime createdAt)
    {
        Id            = id;
        RoomId        = roomId;
        GuestId       = guestId;
        CheckInDate   = checkInDate;
        CheckOutDate  = checkOutDate;
        Status        = status;
        PaymentStatus = paymentStatus;
        TotalPrice    = totalPrice;
        Notes         = notes ?? string.Empty;
        CreatedAt     = createdAt;
    }

    /// <summary>
    /// Domain constructor — для створення нових бронювань через бізнес-логіку.
    /// Якщо totalPrice не передано — обчислюється як pricePerNight × кількість ночей.
    /// </summary>
    public Booking(int id, int roomId, int guestId,
                   DateTime checkIn, DateTime checkOut,
                   decimal pricePerNight, string notes = "",
                   decimal? totalPrice = null)
    {
        if (checkIn.Date < DateTime.Today)
            throw new ArgumentException("Check-in cannot be in the past.");
        if (checkOut <= checkIn)
            throw new ArgumentException("Check-out must be after check-in.");

        Id            = id;
        RoomId        = roomId;
        GuestId       = guestId;
        CheckInDate   = checkIn.Date;
        CheckOutDate  = checkOut.Date;
        Notes         = notes;
        Status        = BookingStatus.Pending;
        PaymentStatus = PaymentStatus.Unpaid;
        CreatedAt     = DateTime.UtcNow;
        TotalPrice    = totalPrice ?? pricePerNight * Nights;
    }

    public int Nights => (CheckOutDate - CheckInDate).Days;

    public void Confirm()
    {
        if (Status != BookingStatus.Pending)
            throw new InvalidOperationException($"Cannot confirm booking in {Status} status.");
        Status = BookingStatus.Confirmed;
    }

    public void CheckIn()
    {
        if (Status != BookingStatus.Confirmed)
            throw new InvalidOperationException("Booking must be confirmed before check-in.");
        Status = BookingStatus.CheckedIn;
    }

    public void CheckOut()
    {
        if (Status != BookingStatus.CheckedIn)
            throw new InvalidOperationException("Guest is not checked in.");
        Status = BookingStatus.CheckedOut;
        // CheckOut завжди завершує оплату — гість розраховується при виїзді
        PaymentStatus = PaymentStatus.Paid;
    }

    public void Cancel(string reason = "")
    {
        if (Status is BookingStatus.CheckedIn or BookingStatus.CheckedOut)
            throw new InvalidOperationException("Cannot cancel an active or completed booking.");
        Status = BookingStatus.Cancelled;
        if (!string.IsNullOrWhiteSpace(reason))
            Notes = $"{Notes} [Cancelled: {reason}]".Trim();
    }

    public void MarkPaid() => PaymentStatus = PaymentStatus.Paid;

    public bool OverlapsWith(DateTime checkIn, DateTime checkOut) =>
        CheckInDate < checkOut && CheckOutDate > checkIn;

    /// <summary>Поліморфна реалізація Display() з EntityBase.</summary>
    public override string Display() =>
        $"Booking #{Id} | Room {RoomId} | Guest {GuestId} | " +
        $"{CheckInDate:d}-{CheckOutDate:d} ({Nights}n) | {Status} | {TotalPrice:C}";
}
