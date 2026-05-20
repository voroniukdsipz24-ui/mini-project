# Release Plan — Hotel Booking System

## v1.0.0 — Release (2026-05-11)

### Що входить у v1.0.0
- 5 use cases (search/book/lifecycle/cancel/register) реалізовані end-to-end
- **Web GUI** (HTML/CSS/JS) + REST API (15 endpoints)
- Console-версія (Lab 34 baseline) як доказ DIP
- JSON persistence з атомарним записом і recovery
- **Observer pattern**: audit log усіх змін статусу бронювання
- 122 автоматизованих тестів (unit + integration + Theory + observer)
- Повна документація (15 файлів у docs/ + 7 у корені)
- CI з coverage gate

### Що переноситься в "після курсу"
- Авторизація (JWT, ролі Admin/Manager)
- Email-нотифікації (SMTP, можна додати другий IBookingEventHandler)
- Платіжна інтеграція (Stripe/LiqPay)
- Multi-tenant (кілька готелів)
- React/Vue SPA замість vanilla JS
- SignalR для real-time оновлення

### Допустимий технічний борг

| Борг | Чому залишився | Коли закриємо |
|------|---------------|---------------|
| Винятки замість Result\<T\> | Великий рефакторинг, поточні працюють | v2.0 |
| Linear scan у JSON repositories | На <10k записів прийнятно | при переході на БД |
| Pagination відсутній | full-list повертається у відповідях | v1.1 |
| Concurrent write race condition | Single-admin сценарій MVP | v2.0 (optimistic locking) |
| Cache відсутній | Звіти швидкі без кешу | при потребі |

### Покриття курсу — статус
| Категорія | Стан |
|-----------|------|
| Обов'язкові теми (17) | ✅ всі покриті |
| Додаткові реалізовані | Observer ✅, XML docs ✅, performance analysis ✅ |
| Свідомо залишені | Decorator, Retry, Result\<T\>, Pipeline (extension-plan) |

## Releasing

```bash
# Build всіх проєктів
dotnet build -c Release

# Тести
dotnet test
# → 129 passed

# Запуск Web
dotnet run --project src/HotelBooking.Web -c Release
# → http://localhost:5000

# Або Console (Lab 34 baseline)
dotnet run --project src/HotelBooking.Console
```

### Git tag

```bash
git tag -a v1.0.0 -m "v1.0.0 — Capstone release (Lab 37 complete)"
git push origin v1.0.0
```

Артефакти для здачі: весь репозиторій + `dotnet run --project src/HotelBooking.Web` демонстрація в браузері.

---

## Roadmap (post-v1.0)

| Версія | Функціонал | Орієнтовний обсяг |
|--------|-----------|-------------------|
| v1.1 | Pagination, search performance, error handling уніфікація | 1-2 тижні |
| v2.0 | Auth (JWT), Decorator (LoggingService), Result\<T\> | 2-4 тижні |
| v2.1 | Email-підтвердження (SMTP IBookingEventHandler) | 1 тиждень |
| v2.2 | Observer events + audit log UI | 1 тиждень |
| v3.0 | Multi-tenant: кілька готелів, tenant isolation | 1+ місяць |
| v3.1 | React SPA замість vanilla JS | 2-3 тижні |
| v3.2 | SignalR real-time updates | 1 тиждень |
