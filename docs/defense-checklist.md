# Defense Checklist — Hotel Booking System

## Перед захистом

### Код
- [ ] `dotnet build` — без помилок і попереджень
- [ ] `dotnet test` — всі 98 тестів зелені
- [ ] `dotnet run --project src/HotelBooking.Web` запускається без помилок
- [ ] http://localhost:5000 відкривається у браузері, GUI працює

### Документація
- [ ] README.md — актуальний, є quick start для Web
- [ ] CHANGELOG.md — відображає еволюцію 1.0 → 2.0
- [ ] docs/vision.md — 5 UC, 5 NFR, обмеження
- [ ] docs/backlog.md — user stories, розбиті по 4 ітераціях
- [ ] docs/class-diagram.md — UML з Web layer (Mermaid)
- [ ] docs/sequence-diagram.md — 5 sequence діаграм (включно з Web flow)
- [ ] TESTING.md, docs/test-strategy.md, docs/test-matrix.md
- [ ] USER_GUIDE.md — посібник адміністратора для веб-інтерфейсу
- [ ] DEVELOPER_GUIDE.md — технічна документація
- [ ] FINAL_REPORT.md — підсумковий звіт
- [ ] DEMO.md — сценарії демонстрації для Web
- [ ] docs/syllabus-coverage.md — покриття тем курсу

### Демонстрація
- [ ] Браузер відкритий на http://localhost:5000
- [ ] Demo-дані сідовані (9 номерів, 3 гості)
- [ ] Підготовлені сценарії: happy path → негативний → звіт
- [ ] Можу показати JSON-файли у `data/` після операції
- [ ] DevTools відкритий (Network tab) — показати реальні HTTP-запити

### Питання для захисту
- [ ] "Поясни Clean Architecture" — Domain не знає про Infrastructure
- [ ] "Чому Web і Console одночасно?" — доказ незалежності UI від бекенду
- [ ] "Покажи Unit of Work у коді" — JsonUnitOfWork.SaveAsync
- [ ] "Де SOLID?" — конкретні приклади з кожного шару
- [ ] "Покажи найскладніший LINQ" — ReportService.GetTopGuestsAsync
- [ ] "Чому Fake замість Mock?" — Fake = реальна реалізація без зайвих залежностей
- [ ] "Як Web додавали без зміни Domain?" — DI у Program.cs, ті самі сервіси
- [ ] "Що таке атомарний запис?" — tmp + File.Move(overwrite:true)
- [ ] "Чому Minimal API?" — тонкий шар, endpoints викликають services без бізнес-логіки
- [ ] "Які прогалини?" — Observer, Decorator, Retry; задокументовано в extension-plan

## На захисті

1. **Огляд інтерфейсу** (30 сек): дашборд → бронювання → номери → гості → звіти
2. **Створення бронювання** (1 хв): обрати гостя/номер/дати → live-розрахунок → submit → toast
3. **Lifecycle** (1 хв): Confirm → Check-in (room → Occupied) → Check-out (room → Available)
4. **Негативний сценарій** (30 сек): спроба конфлікту дат → toast червоний
5. **Звіти** (30 сек): показати топ гостей + графік за місяцями
6. **Persistence** (30 сек): перезапуск сервера → дані збережені
7. **Тести** (30 сек): `dotnet test` → 98 passed
8. **Архітектура** (1 хв): показати docs/class-diagram.md, пояснити шари і DIP
9. **Console-версія** (30 сек, опціонально): показати що той самий бекенд працює через Console UI — доказ DIP
