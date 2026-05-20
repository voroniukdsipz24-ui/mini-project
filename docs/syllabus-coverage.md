# Syllabus Coverage Matrix — Hotel Booking System

## Обов'язкові теми (основний шлях)

| Тема курсу | Де реалізована | Файли | Статус |
|------------|---------------|-------|--------|
| **ООП: класи, інкапсуляція, конструктори** | Room, Guest, Booking, Hotel — private setters, validation in ctor | `Domain/Entities/*.cs` | ✅ |
| **Абстракції: інтерфейси, абстрактні класи** | EntityBase, PersonBase, IUnitOfWork, IRepository\<T,TId\> | `Domain/Entities/EntityBase.cs`, `Domain/Interfaces/IRepositories.cs` | ✅ |
| **Поліморфізм** | Перевизначення `Display()` у кожній сутності; `EntityBase.Equals` через GetType + Id | `Domain/Entities/*.cs` | ✅ |
| **Generics і колекції** | `JsonRepositoryBase<T>`, `IRepository<T, TId>`, `List<T>`, `Dictionary<K,V>`, `IReadOnlyList<T>` | `Infrastructure/Repositories/`, `Application/` | ✅ |
| **LINQ** | ReportService: GroupBy, Select, Where, OrderBy, Sum, Average, Take, ToDictionary | `Application/Services/ReportService.cs` | ✅ |
| **Custom LINQ extensions** | BookingExtensions: Active, ForRoom, TotalRevenue, HasConflict, ByCheckIn | `Application/Extensions/BookingExtensions.cs` | ✅ |
| **Делегати/події** | Observer pattern: `IBookingEventHandler` колекція через DI | `Domain/Interfaces/IBookingEventHandler.cs` | ✅ (Lab 37) |
| **Обробка помилок** | DomainException ієрархія; try-catch у Web endpoints; валідація в конструкторах | `Domain/Exceptions/`, `Web/Program.cs` | ✅ |
| **Persistence** | JSON з атомарним записом, graceful recovery від corruption | `Infrastructure/Repositories/JsonRepositoryBase.cs` | ✅ |
| **SOLID (5/5)** | SRP, OCP, LSP, ISP, DIP — приклади з кожного шару | Вся кодова база | ✅ |
| **Мінімум 2 патерни** | Unit of Work, Template Method, Repository, Facade, **Observer** | `Infrastructure/`, `Application/`, `Domain/Interfaces/` | ✅ (5 патернів) |
| **UML** | Class Diagram + 5 Sequence Diagrams (Mermaid) | `docs/class-diagram.md`, `docs/sequence-diagram.md` | ✅ |
| **Тестування** | 129 тести: unit + integration + Theory + fault + observer | `tests/HotelBooking.Tests/` | ✅ |
| **Рефакторинг** | PricingEngine витягнено; Type-multiplier прибрано; Console → Web без змін Application | CHANGELOG, iteration-4.md | ✅ |
| **async/await** | Скрізь де є I/O — Application services, Infrastructure, Web endpoints, Observer | Вся кодова база | ✅ |
| **Web/HTTP API** | ASP.NET Core Minimal API + 15 REST endpoints | `Web/Program.cs` | ✅ |
| **Продуктивність** | Аналіз структур даних з бенчмарком (Dictionary lookup 225× прискорення) | `docs/performance-analysis.md` | ✅ (Lab 37) |

---

## Додаткові теми (бажано / розширення)

| Тема | Де реалізована | Рівень |
|------|---------------|--------|
| **HashSet / спеціалізовані колекції** | Dictionary у ReportService; **ConcurrentDictionary у MemoryCache** (Сам. 29) | ✅ |
| **async I/O з CancellationToken** | Всі репозиторії, Web endpoints, Observer | ✅ |
| **Web frontend (vanilla)** | wwwroot/index.html — HTML/CSS/JS без фреймворків | ✅ (Lab 37) |
| **REST API** | 15 endpoints у Minimal API стилі | ✅ (Lab 37) |
| **DI Container** | ASP.NET Core IServiceCollection + AddSingleton patterns | ✅ (Lab 37) |
| **Static file serving** | UseDefaultFiles + UseStaticFiles | ✅ (Lab 37) |
| **Observer pattern** | `IBookingEventHandler` + `FileAuditLogHandler` + `InMemoryAuditLogHandler` | ✅ (Lab 37) |
| **Generic utility** | `MemoryCache<TKey, TValue>` з TTL і thread-safety | ✅ (Сам. 29) |
| **Strategy через делегат** | `CacheInvalidationHandler` приймає `Func<BookingEvent, IEnumerable<string>>` | ✅ (Сам. 29) |
| **Параметризовані performance-тести** | `PerformanceTests` з `[Theory] [InlineData]` і `Stopwatch` | ✅ (Сам. 29) |
| **Lazy\<T\>** | `Lazy<Task<TValue>>` у MemoryCache для атомарного factory | ✅ (Сам. 29) |
| **XML documentation** | Публічні API `BookingService`, `IBookingEventHandler`, `IMemoryCache` | ✅ |
| **Decorator** | — | ❌ extension-plan.md (немає реального use case) |
| **Retry-policies (Polly)** | — | ❌ extension-plan.md (файлове I/O стабільне) |
| **Pipeline / MediatR** | — | ❌ extension-plan.md (overkill для розміру) |
| **Result\<T\>** | — | ❌ extension-plan.md (замість винятків — рефакторинг великий) |

---

## Аналіз

### Сильні сторони
- **Всі обов'язкові теми присутні в основному коді**, не як штучні додатки
- LINQ у ReportService вирішує реальну бізнес-задачу
- **5 патернів** виникли природно з задачі (не «для галочки»)
- Observer (Lab 37) інтегрований без переписування коду — доказ Clean Architecture
- async/await покриває весь I/O і HTTP шар
- Web GUI повноцінний: дашборд, CRUD, аналітика, валідація, toast
- Console залишений як baseline (доказ DIP)
- **Performance analysis** з реальним мікроаналізом і бенчмарком

### Прогалини, що залишились (свідомо)
| Прогалина | Чому не зробив | Plan |
|-----------|---------------|------|
| Decorator | Не виник природний use case (Observer покрив cross-cutting потребу) | extension-plan |
| Retry policies | Файлове I/O рідко падає; в продакшені — Polly | extension-plan |
| Result\<T\> | Винятки працюють, рефакторинг великий | extension-plan |
| Pipeline / MediatR | Overkill для розміру проєкту | extension-plan |

Усе задокументовано в `extension-plan.md` — це не «забув», а свідомо НЕ-зроблено.

---

## Як читати цю матрицю

| Символ | Значення |
|--------|----------|
| ✅ | Реалізовано, є код + тести |
| ✅ (Lab N) | Реалізовано на конкретній ітерації |
| ❌ | Свідомо не зроблено, є план у extension-plan.md |
