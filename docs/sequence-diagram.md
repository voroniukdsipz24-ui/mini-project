# Sequence Diagrams — Hotel Booking System

> **Архітектура**: Web GUI → REST API → Application Service → Domain → Infrastructure

---

## UC-1: Створення бронювання (через Web GUI)

```mermaid
sequenceDiagram
    actor Admin
    participant UI as Web GUI<br/>(index.html)
    participant API as Web API<br/>(Minimal API)
    participant BS as BookingService
    participant UoW as IUnitOfWork
    participant PE as PricingEngine
    participant Repo as JsonRepositories

    Admin->>UI: Натискає "+ Нове бронювання"
    UI->>UI: Відкриває модальне вікно
    Admin->>UI: Обирає гостя, номер, дати
    UI->>UI: Live-розрахунок ціни (preview)
    Admin->>UI: Клікає "Створити"

    UI->>API: POST /api/bookings<br/>{guestId, roomId, checkIn, checkOut}
    API->>BS: CreateBookingAsync(...)

    BS->>UoW: Guests.GetByIdAsync(guestId)
    UoW->>Repo: SELECT guest
    Repo-->>BS: Guest

    BS->>UoW: Rooms.GetByIdAsync(roomId)
    UoW->>Repo: SELECT room
    Repo-->>BS: Room

    BS->>BS: room.IsAvailable()
    BS->>UoW: Bookings.GetByRoomIdAsync(roomId)
    Repo-->>BS: existing bookings

    BS->>BS: Check overlaps with LINQ
    BS->>PE: Calculate(room, checkIn, checkOut)
    PE-->>BS: totalPrice

    BS->>UoW: Bookings.AddAsync(newBooking)
    BS->>UoW: Rooms.UpdateAsync(room → Reserved)
    BS->>UoW: SaveAsync()
    UoW->>Repo: Persist JSON files

    BS-->>API: Booking
    API-->>UI: 201 Created<br/>{ id, status, totalPrice, ... }
    UI->>UI: Toast "Бронювання створено!"
    UI->>API: GET /api/bookings (refresh)
    UI-->>Admin: Оновлена таблиця бронювань
```

---

## UC-2: Check-in (через Web GUI)

```mermaid
sequenceDiagram
    actor Admin
    participant UI as Web GUI
    participant API as Web API
    participant BS as BookingService
    participant UoW as IUnitOfWork

    Admin->>UI: Клік "Check-in" у рядку таблиці
    UI->>API: PUT /api/bookings/{id}/checkin
    API->>BS: CheckInAsync(bookingId)

    BS->>UoW: Bookings.GetByIdAsync(id)
    UoW-->>BS: Booking (status=Confirmed)

    BS->>BS: booking.CheckIn() [Confirmed → CheckedIn]
    BS->>UoW: Rooms.GetByIdAsync(roomId)
    BS->>BS: room.SetStatus(Occupied)
    BS->>UoW: Rooms.UpdateAsync(room)
    BS->>UoW: Bookings.UpdateAsync(booking)
    BS->>UoW: SaveAsync()

    BS-->>API: Booking
    API-->>UI: 200 OK
    UI->>UI: Toast "Check-in виконано"
    UI-->>Admin: Статус оновлено в UI
```

---

## UC-3: Скасування з помилковим станом (негативний сценарій)

```mermaid
sequenceDiagram
    actor Admin
    participant UI as Web GUI
    participant API as Web API
    participant BS as BookingService

    Admin->>UI: Клік "Скас." на бронюванні з CheckedIn
    UI->>API: DELETE /api/bookings/{id}?reason=...
    API->>BS: CancelBookingAsync(bookingId, reason)

    BS->>BS: booking.Cancel()
    Note over BS: Status = CheckedIn → InvalidOperationException
    BS-->>API: throw InvalidOperationException
    API-->>UI: 400 Bad Request<br/>{ "error": "Cannot cancel an active booking" }
    UI->>UI: Toast (червоний) з повідомленням
    UI-->>Admin: Помилка показана, стан не змінено
```

---

## UC-4: Завантаження дашборду (READ-only сценарій)

```mermaid
sequenceDiagram
    actor Admin
    participant UI as Web GUI
    participant API as Web API
    participant SVC as Services
    participant UoW as IUnitOfWork

    Admin->>UI: Відкриває /
    UI->>UI: loadAll() — паралельні fetch

    par
        UI->>API: GET /api/bookings
        API->>SVC: BookingService.GetAllBookingsAsync()
        SVC->>UoW: Bookings.GetAllAsync()
        UoW-->>UI: bookings[]
    and
        UI->>API: GET /api/rooms
        API->>SVC: RoomSearchService.GetAllRoomsAsync()
        SVC->>UoW: Rooms.GetAllAsync()
        UoW-->>UI: rooms[]
    and
        UI->>API: GET /api/guests
        API->>SVC: GuestService.GetAllGuestsAsync()
        SVC->>UoW: Guests.GetAllAsync()
        UoW-->>UI: guests[]
    end

    UI->>UI: renderDashboard()
    UI-->>Admin: Дашборд з 4 stat-картками + останні 6 бронювань
```

---

## UC-5: Звіт ТОП гостей (LINQ-аналітика)

```mermaid
sequenceDiagram
    actor Manager
    participant UI as Web GUI
    participant API as Web API
    participant RS as ReportService
    participant UoW as IUnitOfWork

    Manager->>UI: Перехід в розділ "Звіти"
    UI->>API: GET /api/reports/top-guests?top=5
    API->>RS: GetTopGuestsAsync(5)

    RS->>UoW: Bookings.GetAllAsync()
    UoW-->>RS: bookings[]
    RS->>UoW: Guests.GetAllAsync()
    UoW-->>RS: guests[]

    RS->>RS: ToDictionary + GroupBy + Select<br/>+ OrderByDescending + Take(5)

    RS-->>API: [(Guest, Bookings, Spent), ...]
    API-->>UI: 200 OK { top guests JSON }
    UI->>UI: renderReports() з горизонтальними барами
    UI-->>Manager: Топ-5 гостей з сумами витрат
```

---

## Архітектурний потік (загальний)

```
Admin/Manager (browser)
        ↓ HTML form
   Web GUI (vanilla JS)
        ↓ fetch('/api/...', {method, body})
   ASP.NET Core Minimal API endpoint
        ↓ DI inject
   Application Service (e.g. BookingService)
        ↓ uses interface
   IUnitOfWork (Domain contract)
        ↓ implementation
   JsonUnitOfWork → JsonRepository → File I/O
        ↑ result
        ↑ JSON serialized
        ↑ Results.Ok(domainObject)
   GUI: state.bookings = json; render*()
```
