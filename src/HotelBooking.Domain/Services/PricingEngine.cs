using HotelBooking.Domain.Entities;

namespace HotelBooking.Domain.Services;

/// <summary>
/// Pricing engine — розрахунок вартості бронювання.
/// Базова ціна номера вже відображає його тип (Standard/Deluxe/Suite/Penthouse),
/// тому додатковий type-множник не застосовується. Єдиний множник — сезонний.
/// </summary>
public static class PricingEngine
{
    public static decimal Calculate(Room room, DateTime checkIn, DateTime checkOut)
    {
        int nights = (checkOut - checkIn).Days;
        if (nights <= 0) throw new ArgumentException("Check-out must be after check-in.");

        decimal basePrice = room.PricePerNight * nights;
        decimal seasonMultiplier = GetSeasonMultiplier(checkIn);

        return Math.Round(basePrice * seasonMultiplier, 2);
    }

    private static decimal GetSeasonMultiplier(DateTime date) =>
        date.Month switch
        {
            6 or 7 or 8 or 12 => 1.25m,   // peak: червень/липень/серпень/грудень
            3 or 4 or 9 or 10 => 1.10m,   // shoulder: березень/квітень/вересень/жовтень
            _ => 1.0m                       // off-peak: січень/лютий/травень/листопад
        };

    public static string PriceBreakdown(Room room, DateTime checkIn, DateTime checkOut)
    {
        int nights = (checkOut - checkIn).Days;
        decimal season = GetSeasonMultiplier(checkIn);
        decimal total  = Calculate(room, checkIn, checkOut);

        return $"Base: {room.PricePerNight:C} × {nights}n × {season}× (season) = {total:C}";
    }
}
