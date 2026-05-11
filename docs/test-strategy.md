# Test Strategy — Hotel Booking System

## Мета тестової стратегії

Визначити, що і як тестується, щоб quality gate не був формальним, а реально захищав систему від регресій перед релізом (Lab 37).

---

## 1. Критичні сценарії

| Сценарій | Чому критичний | Тип тесту |
|----------|---------------|-----------|
| Конфлікт бронювань (подвійне бронювання) | Головна бізнес-помилка готелю | Unit + Integration |
| State machine Booking | Неправильний перехід статусу ламає бізнес-логіку | Unit (кожна гілка) |
| Збереження/відновлення стану | Втрата даних між сесіями = критичний збій | Integration |
| Розрахунок ціни (PricingEngine) | Неправильна ціна = фінансова помилка | Unit (Theory) |
| Обробка пошкодженого JSON | Падіння при старті = повна відмова системи | Integration (fault) |
| Email-унікальність гостя | Дублі ламають звіти і ідентифікацію | Unit |

---

## 2. Частини коду, що найважче тестувати

| Компонент | Складність | Причина |
|-----------|-----------|---------|
| `JsonRepositoryBase<T>` | Висока | Залежить від файлової системи → потрібна тимчасова директорія або mock FileSystem |
| `PricingEngine.GetSeasonMultiplier` | Середня | Private static → тестується через публічний Calculate |
| `ConsoleUI.MainMenu` | Висока | Залежить від Console.ReadLine/Write → не тестується автоматично |
| `DataSeeder` | Низька | Побічний ефект → тестується як частина InMemory інтеграції |

---

## 3. Де потрібні моки, де реальна інтеграція

### Fake-репозиторії (не мок-фреймворк)
**Коли використовувати**: тести Application Services (BookingService, GuestService, ReportService).  
**Чому Fake, а не Moq**: Fake реалізує реальний контракт IRepository, поведінка стабільна та читабельна. Moq доцільний, коли потрібна верифікація *кількості викликів*.

```
BookingService tests → FakeUnitOfWork (in-memory, швидко, ізольовано)
```

### Реальна інтеграція (тимчасова директорія)
**Коли використовувати**: тести `JsonUnitOfWork`, round-trip save/reload, обробка I/O помилок.

```
JsonUnitOfWork tests → Path.GetTempPath() + IDisposable cleanup
```

---

## 4. Негативні сценарії, що можуть зламати проєкт перед захистом

| Ризик | Тест | Файл |
|-------|------|------|
| Corrupted JSON при старті | `EnsureLoadedAsync_CorruptedJson_StartsEmptyWithWarning` | PersistenceAndExtensionsTests |
| GuestNotFoundException не є DomainException | `GuestNotFoundException_IsTyped_DomainException` | Lab36Tests |
| Conflict detection після reload | `ConflictDetection_AfterReload_StillPreventsDoubleBooking` | Lab36Tests |
| Cancel після CheckedIn | `Cancel_FromCheckedOut_Throws` | Lab36Tests |
| Подвійне підтвердження | `Confirm_AlreadyConfirmed_Throws` | DomainTests |
| Відсутній файл при старті | `EnsureLoadedAsync_MissingFile_StartsEmpty` | PersistenceAndExtensionsTests |

---

## 5. Піраміда тестів

```
         /\
        /  \  Integration (15)
       /----\
      /      \  Service/Application (12)
     /--------\
    /          \  Unit / Domain (71)
   /____________\
```

**Розподіл**: ~72% unit, ~12% application (fake repos), ~15% integration.  
Unit тести — швидкі, без I/O. Integration — з реальним файловим I/O, повільніші.

---

## 6. AAA Pattern — стандарт для всіх тестів

```csharp
[Fact]
public async Task CreateBooking_GuestNotFound_ThrowsGuestNotFoundException()
{
    // Arrange
    var (svc, _) = BuildSut(seedGuest: false);

    // Act & Assert
    await Assert.ThrowsAsync<GuestNotFoundException>(() =>
        svc.CreateBookingAsync(99, 1, DateTime.Today.AddDays(1), DateTime.Today.AddDays(3)));
}
```

---

## 7. Що НЕ тестується і чому

| Компонент | Причина виключення |
|-----------|-------------------|
| ConsoleUI (MainMenu) | Presentation layer — ручне тестування за DEMO.md |
| DataSeeder | Demo-дані, не бізнес-логіка |
| Program.cs (composition root) | Конфігурація, не логіка |
