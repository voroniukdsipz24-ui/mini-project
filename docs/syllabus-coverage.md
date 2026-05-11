# Syllabus Coverage Matrix — Hotel Booking System

## Обов'язкові теми (основний шлях)

| Тема курсу | Де реалізована | Файли | Статус |
|------------|---------------|-------|--------|
| **ООП: класи, інкапсуляція, конструктори** | Room, Guest, Booking, Hotel — private setters, validation in ctor | `Domain/Entities/*.cs` | ✅ |
| **Абстракції: інтерфейси, абстрактні класи** | EntityBase, PersonBase, IUnitOfWork, IRepository\<T,TId\> | `Domain/Entities/EntityBase.cs`, `Domain/Interfaces/IRepositories.cs` | ✅ |
| **Generics і колекції** | `JsonRepositoryBase<T>`, `IRepository<T, TId>`, `List<T>`, `Dictionary<K,V>` | `Infrastructure/Repositories/`, `Domain/Services/PricingEngine.cs` | ✅ |
| **LINQ** | ReportService: GroupBy, Select, Where, OrderBy, Sum, Average, Take, ToDictionary | `Application/Services/ReportService.cs` | ✅ |
| **Custom LINQ extensions** | BookingExtensions: Active, ForRoom, TotalRevenue, HasConflict, ByCheckIn | `Application/Extensions/BookingExtensions.cs` | ✅ |
| **Обробка помилок** | DomainException ієрархія; try-catch у Web endpoints; валідація в конструкторах | `Domain/Exceptions/`, `Web/Program.cs` | ✅ |
| **SOLID** | SRP, OCP, LSP, ISP, DIP — повне покриття | Вся кодова база | ✅ |
| **Мінімум 2 патерни** | Unit of Work, Template Method, Repository, Facade | `Infrastructure/` | ✅ (4 патерни) |
| **UML** | Class Diagram + 5 Sequence Diagrams (Mermaid) | `docs/class-diagram.md`, `docs/sequence-diagram.md` | ✅ |
| **Тестування** | 98 тестів: unit + integration + Theory + fault | `tests/HotelBooking.Tests/` | ✅ |
| **Рефакторинг** | PricingEngine витягнено; Console → Web (без змін Application) | CHANGELOG, commits | ✅ |
| **async/await** | Скрізь де є I/O — Application services, Infrastructure, Web endpoints | Вся кодова база | ✅ |
| **Web/HTTP API** | ASP.NET Core Minimal API + REST endpoints | `Web/Program.cs` | ✅ (Lab 37) |

---

## Додаткові теми (бажано / розширення)

| Тема | Де реалізована | Рівень |
|------|---------------|--------|
| **HashSet / спеціалізовані колекції** | Dictionary у PricingEngine + ReportService (ToDictionary) | ✅ |
| **async I/O** | Всі репозиторії, Web endpoints — async/await pattern | ✅ |
| **Web frontend** | wwwroot/index.html — vanilla HTML/CSS/JS GUI | ✅ (Lab 37) |
| **REST API** | 15 endpoints у Minimal API стилі | ✅ (Lab 37) |
| **DI Container** | ASP.NET Core IServiceCollection | ✅ (Lab 37) |
| **Static file serving** | UseDefaultFiles + UseStaticFiles | ✅ (Lab 37) |
| **Observer** | — | ❌ Не реалізовано (extension-plan) |
| **Decorator** | — | ❌ Не реалізовано (extension-plan) |
| **Retry-policies** | — | ❌ Не реалізовано (extension-plan) |
| **Pipeline / делегати** | — | ❌ Не реалізовано |

---

## Аналіз

### Сильні сторони
- Всі 13 обов'язкових тем присутні в **основному коді**, не як штучні додатки
- LINQ у ReportService вирішує реальну бізнес-задачу
- 4 патерни виникли природно з задачі (не «для галочки»)
- async/await покриває весь I/O і HTTP шар
- **Web GUI повноцінний**: дашборд, CRUD, аналітика, валідація, toast — як справжній SaaS
- **Console залишений** як доказ DIP — той самий бекенд через інший UI

### Прогалини та план закриття
| Прогалина | Спосіб закриття | Файл розширення |
|-----------|----------------|----------------|
| Observer | `IBookingEventHandler` + події на зміну статусу | extension-plan.md |
| Decorator | `LoggingBookingService` як обгортка | extension-plan.md |
| Retry | Polly або власна обгортка для `PersistAsync` | extension-plan.md |
