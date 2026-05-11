# Iteration 2 Plan — Lab 35

> Документ створюється до початку ітерації як план. Факт виконання фіксується в iteration-2.md.

## Сценарії з backlog, що переходять у реалізацію

| ID | User Story | Пріоритет |
|----|-----------|-----------|
| B-2 | Підтвердити бронювання (Pending → Confirmed) | High |
| B-3 | Check-in (Confirmed → CheckedIn, Room → Occupied) | High |
| B-4 | Check-out (CheckedIn → CheckedOut, Room → Available, Paid) | High |
| B-5 | Скасування з причиною (звільнення номера) | High |
| A-1 | Звіт завантаженості (occupancy %, revenue) | Medium |
| A-2 | Звіт за типами номерів (LINQ GroupBy) | Medium |
| A-3 | ТОП гостей за витратами | Medium |

## Класи з Lab 34, що залишаються без змін

| Клас | Причина |
|------|---------|
| `EntityBase`, `PersonBase` | Стабільна абстракція |
| `Room`, `Guest`, `Hotel`, Enums | Доменні сутності стабільні |
| `Booking` (entities state machine) | Методи Confirm/CheckIn/CheckOut/Cancel вже реалізовані |
| `PricingEngine` | Логіка розрахунку ціни не змінюється |
| `DomainException` ієрархія | Повний набір exceptions вже є |
| `BookingService.CreateBookingAsync` | Перший vertical slice з Lab 34 |

## Точки розширення, що використовуються

| Точка розширення | Як використовується на Lab 35 |
|-----------------|------------------------------|
| `IUnitOfWork` | Замінюємо `InMemoryUnitOfWork` → `JsonUnitOfWork` в `Program.cs` |
| `IBookingRepository` | `JsonBookingRepository` реалізує контракт без змін у сервісах |
| `Booking.Confirm/CheckIn/CheckOut/Cancel` | Підключаємо до `BookingService` |
| `JsonRepositoryBase<T>` (Template Method) | Конкретні репозиторії — лише `GetId()` |

## Ризики

| Ризик | Ймовірність | Вплив | Мітигація |
|-------|-------------|-------|-----------|
| JSON serialization з private setters | Висока | Критичний | System.Text.Json потребує конструктори або public setters — тестуємо в першу чергу |
| Corrupted JSON при збої запису | Середня | Критичний | Атомарний запис через tmp-файл + rename |
| Конфлікти при паралельному бронюванні | Низька | Середній | Single-user console — не в scope |
| Дублювання логіки між UI і сервісами | Середня | Середній | ConsoleUI викликає лише сервіси, жодної бізнес-логіки в меню |
| Складність LINQ звітів при великій кількості даних | Низька | Низький | In-memory LINQ, розмір обмежений demo-даними |

## Плановані нові класи/файли

- `src/Infrastructure/Repositories/JsonRepositoryBase.cs` — оновлений з error handling, CancellationToken
- `src/Infrastructure/Repositories/JsonRepositories.cs` — 3 конкретні репозиторії
- `src/Infrastructure/JsonUnitOfWork.cs` — оновлений
- `src/Application/Services/ReportService.cs` — LINQ analytics
- `src/Application/Extensions/BookingExtensions.cs` — custom LINQ extension methods
- `docs/iteration-2-plan.md` (цей файл)
- `docs/iteration-2.md` — пост-фактум звіт
