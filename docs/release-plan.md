# Release Plan — Hotel Booking System

## v2.0.0 — Web Release (поточна)

### Критерії виходу
- [x] Всі 10 use cases реалізовані
- [x] **Web GUI** + REST API працюють end-to-end
- [x] Console-версія залишена як baseline (Lab 34)
- [x] 98 тестів зелені
- [x] Documentation актуальна (README, USER_GUIDE, DEVELOPER_GUIDE, TESTING, FINAL_REPORT, DEMO)
- [x] CHANGELOG актуальний
- [x] syllabus-coverage.md заповнений
- [x] UML діаграми оновлені під Web архітектуру
- [x] Код компілюється без попереджень

### Не входить до v2.0.0 (backlog)
- Авторизація (single-admin context)
- Email-нотифікації
- Платіжна інтеграція
- Multi-tenant (кілька готелів)
- React/Vue SPA замість vanilla JS
- SignalR для real-time оновлення

## Releasing

```bash
# Build всіх проєктів
dotnet build -c Release

# Тести
dotnet test
# → 98 passed

# Запуск Web
dotnet run --project src/HotelBooking.Web -c Release
# → http://localhost:5000

# Або Console (Lab 34 baseline)
dotnet run --project src/HotelBooking.Console
```

Артефакти для здачі: весь репозиторій + `dotnet run --project src/HotelBooking.Web` демонстрація в браузері.

---

## Roadmap (post-v2.0)

| Версія | Функціонал |
|--------|-----------|
| v2.1 | Auth (JWT), ролі (Admin / Manager) |
| v2.2 | Email-підтвердження бронювання (SMTP) |
| v2.3 | Observer events + audit log |
| v3.0 | Multi-tenant: кілька готелів, тенант ізоляція |
| v3.1 | React SPA замість vanilla JS |
| v3.2 | SignalR real-time updates |
