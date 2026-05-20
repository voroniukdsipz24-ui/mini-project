# Changelog — Hotel Booking System

Формат за [Keep a Changelog](https://keepachangelog.com/).

---

## [1.1.0] — 2026-05-20 (Самостійна 29)

### Added
- **Generic MemoryCache<TKey, TValue>** — thread-safe кеш з TTL і атомарним factory call
  через `Lazy<Task<T>>`. Закриває прогалину «generic utility».
- **CacheInvalidationHandler** — Observer-handler що інвалідує ключі кешу за стратегією,
  переданою як делегат `Func<BookingEvent, IEnumerable<string>>`. Реалізує Strategy
  pattern через делегат, закриває прогалину «делегати як параметр методу».
- **PerformanceTests** — параметризовані тести через `[Theory]` з замірами часу:
  cold vs warm path, regression-захист продуктивності.
- `docs/self-audit.md` — чесний аудит покриття курсу
- `docs/extension-plan.md` — план 3 залежних розширень
- `docs/extension-report.md` — звіт про реалізацію кожного розширення
- Інтеграція кеша в `ReportService.GetTopGuestsAsync` (опціонально через ctor)
- DI композиція в `Web/Program.cs`: кеш як Singleton + другий handler

### Changed
- `ReportService` приймає опціональний `IMemoryCache<string, object>` через ctor
- `defense-checklist.md` оновлено за вимогами Самостійної 29 (5+5+3 питань з відповідями)
- `syllabus-coverage.md` — нові ✅ для делегатів-стратегії, generic utility, perf-тестів

### Tests: 144 (з 129 — додано 15 тест-кейсів)
- 6 для MemoryCache (включно з concurrent factory test)
- 3 для CacheInvalidationHandler
- 10 для PerformanceTests (через Theory + InlineData)

---

## [1.0.0] — 2026-05-11 (Release / Lab 37)

### Added
- **Observer pattern**: `IBookingEventHandler` + `BookingEvent` запис
- `FileAuditLogHandler` — записує всі зміни статусу в `data/audit.log`
- `InMemoryAuditLogHandler` — для тестів і dev-режиму
- `BookingService` нотифікує observer-ів після кожної lifecycle-операції
- `docs/performance-analysis.md` — мікроаналіз структур даних з бенчмарком
- `docs/iteration-4.md` — звіт по release hardening
- `docs/presentation.md` — слайди для захисту (5-7)
- XML-коментарі до публічних API `BookingService` і `IBookingEventHandler`
- 7 тестів для Observer (`ObserverTests.cs` + `FileAuditLogTests`)

### Changed
- DI у Web Program.cs реєструє `IBookingEventHandler` як collection
- BookingService приймає необов'язкову колекцію handlers через ctor

### Fixed
- (нічого критичного — це release hardening, не bug fix release)

### Tests: 129 (118 + 11 нових)

---

## [0.4.0] — Lab 37 preview (web GUI)

### Added
- `HotelBooking.Web` — ASP.NET Core Minimal API проєкт
- 15 REST endpoints: bookings, rooms, guests, reports
- `wwwroot/index.html` — повний веб-інтерфейс адміністратора (~720 рядків)
- Live-розрахунок ціни в формі бронювання
- Toast-нотифікації, status indicator API
- Українізовані статуси і кнопки (Підтвердити / Заселити / Виселити / Скасувати)
- USER_GUIDE.md, DEMO.md, FINAL_REPORT.md під веб-архітектуру
- docs/class-diagram.md з Web layer
- docs/sequence-diagram.md — 5 діаграм під веб-flow

### Changed
- README — Web як основний запуск, Console як legacy/baseline
- DI: AddSingleton для всіх services (JsonUnitOfWork тримає shared state)
- ConfigureHttpJsonOptions з JsonStringEnumConverter (frontend очікує рядки)

### Fixed
- Type-multiplier у PricingEngine видалений (нелогічно для готелю)
- Booking deserialization через окремий `[JsonConstructor]`
- `Booking.CheckOut()` автоматично виставляє `PaymentStatus.Paid`
- `Guest.Age` — формула виправлена з off-by-one bug

---

## [0.3.0] — Lab 36 (Quality Gate)

### Added
- 47 нових тестів у Lab36Tests.cs (Theory, boundary, state machine, fault handling)
- 9 інтеграційних full-cycle тестів (save→reload→operate)
- `coverlet.msbuild` пакет
- `docs/test-strategy.md`, `docs/test-matrix.md`
- CI з coverage gate

### Changed
- `EntityBase.Equals/GetHashCode` — рівність за Id + Type
- `JsonRepositoryBase` — атомарний запис (tmp + rename)
- `IRepository<T, TId>` — generic base interface

---

## [0.2.0] — Lab 35 (Business Logic + Persistence)

### Added
- `JsonUnitOfWork`, `JsonRepositoryBase<T>` (Template Method)
- `JsonBookingRepository`, `JsonRoomRepository`, `JsonGuestRepository`
- Повний lifecycle: Confirm → CheckIn → CheckOut + Cancel
- `ReportService` з LINQ-аналітикою
- `BookingExtensions` (custom LINQ)
- `DataSeeder` (9 номерів, 3 гості)
- CancellationToken у всіх async методах

---

## [0.1.0] — Lab 34 (Foundation)

### Added
- Доменні сутності: Room, Guest, Booking, Hotel, EntityBase, PersonBase
- DomainException ієрархія
- IUnitOfWork + 3 IRepository
- PricingEngine
- InMemoryUnitOfWork
- BookingService.CreateBooking
- HotelBooking.Console з MainMenu
- 22 unit тести
- README, docs/vision, docs/backlog, docs/class-diagram, docs/sequence-diagram, CI
