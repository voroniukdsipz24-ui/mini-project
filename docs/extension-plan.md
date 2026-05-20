# Extension Plan — Самостійна 29

> Три **залежні** розширення (А → Б → В): кожне використовує результат попереднього.

## Загальний контекст

`docs/performance-analysis.md` вже виявив: **звіт ТОП-гостей** — найгарячіша точка системи.
Без кешу при кожному відкритті сторінки «Звіти» весь обсяг бронювань заново
сканується, групується, агрегується. На тестових даних це 8 мс, але:

- При **сотні запитів за секунду** (декілька адміністраторів одночасно) — це 800 мс CPU/с
- Дані змінюються рідко (зміни статусу), читання — часто
- Без invalidation кеш буде stale

Висновок: потрібен **TTL-кеш з event-based invalidation**.

---

## Розширення А — Generic MemoryCache\<TKey, TValue\>

### Що додається
Generic утиліта в `HotelBooking.Application/Caching/MemoryCache.cs`:
- `Task<TValue> GetOrAddAsync(TKey, Func<Task<TValue>>, TimeSpan?)` — атомарне отримання
- `void Invalidate(TKey)` — інвалідація конкретного ключа
- `void Clear()` — повна очистка
- Внутрішня структура: `ConcurrentDictionary<TKey, CacheEntry>` з TTL

### Прогалини курсу що закриває
- ✅ **Generic utility** (раніше було лише `JsonRepositoryBase<T>`)
- ✅ **HashSet/ConcurrentDictionary** (раніше було лише `List<T>` / `Dictionary<T>`)
- ✅ Делегати `Func<Task<TValue>>` як параметр (раніше лише в LINQ-лямбдах)

### Тести
- Базові: get-or-add, hit, miss
- TTL expiration
- Invalidation одного ключа
- Thread safety (паралельний доступ)

---

## Розширення Б — Cache invalidation через Observer

### Що додається
Новий `IBookingEventHandler`: `CacheInvalidationHandler` — слухає події BookingService
і викликає `Invalidate(...)` на кеші коли стан змінився.

Делегат `Action<string>` (або `Func<BookingEvent, IEnumerable<string>>`) визначає
**стратегію інвалідації**: які ключі інвалідувати на яку подію. Це дозволяє
підключити різні політики без зміни Handler.

```csharp
public class CacheInvalidationHandler : IBookingEventHandler
{
    private readonly IMemoryCache _cache;
    private readonly Func<BookingEvent, IEnumerable<string>> _invalidationStrategy;
    // на кожну подію викликаємо стратегію → отримуємо ключі → інвалідуємо
}
```

`ReportService.GetTopGuestsAsync` обгортається у `GetOrAddAsync("top-guests-5", ...)`.

### Прогалини курсу що закриває
- ✅ **Делегати як стратегія/політика** (`Func<BookingEvent, IEnumerable<string>>`)
- ✅ Інтеграція з існуючим Observer pattern (Lab 37) — реальний use case
- ✅ Cache invalidation як архітектурна проблема — у `extension-report.md`

### Тести
- Створення бронювання → кеш ТОП-гостей інвалідовано
- Кеш статей не інвалідується від подій що його не торкаються
- Стратегія через делегат: різні стратегії — різні набори інвалідованих ключів

---

## Розширення В — Параметризовані performance-тести

### Що додається
Новий тест-клас `PerformanceTests` з замірами:

1. **Cold path** (без кешу) vs **warm path** (з кешем) — асерт що warm < cold/5
2. **Theory з різними обсягами**: 100, 1000, 10000 бронювань — асерт верхньої межі мс
3. Тест на інвалідацію: після події кеш miss-ить → cold path знову

Це не повноцінний BenchmarkDotNet (overkill для навчального проєкту), але **реальні
замірювання у тестовому фреймворку** з `Stopwatch` і `[Theory] [InlineData]`.

### Прогалини курсу що закриває
- ✅ **Параметризовані тести з performance** (Theory + різні обсяги)
- ✅ Фактичні замірювання, а не теоретичні цифри
- ✅ Регресійний захист — якщо хтось зламає кеш, тест впаде

---

## Залежності між розширеннями

```
        А: MemoryCache<TKey, TValue>
                  ↓
     (без А Б не має на чому працювати)
                  ↓
        Б: CacheInvalidationHandler + delegate strategy
                  ↓
        (без Б тест В на invalidation неможливий)
                  ↓
        В: Параметризовані performance-тести
```

## Що оновиться у документації

- ✅ `docs/extension-report.md` — фінальний звіт по 3 розширеннях
- ✅ `docs/syllabus-coverage.md` — закриваємо generic utility, делегати-стратегію, perf-тести
- ✅ `docs/performance-analysis.md` — додаємо секцію "After caching"
- ✅ `DEMO.md` — додаємо сценарій з демонстрацією що звіти повторно швидші
- ✅ `FINAL_REPORT.md` — оновлюємо secition «Що зробив би інакше»
- ✅ `CHANGELOG.md` — v1.1.0
