# Hotel Booking System — HotelBooking

**v1.1.0** | Капстоун-проєкт | Блок 4.5 | .NET 8 | ASP.NET Core + Web GUI

![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet) ![Tests](https://img.shields.io/badge/tests-144_passed-brightgreen) ![License](https://img.shields.io/badge/license-MIT-blue)

> Система бронювання готелю «Grand Palais» — веб-додаток на ASP.NET Core Minimal API
> з повнофункціональним HTML/CSS/JS GUI, JSON persistence, аналітичними звітами та
> 144 автоматизованими тестами з кешем і event-based invalidation.

---

## Швидкий старт

### Веб-версія (основна)
```bash
git clone <repo-url>
cd HotelBooking

dotnet run --project src/HotelBooking.Web
# Відкрийте: http://localhost:5000
```

При першому запуску автоматично сідуються demo-дані (9 номерів, 3 гості).

### Консольна версія (Lab 34 baseline, для демонстрації архітектури)
```bash
dotnet run --project src/HotelBooking.Console
```

## Запуск тестів

```bash
dotnet test
# З coverage:
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover
```

---

## Архітектура

```
HotelBooking/
├── src/
│   ├── HotelBooking.Domain/          # Entities, Interfaces, Domain Services
│   ├── HotelBooking.Application/     # Use-case Services + LINQ Extensions
│   ├── HotelBooking.Infrastructure/  # JSON Repositories, UoW, Seeder
│   ├── HotelBooking.Web/             # 🌐 ASP.NET Core API + GUI (wwwroot/)
│   └── HotelBooking.Console/         # 💻 Console UI (Lab 34 baseline)
├── tests/
│   └── HotelBooking.Tests/           # 98 тестів (unit + integration)
└── docs/                             # Документація + UML
```

**Clean Architecture з 4 шарами**: Web/Console (presentation) → Application → Domain ← Infrastructure.

Web і Console — два різні presentation-шари, обидва покладаються на ті самі Application services. Це доводить, що Domain і Application повністю незалежні від UI.

---

## REST API

| Метод | URL | Опис |
|-------|-----|------|
| GET | `/api/bookings` | Всі бронювання |
| GET | `/api/bookings/{id}` | Одне бронювання |
| POST | `/api/bookings` | Створити бронювання |
| PUT | `/api/bookings/{id}/confirm` | Підтвердити |
| PUT | `/api/bookings/{id}/checkin` | Check-in |
| PUT | `/api/bookings/{id}/checkout` | Check-out |
| DELETE | `/api/bookings/{id}?reason=...` | Скасувати |
| GET | `/api/rooms` | Всі номери |
| GET | `/api/rooms/available?checkIn=...&checkOut=...` | Пошук вільних |
| POST | `/api/rooms` | Додати номер |
| GET | `/api/guests` | Всі гості |
| POST | `/api/guests` | Зареєструвати гостя |
| GET | `/api/reports/occupancy?from=...&to=...` | Завантаженість |
| GET | `/api/reports/room-types` | Дохід за типами |
| GET | `/api/reports/top-guests?top=5` | ТОП гостей |

---

## Можливості GUI

- **Дашборд** — 4 stat-картки (завантаженість, активні, гості, дохід) + таблиця останніх бронювань
- **Бронювання** — повна таблиця з фільтрами, кнопки lifecycle (Confirm / Check-in / Check-out / Cancel)
- **Номери** — сітка з фільтрами за типом і статусом
- **Гості** — реєстр з пошуком
- **Звіти** — горизонтальні бари (дохід, ТОП), стовпчастий графік за місяцями
- **Модальні форми** — створення бронювання з live-розрахунком ціни, реєстрація гостя
- **Toast-нотифікації** — успіх/помилка з валідацією бізнес-правил
- **Status indicator** — Online/Offline статус API в шапці

---

## Технологічний стек

- **Платформа**: .NET 8, C# 12
- **Web**: ASP.NET Core Minimal API
- **Frontend**: Vanilla HTML / CSS / JavaScript (без фреймворків)
- **Persistence**: System.Text.Json (з атомарним записом через tmp + rename)
- **Тестування**: xUnit + coverlet (98 тестів: 71 unit + 27 integration)
- **Архітектура**: Clean Architecture (Domain / Application / Infrastructure / Web|Console)

---

## Документація

| Файл | Опис |
|------|------|
| [docs/vision.md](docs/vision.md) | Бачення продукту, 5 use cases, 5 NFR |
| [docs/backlog.md](docs/backlog.md) | Backlog по 4 ітераціях |
| [docs/class-diagram.md](docs/class-diagram.md) | UML класів (Mermaid) — повна архітектура з Web |
| [docs/sequence-diagram.md](docs/sequence-diagram.md) | UML послідовності — Web flow |
| [docs/self-audit.md](docs/self-audit.md) | **Сам. 29** — чесний аудит покриття курсу |
| [docs/extension-plan.md](docs/extension-plan.md) | **Сам. 29** — план 3 залежних розширень |
| [docs/extension-report.md](docs/extension-report.md) | **Сам. 29** — звіт по розширеннях (cache, invalidation, perf-тести) |
| [docs/performance-analysis.md](docs/performance-analysis.md) | **Lab 37** — аналіз структур даних з бенчмарком |
| [docs/iteration-4.md](docs/iteration-4.md) | **Lab 37** — release hardening report |
| [docs/presentation.md](docs/presentation.md) | **Lab 37** — слайди для захисту |
| [docs/syllabus-coverage.md](docs/syllabus-coverage.md) | Покриття тем курсу |
| [docs/release-plan.md](docs/release-plan.md) | Що в v1.0.0, що поза scope |
| [docs/defense-checklist.md](docs/defense-checklist.md) | Чек-ліст перед захистом + Q&A |
| [docs/defense-qa.md](docs/defense-qa.md) | Розширені Q&A для захисту |
| [TESTING.md](TESTING.md) | Стратегія тестування |
| [docs/test-strategy.md](docs/test-strategy.md) | Деталі тестового підходу |
| [docs/test-matrix.md](docs/test-matrix.md) | Відповідність UC ↔ тести |
| [USER_GUIDE.md](USER_GUIDE.md) | Посібник користувача (адміністратора) |
| [DEVELOPER_GUIDE.md](DEVELOPER_GUIDE.md) | Технічна документація |
| [FINAL_REPORT.md](FINAL_REPORT.md) | Фінальний звіт капстоуну |
| [DEMO.md](DEMO.md) | Сценарій демонстрації |
| [CHANGELOG.md](CHANGELOG.md) | Історія змін за версіями |
