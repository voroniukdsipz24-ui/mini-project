# Test Matrix — Hotel Booking System

Відповідність між use cases / business rules і конкретними тестами.

## Use Case Coverage

| Use Case | Тест | Файл | Тип |
|----------|------|------|-----|
| UC-1: Пошук вільних номерів | `SearchAvailable_*` (через RoomSearchService) | BookingServiceTests | Integration |
| UC-2: Створення бронювання | `CreateBooking_ValidInput_ReturnsBooking` | BookingServiceTests | Integration |
| UC-2 (negative): гість не знайдений | `CreateBooking_GuestNotFound_ThrowsGuestNotFoundException` | BookingServiceTests | Integration |
| UC-2 (negative): номер не знайдений | `CreateBooking_RoomNotFound_ThrowsRoomNotFoundException` | BookingServiceTests | Integration |
| UC-2 (negative): конфлікт дат | `CreateBooking_OverlappingDates_ThrowsRoomNotAvailable` | BookingServiceTests | Integration |
| UC-3: Підтвердження | `ConfirmBooking_Pending_SetsConfirmed` | BookingServiceTests | Integration |
| UC-3 (negative): вже підтверджено | `Confirm_AlreadyConfirmed_Throws` | DomainTests | Unit |
| UC-3 (negative): скасоване | `Confirm_FromCancelled_Throws` | Lab36Tests | Unit |
| UC-4: Check-in | `CheckIn_ConfirmedBooking_SetsCheckedIn` | BookingServiceTests | Integration |
| UC-4 (negative): без Confirm | `CheckIn_WithoutConfirm_Throws` | DomainTests | Unit |
| UC-4 (negative): зі статусу Pending | `CheckIn_FromPending_Throws` | Lab36Tests | Unit |
| UC-4 (negative): зі статусу Cancelled | `CheckIn_FromCancelled_Throws` | Lab36Tests | Unit |
| UC-5: Check-out | `CheckOut_CheckedIn_SetsCheckedOutAndRoomAvailable` | BookingServiceTests | Integration |
| UC-5 (negative): без CheckIn | `CheckOut_FromPending_Throws` | Lab36Tests | Unit |
| UC-5 (negative): зі Confirmed | `CheckOut_FromConfirmed_Throws` | Lab36Tests | Unit |
| UC-6: Скасування | `CancelBooking_Pending_SetsRoomAvailable` | BookingServiceTests | Integration |
| UC-6 (negative): CheckedIn | `Cancel_WhenCheckedIn_Throws` | DomainTests | Unit |
| UC-6 (negative): CheckedOut | `Cancel_FromCheckedOut_Throws` | Lab36Tests | Unit |
| UC-7: Реєстрація гостя | `RegisterGuest_ValidData_ReturnsGuest` | BookingServiceTests | Integration |
| UC-7 (negative): дублікат email | `RegisterGuest_DuplicateEmail_Throws` | BookingServiceTests + Lab36Tests | Integration + Unit |
| UC-8: Звіт завантаженості | `GetOccupancyReport_NoBookings_ReturnsZero` | BookingServiceTests | Integration |
| UC-10: ТОП гостей | Через ReportService (manual test) | — | Manual |

## Business Rules Coverage

| Business Rule | Тест | Файл |
|---------------|------|------|
| BR-1: check-in не в минулому | `Constructor_PastCheckIn_Throws` | DomainTests |
| BR-2: checkout після checkin | `Constructor_CheckOutBeforeCheckIn_Throws` | DomainTests |
| BR-3: Confirm лише з Pending | `Confirm_AlreadyConfirmed_Throws`, `Confirm_FromCancelled_Throws` | DomainTests, Lab36Tests |
| BR-4: CheckIn лише з Confirmed | `CheckIn_WithoutConfirm_Throws`, `CheckIn_FromPending_Throws` | DomainTests, Lab36Tests |
| BR-5: Cancel лише з Pending/Confirmed | `Cancel_WhenCheckedIn_Throws`, `Cancel_FromCheckedOut_Throws` | DomainTests, Lab36Tests |
| BR-6: Conflict detection | `CreateBooking_OverlappingDates_ThrowsRoomNotAvailable` | BookingServiceTests |
| BR-7: Email унікальний | `RegisterGuest_DuplicateEmail_Throws` | BookingServiceTests, Lab36Tests |

