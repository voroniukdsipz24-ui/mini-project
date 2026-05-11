# Self-Audit — Самостійна 29

## Аудит покриття тем курсу

### Повністю покриті (Core)
- ✅ ООП: класи, інкапсуляція, конструктори — Room, Guest, Booking
- ✅ Абстракції: інтерфейси — IUnitOfWork, IBookingRepository, IRoomRepository, IGuestRepository
- ✅ Generics — `JsonRepositoryBase<T>`, `IReadOnlyList<T>`
- ✅ LINQ — ReportService (GroupBy, Select, Sum, Average, Where, OrderBy, Take, ToDictionary)
- ✅ Обробка помилок — DomainException ієрархія + try-catch у UI
- ✅ SOLID — всі 5 принципів
- ✅ Патерни — Unit of Work, Template Method, Repository, Facade (4 з мінімум 2)
- ✅ UML — Class Diagram + Sequence Diagrams (Mermaid)
- ✅ Тестування — 25+ тестів, unit + integration, негативні сценарії
- ✅ Рефакторинг — витягнення PricingEngine, введення UoW

### Частково покриті
- ⚠️ async/await — є, але без реального async I/O (File.ReadAllTextAsync — формально async)
- ⚠️ Колекції — List<T>, Dictionary<K,V>, але без HashSet, Queue, Stack

### Не покриті (прогалини)
- ❌ Observer / Event-driven — немає нотифікацій
- ❌ Decorator — немає decorators (логування, кешування)
- ❌ Custom LINQ extensions — немає методів розширення
- ❌ Retry-policies — немає
- ❌ Делегати і pipeline — немає

## Пріоритет закриття прогалин

| Прогалина | Важливість | Складність | Рішення |
|-----------|-----------|-----------|---------|
| Custom LINQ extensions | High | Low | `BookingExtensions.cs` |
| Observer / Events | Medium | Medium | `IBookingEventHandler` |
| Decorator (logging) | Medium | Low | `LoggingBookingService` |
| Retry | Low | Medium | Обгортка для `PersistAsync` |
| Делегати | Low | Low | `Func<Booking, bool>` у фільтрах |
