namespace HotelBooking.Domain.Entities;

/// <summary>
/// Номер готелю. Наслідує EntityBase — отримує поліморфний Display() та Equals/GetHashCode.
/// </summary>
public class Room : EntityBase
{
    public int Number           { get; private set; }
    public int Floor            { get; private set; }
    public RoomType Type        { get; private set; }
    public decimal PricePerNight { get; private set; }
    public RoomStatus Status    { get; private set; }
    public string Description   { get; private set; }
    public int Capacity         { get; private set; }

    // For JSON deserialization
    private Room() { Description = string.Empty; }

    public Room(int id, int number, int floor, RoomType type,
                decimal pricePerNight, int capacity, string description = "")
    {
        if (number <= 0)       throw new ArgumentException("Room number must be positive.", nameof(number));
        if (pricePerNight <= 0) throw new ArgumentException("Price must be positive.", nameof(pricePerNight));
        if (capacity <= 0)     throw new ArgumentException("Capacity must be positive.", nameof(capacity));

        Id           = id;
        Number       = number;
        Floor        = floor;
        Type         = type;
        PricePerNight = pricePerNight;
        Capacity     = capacity;
        Description  = description;
        Status       = RoomStatus.Available;
    }

    public void SetStatus(RoomStatus status) => Status = status;

    public void UpdatePrice(decimal newPrice)
    {
        if (newPrice <= 0) throw new ArgumentException("Price must be positive.");
        PricePerNight = newPrice;
    }

    public bool IsAvailable() => Status == RoomStatus.Available;

    /// <summary>Поліморфна реалізація Display() з EntityBase.</summary>
    public override string Display() =>
        $"[{Id}] Room {Number} (Floor {Floor}) | {Type} | {PricePerNight:C}/night | Cap:{Capacity} | {Status}";
}
