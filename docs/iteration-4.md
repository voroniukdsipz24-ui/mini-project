# Iteration 4 (Lab 37) — Release Hardening

## Завдання

Завершити проєкт як release v1.0.0: стабілізація, документація, демонстрація,
закриття прогалин курсу через цільові розширення.

## Що зроблено

### 1. Release scope зафіксовано

[docs/release-plan.md](release-plan.md) — що увійшло в v1.0.0, що залишилось
поза scope (auth, email, multi-tenant), допустимий технічний борг.

### 2. Цільовий рефакторинг

| Що зробив | Чому |
|-----------|------|
| Витягнув `PricingEngine` з `BookingService` (ще Lab 35, доополірований у Lab 37) | SRP: розрахунок ціни — окрема відповідальність, тестується ізольовано |
| Прибрав type-multiplier з `PricingEngine` | Бізнес-нелогічність: ціна вже відображає клас номера через `PricePerNight` |
| Додав `[JsonConstructor]` на `Booking` | Виправив падіння deserialization після введення параметра `totalPrice` у domain constructor |
| Виправив формулу `Guest.Age` | TotalDays/365.25 округлював вниз → off-by-one. Тепер через `today.AddYears(-age)` |
| `Booking.CheckOut()` автоматично виставляє `PaymentStatus.Paid` | Реалістично: гість при виїзді розраховується |
| **Додав Observer pattern**: `IBookingEventHandler` + `FileAuditLogHandler` + `InMemoryAuditLogHandler` | Закриває прогалину курсу: не було реалізованого спостерігача. Тепер lifecycle бронювання генерує події для аудит-логу. Базова інфраструктура для майбутніх Email/SMS notifications. |
| Додав XML-коментарі до публічних API `BookingService` | Документація рівня коду; IntelliSense у IDE покаже опис кожного методу |

### 3. Performance analysis

[docs/performance-analysis.md](performance-analysis.md) — детальний розбір
критичного шляху (звіт ТОП-гостей):
- `Dictionary<int, Guest>` через `ToDictionary` → O(1) lookup замість O(B×G)
- Бенчмарк: 8 мс vs 1800 мс на синтетичних даних (~225× прискорення)
- Розбір усіх інших структур у проєкті з обґрунтуванням

### 4. Документація приведена в актуальний стан

| Файл | Стан |
|------|------|
| README.md | Quick start для Web, links на всі документи |
| USER_GUIDE.md | Повний посібник адміністратора |
| DEVELOPER_GUIDE.md | Архітектура, правила розширення, додавання use case |
| TESTING.md | Стратегія тестування |
| FINAL_REPORT.md | Технічний звіт |
| DEMO.md | 7 сценаріїв демонстрації |
| CHANGELOG.md | Формат Keep a Changelog, v1.0.0 фіксація |
| docs/syllabus-coverage.md | Покриття курсу: основні + додаткові |
| docs/defense-checklist.md, defense-qa.md | Підготовка до захисту |
| docs/performance-analysis.md | **НОВЕ** в Lab 37 |
| docs/iteration-4.md | **ЦЕЙ ФАЙЛ** |

### 5. Розширення курсу (закриття прогалин)

Раніше задокументовано в `extension-plan.md`. У Lab 37 **реально реалізовано Observer**:

```
BookingService → IBookingEventHandler.HandleAsync(BookingEvent)
                              ↓
              ┌───────────────┴────────────────┐
      FileAuditLogHandler              InMemoryAuditLogHandler
       (production)                       (tests/dev)
```

Це додає до проєкту:
- Реальний use case патерну Observer (не «для галочки»)
- Інтеграційний механізм для майбутніх Email/SMS/Analytics handlers
- Аудит-лог із усіма змінами стану бронювань — реальна цінність для бізнесу
- 7 нових тестів (`ObserverTests.cs` + `FileAuditLogTests`)

### 6. CI готовий до релізу

- `.github/workflows/ci.yml` — restore → build → test з coverage
- Жоден тест не падає (129 passed)
- Coverage звіт зберігається як артефакт
- Готовий до тегу v1.0.0

## Що б зробив наступного разу

- Decorator (LoggingBookingService) — на той же гачок DI що Observer
- Retry policy (Polly) на PersistAsync — захист від transient I/O помилок
- Pagination у репозиторіях
- Result<T> замість винятків для очікуваних бізнес-помилок

Усе — в `extension-plan.md`.

## Контрольні питання Lab 37 — відповіді

**Q: Які рефакторинги були найважливішими саме перед релізом?**
A: Найважливішими виявились НЕ розмірні рефакторинги, а точкові:
1. JsonConstructor на Booking — без нього deserialization падала після додавання параметра.
2. Guest.Age формула — нестабільний тест, потрібно стабілізувати перед демо.
3. Type-multiplier у PricingEngine — UX-проблема (preview ≠ збережена ціна).
4. Observer + audit log — закриття курсової прогалини без переписування основного коду.

**Q: Чим FINAL_REPORT.md відрізняється від README.md?**
A: README відповідає «що це і як запустити». FINAL_REPORT — «які рішення прийняв, чому,
що вийшло, що б змінив, як цей проєкт демонструє інтеграцію тем курсу».

**Q: Які теми проєкт демонструє найкраще, а які довелось добирати?**
A: **Найкраще**: ООП ієрархія через EntityBase/PersonBase, LINQ-аналітика в ReportService,
Clean Architecture з реальним правилом залежностей, async/await скрізь.
**Добирали окремо**: Observer (через audit log), Theory тести для PricingEngine.
**Поза scope**: Decorator, Proxy, Adapter — задокументовані в extension-plan.

**Q: Яке архітектурне рішення я б змінив?**
A: Використав би `Result<T>` замість винятків для очікуваних бізнес-помилок
(out-of-stock, duplicate-email). Винятки залишив би тільки для дійсно exceptional
ситуацій. Зараз кожен Web endpoint має `try-catch` що повторюється.

**Q: Як я доводжу, що проєкт готовий до захисту?**
A:
1. CI зелений (129 passed).
2. dotnet run --project src/HotelBooking.Web запускається без помилок.
3. Кожен пункт `defense-checklist.md` підготовлений.
4. README → всі документи доступні і узгоджені з кодом.
5. Demo-сценарій (`DEMO.md`) проганяється за 3-5 хвилин і покриває happy/negative path + persistence.
