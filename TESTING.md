# Testing Guide — Hotel Booking System

## Швидкий старт

```bash
# Запустити всі тести
dotnet test

# Запустити з coverage (XPlat)
dotnet test --collect:"XPlat Code Coverage" --results-directory ./coverage

# Запустити з coverage (MSBuild — виводить метрики в консоль)
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover

# Фільтр по класу
dotnet test --filter "FullyQualifiedName~BookingStateMachineTests"

# Verbose
dotnet test --logger "console;verbosity=detailed"
```

## Генерація HTML Coverage Report

```bash
# Встановити reportgenerator (одноразово)
dotnet tool install -g dotnet-reportgenerator-globaltool

# Запустити тести з opencover
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover /p:CoverletOutput=./coverage/

# Згенерувати HTML
reportgenerator -reports:"./coverage/coverage.opencover.xml" -targetdir:"./coverage-report" -reporttypes:Html

# Відкрити
open ./coverage-report/index.html   # macOS
start ./coverage-report/index.html  # Windows
```

---

## Структура тестів

```
tests/HotelBooking.Tests/
├── DomainTests.cs                    ← Unit: Room, Guest, Booking, PricingEngine (23 тести)
├── BookingServiceTests.cs            ← Integration: BookingService + FakeUnitOfWork (12 тестів)
├── PersistenceAndExtensionsTests.cs  ← Unit: BookingExtensions (9) + Integration: Persistence (6)
├── Lab36Tests.cs                     ← Unit: Boundary/Theory/StateMachine (47 тестів)
│   ├── RoomBoundaryTests             ← Theory: Room validation, Pricing multipliers
│   ├── BookingStateMachineTests      ← Всі переходи стану + граничні дати
│   ├── GuestBoundaryTests            ← Theory: email/name validation
│   ├── HotelEntityTests              ← Theory: star rating, Hotel.Display
│   ├── EntityBasePolymorphismTests   ← Display(), Equals, GetHashCode
│   ├── FullCycleIntegrationTests     ← 9 інтеграційних (save/reload/operate)
│   └── FaultHandlingTests            ← 6 тестів fault handling
└── Fakes/
    └── FakeRepositories.cs           ← In-memory fakes (без файлового I/O)
```

---

## Категорії тестів

| Категорія | Файл | Кількість | Тип |
|-----------|------|-----------|-----|
| Domain entities (Room, Guest, Booking) | DomainTests.cs | 23 | Unit |
| Boundary values + Theory | Lab36Tests.cs | ~20 | Unit |
| State machine (Booking transitions) | Lab36Tests.cs | 11 | Unit |
| EntityBase polymorphism | Lab36Tests.cs | 5 | Unit |
| Fault handling (Exception types) | Lab36Tests.cs | 6 | Unit |
| BookingExtensions (LINQ) | PersistenceAndExtensionsTests.cs | 9 | Unit |
| BookingService (Fake repos) | BookingServiceTests.cs | 12 | Integration |
| Persistence (JsonUnitOfWork) | PersistenceAndExtensionsTests.cs | 6 | Integration |
| Full cycle (save→reload→business op) | Lab36Tests.cs | 9 | Integration |
| **Разом** | | **~98** | |

---

## Покриття (орієнтовне)

| Шар | Покриття |
|-----|---------|
| Domain (entities, services, exceptions) | ~90% |
| Application (services, extensions) | ~80% |
| Infrastructure (repositories, UoW) | ~75% |
| Console (UI) | ~0% (ручне тестування) |

---

## Quality Gate

Pipeline відмовляє якщо:
- будь-який тест червоний (`dotnet test` → exit code ≠ 0)
- код не компілюється (`dotnet build` → exit code ≠ 0)

Рекомендований мінімальний поріг coverage: **70% (line)** — встановлено у `.csproj`.

---

## Сценарії, покриті тестами

### Happy Path
- Повний lifecycle: Pending → Confirmed → CheckedIn → CheckedOut
- Збереження і відновлення з JSON між сесіями
- Пошук вільних номерів
- Реєстрація гостя

### Негативні сценарії (~30 тестів)
- Конфлікт дат при бронюванні
- Всі заборонені переходи статусу Booking
- GuestNotFoundException, RoomNotFoundException, RoomNotAvailableException
- Email-дублікат при реєстрації
- Пошкоджений JSON файл
- Відсутній файл при старті

### Граничні значення (Theory)
- Room: number=0, price=-1, capacity=0
- Hotel: stars=0, stars=6
- Guest: email без @, порожнє ім'я
- PricingEngine: всі 4 типи номерів × 7 місяців
- Booking: checkIn=today (valid), 1 ніч (minimum)

---

## Fake vs Real

| Коли Fake (FakeUnitOfWork) | Коли Real (JsonUnitOfWork + tempDir) |
|---------------------------|-------------------------------------|
| Тестуємо Application Services | Тестуємо persistence round-trip |
| Швидко, без I/O | Перевіряємо JSON serialization |
| Ізолюємо від файлів | Перевіряємо обробку помилок I/O |
| Тести Domain rules | Тести full-cycle сценаріїв |
