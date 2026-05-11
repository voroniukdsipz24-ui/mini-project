# Iteration 2 — Lab 35

## Мета
Розширити baseline із Lab 34 у повноцінний застосунок: persistence, повний lifecycle бронювання, LINQ-аналітика, обробка помилок I/O, патерни розширення.

## Вхідні артефакти з Lab 34
- Доменна модель: EntityBase, PersonBase, Room, Guest, Booking, Hotel, Enums
- Інтерфейси: IUnitOfWork, IBookingRepository, IRoomRepository, IGuestRepository
- Перший vertical slice: search rooms + create booking (InMemoryUnitOfWork)
- 22 unit тести, CI

---

## Use Cases — повністю готові

| UC | Опис | Статус |
|----|------|--------|
| UC-1 | Пошук вільних номерів (multi-filter) | ✅ |
| UC-2 | Створення бронювання з conflict detection | ✅ |
| UC-3 | Підтвердження бронювання (Pending → Confirmed) | ✅ |
| UC-4 | Check-in (Confirmed → CheckedIn, Room → Occupied) | ✅ |
| UC-5 | Check-out (CheckedIn → CheckedOut, Room → Available, Paid) | ✅ |
| UC-6 | Скасування (з причиною, звільнення номера) | ✅ |
| UC-7 | Реєстрація гостя з дедуплікацією email | ✅ |
| UC-8 | Звіт завантаженості (LINQ, occupancy %, revenue) | ✅ |
| UC-9 | Звіт за типами номерів (LINQ GroupBy) | ✅ |
| UC-10 | ТОП гостей за витратами (LINQ Take + Sum) | ✅ |

---

## Бізнес-правила (мінімум 5 — зафіксовані у коді, тестах і тут)

| BR | Правило | Де перевіряється | Тест |
|----|---------|-----------------|------|
| BR-1 | Дата заїзду не може бути в минулому | `Booking` constructor | `Constructor_PastCheckIn_Throws` |
| BR-2 | Дата виїзду повинна бути після дати заїзду | `Booking` constructor | `Constructor_CheckOutBeforeCheckIn_Throws` |
| BR-3 | Бронювання можна підтвердити лише зі статусу Pending | `Booking.Confirm()` | `Confirm_AlreadyConfirmed_Throws` |
| BR-4 | Check-in можливий лише після Confirmed | `Booking.CheckIn()` | `CheckIn_WithoutConfirm_Throws` |
| BR-5 | Не можна скасувати активне або завершене бронювання | `Booking.Cancel()` | `Cancel_WhenCheckedIn_Throws` |
| BR-6 | Не можна забронювати номер з датами, що перетинаються | `BookingService.CreateBookingAsync` | `CreateBooking_OverlappingDates_Throws` |
| BR-7 | Email гостя повинен бути унікальним | `GuestService.RegisterGuestAsync` | `RegisterGuest_DuplicateEmail_Throws` |

---

## Persistence

### Реалізація
- `JsonRepositoryBase<T>` — Template Method: `EnsureLoadedAsync`, `PersistAsync`, `GetId()` (abstract hook)
- `JsonBookingRepository`, `JsonRoomRepository`, `JsonGuestRepository`
- `JsonUnitOfWork` — Unit of Work + Facade: атомарний `SaveAsync`

### Обробка помилок I/O
| Ситуація | Поведінка |
|----------|-----------|
| Файл відсутній | Стартує з порожньою колекцією (не падає) |
| Пошкоджений JSON | Виводить попередження, стартує з порожньою колекцією |
| IOException при збереженні | Кидає `DomainException` з деталями |
| Запис | Атомарний: tmp-файл + `File.Move(overwrite: true)` |

### CancellationToken
`SaveAsync(CancellationToken ct = default)` і `LoadAsync` підтримують скасування операцій.

---

## Патерн проєктування: Template Method

**Де**: `JsonRepositoryBase<T>` → конкретні репозиторії.

**Чому обрано**: Логіка завантаження з файлу, обробка помилок, серіалізація і nextId — однакові для всіх сутностей. Відрізняється лише `GetId(T item)` — hook-метод, що перевизначається.

**Що складніше без нього**: Дублювання ~50 рядків error handling у кожному з 3 репозиторіїв. Помилка в одному — не виправляється в інших.

**На Lab 37**: Можна додати `JsonDataStoreAdapter<T>` — Adapter між `JsonRepositoryBase` і зовнішнім `IDataStore<T>` контрактом, щоб підтримати XML або CSV export без зміни репозиторіїв.

---

## LINQ-запити (мінімум 4)

| # | Запит | Операції | Файл |
|---|-------|----------|------|
| 1 | Пошук доступних номерів з фільтрами | Where + OrderBy | RoomSearchService |
| 2 | Звіт завантаженості | Where + Sum + Count | ReportService |
| 3 | Групування за типами номерів | GroupBy + Select + Sum + Average + OrderByDescending | ReportService |
| 4 | ТОП гостей | GroupBy + Select + OrderByDescending + Take | ReportService |
| 5 | HasConflict — перевірка перетину дат | ForRoom + Where + Any | BookingExtensions |
| 6 | Active, ForPeriod, ByCheckIn | Where + OrderBy | BookingExtensions |

---

## Спеціалізовані колекції

| Колекція | Де | Навіщо |
|----------|-----|--------|
| `Dictionary<int, Room>` | `ReportService.GetRoomTypeReportAsync` | O(1) lookup кімнати при групуванні бронювань |
| `Dictionary<int, Guest>` | `ReportService.GetTopGuestsAsync` | O(1) lookup гостя при агрегації |
| `List<T>` | всі репозиторії | in-memory storage |

---

## Класи та контракти, що змінились

| Елемент | Зміна |
|---------|-------|
| `IBookingRepository`, `IRoomRepository`, `IGuestRepository` | Розширюють `IRepository<T, TId>` (generic); додано `CancellationToken` |
| `IUnitOfWork` | `SaveAsync(CancellationToken ct = default)` |
| `JsonRepositoryBase<T>` | Додано error handling, atomic write, CancellationToken |
| `Program.cs` | `InMemoryUnitOfWork` → `JsonUnitOfWork` |
| Нові файли | `ReportService.cs`, `BookingExtensions.cs`, `iteration-2-plan.md` |

---

## Ризики для тестування на Lab 36

| Ризик | Рекомендований тип тесту |
|-------|-------------------------|
| JSON round-trip з enum (BookingStatus) | Integration: save → reload → перевірка статусу |
| Conflict detection при граничних датах | Unit: OverlapsWith з суміжними датами |
| Атомарний запис (tmp → rename) | Integration: перевірка цілісності після симуляції збою |
| LINQ-агрегації при порожніх даних | Unit: ReportService з порожнім репозиторієм |
| State machine — всі неправильні переходи | Unit: кожна гілка throw у Booking |

---

## Статистика ітерації

- Нових класів: 6
- Нових тестів: 14 (PersistenceAndExtensionsTests + BookingExtensionsTests)
- Всього тестів: 35+
- Use cases: 10 завершених
- Бізнес-правил задокументовано: 7
