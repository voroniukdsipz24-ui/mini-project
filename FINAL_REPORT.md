# Final Report — Hotel Booking System

## Огляд проєкту

**Назва**: Hotel Booking System («Grand Palais»)
**Версія**: **v1.1.0** (Самостійна 29 — gap closure)
**Платформа**: .NET 8, ASP.NET Core, C# 12
**Тривалість**: 4 ітерації (Lab 34–37) + Самостійна 29
**Тестів**: 144 ✅

---

## 1. Реалізована функціональність

### Use Cases (10)
1. **Пошук вільних номерів** — мультифільтр (дати, тип, ціна, гості)
2. **Створення бронювання** — з conflict detection і live-розрахунком ціни в UI
3. **Підтвердження бронювання** — Pending → Confirmed
4. **Check-in** — Confirmed → CheckedIn, Room → Occupied
5. **Check-out** — CheckedIn → CheckedOut, Paid, Room → Available
6. **Скасування з причиною** — звільнення номера, валідація стану
7. **Реєстрація гостя** — дедуплікація email
8. **Звіт завантаженості** — % occupancy, revenue
9. **Звіт за типами номерів** — LINQ GroupBy
10. **ТОП гостей** — LINQ Take + Sum

### Доменні класи (6)
`EntityBase`, `PersonBase`, `Room`, `Guest`, `Booking`, `Hotel` + `PricingEngine` (static)

### Persistence
JSON-файли у `data/`: `bookings.json`, `rooms.json`, `guests.json`. Атомарний запис через tmp-файл + rename.

### UI шари
| Шар | Технологія |
|-----|-----------|
| **Web (основний)** | ASP.NET Core Minimal API + HTML/CSS/JS у wwwroot |
| **Console (legacy)** | Lab 34 baseline — той самий бекенд через інший UI |

---

## 2. Архітектурні рішення

### Clean Architecture
```
Web GUI / Console UI → Application → Domain ← Infrastructure
```
- **Domain**: entities, interfaces, domain services — без зовнішніх залежностей
- **Application**: use-case services, залежать лише від Domain
- **Infrastructure**: JSON-репозиторії, реалізують Domain interfaces
- **Web/Console**: composition root, обидва на одних Application services

**Ключова перевага**: можна додати React SPA, мобільний додаток або Blazor без жодних змін у Domain/Application/Infrastructure.

### Принципи SOLID

| Принцип | Де застосовано |
|---------|----------------|
| **S** | `BookingService` лише оркеструє use cases; `PricingEngine` лише рахує ціну; UI лише відображає |
| **O** | `JsonRepositoryBase<T>` відкритий для розширення (новий тип сутності — новий підклас) |
| **L** | Всі реалізації `IUnitOfWork` (JSON, InMemory) взаємозамінні |
| **I** | `IBookingRepository`, `IRoomRepository`, `IGuestRepository` — окремі контракти |
| **D** | Application services залежать від `IUnitOfWork`, не від `JsonUnitOfWork` |

### Патерни проєктування

| Патерн | Де | Навіщо |
|--------|-----|--------|
| **Unit of Work** | `JsonUnitOfWork` | Атомарне збереження всіх репозиторіїв |
| **Template Method** | `JsonRepositoryBase<T>` | Спільна логіка, hook `GetId()` |
| **Facade** | `JsonUnitOfWork` | Єдина точка доступу до репозиторіїв |
| **Repository** | `IBookingRepository` та ін. | Абстракція доступу до даних |
| **Observer** | `IBookingEventHandler` → `FileAuditLogHandler` | Аудит-лог змін статусу без зміни BookingService |
| **Strategy (через делегат)** | `CacheInvalidationHandler.Strategy` | Політика інвалідації кешу як `Func<BookingEvent, IEnumerable<string>>` (Сам. 29) |

---

## 3. REST API

| Метод | URL | Опис |
|-------|-----|------|
| GET | `/api/bookings` | Всі бронювання |
| POST | `/api/bookings` | Створити бронювання |
| PUT | `/api/bookings/{id}/confirm` | Підтвердити |
| PUT | `/api/bookings/{id}/checkin` | Check-in |
| PUT | `/api/bookings/{id}/checkout` | Check-out |
| DELETE | `/api/bookings/{id}` | Скасувати |
| GET | `/api/rooms` | Всі номери |
| GET | `/api/rooms/available` | Пошук вільних |
| GET | `/api/guests`, POST | Гості |
| GET | `/api/reports/*` | Звіти (occupancy, room-types, top-guests) |

