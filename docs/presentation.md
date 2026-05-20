# Presentation Slides — Hotel Booking System

> 5-7 слайдів для захисту капстоуну (3-5 хвилин). Кожен слайд має title, ключові пункти і
> "що сказати". Адаптуйте у PowerPoint або Keynote.

---

## Slide 1: Title

**Заголовок**: Система бронювання готелю «Grand Palais»
**Підзаголовок**: Капстоун-проєкт | Блок 4.5 | .NET 8 + ASP.NET Core

- Студент: [Ім'я]
- Lviv, 2026

**Що сказати** (15 сек):
> Я представляю систему бронювання готелю — це капстоун-проєкт, що демонструє
> інтеграцію тем курсу 4.5: від ООП-основ до Clean Architecture з Web API.

---

## Slide 2: Що це за продукт

**Заголовок**: Що вирішує

- Малі готелі ведуть бронювання в Excel/папері
- Подвійні бронювання, помилки в розрахунку ціни, відсутність аналітики
- Потрібен інструмент адміністратора з REST API

**3 ключові скріншоти/демо**: дашборд, форма бронювання, звіти

**Що сказати** (30 сек):
> Адміністратор готелю отримує повну панель: створення бронювань з live-розрахунком ціни,
> lifecycle (підтвердження → check-in → check-out), і звіти з ТОП-гостями.
> 5 use cases, всі через веб-інтерфейс.

---

## Slide 3: Архітектура

**Заголовок**: Clean Architecture з 4 шарами

```
Web GUI (HTML/JS) → REST API → Application → Domain ← Infrastructure
                                                ↑
                                          IUnitOfWork + Observer
```

- Domain не знає Infrastructure (правило залежностей)
- Web і Console обидва на одних Application services — **доказ DIP**
- ASP.NET Core Minimal API (18 endpoints)
- JSON persistence з атомарним записом
- Observer для audit log

**Що сказати** (45 сек):
> Architecture — Clean Architecture. Залежності всередину. Domain ні від чого не залежить,
> Infrastructure реалізує контракти Domain. Це доводить дві речі: по-перше, Console-версія
> залишилась працездатною після додавання Web — той самий BookingService обслуговує і
> consoleUI, і HTTP endpoints. По-друге, Observer pattern додано через DI без зміни
> жодного рядка в Domain або Application services.

---

## Slide 4: Інтеграція тем курсу

**Заголовок**: Що з курсу інтегровано

| Тема | Де |
|------|-----|
| ООП + поліморфізм | EntityBase, PersonBase (abstract) |
| Generics | `JsonRepositoryBase<T>`, `IRepository<T, TId>` |
| LINQ | ReportService: GroupBy + Sum + Take + ToDictionary |
| Custom extensions | BookingExtensions: Active(), TotalRevenue() |
| Колекції | List, Dictionary, IReadOnlyList |
| async/await | Всі I/O |
| SOLID | Усі 5 принципів з прикладами |
| Патерни | Unit of Work, Template Method, Facade, **Observer** |
| Тестування | 129 тести: unit + integration + Theory |
| UML | Class + 5 Sequence (Mermaid) |

**Що сказати** (45 сек):
> Кожна тема курсу присутня в основному коді, не як штучний додаток. LINQ-аналітика в
> ReportService вирішує реальну задачу — побудову ТОП-гостей за витратами. Чотири паттерни
> виникли природно: UoW для атомарного збереження, Template Method у generic базовому
> repository, Facade у JsonUnitOfWork, і Observer додано в Lab 37 для аудит-логу.

---

## Slide 5: Тестування і якість

**Заголовок**: Якість як артефакт

- **129 тестів**: Domain (40) + Service (17) + Persistence (12) + Observer (7) + інші
- xUnit + coverlet + Theory + InlineData
- Integration full-cycle: save → reload → operate
- CI з coverage gate
- **Performance analysis** (docs/performance-analysis.md): Dictionary lookup → 225× прискорення

**Що сказати** (30 сек):
> 129 тести зелені. Не тільки кількість — структура: Domain тестується ізольовано без I/O,
> Application через Fake UoW, інтеграційні через тимчасові директорії з JSON. Performance —
> у звіті ТОП-гостей використано Dictionary для O(1) lookup, що дає 225-кратне прискорення
> порівняно з наївним FirstOrDefault.

---

## Slide 6: Демо (LIVE)

**Заголовок**: Демонстрація

1. Дашборд → 4 KPI картки, остання активність
2. Створити бронювання → live-розрахунок ціни
3. Lifecycle: Підтвердити → Заселити → Виселити
4. Спроба конфлікту дат → toast (червоний)
5. Звіти: дохід за типами номерів, ТОП-гості
6. Audit log: показати `data/audit.log`
7. Перезапустити, дані живі

**Що сказати** (під час демо, ~2 хв):
> Зараз покажу систему в дії. [клік] Дашборд. [клік] Створюю бронювання, дивлюсь
> live-розрахунок: 2 ночі × 100 грн × 1.25 (сезон) = 250 грн. [клік] Підтверджую,
> заселяю, виселяю — номер вже вільний. [клік] Спроба конфлікту → червоний toast.
> Все це фіксується в audit.log — показую файл.

---

## Slide 7: Висновок і Q&A

**Заголовок**: Що вийшло

✓ 5 use cases, 18 REST endpoints, повний веб-інтерфейс
✓ 129 тести, зелений CI
✓ Документація: README, USER/DEVELOPER guides, FINAL_REPORT, UML, performance, audit
✓ Observer додано без переписування коду (доказ Clean Architecture)
✓ Console залишена як baseline (доказ DIP)

**Що б зробив інакше**:
- Result<T> замість винятків для очікуваних помилок
- Pagination у репозиторіях
- Real-time через SignalR

**Що сказати** (20 сек):
> Проєкт пройшов 4 ітерації від Lab 34 до Lab 37 — і Console з Lab 34 досі працює,
> бо архітектура витримала всі додавання. Готовий до запитань.

---

## Tips для презентації

- **Не читай слайди** — слайди для аудиторії, не для тебе
- **Показуй код** на ключових моментах: Class Diagram, BookingService.CreateBookingAsync, Observer
- **Час**: 3-5 хв загалом. Slide 6 (demo) — найдовший
- **Якщо запитають про обмеження** → відповідай чесно: "у scope MVP не входить auth, email, multi-tenant. Все задокументовано в extension-plan.md"
- **Якщо запитають про продуктивність** → docs/performance-analysis.md
- **Якщо запитають про прогалини курсу** → docs/syllabus-coverage.md
