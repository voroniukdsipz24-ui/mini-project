# Iteration 3 — Lab 36

## Мета
Quality gate, автоматизоване тестування, fault handling і контроль якості. Довести, що архітектура витримує негативні сценарії, регресії та зміну вимог.

## Вхідні артефакти з Lab 35
- 10 завершених use cases
- JsonUnitOfWork з error handling
- 50 тестів (unit + integration)
- 7 зафіксованих бізнес-правил
- BookingExtensions (custom LINQ)

---

## Що виправлено / рефакторизовано на цій ітерації

### Цільовий рефакторинг (без переписування)

| Зміна | Причина | Вплив на тестованість |
|-------|---------|----------------------|
| `EntityBase.Equals/GetHashCode` | Визначення рівності за Id + Type, а не за посиланням | Тести `EntityBase_Equals_*` стали можливими |
| `Booking.Cancel()` → перевірка `CheckedOut` | BR-5 не покривав статус CheckedOut | Закрив `Cancel_FromCheckedOut_Throws` |
| `JsonRepositoryBase` — атомарний запис (tmp + rename) | Race condition при збої запису | Integration fault tests стали надійними |
| `IRepository<T, TId>` — generic base | ISP: репозиторії явно реалізують загальний контракт | Тести через інтерфейс, не конкретні класи |

### Виявлені та усунені code smells

| Smell | Де | Виправлення |
|-------|-----|-------------|
| Дублювання валідації імені/email | Guest + можливі майбутні Person-типи | Витягнуто у `PersonBase` |
| Console.WriteLine у persistence (warning) | JsonRepositoryBase | Прийнятно для MVP; на Lab 37 → ILogger |
| `private Room()` без коментаря | Room.cs | Додано коментар `// For JSON deserialization` |

---

## Coverage

```bash
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover
```

| Шар | Орієнтовне покриття |
|-----|---------------------|
| Domain (entities, PricingEngine, exceptions) | ~90% |
| Application (services, extensions) | ~80% |
| Infrastructure (repositories, UoW) | ~75% |
| Console (UI) | ~0% (не автоматизовано) |

**Примітка**: ConsoleUI не тестується автоматично — тільки вручну за DEMO.md.

---

## Статистика тестів

| Категорія | Кількість |
|-----------|-----------|
| Unit — Domain entities (Room, Guest, Booking, Hotel) | 24 |
| Unit — Boundary values (Theory) | 11 інlineData → ~4 Theory методи |
| Unit — State machine (Booking transitions) | 11 |
| Unit — EntityBase polymorphism | 5 |
| Unit — Fault handling (Exception hierarchy) | 6 |
| Unit — BookingExtensions (LINQ) | 9 |
| Integration — Service + FakeUnitOfWork | 12 |
| Integration — JsonUnitOfWork (persistence) | 6 |
| Integration — Full cycle (save→reload→operate) | 9 |
| **Разом** | **~98** |

### За типом
- Unit тести: **~71** (вимога Lab 36: мінімум 20) ✅
- Integration тести: **~27** (вимога Lab 36: мінімум 8) ✅
- Негативних сценаріїв: **~30+** (вимога: мінімум 3) ✅
- Theory параметризованих: **11 InlineData** ✅

---

## CI Quality Gate

`.github/workflows/ci.yml`:
```yaml
- name: Run tests with coverage
  run: |
    dotnet test HotelBooking.sln \
      --collect:"XPlat Code Coverage" \
      --results-directory ./coverage \
      --logger "trx;LogFileName=results.trx"

- name: Upload coverage report
  uses: actions/upload-artifact@v4
  with:
    name: coverage-report
    path: ./coverage/
```

Pipeline відмовляє якщо будь-який тест не проходить (`dotnet test` повертає ненульовий exit code).

---

## Ризики, що залишаються перед Lab 37

| Ризик | Критичність | Що зробити на Lab 37 |
|-------|-------------|---------------------|
| Console.Error.WriteLine у persistence (не ILogger) | Низька | Замінити на ILogger або залишити для MVP |
| ConsoleUI не покрита автотестами | Середня | Ручний тест за DEMO.md; або Extract method |
| JSON serialization з private setters (`[JsonConstructor]`) | Середня | Перевірити при запуску реального app з JSON |
| Відсутній Decorator/Observer (розширення) | Низька | Задокументувати в extension-plan.md |
| ReportService з великими даними — продуктивність | Низька | In-memory LINQ ок для MVP |

---

## Підготовка до Lab 37 (релізу)

- [x] TESTING.md актуальний
- [x] docs/test-strategy.md заповнений
- [x] docs/test-matrix.md — повна відповідність UC ↔ тести
- [ ] USER_GUIDE.md — перевірити актуальність ✅ (вже є)
- [ ] FINAL_REPORT.md — оновити метрики ✅ (є базова версія)
- [ ] DEMO.md — перевірити сценарії за актуальним кодом
- [ ] docs/syllabus-coverage.md — фінальна перевірка

---

## Доповнення для Lab 37: підготовка до Web GUI

Рефакторинг ITestable архітектури на Lab 36 виявився інвестицією у Lab 37: коли додавали ASP.NET Core Web проєкт, **Application/Domain/Infrastructure не змінились жодним рядком**. Web `Program.cs` просто отримав ті самі сервіси через DI:

```csharp
builder.Services.AddSingleton<IUnitOfWork>(uow);
builder.Services.AddScoped(sp => new BookingService(sp.GetRequiredService<IUnitOfWork>()));
// + Minimal API endpoints
```

Тести залишились ті самі — Web layer тестується через ручний smoke (DEMO.md).
