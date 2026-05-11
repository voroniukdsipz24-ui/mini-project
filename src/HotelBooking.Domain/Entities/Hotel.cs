namespace HotelBooking.Domain.Entities;

/// <summary>
/// Готель — aggregate root. Наслідує EntityBase.
/// </summary>
public class Hotel : EntityBase
{
    public string Name       { get; private set; }
    public string Address    { get; private set; }
    public int StarRating    { get; private set; }
    public int TotalFloors   { get; private set; }

    private Hotel() { Name = Address = string.Empty; }

    public Hotel(int id, string name, string address, int starRating, int totalFloors)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Hotel name required.");
        if (starRating is < 1 or > 5)        throw new ArgumentOutOfRangeException(nameof(starRating), "Stars must be 1-5.");

        Id          = id;
        Name        = name;
        Address     = address;
        StarRating  = starRating;
        TotalFloors = totalFloors;
    }

    public override string Display() => $"{Name} ({StarRating}★) | {Address}";
}
