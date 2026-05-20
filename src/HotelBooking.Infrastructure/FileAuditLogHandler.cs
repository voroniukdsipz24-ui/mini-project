using HotelBooking.Domain.Entities;
using HotelBooking.Domain.Interfaces;

namespace HotelBooking.Infrastructure;

/// <summary>
/// Observer-handler, що записує події бронювань у файл аудит-логу.
/// Дозволяє ретроспективно відстежити lifecycle будь-якого бронювання
/// без необхідності інспектувати JSON-файли.
/// </summary>
public class FileAuditLogHandler : IBookingEventHandler
{
    private readonly string _logPath;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public FileAuditLogHandler(string dataDir)
    {
        Directory.CreateDirectory(dataDir);
        _logPath = Path.Combine(dataDir, "audit.log");
    }

    /// <inheritdoc/>
    public async Task HandleAsync(BookingEvent evt, CancellationToken ct = default)
    {
        var line = $"{evt.At:O} | booking #{evt.BookingId} | {evt.PreviousStatus?.ToString() ?? "—"} → {evt.NewStatus}" +
                   (string.IsNullOrEmpty(evt.Note) ? "" : $" | {evt.Note}");

        await _lock.WaitAsync(ct);
        try
        {
            await File.AppendAllTextAsync(_logPath, line + Environment.NewLine, ct);
        }
        finally { _lock.Release(); }
    }
}

/// <summary>
/// In-memory observer для тестів і dev-режиму. Зберігає події у списку
/// з потокобезпечним доступом, корисний для перевірки що подія справді
/// відбулась (assert у тестах).
/// </summary>
public class InMemoryAuditLogHandler : IBookingEventHandler
{
    private readonly List<BookingEvent> _events = new();
    private readonly object _lock = new();

    public IReadOnlyList<BookingEvent> Events
    {
        get { lock (_lock) { return _events.ToList().AsReadOnly(); } }
    }

    public Task HandleAsync(BookingEvent evt, CancellationToken ct = default)
    {
        lock (_lock) { _events.Add(evt); }
        return Task.CompletedTask;
    }
}
