# Defense Checklist — Hotel Booking System

> Підготовка до захисту згідно з вимогами Самостійної 29:
> головний сценарій, негативний сценарій, 5+5+3 питань з відповідями.

---

## Перед захистом — фінальний чек

- [ ] `dotnet build` — без помилок і попереджень
- [ ] `dotnet test` — усі ~144 тестів зелені
- [ ] `dotnet run --project src/HotelBooking.Web` запускається без помилок
- [ ] http://localhost:5000 відкривається у браузері
- [ ] Demo-дані сідовані (9 номерів, 3 гості)
- [ ] `data/audit.log` створюється і пишеться (з Lab 37 Observer)
- [ ] README має badges і v1.0.0
- [ ] Усі документи актуальні

---

## Головний сценарій демонстрації (3-5 хв)

1. **Дашборд** — показати 4 KPI картки, останні бронювання
2. **«+ Нове бронювання»** — обрати гостя, номер, дати → live-розрахунок ціни
3. **Створити** → toast «Бронювання створено»
4. **Lifecycle**: Підтвердити → Заселити → Виселити (номер змінює статус)
5. **Звіти** → показати ТОП-гостей (з кешу!)
6. **Audit log**: відкрити `data/audit.log`, показати записи по lifecycle
7. **Persistence**: Ctrl+C, перезапуск → дані живі

---

## Негативний сценарій

1. Спроба створити бронювання з конфліктом дат → toast (червоний) `Room N is not available...`
2. Спроба check-out для не-checked-in бронювання → 400 з повідомленням
3. Корумпуємо `data/bookings.json` (вставляємо «<<< invalid >>>») → перезапуск → система стартує з порожньою колекцією, в stderr `[WARN]` (graceful recovery)

---

## 5 типових питань про архітектуру

### Q1: Де у проєкті видно SRP/OCP/DIP?
**SRP**:
- `BookingService` лише оркеструє use cases (виклик репозиторіїв + domain методів + нотифікація)
- `PricingEngine` — лише розрахунок ціни (чиста функція)
- `MemoryCache<TKey, TValue>` — лише кеш, не знає про звіти

**OCP**:
- `JsonRepositoryBase<T>` — щоб додати новий тип сутності, не змінюємо клас, наслідуємо
- `IBookingEventHandler` — щоб додати нову реакцію на події, реєструємо новий handler у DI
  (зробив це двічі: `FileAuditLogHandler` і `CacheInvalidationHandler`)

**DIP**:
- `BookingService(IUnitOfWork uow)` — сервіс залежить від абстракції
- `ReportService(IUnitOfWork uow, IMemoryCache? cache)` — обидві залежності — абстракції
- Завдяки цьому Console і Web обидва працюють на тих самих сервісах

---

### Q2: Чому обрали саме ці патерни?
- **Unit of Work** (Lab 35): атомарне збереження кількох репозиторіїв. Без нього при створенні
  бронювання `Bookings.Add` міг би зберегтись, а `Rooms.Update` — ні.
- **Template Method** (Lab 35): `JsonRepositoryBase<T>` має готовий load/save/NextId,
  конкретні класи лише перевизначають `GetId(T)`. Без нього було б 3 копії однакового коду.
- **Observer** (Lab 37): lifecycle бронювань генерує події, на які можуть реагувати різні
  компоненти. Без нього додавання аудит-логу вимагало б змін у BookingService.
- **Strategy через делегат** (Сам. 29): `CacheInvalidationHandler` приймає
  `Func<BookingEvent, IEnumerable<string>>` як політику. Без нього був би
  жорсткий список ключів у самому handler.

---

### Q3: Яке правило предметної області найважливіше?
**Унікальність номера в часі**: один номер не може мати **два бронювання, які перетинаються**
у часі, поки обидва активні (Pending/Confirmed/CheckedIn).

Це правило живе в `BookingService.CreateBookingAsync`:
```csharp
var conflicts = await _uow.Bookings.GetByRoomIdAsync(roomId);
bool hasConflict = conflicts
    .Where(b => b.Status is not BookingStatus.Cancelled and not BookingStatus.CheckedOut)
    .Any(b => b.OverlapsWith(checkIn, checkOut));
if (hasConflict) throw new RoomNotAvailableException(...);
```

Тестується через `BookingServiceTests.CreateBooking_OverlappingDates_ThrowsRoomNotAvailable`.

---

### Q4: Чому Dictionary краще за List у вашому сценарії?
У `ReportService.GetTopGuestsAsync` — на кожне бронювання треба підняти об'єкт гостя за `b.GuestId`.

З `List<Guest>`: `guests.FirstOrDefault(g => g.Id == b.GuestId)` — **O(n) на кожне бронювання** → O(B×G).
З `Dictionary<int, Guest>` (через `ToDictionary`): `gd[b.GuestId]` — **O(1) на кожне бронювання** → O(B+G).

`performance-analysis.md` має реальний бенчмарк: 1800 мс vs 8 мс на 10k бронювань — **225× прискорення**.

---

### Q5: Як ви додали кеш без зміни Domain і Application?
- **Domain** не змінився жодним рядком
- **Application/Services/ReportService** прийняв опціональний параметр `IMemoryCache? cache = null`
- Якщо `null` → старий шлях, всі попередні тести зелені
- Якщо інжектовано → wrap у `GetOrAddAsync`
- **Web/Program.cs** зареєстрував `MemoryCache` як Singleton + другий `IBookingEventHandler`
  (`CacheInvalidationHandler`)

