using System.Text.Json;
using System.Text.Json.Serialization;
using HotelBooking.Domain.Exceptions;

namespace HotelBooking.Infrastructure.Repositories;

/// <summary>
/// Generic file-based persistence.
/// Pattern: Template Method — конкретні репозиторії перевизначають лише GetId().
/// </summary>
public abstract class JsonRepositoryBase<T> where T : class
{
    private readonly string _filePath;
    protected List<T> _items = new();
    private bool _loaded;

    protected static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };

    protected JsonRepositoryBase(string dataDirectory, string fileName)
    {
        Directory.CreateDirectory(dataDirectory);
        _filePath = Path.Combine(dataDirectory, fileName);
    }

    /// <summary>
    /// Завантажує дані з файлу при першому зверненні (lazy loading).
    /// Обробляє: відсутній файл, пошкоджений JSON, помилки I/O.
    /// </summary>
    protected async Task EnsureLoadedAsync(CancellationToken ct = default)
    {
        if (_loaded) return;

        if (!File.Exists(_filePath))
        {
            _items = new List<T>();
            _loaded = true;
            return;
        }

        try
        {
            var json = await File.ReadAllTextAsync(_filePath, ct);

            if (string.IsNullOrWhiteSpace(json))
            {
                _items = new List<T>();
                _loaded = true;
                return;
            }

            _items = JsonSerializer.Deserialize<List<T>>(json, JsonOptions) ?? new List<T>();
            _loaded = true;
        }
        catch (JsonException ex)
        {
            // Пошкоджений JSON — логуємо і стартуємо з порожньою колекцією
            Console.Error.WriteLine($"[WARN] File '{_filePath}' contains invalid JSON. Starting fresh. ({ex.Message})");
            _items = new List<T>();
            _loaded = true;
        }
        catch (IOException ex)
        {
            throw new DomainException($"Failed to read data file '{Path.GetFileName(_filePath)}'.", ex);
        }
    }

    /// <summary>
    /// Зберігає дані у файл асинхронно.
    /// Використовує тимчасовий файл + rename для атомарного запису (не втратити дані при збої).
    /// </summary>
    public async Task PersistAsync(CancellationToken ct = default)
    {
        try
        {
            var json = JsonSerializer.Serialize(_items, JsonOptions);
            var tmpPath = _filePath + ".tmp";
            await File.WriteAllTextAsync(tmpPath, json, ct);
            File.Move(tmpPath, _filePath, overwrite: true);
        }
        catch (IOException ex)
        {
            throw new DomainException($"Failed to save data file '{Path.GetFileName(_filePath)}'.", ex);
        }
    }

    public async Task<int> NextIdAsync(CancellationToken ct = default)
    {
        await EnsureLoadedAsync(ct);
        return _items.Count == 0 ? 1 : _items.Max(GetId) + 1;
    }

    protected abstract int GetId(T item);
}
