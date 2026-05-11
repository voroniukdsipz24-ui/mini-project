# Iteration 1 — Lab 34

## Що вже працює

### Вертикальний зріз (Console → Application → Domain → Repository → Console)
1. **Пошук вільних номерів** — адміністратор вводить дати, бачить список доступних номерів
2. **Створення бронювання** — перевірка гостя, перевірка номера, conflict detection, розрахунок ціни (PricingEngine), збереження
3. **Реєстрація гостя** — валідація email, дедуплікація
4. **Перегляд списку гостей** — вивід усіх зареєстрованих гостей

Запуск: `dotnet run --project src/HotelBooking.Console`

### Тести
- `RoomTests` (5 тестів): конструктор, SetStatus, UpdatePrice, IsAvailable, негативні сценарії
- `GuestTests` (4 тести): конструктор, email lowercase, FullName, негативні сценарії
- `BookingTests` (9 тестів): state machine, Nights, OverlapsWith, негативні сценарії
- `PricingEngineTests` (4 тести): Standard, Suite multiplier, peak season, invalid dates

Запуск: `dotnet test`

---

## Артефакти в репозиторії

| Файл | Опис |
|------|------|
| `docs/vision.md` | Проблема, користувачі, 5 UC, 5 NFR, обмеження ітерації 1 |
| `docs/backlog.md` | Backlog розбитий по 4 ітераціях |
| `docs/class-diagram.md` | UML класів (Mermaid) |
| `docs/sequence-diagram.md` | UML послідовності: UC-1, UC-2, негативний сценарій |
| `src/HotelBooking.Domain/` | EntityBase, PersonBase, Room, Guest, Booking, Hotel, Enums, PricingEngine, DomainExceptions, Interfaces |
| `src/HotelBooking.Application/` | BookingService (CreateBooking), RoomSearchService, GuestService |
| `src/HotelBooking.Infrastructure/` | InMemoryUnitOfWork (in-memory, Lab 34) |
| `src/HotelBooking.Console/` | Program.cs, MainMenu.cs |
| `tests/HotelBooking.Tests/` | DomainTests.cs (22 тести), Fakes/ |
| `.github/workflows/ci.yml` | GitHub Actions: restore → build → test |
| `.gitignore` | .NET стандартний gitignore |
| `README.md` | Опис, quick start, структура |

---

## Сценарії для розширення на Lab 35

1. **Confirm / CheckIn / CheckOut / Cancel** — повний lifecycle бронювання (методи в Booking вже є, потрібно підключити в BookingService і UI)
2. **JSON persistence** — замінити InMemoryUnitOfWork на JsonUnitOfWork (інтерфейс IUnitOfWork вже стабільний)
3. **LINQ-звіти** — ReportService з occupancy, revenue по типах номерів, ТОП гостей

---

## Ризики та невизначеності

| Ризик | Ймовірність | Мітигація |
|-------|-------------|-----------|
| JSON serialization з private setters | Середня | System.Text.Json підтримує конструктори з `[JsonConstructor]` або потребує public setters; вирішимо на Lab 35 |
| Conflict detection при паралельному доступі | Низька | In-scope: single-user console; поза scope: lock/transaction |
| Складність LINQ звітів | Низька | ReportService вже частково набросано |

---

## Класи/інтерфейси, свідомо підготовлені під розширення

| Елемент | Чому готовий до розширення |
|---------|--------------------------|
| `IUnitOfWork` | Замінюємо InMemoryUnitOfWork → JsonUnitOfWork без змін у сервісах |
| `IBookingRepository.GetByRoomIdAsync()` | Потрібен для conflict detection (вже є) |
| `Booking.Confirm/CheckIn/CheckOut/Cancel()` | State machine повністю реалізований у Domain |
| `EntityBase` (abstract) | Можна додати Staff, Manager як нащадків PersonBase |
| `PricingEngine` (static) | Можна перетворити на Strategy pattern на Lab 35 |
| `BookingExtensions` | Custom LINQ extensions вже готові для використання у ReportService |