Це — **Open/Closed Principle на практиці**. Інтеграція кеша через композицію DI, а не зміну коду.

---

## 5 типових питань про тестування

### Q6: Який тест дає найбільшу впевненість у коректності системи?
**`FullCycleIntegrationTests.FullCycle_CreateThenSaveReloadThenCheckIn`** — створює бронювання,
персистить у JSON, перезапускає сесію (новий UoW), завантажує, виконує check-in. Цей тест:
- покриває серіалізацію/десеріалізацію всього графу (`Booking` з `Items`)
- покриває lifecycle після перезавантаження
- покриває реальну файлову систему (тимчасова директорія з `IDisposable` cleanup)

Якщо він зелений — система реально працює end-to-end.

---

### Q7: Чому Fake а не Mock?
- Mock потребує `mock.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(guest)` — fragile до зміни сигнатур
- Fake = справжня реалізація з in-memory `List<T>`
- Тести коротші, читабельніші, не зламаються від рефакторингу
- Якщо змінити сигнатуру методу інтерфейсу — компілятор покаже Fake як не-реалізує-всі-методи,
  не runtime-error

---

### Q8: Як забезпечена thread safety у тестах MemoryCache?
Тест `MemoryCacheTests.ConcurrentAccess_FactoryRunsOnce`:
```csharp
var tasks = Enumerable.Range(0, 20).Select(_ =>
    cache.GetOrAddAsync("hot-key", async () => {
        Interlocked.Increment(ref factoryCalls);
        await Task.Delay(10);
        return 42;
    }));
var results = await Task.WhenAll(tasks);
Assert.Equal(1, factoryCalls);  // ОДИН раз
```

20 паралельних завдань з одним ключем → factory виконується рівно один раз.
Це доводить, що `Lazy<Task<TValue>>` працює як треба.

---

### Q9: Як тестуєте performance — це ж нестабільно на CI?
`PerformanceTests.GetTopGuests_WithCache_WarmIsFasterThanCold`:
- Не асерт «warm < N мс» (CI varies)
- А **відносний асерт**: `warm < cold × 5` (10 warm викликів менші за 5 cold)
- Тобто ми тестуємо що **кеш справді працює** як кеш, не абсолютну швидкість

Це регресійний захист — якщо хтось зламає кеш, тест впаде на будь-якій машині.

---

### Q10: Які тести наявні для негативних сценаріїв?
- `CreateBooking_GuestNotFound_ThrowsGuestNotFoundException`
- `CreateBooking_RoomNotFound_ThrowsRoomNotFoundException`
- `CreateBooking_OverlappingDates_ThrowsRoomNotAvailable`
- `CheckOut_NotCheckedIn_ThrowsInvalidOperation`
- `Booking_ConstructorWithPastDate_ThrowsArgument`
- `PersistenceTests.CorruptedJson_RecoversGracefully`

Тобто на кожне правило домену є тест, що перевіряє його порушення.

---

## 3 питання про компроміси/альтернативи

### Q11: Чому винятки, а не Result\<T\>?
Винятки простіше і шанс рідких помилок не виправдовує переписування всього API.
Але це **технічний борг**: у Web кожен endpoint має `try-catch`, що дублюється.

**Альтернатива**: `Result<T>` з `Success/Failure`. Web endpoints стали б:
```csharp
var r = await svc.CreateBookingAsync(...);
return r.IsSuccess ? Results.Created(...) : Results.BadRequest(new { error = r.Error });
```

DRY вище, але рефакторинг великий. Залишено для v2.0 (`docs/extension-plan.md`).

---

### Q12: Чому JSON, а не SQLite/PostgreSQL?
- **JSON**: zero-config, людино-читабельний, легко переглядати в редакторі
- **SQLite**: швидше при тисячах записів, але треба EF Core або Dapper

На обсягах MVP (~сотні записів) різниця непомітна. Якщо проєкт виросте — `JsonUnitOfWork`
заміниться на `SqliteUnitOfWork` без зміни Domain/Application (доказ DIP).

---

### Q13: Чому invalidation консервативна (інвалідуємо top-1..10 на будь-яку подію)?
Тонкий taргетинг (наприклад «лише top-5 на Cancelled») складно правильно зробити:
- Що якщо подія Cancelled торкається гостя з ТОП-3, але не з ТОП-1?
- Стратегії стають крихкими і важко тестуються

**Консервативна політика безпечніша**: кеш все одно перебудується за секунди, краще
показати свіжі дані частіше, ніж stale через помилку інвалідації.

Якщо стане проблемою — стратегію легко змінити, бо вона **делегат**:
```csharp
new CacheInvalidationHandler(cache, customStrategy);
```

---

## Контрольні питання Самостійної 29 — короткі відповіді

**Як я доводжу готовність до захисту?**
1. `dotnet test` — 144 passed
2. Браузер на http://localhost:5000 — GUI працює
3. Кожен пункт цього checklist підготовлений
4. `extension-report.md` показує що Самостійна 29 справді закрила прогалини
5. Demo-сценарій працює стабільно