---

## 4. Тестування

| Категорія | Кількість |
|-----------|-----------|
| Unit — Domain entities (Room, Guest, Booking, Hotel) | 24 |
| Unit — Boundary values (Theory) | ~20 (11 InlineData) |
| Unit — State machine transitions | 11 |
| Unit — EntityBase polymorphism | 5 |
| Unit — Fault handling | 6 |
| Unit — BookingExtensions (LINQ) | 9 |
| Integration — Service + FakeUnitOfWork | 12 |
| Integration — JsonUnitOfWork persistence | 6 |
| Integration — Full cycle (save→reload→operate) | 9 |
| **Observer pattern** (Lab 37) | 7 |
| Інші unit (utility, edge cases) | ~13 |
| **Разом** | **129** ✅ |

- **Framework**: xUnit + coverlet
- **Coverage**: Domain ~90%, Application ~85%, Infrastructure ~80%
- **Quality Gate**: CI відмовляє при будь-якому червоному тесті
- **Performance analysis**: [docs/performance-analysis.md](docs/performance-analysis.md) — критичний шлях ТОП-гостей з бенчмарком (225× прискорення через Dictionary lookup)

---

## 5. Інтеграція тем курсу

| Тема | Де проявилась |
|------|--------------|
| ООП: класи, інкапсуляція | Room, Guest, Booking — private setters, public methods |
| Абстракції: інтерфейси, абстрактні класи | `EntityBase`, `PersonBase`, `IUnitOfWork`, `IRepository<T,TId>` |
| Generics | `JsonRepositoryBase<T>`, `IRepository<T, TId>` |
| LINQ | ReportService: GroupBy, Select, Where, OrderBy, Sum, Average, Take, ToDictionary |
| Custom LINQ extensions | BookingExtensions: Active, ForRoom, TotalRevenue, HasConflict |
| Колекції | List, Dictionary, IReadOnlyList |
| async/await | Скрізь де є I/O (всі репозиторії, services) |
| Обробка помилок | DomainException ієрархія + try-catch у Web/Console |
| SOLID | Всі 5 принципів |
| Патерни | Unit of Work, Template Method, Facade, Repository, **Observer (Lab 37)** |
| UML | Class diagram + 5 sequence diagrams (Mermaid) |
| Тестування | 129 тести: unit + integration + Theory + fault + observer |
| Рефакторинг | Витягнення PricingEngine; видалення type-multiplier; XML docs (Lab 37) |
| Продуктивність | Dictionary lookup замість FirstOrDefault — 225× прискорення |
| **Web/REST** | ASP.NET Core Minimal API |

---

## 6. Архітектурна еволюція по ітераціях

| Ітерація | Lab | Що додалось |
|----------|-----|-------------|
| 1 | Lab 34 | Domain + Application + Console UI + InMemoryUnitOfWork |
| 2 | Lab 35 | JsonUnitOfWork (persistence), повний lifecycle, ReportService, LINQ extensions |
| 3 | Lab 36 | Quality gate: тести, fault handling, coverage у CI |
| 4 | Lab 37 | **Web GUI**: ASP.NET Core API + HTML/CSS/JS, **Observer pattern**, performance analysis, **release v1.0.0** |

---

## 7. Що б зробив інакше

- Ввів би `Result<T>` замість винятків для очікуваних бізнес-помилок (DRY у Web endpoints)
- Додав би Decorator (`LoggingBookingService`) на той самий DI-гачок що Observer
- Підключив би SignalR для real-time оновлення UI у кількох вкладках
- Замінив би vanilla JS на React/Vue для більш складних форм (редагування бронювань)
- Pagination + index у repositories для масштабованості
- Polly retry policy на `PersistAsync` — захист від transient I/O

---

## 8. Висновок

Проєкт пройшов повну еволюцію від простої доменної моделі (Lab 34) до повноцінної веб-системи з REST API і браузерним GUI (Lab 37). Кожна ітерація **будувалась на артефактах попередньої без переписування з нуля**.

Найважливіше — Console-версія залишилась працездатною після додавання Web. Це довело, що **Application і Domain справді не залежать від UI**: один і той самий BookingService обслуговує і консольне меню, і HTTP endpoint.
