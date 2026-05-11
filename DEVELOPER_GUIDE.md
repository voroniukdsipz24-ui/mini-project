# Developer Guide — Hotel Booking System

## Архітектура

Проєкт побудований за **Clean Architecture** з 4 шарами + дві альтернативні presentation:

```
src/HotelBooking.Web         ← 🌐 ASP.NET Core Web API + GUI (основна)
src/HotelBooking.Console     ← 💻 Console UI (Lab 34 baseline)
src/HotelBooking.Application ← Use-case Services
src/HotelBooking.Domain      ← Entities, Interfaces, Domain Services
src/HotelBooking.Infrastructure ← Persistence (JSON)
tests/HotelBooking.Tests     ← Unit + Integration Tests
```

**Правило залежностей**: залежності спрямовані всередину. Infrastructure реалізує інтерфейси Domain, але Domain не знає про Infrastructure. Web і Console обидва покладаються на Application — взаємозамінні.

---

## Шар Domain

### Entities

| Клас | Відповідальність | Ключові правила |
|------|-----------------|-----------------|
| `Room` | Номер готелю | Status machine: Available/Reserved/Occupied/Maintenance |
| `Guest` | Гість | Email унікальний; lowercase-нормалізація |
| `Booking` | Бронювання | State machine: Pending→Confirmed→CheckedIn→CheckedOut/Cancelled |
| `Hotel` | Готель | Stars 1–5 |

### Domain Services

**PricingEngine** (static):
```
Total = BasePrice × Nights × SeasonMultiplier
```
- BasePrice — індивідуальна ціна за ніч кожного номера (вже відображає клас Standard/Deluxe/Suite/Penthouse)
- SeasonMultiplier: Jun/Jul/Aug/Dec=1.25, Mar/Apr/Sep/Oct=1.10, інакше=1.0

### Exceptions ієрархія
```
Exception
└── DomainException
    ├── RoomNotAvailableException
    ├── BookingNotFoundException
    ├── GuestNotFoundException
    ├── RoomNotFoundException
    └── InvalidBookingOperationException
```

---

## Шар Application

### IUnitOfWork
```csharp
public interface IUnitOfWork
{
    IBookingRepository Bookings { get; }
    IRoomRepository    Rooms    { get; }
    IGuestRepository   Guests   { get; }
    Task SaveAsync(CancellationToken ct = default);
}
```

Application services отримують `IUnitOfWork` через конструктор. Жодна бізнес-логіка не знає про конкретну реалізацію (JSON / InMemory / SQL).

### Services

| Сервіс | Use cases |
|--------|-----------|
| `BookingService` | Create, Confirm, CheckIn, CheckOut, Cancel |
| `RoomSearchService` | SearchAvailable, GetAll, Add, SetStatus |
| `GuestService` | Register, GetAll, FindByEmail |
| `ReportService` | Occupancy, RoomTypes, TopGuests (LINQ) |

---

## Шар Infrastructure

### JsonRepositoryBase\<T\> (Template Method)
Спільна логіка завантаження/збереження. Конкретні репо перевизначають лише `protected abstract int GetId(T item)`.

### Атомарний запис
```csharp
var tmp = _filePath + ".tmp";
await File.WriteAllTextAsync(tmp, json, ct);
File.Move(tmp, _filePath, overwrite: true);
```
Захист від часткових даних при збої.

---

## Шар Web (ASP.NET Core)

### Composition Root (`Program.cs`)
```csharp
var uow = new JsonUnitOfWork(dataDir);
builder.Services.AddSingleton<IUnitOfWork>(uow);
builder.Services.AddScoped(sp => new BookingService(sp.GetRequiredService<IUnitOfWork>()));
// ...

app.UseDefaultFiles();   // serves wwwroot/index.html
app.UseStaticFiles();    // serves all wwwroot/*

app.MapGet("/api/bookings", async (BookingService svc) => Results.Ok(await svc.GetAllBookingsAsync()));
// ...
```

