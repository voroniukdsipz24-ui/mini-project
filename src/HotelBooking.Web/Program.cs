using HotelBooking.Application.Services;
using HotelBooking.Domain.Entities;
using HotelBooking.Domain.Exceptions;
using HotelBooking.Domain.Interfaces;
using HotelBooking.Infrastructure;

// ── Composition Root ─────────────────────────────────────────────────────
var builder = WebApplication.CreateBuilder(args);

string dataDir = Path.Combine(builder.Environment.ContentRootPath, "data");
var uow = new JsonUnitOfWork(dataDir);

// Singleton: репозиторії містять in-memory колекції — спільний стан між запитами.
// Application services не мають власного стану — Singleton безпечний.
builder.Services.AddSingleton<IUnitOfWork>(uow);
builder.Services.AddSingleton(sp => new BookingService(sp.GetRequiredService<IUnitOfWork>()));
builder.Services.AddSingleton(sp => new RoomSearchService(sp.GetRequiredService<IUnitOfWork>()));
builder.Services.AddSingleton(sp => new GuestService(sp.GetRequiredService<IUnitOfWork>()));
builder.Services.AddSingleton(sp => new ReportService(sp.GetRequiredService<IUnitOfWork>()));

builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

// Серіалізація enum як рядки ("Standard", "Deluxe"…) замість чисел —
// інакше frontend не зможе порівнювати r.type === "Standard"
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(
        new System.Text.Json.Serialization.JsonStringEnumConverter());
});

var app = builder.Build();

// Seed demo data if the JSON files are empty
await DataSeeder.SeedAsync(uow);

app.UseCors();
app.UseDefaultFiles();   // serves index.html from wwwroot
app.UseStaticFiles();

// ═══════════ BOOKING ENDPOINTS ═══════════
app.MapGet("/api/bookings", async (BookingService svc) =>
    Results.Ok(await svc.GetAllBookingsAsync()));

app.MapGet("/api/bookings/{id:int}", async (int id, BookingService svc) =>
{
    var b = await svc.GetBookingAsync(id);
    return b is null ? Results.NotFound() : Results.Ok(b);
});

app.MapPost("/api/bookings", async (CreateBookingRequest req, BookingService svc) =>
{
    try
    {
        var b = await svc.CreateBookingAsync(req.GuestId, req.RoomId, req.CheckIn, req.CheckOut);
        return Results.Created($"/api/bookings/{b.Id}", b);
    }
    catch (DomainException ex) { return Results.BadRequest(new { error = ex.Message }); }
    catch (Exception ex)        { return Results.BadRequest(new { error = ex.Message }); }
});

app.MapPut("/api/bookings/{id:int}/confirm", async (int id, BookingService svc) =>
{
    try { return Results.Ok(await svc.ConfirmBookingAsync(id)); }
    catch (Exception ex) { return Results.BadRequest(new { error = ex.Message }); }
});

app.MapPut("/api/bookings/{id:int}/checkin", async (int id, BookingService svc) =>
{
    try { return Results.Ok(await svc.CheckInAsync(id)); }
    catch (Exception ex) { return Results.BadRequest(new { error = ex.Message }); }
});

app.MapPut("/api/bookings/{id:int}/checkout", async (int id, BookingService svc) =>
{
    try { return Results.Ok(await svc.CheckOutAsync(id)); }
    catch (Exception ex) { return Results.BadRequest(new { error = ex.Message }); }
});

app.MapDelete("/api/bookings/{id:int}", async (int id, string? reason, BookingService svc) =>
{
    try { return Results.Ok(await svc.CancelBookingAsync(id, reason ?? "")); }
    catch (Exception ex) { return Results.BadRequest(new { error = ex.Message }); }
});

// ═══════════ ROOM ENDPOINTS ═══════════
app.MapGet("/api/rooms", async (RoomSearchService svc) =>
    Results.Ok(await svc.GetAllRoomsAsync()));

app.MapGet("/api/rooms/available", async (
    DateTime checkIn, DateTime checkOut,
    RoomType? type, decimal? maxPrice, int guests,
    RoomSearchService svc) =>
    Results.Ok(await svc.SearchAvailableAsync(checkIn, checkOut, type, guests, maxPrice)));

app.MapPost("/api/rooms", async (AddRoomRequest req, RoomSearchService svc) =>
{
    try
    {
        var r = await svc.AddRoomAsync(req.Number, req.Floor, req.Type, req.Price, req.Capacity, req.Description);
        return Results.Created($"/api/rooms/{r.Id}", r);
    }
    catch (Exception ex) { return Results.BadRequest(new { error = ex.Message }); }
});

// ═══════════ GUEST ENDPOINTS ═══════════
app.MapGet("/api/guests", async (GuestService svc) =>
    Results.Ok(await svc.GetAllGuestsAsync()));

app.MapPost("/api/guests", async (RegisterGuestRequest req, GuestService svc) =>
{
    try
    {
        var g = await svc.RegisterGuestAsync(
            req.FirstName, req.LastName, req.Email, req.Phone, req.Passport, req.DateOfBirth);
        return Results.Created($"/api/guests/{g.Id}", g);
    }
    catch (Exception ex) { return Results.BadRequest(new { error = ex.Message }); }
});

// ═══════════ REPORT ENDPOINTS ═══════════
app.MapGet("/api/reports/occupancy", async (DateTime from, DateTime to, ReportService svc) =>
    Results.Ok(await svc.GetOccupancyReportAsync(from, to)));

app.MapGet("/api/reports/room-types", async (ReportService svc) =>
    Results.Ok(await svc.GetRoomTypeReportAsync()));

app.MapGet("/api/reports/top-guests", async (int top, ReportService svc) =>
{
    var r = await svc.GetTopGuestsAsync(top <= 0 ? 5 : top);
    return Results.Ok(r.Select(x => new { x.Guest, x.Bookings, x.Spent }));
});

app.Run();

// ── Request DTOs ──────────────────────────────────────────────────────────
record CreateBookingRequest(int GuestId, int RoomId, DateTime CheckIn, DateTime CheckOut);
record AddRoomRequest(int Number, int Floor, RoomType Type, decimal Price, int Capacity, string Description = "");
record RegisterGuestRequest(string FirstName, string LastName, string Email, string Phone, string Passport, DateTime DateOfBirth);
