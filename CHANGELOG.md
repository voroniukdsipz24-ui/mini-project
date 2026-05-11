# Changelog — Hotel Booking System

Усі значущі зміни документуються тут у форматі [Keep a Changelog](https://keepachangelog.com/).

---

## [2.0.0] — Lab 37 (Release + Web GUI)

### Added
- **HotelBooking.Web** — ASP.NET Core Minimal API проєкт
- **15 REST endpoints**: `/api/bookings`, `/api/rooms`, `/api/guests`, `/api/reports/*`
- **wwwroot/index.html** — повноцінний веб-інтерфейс адміністратора:
  - Дашборд зі статистикою (4 stat-картки)
  - Таблиця бронювань з фільтрами і кнопками lifecycle
  - Сітка номерів з фільтрами
  - Реєстр гостей з пошуком
  - Звіти: горизонтальні бари + стовпчастий графік за місяцями
  - Модальні форми (нове бронювання з live-розрахунком, реєстрація гостя)
  - Toast-нотифікації, status indicator API
- USER_GUIDE.md, FINAL_REPORT.md, DEMO.md — оновлені під веб-інтерфейс
- docs/class-diagram.md — додано Web layer
- docs/sequence-diagram.md — переписано під Web flow (5 діаграм)

### Changed
- `Program.cs` Web — composition root з ASP.NET Core DI
- README.md — Web як основний запуск, Console як legacy/baseline

### Kept
- HotelBooking.Console залишено як Lab 34 baseline (доказ, що архітектура витримує заміну UI)
- Domain / Application / Infrastructure — без змін (підтверджує DIP)

---

## [1.2.0] — Lab 36 (Quality Gate)

### Added
- 47 нових тестів у Lab36Tests.cs:
  - Theory тести для PricingEngine (RoomType × Season multipliers)
  - Boundary value тести для Room, Guest, Hotel
  - State machine тести (всі 6 заборонених і 4 дозволених переходи Booking)
  - EntityBase polymorphism тести
  - Fault handling: типізація винятків, інтеграційні fault сценарії
- 9 інтеграційних full-cycle тестів (save→reload→operate)
- `coverlet.msbuild` пакет
- docs/test-strategy.md, docs/test-matrix.md
- CI з coverage gate (XPlat + msbuild)

### Refactored
- `EntityBase.Equals/GetHashCode` — рівність за Id + Type
- `Booking.Cancel()` — додано перевірку CheckedOut статусу
- `JsonRepositoryBase` — атомарний запис (tmp + rename)
- `IRepository<T, TId>` — generic base інтерфейс

### Total tests: 98 (71 unit + 27 integration)

---

## [1.1.0] — Lab 35 (Business Logic + Persistence)

### Added
- `JsonUnitOfWork`, `JsonRepositoryBase<T>` (Template Method)
- `JsonBookingRepository`, `JsonRoomRepository`, `JsonGuestRepository`
- Повний lifecycle: ConfirmBookingAsync, CheckInAsync, CheckOutAsync, CancelBookingAsync
- `ReportService` з LINQ-аналітикою (occupancy, room-types, top-guests)
- `BookingExtensions` (custom LINQ методи: Active, ForRoom, TotalRevenue, HasConflict)
- `DataSeeder` — 9 номерів, 3 гості
- `CancellationToken` у всіх async методах
- Обробка помилок I/O: corrupted JSON → graceful recovery
- docs/iteration-2-plan.md, docs/iteration-2.md

### Changed
- Console: повне меню (8 пунктів)
- IRepository: розширюють `IRepository<T, TId>`

---

## [1.0.0] — Lab 34 (Foundation)

### Added
- Доменні сутності: Room, Guest, Booking, Hotel, EntityBase, PersonBase, Enums
- DomainException ієрархія (5 типів)
- Domain interfaces: IBookingRepository, IRoomRepository, IGuestRepository, IUnitOfWork
- PricingEngine (static domain service)
- InMemoryUnitOfWork
- BookingService.CreateBooking
- HotelBooking.Console: MainMenu (пошук + створення бронювання)
- 22 unit тести (Room, Guest, Booking, PricingEngine)
- docs/vision.md, docs/backlog.md, docs/class-diagram.md, docs/sequence-diagram.md
- README, .gitignore, CI (.github/workflows/ci.yml)
