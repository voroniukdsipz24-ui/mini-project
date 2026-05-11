# Defense Q&A — Hotel Booking System

## Питання з архітектури

**Q: Чому Clean Architecture, а не MVC або просто один проєкт?**  
A: Clean Architecture дозволяє змінювати Infrastructure (JSON → SQL) не торкаючись Domain або Application. Тести Application-шару не залежать від файлової системи завдяки FakeRepositories. Це підтверджує DIP і SRP.

**Q: Навіщо IUnitOfWork, якщо є окремі репозиторії?**  
A: Unit of Work координує атомарне збереження — якщо бронювання додане, але статус номера не збережено, система суперечлива. `SaveAsync()` гарантує, що обидва файли записуються в одній "транзакції" (на рівні можливостей JSON).

**Q: Як Domain не знає про Infrastructure?**  
A: Domain визначає інтерфейси (IBookingRepository). Infrastructure *реалізує* їх. Залежність іде від Infrastructure до Domain, але не навпаки. Це класичний Dependency Inversion.

---

## Питання з патернів

**Q: Покажи Template Method у коді.**  
A: `JsonRepositoryBase<T>` містить спільну логіку (EnsureLoadedAsync, PersistAsync, NextIdAsync). Конкретні класи перевизначають лише `protected abstract int GetId(T item)` — це і є hook Template Method.

**Q: Де Observer міг би підійти?**  
A: При зміні статусу бронювання (Confirmed, CheckedIn) можна генерувати події. Підписники — NotificationService (email), AuditService (лог), ReportCache (інвалідація). Наразі це відсутнє — зазначено в syllabus-coverage.md як прогалина.

---

## Питання з LINQ

**Q: Покажи найскладніший LINQ-запит.**  
A: ReportService.GetTopGuestsAsync():
```csharp
return bookings
    .Where(b => b.Status is not BookingStatus.Cancelled)
    .Where(b => guestDict.ContainsKey(b.GuestId))
    .GroupBy(b => b.GuestId)
    .Select(g => (
        Guest: guestDict[g.Key],
        Bookings: g.Count(),
        Spent: g.Sum(b => b.TotalPrice)))
    .OrderByDescending(x => x.Spent)
    .Take(top)
    .ToList()
    .AsReadOnly();
```
GroupBy + Select з анонімним tuple + Sum + OrderByDescending + Take.

---

## Питання з тестування

**Q: Чому Fake, а не Mock (Moq)?**  
A: Fakes простіші в розумінні і не потребують додаткових залежностей. Для цього проєкту поведінка репозиторіїв проста і без ускладнень — Fake є кращим вибором ніж mock framework. Moq доцільний коли потрібно верифікувати порядок і кількість викликів.

**Q: Як перевіряєш негативні сценарії?**  
A: `Assert.ThrowsAsync<SpecificException>(() => ...)` — xUnit дозволяє перевіряти як сам факт винятку, так і його тип. Наприклад, `GuestNotFoundException` — це підклас `DomainException`, але тест перевіряє точний тип.

---

## Питання з ООП

**Q: Навіщо private setters у Booking?**  
A: Інкапсуляція state machine. Якщо Status public — будь-який код може записати `booking.Status = CheckedOut` в обхід бізнес-правил. Private setter + public methods (CheckIn, CheckOut) гарантують, що переходи валідуються.

**Q: Що таке aggregate root в цьому проєкті?**  
A: Hotel — верхній рівень, Booking — агрегат з Room та Guest як зовнішніми посиланнями (через Id, не навігаційні об'єкти у persistence). Booking "owns" свій lifecycle.

---

## Питання з SOLID

**Q: Покажи приклад SRP.**  
A: `PricingEngine` відповідає лише за розрахунок ціни. `BookingService` — лише за orchestration use case. Якби ціноутворення лишилось у BookingService — порушення SRP.

**Q: Покажи OCP.**  
A: `JsonRepositoryBase<T>` відкритий для розширення (новий тип сутності — новий підклас), але закритий для модифікації (логіку завантаження/збереження не чіпаємо).

---

## Питання про Web архітектуру (Lab 37)

**Q: Чому ASP.NET Core Minimal API, а не контролери?**
A: Для цього розміру проєкту контролери — overkill. Minimal API:
- Менше boilerplate (немає окремого контролера на кожен ресурс)
- Endpoints явно видно у `Program.cs` — легко переглянути всю API на одному екрані
- Працює як тонкий transport layer: ловить виняток → повертає 400 з JSON

**Q: Як Web layer не знає про конкретну реалізацію репозиторіїв?**
A: Через DI:
```csharp
builder.Services.AddSingleton<IUnitOfWork>(uow);
builder.Services.AddScoped(sp => new BookingService(sp.GetRequiredService<IUnitOfWork>()));
```
`BookingService` отримує `IUnitOfWork`, не `JsonUnitOfWork`. Хочемо тестову версію — передаємо `InMemoryUnitOfWork`. Хочемо БД — пишемо `SqlUnitOfWork`. Жодних змін у Application або Domain.

**Q: Чому frontend без фреймворків (React/Vue)?**
A: Розмір проєкту не виправдовує overhead. Vanilla JS + fetch покриває всі потреби: state в одному об'єкті `state = {bookings, rooms, guests}`, render-функції викликаються на оновленні. Якщо проєкт виросте — можна замінити frontend без зміни API.

**Q: Як ви обробляєте помилки на API?**
A: Тришарово:
1. Domain кидає `DomainException` (або підклас)
2. Application services пропускають їх вгору
3. Web endpoints ловлять і конвертують у `BadRequest({ error })`:
```csharp
catch (DomainException ex) { return Results.BadRequest(new { error = ex.Message }); }
```
GUI у `fetch().catch()` показує toast.

**Q: Чому Console залишили, якщо є Web?**
A: Console — це **Lab 34 baseline і доказ архітектурної чистоти**. Один і той самий `BookingService` обслуговує обидва UI. Якби довелось змінювати Application або Domain коли додавали Web — це означало б порушення DIP. Збережена Console-версія підтверджує: бекенд незалежний від UI.

**Q: Що буде, якщо два запити прийдуть одночасно на одне бронювання?**
A: В поточній версії — race condition можлива (single-user сценарій). Для production треба:
- Optimistic concurrency через timestamp/version field
- Lock у `BookingService.CreateBookingAsync` (semaphore)
- Або повна транзакційність через БД з ізоляцією

Зазначено в extension-plan як майбутнє покращення.

**Q: Як тестується Web?**
A: API тестується через інтеграційні тести `WebApplicationFactory<Program>` (TODO для Lab 37). GUI — ручне тестування за DEMO.md. Headless E2E (Playwright) — поза scope MVP.