### REST Endpoints
Тонкий шар: ловить виняток → перетворює в HTTP 400 з JSON-повідомленням.
```csharp
app.MapPost("/api/bookings", async (CreateBookingRequest req, BookingService svc) =>
{
    try
    {
        var b = await svc.CreateBookingAsync(req.GuestId, req.RoomId, req.CheckIn, req.CheckOut);
        return Results.Created($"/api/bookings/{b.Id}", b);
    }
    catch (DomainException ex) { return Results.BadRequest(new { error = ex.Message }); }
});
```

### Frontend (`wwwroot/index.html`)
Vanilla HTML / CSS / JavaScript без фреймворків. Всі дані через `fetch('/api/...')`. Стан — в одному JS об'єкті `state = {bookings, rooms, guests}`.

---

## Шар Console (legacy)

Залишений як **доказ архітектурної чистоти**: той самий `BookingService` працює і через Console MainMenu, і через Web API. Доводить, що Domain і Application повністю незалежні від UI.

---

## Патерни проєктування

| Патерн | Де | Навіщо |
|--------|-----|--------|
| **Unit of Work** | `JsonUnitOfWork` | Атомарне збереження всіх репозиторіїв |
| **Template Method** | `JsonRepositoryBase<T>` | Спільна логіка, hook `GetId()` |
| **Facade** | `JsonUnitOfWork` | Єдина точка доступу до 3 репозиторіїв |
| **Repository** | `IBookingRepository` та ін. | Абстракція доступу до даних |

---

## Додавання нового use case

1. Додайте метод у відповідний Application Service
2. Якщо потрібна нова операція з даними — оновіть інтерфейс у Domain і реалізацію у Json* repository
3. Додайте endpoint у `HotelBooking.Web/Program.cs`
4. Додайте кнопку/форму у `wwwroot/index.html`
5. Напишіть тест у `tests/`

### Приклад: «Переведення номера на обслуговування»

```csharp
// 1. Application — RoomSearchService.cs (вже є SetRoomStatusAsync)
// (нічого додавати не треба)

// 2. Web — Program.cs
app.MapPut("/api/rooms/{id:int}/maintenance", async (int id, RoomSearchService svc) =>
{
    try { await svc.SetRoomStatusAsync(id, RoomStatus.Maintenance); return Results.Ok(); }
    catch (Exception ex) { return Results.BadRequest(new { error = ex.Message }); }
});

// 3. wwwroot/index.html — додати кнопку у room-card
// + onclick="setMaintenance(roomId)"

// 4. tests/Lab36Tests.cs
[Fact]
public async Task SetMaintenance_OccupiedRoom_Throws() { ... }
```

---

## Запуск тестів

```bash
# Всі тести
dotnet test

# З coverage (XPlat)
dotnet test --collect:"XPlat Code Coverage"

# З coverage (msbuild — метрики у консоль)
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover

# Фільтр по класу
dotnet test --filter "FullyQualifiedName~RoomBoundaryTests"
```

---

## Структура JSON-файлів

### `data/bookings.json`
```json
[
  {
    "id": 1,
    "roomId": 1,
    "guestId": 1,
    "checkInDate": "2026-07-01T00:00:00",
    "checkOutDate": "2026-07-05T00:00:00",
    "status": "CheckedOut",
    "paymentStatus": "Paid",
    "totalPrice": 500.00,
    "notes": "",
    "createdAt": "2026-04-22T10:00:00"
  }
]
```

### `data/rooms.json`, `data/guests.json` — аналогічна структура.

---

## Code Style

- `private` поля з underscore: `_uow`, `_items`
- `async/await` скрізь де є I/O
- `ArgumentException` для порушення контракту конструктора
- `DomainException` (або підклас) для бізнес-правил
- `InvalidOperationException` для неправильних переходів стану
- Nullable reference types увімкнені (`<Nullable>enable</Nullable>`)
- HTTP endpoints **не** містять бізнес-логіки — лише делегують у Application services
