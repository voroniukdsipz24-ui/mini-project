# Demo Script — Hotel Booking System

## Підготовка

```bash
dotnet run --project src/HotelBooking.Web
```

Відкрийте: **http://localhost:5000**

При першому запуску автоматично завантажуються demo-дані. Якщо хочете «чистий» старт — видаліть папку `data/`.

---

## Сценарій 1: Огляд системи (30 секунд)

1. Відкрити дашборд: показати 4 картки + останні бронювання
2. Перейти в «Номери» — показати сітку, фільтри за типом і статусом
3. Перейти в «Гості» — показати список з кількістю бронювань кожного

**Що показати**: GUI відразу зрозумілий, не потребує навчання, статус API «● Online» в шапці.

---

## Сценарій 2: Створення бронювання (Happy Path) — 1 хвилина

1. Розділ «Бронювання» → «**+ Нове бронювання**»
2. Обрати:
   - Гість: **Олена Шевченко**
   - Номер: **№301 — Suite** (200 грн/ніч)
   - Дати: 01.07.2026 — 05.07.2026 (4 ночі, пік-сезон)
3. Звернути увагу на live-розрахунок:
   `4 ніч × 200 грн × 2.0× (тип) × 1.25× (сезон) = 2 000 грн`
4. Натиснути «Створити бронювання»
5. Toast: «Бронювання створено!»
6. Нове бронювання у статусі **Pending** в таблиці

**Що показати**: PricingEngine, conflict detection, валідація.

---

## Сценарій 3: Повний lifecycle (1 хвилина)

З бронюванням з попереднього сценарію:

1. Клік «**Підтвердити**» → статус → **Confirmed**, з'являється кнопка «Check-in»
2. Клік «**Check-in**» → статус → **CheckedIn**, у розділі «Номери» №301 → **Occupied**
3. Клік «**Check-out**» → статус → **CheckedOut**, оплата → **Paid**, №301 → **Available**

**Що показати**: state machine Booking, синхронізація статусу Room між модулями.

---

## Сценарій 4: Негативний сценарій (30 секунд)

Спроба створити конфліктуюче бронювання:
1. «+ Нове бронювання» → той самий №301 на дати 02.07.2026 — 04.07.2026
2. Натиснути «Створити»
3. Toast (червоний): `Room 7 is not available from 02.07.2026 to 04.07.2026`

**Що показати**: бізнес-правила захищають дані; помилка не ламає UI.

---

## Сценарій 5: Реєстрація гостя + дублікат (30 секунд)

1. «**+ Новий гість**» у сайдбарі
2. Заповнити форму, наприклад: Іван Франко, ivan@test.com, …
3. Toast: «Гостя зареєстровано!»
4. Спробувати ще раз з тим самим email
5. Toast (червоний): `Guest with email 'ivan@test.com' already exists`

**Що показати**: дедуплікація email на рівні Application service.

---

## Сценарій 6: Звіти (30 секунд)

Перейти в «**Звіти**»:
1. **Дохід за типами номерів** — горизонтальні бари
2. **ТОП гостей** — топ-5 за витратами
3. **Зайнятість за місяцями** — стовпчастий графік на 12 місяців

**Що показати**: LINQ-аналітика (GroupBy, Select, Sum, OrderByDescending, Take).

---

## Сценарій 7: Persistence (30 секунд)

1. Створити кілька бронювань через GUI
2. Зупинити сервер (Ctrl+C)
3. Показати у файловому менеджері: `data/bookings.json`, `rooms.json`, `guests.json`
4. Відкрити `bookings.json` — показати збережені дані
5. Перезапустити: `dotnet run --project src/HotelBooking.Web`
6. Дашборд показує всі дані як до перезапуску

**Що показати**: JSON persistence, атомарний запис, відновлення стану.

---

## Технічна частина (для захисту)

### Архітектура
```bash
# Показати структуру проєктів
ls src/
# → HotelBooking.Domain, .Application, .Infrastructure, .Web, .Console
```
- Domain не має жодних залежностей на інші шари
- Web і Console — два варіанти presentation, обидва на одних Application services

### Тести
```bash
dotnet test
# → 98 passed
```

Показати конкретні файли тестів:
- `DomainTests.cs` — unit для entities + PricingEngine
- `Lab36Tests.cs` — Theory, state machine, fault handling
- `PersistenceAndExtensionsTests.cs` — JSON round-trip, corrupted JSON recovery

### REST API (показати в браузері або curl)
```bash
curl http://localhost:5000/api/bookings | jq .
curl http://localhost:5000/api/reports/top-guests?top=5 | jq .
```

### Архітектурні діаграми
- `docs/class-diagram.md` — повна Mermaid діаграма з Web layer
- `docs/sequence-diagram.md` — UC-1 (створення), UC-2 (check-in), UC-3 (негативний), UC-4 (дашборд), UC-5 (звіт)

---

## Опціонально: Console-версія (Lab 34 baseline)

Для демонстрації, що архітектура витримує заміну UI:

```bash
dotnet run --project src/HotelBooking.Console
```

Той самий бекенд (Application/Domain/Infrastructure), інший presentation. Доводить **Dependency Inversion Principle** на практиці.
