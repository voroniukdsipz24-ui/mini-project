# Product Backlog — Hotel Booking System

## Ітерація 1 — Lab 34 (Baseline)

**Мета**: постановка задачі, доменна модель, перший вертикальний зріз, базові тести, CI.

| ID | User Story | Acceptance Criteria | Статус |
|----|-----------|---------------------|--------|
| R-1 | Як адміністратор, хочу переглянути всі номери | Список з типом, ціною, статусом, поверхом | ✅ Done |
| R-2 | Як адміністратор, хочу знайти вільні номери за датами | Фільтр за check-in/out і кількістю гостей | ✅ Done |
| B-1 | Як адміністратор, хочу створити бронювання | Перевірка доступності, розрахунок ціни, in-memory збереження | ✅ Done |
| G-1 | Як адміністратор, хочу зареєструвати гостя | Валідація email, уникнення дублів | ✅ Done |
| G-2 | Як адміністратор, хочу переглянути список гостей | Список з контактами | ✅ Done |

**Архітектурні завдання ітерації 1:**
- [x] Domain: EntityBase (abstract), PersonBase (abstract), Room, Guest, Booking, Hotel, Enums
- [x] Domain: PricingEngine (domain service), DomainException ієрархія
- [x] Domain: IBookingRepository, IRoomRepository, IGuestRepository, IUnitOfWork (інтерфейси)
- [x] Infrastructure: InMemoryUnitOfWork (in-memory, без persistence)
- [x] Application: BookingService.CreateBooking, RoomSearchService.SearchAvailable, GuestService.RegisterGuest
- [x] Console: MainMenu (пошук + створення бронювання + список гостей)
- [x] Tests: 5+ юніт-тестів для Domain
- [x] CI: GitHub Actions (build + test)
- [x] Docs: vision.md, backlog.md, class-diagram.md, sequence-diagram.md, iteration-1.md

---

## Ітерація 2 — Lab 35 (Business Logic + Persistence)

**Мета**: розширення use cases, JSON persistence, LINQ-запити, патерни розширення.

| ID | User Story | Acceptance Criteria | Статус |
|----|-----------|---------------------|--------|
| B-2 | Підтвердити бронювання | Pending → Confirmed | ✅ Done |
| B-3 | Check-in | Confirmed → CheckedIn, Room → Occupied | ✅ Done |
| B-4 | Check-out | CheckedIn → CheckedOut, Room → Available, Paid | ✅ Done |
| B-5 | Скасувати бронювання | З причиною, звільнення номера | ✅ Done |
| B-6 | Переглянути всі бронювання | Список з сортуванням | ✅ Done |
| A-1 | Звіт завантаженості | % occupancy, revenue, avg rate | ✅ Done |
| A-2 | Звіт за типами номерів | Revenue per room type | ✅ Done |
| A-3 | ТОП гостей | Top-5 by total spent | ✅ Done |

**Архітектурні завдання ітерації 2:**
- [x] Infrastructure: JsonUnitOfWork, JsonRepositoryBase<T> (Template Method)
- [x] Infrastructure: JsonBookingRepository, JsonRoomRepository, JsonGuestRepository
- [x] Application: повний BookingService (5 use cases)
- [x] Application: ReportService (LINQ analytics)
- [x] Application: BookingExtensions (custom LINQ methods)
- [x] Console: повне меню (8 пунктів)
- [x] DataSeeder: 9 номерів, 3 гості

---

## Ітерація 3 — Lab 36 (Quality Gate)

**Мета**: quality gate, unit/integration tests, fault handling, coverage.

| ID | Завдання | Статус |
|----|---------|--------|
| T-1 | Fake репозиторії для ізольованого тестування | ✅ Done |
| T-2 | 25+ тестів: unit + integration + негативні сценарії | ✅ Done |
| T-3 | TESTING.md: стратегія, матриця, quality gate | ✅ Done |
| T-4 | CI з quality gate | ✅ Done |

---

## Ітерація 4 — Lab 37 (Release)

**Мета**: release hardening, фінальна документація, демо.

| ID | Завдання | Статус |
|----|---------|--------|
| D-1 | USER_GUIDE.md | ✅ Done |
| D-2 | DEVELOPER_GUIDE.md | ✅ Done |
| D-3 | CHANGELOG.md | ✅ Done |
| D-4 | FINAL_REPORT.md | ✅ Done |
| D-5 | DEMO.md | ✅ Done |
| D-6 | docs/syllabus-coverage.md | ✅ Done |
| D-7 | HotelBooking.Web (ASP.NET Core API + GUI) | ✅ Done |
| D-8 | wwwroot/index.html (HTML/CSS/JS GUI) | ✅ Done |

---

## Backlog (не реалізовано — поза scope)

| ID | User Story | Пріоритет |
|----|-----------|-----------|
| X-1 | Email-підтвердження бронювання | Medium |
| X-2 | Observer для нотифікацій | Medium |
| X-3 | Онлайн-оплата | Low |
| X-4 | Мобільний додаток | Low |
