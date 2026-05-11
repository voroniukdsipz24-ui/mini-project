using HotelBooking.Application.Services;
using HotelBooking.Infrastructure;

// ── Composition Root (Lab 34 — In-Memory) ─────────────────────────────────
// На Lab 35 InMemoryUnitOfWork замінюється на JsonUnitOfWork з file persistence.

var uow = new InMemoryUnitOfWork();
await uow.SeedDemoDataAsync();

Console.WriteLine("✓ Demo data seeded (in-memory, Lab 34 mode).\n");

var bookingService = new BookingService(uow);
var roomService    = new RoomSearchService(uow);
var guestService   = new GuestService(uow);
var reportService  = new ReportService(uow);

var menu = new HotelBooking.ConsoleUI.MainMenu(bookingService, roomService, guestService, reportService);
await menu.RunAsync();
