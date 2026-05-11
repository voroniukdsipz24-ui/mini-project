using HotelBooking.Domain.Entities;
using HotelBooking.Domain.Interfaces;

namespace HotelBooking.Infrastructure;

/// <summary>
/// Seeds demo data if the storage is empty on first run.
/// </summary>
public static class DataSeeder
{
    public static async Task SeedAsync(IUnitOfWork uow)
    {
        var rooms = await uow.Rooms.GetAllAsync();
        if (rooms.Count > 0) return; // already seeded

        var roomData = new[]
        {
            (1,  101, 1, RoomType.Standard,  80m,  2, "Стандартний номер з видом на місто"),
            (2,  102, 1, RoomType.Standard,  80m,  2, "Стандартний номер"),
            (3,  103, 1, RoomType.Standard,  80m,  1, "Одномісний стандарт"),
            (4,  201, 2, RoomType.Deluxe,   120m,  2, "Делюкс з балконом"),
            (5,  202, 2, RoomType.Deluxe,   120m,  3, "Делюкс сімейний"),
            (6,  203, 2, RoomType.Deluxe,   130m,  2, "Делюкс преміум"),
            (7,  301, 3, RoomType.Suite,    200m,  2, "Люкс з джакузі"),
            (8,  302, 3, RoomType.Suite,    220m,  4, "Сімейний люкс"),
            (9,  401, 4, RoomType.Penthouse,400m,  4, "Пентхаус з терасою"),
        };

        foreach (var (id, num, floor, type, price, cap, desc) in roomData)
        {
            var room = new Room(id, num, floor, type, price, cap, desc);
            await uow.Rooms.AddAsync(room);
        }

        // Demo guests
        var guestData = new[]
        {
            (1, "Олена",    "Шевченко",  "olena@example.com",  "+380501234567", "AA123456", new DateTime(1990, 3, 15)),
            (2, "Михайло",  "Коваль",    "mykhailo@example.com","+380671234567","BB654321", new DateTime(1985, 7, 22)),
            (3, "Наталія",  "Бондаренко","natalia@example.com", "+380631234567","CC987654", new DateTime(1995, 11, 8)),
        };

        foreach (var (id, fn, ln, email, phone, passport, dob) in guestData)
            await uow.Guests.AddAsync(new Guest(id, fn, ln, email, phone, passport, dob));

        await uow.SaveAsync();
        Console.WriteLine("✓ Demo data seeded successfully.\n");
    }
}
