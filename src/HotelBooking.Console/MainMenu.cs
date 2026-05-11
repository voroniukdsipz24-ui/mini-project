using HotelBooking.Application.Services;
using HotelBooking.Domain.Entities;
using HotelBooking.Domain.Exceptions;

namespace HotelBooking.ConsoleUI;

public class MainMenu
{
    private readonly BookingService    _bookings;
    private readonly RoomSearchService _rooms;
    private readonly GuestService      _guests;
    private readonly ReportService     _reports;

    public MainMenu(BookingService b, RoomSearchService r, GuestService g, ReportService rep)
    {
        _bookings = b; _rooms = r; _guests = g; _reports = rep;
    }

    public async Task RunAsync()
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        PrintBanner();

        while (true)
        {
            PrintMainMenu();
            var key = Console.ReadLine()?.Trim();
            Console.WriteLine();

            try
            {
                switch (key)
                {
                    case "1": await SearchRoomsAsync(); break;
                    case "2": await CreateBookingAsync(); break;
                    case "3": await ManageBookingAsync(); break;
                    case "4": await ListBookingsAsync(); break;
                    case "5": await RegisterGuestAsync(); break;
                    case "6": await ListGuestsAsync(); break;
                    case "7": await ShowReportsAsync(); break;
                    case "8": await ListAllRoomsAsync(); break;
                    case "0": Console.WriteLine("До побачення!"); return;
                    default:  Warn("Невідома команда."); break;
                }
            }
            catch (DomainException ex)
            {
                Error($"[Бізнес-помилка] {ex.Message}");
            }
            catch (InvalidOperationException ex)
            {
                Error($"[Операція неможлива] {ex.Message}");
            }
            catch (Exception ex)
            {
                Error($"[Помилка] {ex.Message}");
            }

            Console.WriteLine();
        }
    }

    // ── Меню ──────────────────────────────────────────────────────────────

    private static void PrintBanner()
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("╔══════════════════════════════════════════╗");
        Console.WriteLine("║   🏨  СИСТЕМА БРОНЮВАННЯ ГОТЕЛЮ          ║");
        Console.WriteLine("║       Hotel «Grand Palais»                ║");
        Console.WriteLine("╚══════════════════════════════════════════╝");
        Console.ResetColor();
        Console.WriteLine();
    }

    private static void PrintMainMenu()
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("── Головне меню ──────────────────────────");
        Console.ResetColor();
        Console.WriteLine(" 1. Пошук вільних номерів");
        Console.WriteLine(" 2. Створити бронювання");
        Console.WriteLine(" 3. Управління бронюванням (підтвердити/check-in/out/скасувати)");
        Console.WriteLine(" 4. Переглянути всі бронювання");
        Console.WriteLine(" 5. Зареєструвати гостя");
        Console.WriteLine(" 6. Список гостей");
        Console.WriteLine(" 7. Звіти");
        Console.WriteLine(" 8. Всі номери");
        Console.WriteLine(" 0. Вихід");
        Console.Write("Ваш вибір: ");
    }

    // ── Use case handlers ─────────────────────────────────────────────────

    private async Task SearchRoomsAsync()
    {
        Header("Пошук вільних номерів");

        var checkIn  = ReadDate("Дата заїзду (дд.мм.рррр)");
        var checkOut = ReadDate("Дата виїзду  (дд.мм.рррр)");
        int guests   = ReadInt("Кількість гостей", 1);

        Console.Write("Тип номера (Standard/Deluxe/Suite/Penthouse або Enter = всі): ");
        var typeStr = Console.ReadLine()?.Trim();
        RoomType? type = Enum.TryParse<RoomType>(typeStr, true, out var t) ? t : null;

        Console.Write("Максимальна ціна за ніч (або Enter = без обмежень): ");
        var maxStr = Console.ReadLine()?.Trim();
        decimal? maxPrice = decimal.TryParse(maxStr, out var mp) ? mp : null;

        var rooms = await _rooms.SearchAvailableAsync(checkIn, checkOut, type, guests, maxPrice);

        if (!rooms.Any()) { Warn("Вільних номерів не знайдено."); return; }

        Success($"Знайдено {rooms.Count} номер(ів):");
        foreach (var r in rooms)
            Console.WriteLine($"  • {r}");
    }

    private async Task CreateBookingAsync()
    {
        Header("Нове бронювання");

        int guestId  = ReadInt("ID гостя");
        int roomId   = ReadInt("ID номера");
        var checkIn  = ReadDate("Дата заїзду (дд.мм.рррр)");
        var checkOut = ReadDate("Дата виїзду  (дд.мм.рррр)");

        var booking = await _bookings.CreateBookingAsync(guestId, roomId, checkIn, checkOut);
        Success($"Бронювання #{booking.Id} створено!");
        Console.WriteLine($"  {booking}");
    }

    private async Task ManageBookingAsync()
    {
        Header("Управління бронюванням");
        int id = ReadInt("ID бронювання");

        Console.WriteLine("Дія: [C]onfirm | check-[I]n | check-[O]ut | [X] cancel | [B]ack");
        var action = Console.ReadLine()?.Trim().ToUpperInvariant();

        Booking result = action switch
        {
            "C" => await _bookings.ConfirmBookingAsync(id),
            "I" => await _bookings.CheckInAsync(id),
            "O" => await _bookings.CheckOutAsync(id),
            "X" => await CancelWithReasonAsync(id),
            _   => throw new InvalidOperationException("Невідома дія.")
        };

        Success($"Виконано: {result}");
    }

    private async Task<Booking> CancelWithReasonAsync(int id)
    {
        Console.Write("Причина скасування: ");
        var reason = Console.ReadLine() ?? "";
        return await _bookings.CancelBookingAsync(id, reason);
    }

    private async Task ListBookingsAsync()
    {
        Header("Всі бронювання");
        var all = await _bookings.GetAllBookingsAsync();
        if (!all.Any()) { Warn("Бронювань немає."); return; }

        foreach (var b in all.OrderByDescending(b => b.CreatedAt))
            Console.WriteLine($"  {b}");
    }

    private async Task RegisterGuestAsync()
    {
        Header("Реєстрація гостя");
        Console.Write("Ім'я: ");              var fn  = Console.ReadLine()!;
        Console.Write("Прізвище: ");          var ln  = Console.ReadLine()!;
        Console.Write("Email: ");             var em  = Console.ReadLine()!;
        Console.Write("Телефон: ");           var ph  = Console.ReadLine()!;
        Console.Write("Номер паспорта: ");    var pp  = Console.ReadLine()!;
        var dob = ReadDate("Дата народження (дд.мм.рррр)");

        var guest = await _guests.RegisterGuestAsync(fn, ln, em, ph, pp, dob);
        Success($"Гостя зареєстровано: {guest}");
    }

    private async Task ListGuestsAsync()
    {
        Header("Список гостей");
        var all = await _guests.GetAllGuestsAsync();
        if (!all.Any()) { Warn("Гостей немає."); return; }
        foreach (var g in all)
            Console.WriteLine($"  {g}");
    }

    private async Task ShowReportsAsync()
    {
        Header("Звіти");
        Console.WriteLine(" 1. Звіт завантаженості");
        Console.WriteLine(" 2. Звіт за типами номерів");
        Console.WriteLine(" 3. ТОП гостей");
        Console.Write("Вибір: ");
        var choice = Console.ReadLine()?.Trim();

        switch (choice)
        {
            case "1":
                var from = ReadDate("Від (дд.мм.рррр)");
                var to   = ReadDate("До  (дд.мм.рррр)");
                var occ  = await _reports.GetOccupancyReportAsync(from, to);
                Console.WriteLine($"\n  Готель: {occ.TotalRooms} номерів");
                Console.WriteLine($"  Зайнято ліжко-ночей: {occ.OccupiedDays}");
                Console.WriteLine($"  Завантаженість: {occ.OccupancyRate}%");
                Console.WriteLine($"  Дохід: {occ.TotalRevenue:C}");
                Console.WriteLine($"  Ср. ціна/ніч: {occ.AverageNightlyRate:C}");
                break;

            case "2":
                var typeRep = await _reports.GetRoomTypeReportAsync();
                foreach (var r in typeRep)
                    Console.WriteLine($"  {r.Type,-12} | {r.Bookings} брон. | {r.Revenue:C} | ср.ночей: {r.AvgNights:F1}");
                break;

            case "3":
                var top = await _reports.GetTopGuestsAsync(5);
                int rank = 1;
                foreach (var (g, cnt, spent) in top)
                    Console.WriteLine($"  #{rank++} {g.FullName,-20} | {cnt} брон. | {spent:C}");
                break;
        }
    }

    private async Task ListAllRoomsAsync()
    {
        Header("Всі номери готелю");
        var rooms = await _rooms.GetAllRoomsAsync();
        foreach (var r in rooms.OrderBy(r => r.Number))
            Console.WriteLine($"  [{r.Id,2}] {r}");
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private static void Header(string title)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"\n── {title} ──────────────────────────────");
        Console.ResetColor();
    }

    private static void Success(string msg)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("✓ " + msg);
        Console.ResetColor();
    }

    private static void Warn(string msg)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("⚠ " + msg);
        Console.ResetColor();
    }

    private static void Error(string msg)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("✗ " + msg);
        Console.ResetColor();
    }

    private static DateTime ReadDate(string prompt)
    {
        while (true)
        {
            Console.Write($"{prompt}: ");
            if (DateTime.TryParseExact(Console.ReadLine(), "dd.MM.yyyy",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out var d))
                return d;
            Warn("Невірний формат. Спробуйте ще раз (дд.мм.рррр).");
        }
    }

    private static int ReadInt(string prompt, int min = 0)
    {
        while (true)
        {
            Console.Write($"{prompt}: ");
            if (int.TryParse(Console.ReadLine(), out int v) && v >= min) return v;
            Warn($"Введіть ціле число >= {min}.");
        }
    }
}