## Pricing Engine Coverage (Theory)

| Параметр | InlineData values | Тест |
|----------|-------------------|------|
| RoomType independence | Standard, Deluxe, Suite, Penthouse — однакова ціна при однаковій базі | `PricingEngine_RoomType_DoesNotAffectPrice` |
| Season multiplier | Jul(1.25), Aug(1.25), Dec(1.25), Apr(1.10), Sep(1.10), Nov(1.0), Jan(1.0) | `PricingEngine_SeasonMultipliers_AreCorrect` |

## Persistence Coverage

| Сценарій | Тест | Файл |
|----------|------|------|
| Save + Reload rooms | `JsonUnitOfWork_SaveAndReload_PreservesRooms` | PersistenceAndExtensionsTests |
| Save + Reload guests | `JsonUnitOfWork_SaveAndReload_PreservesGuests` | PersistenceAndExtensionsTests |
| Save + Reload booking status | `JsonUnitOfWork_SaveAndReload_PreservesBookingStatus` | PersistenceAndExtensionsTests |
| Відсутній файл | `EnsureLoadedAsync_MissingFile_StartsEmpty` | PersistenceAndExtensionsTests |
| Пошкоджений JSON | `EnsureLoadedAsync_CorruptedJson_StartsEmptyWithWarning` | PersistenceAndExtensionsTests |
| NextId після reload | `NextIdAsync_AfterReload_ContinuesFromLastId` | PersistenceAndExtensionsTests |
| Повний lifecycle між сесіями | `CreateBooking_SaveReload_BookingPersistedAndUsable` | Lab36Tests |
| Повний lifecycle (всі статуси) | `FullLifecycle_CreateConfirmCheckinCheckout_AllPersistedCorrectly` | Lab36Tests |
| Скасування + reload | `CancelBooking_SaveReload_RoomIsAvailableAfterReload` | Lab36Tests |
| Кілька бронювань | `MultipleBookings_DifferentRooms_AllPersistedCorrectly` | Lab36Tests |
| Conflict після reload | `ConflictDetection_AfterReload_StillPreventsDoubleBooking` | Lab36Tests |
| NextId між сесіями | `NextId_AfterMultipleSaveReloads_IsAlwaysUnique` | Lab36Tests |
| Empty JSON array | `EmptyJsonArray_LoadsAsEmptyCollection` | Lab36Tests |

## Exception Hierarchy Coverage

| Виняток | Тест |
|---------|------|
| `GuestNotFoundException` (is DomainException) | `GuestNotFoundException_IsTyped_DomainException` |
| `RoomNotFoundException` (is DomainException) | `RoomNotFoundException_IsTyped_DomainException` |
| `RoomNotAvailableException.RoomId` | `RoomNotAvailableException_ContainsRoomIdAndDates` |
| `BookingNotFoundException` (is DomainException) | `BookingNotFoundException_ThrownOnMissingId` |

## Custom LINQ Extensions Coverage

| Extension | Тест |
|-----------|------|
| `.Active()` | `Active_ReturnsOnlyConfirmedAndCheckedIn` |
| `.ForRoom(int)` | `ForRoom_FiltersCorrectly` |
| `.ForGuest(int)` | `ForGuest_FiltersCorrectly` |
| `.TotalRevenue()` | `TotalRevenue_ExcludesCancelled` |
| `.TotalNights()` | `TotalNights_ExcludesCancelled` |
| `.HasConflict()` | `HasConflict_WhenOverlap_ReturnsTrue`, `HasConflict_WhenNoOverlap_ReturnsFalse` |
| `.ByCheckIn()` | `ByCheckIn_SortsAscending` |
| `.ForPeriod()` | `ForPeriod_IncludesOverlappingBookings` |

## Summary

| Категорія | Кількість |
|-----------|-----------|
| Unit тести (Domain entities, PricingEngine, Extensions, State machine, Fault) | ~71 |
| Integration тести (Service + Fakes, Persistence full cycle) | ~27 |
| **Разом** | **~98** |
| Theory ([InlineData]) параметрів | 11 |
| Негативних сценаріїв | ~30 |
