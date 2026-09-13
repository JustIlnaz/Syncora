# Syncora — Статус соответствия ТЗ и план работ

> Проверено по спецификации: docs/MVP-SPECIFICATION.md
> Дата аудита: сеанс DSH (после завершения экрана авторизации)

## 1. Что уже реализовано ✅

### Бэкенд (src/Syncora.Api) — покрытие ~80%

| Область | Статус | Где |
|---|---|---|
| JWT-аутентификация (register/login/me) | ✅ | AuthController, JwtHelper, Program.cs |
| Swagger/OpenAPI с Bearer | ✅ | Program.cs |
| EF Core + PostgreSQL + миграции | ✅ | SyncoraDbContext, Migrations/, DbInitializer |
| Календари + участники (owner/editor/viewer/free-busy) | ✅ | CalendarsController, CalendarService |
| События CRUD с проверкой прав | ✅ | EventsController, EventService |
| Поиск свободных слотов (MeetingSearchService) | ✅ | find-slots: рабочие часы, занятость, пересечение |
| Встречи: создание, участники, respond, confirm-slot | ✅ | MeetingsController, MeetingService |
| Уведомления | ✅ | NotificationsController, NotificationService |
| Списки покупок CRUD | ✅ | ShoppingListsController, ShoppingListService |
| Настройки (рабочие часы, timezone) | ✅ | SettingsController, SettingsService, UsersController |
| Стандартизированные ошибки (ExceptionMiddleware) | ✅ | Middleware/ExceptionMiddleware.cs |
| Автовосстановление пароля, контакты, аватары | ✅ | AuthController, UsersController, wwwroot/avatars |

### Клиент (src/Syncora.Client)

| Область | Статус | Где |
|---|---|---|
| Экран авторизации/регистрации (полный) | ✅ | AuthView + AuthViewModel |
| «Запомнить меня» + сохранение сессии | ✅ | AuthSessionStore |
| App Shell: sidebar, компакт-режим, аватар | ✅ | AppShellView, AppShellViewModel |
| 5 разделов навигации по ТЗ | ✅ (каркас) | AppSection: Calendar, Meetings, ShoppingLists, People, Settings |
| Профиль пользователя | ✅ | ProfilePageView + VM |
| REST-клиенты: calendar, event, meeting, shopping, notification | ✅ | Services/ |
| М自适应 sidebar < 960px | ✅ | UpdateLayoutForWidth |

## 2. Критические расхождения с ТЗ

| # | Пункт ТЗ | Проблема |
|---|---|---|
| Г1 | §10.2 Повторная проверка занятости | MeetingService.CreateAsync НЕ проверяет занятость участников перед созданием встречи. Слот мог быть занят после поиска. |
| Г2 | §18.2 POST /api/meetings/search | Эндпоинт называется find-slots, а не search. Request/Response DTO отличаются от примеров ТЗ (participantIds/durationMinutes/from/to). |
| Г3 | §9 Главная кнопка «Сформировать встречу» | В клиенте раздел «Встречи» — placeholder. Кнопки нет вообще. |
| Г4 | §15.1 Правая панель поиска | Не реализована. Основной экран — только sidebar + placeholder. |
| Г5 | §15.3 Календарь (неделя/день/месяц) | Раздел «Календарь» — placeholder. Нет недельного вида. |
| Г6 | §7 «Люди» — поиск и добавление | Placeholder. GET /api/users/search есть на бэке? — проверить UsersController. |
| Г7 | §13 Списки покупок — UI | Placeholder, хотя бэкенд готов. |
| Г8 | §12 Уведомления — UI | Placeholder. |
| Г9 | §20 Тесты | tests/ содержат только UnitTest1.cs (пустые шаблоны). Ни unit по Meeting Engine, ни integration нет. |
| Г10 | §22 Ошибки в стандартизированном JSON | ExceptionMiddleware есть, но формат { error: { code, message } } — проверить соответствие. |
| Г11 | §19 Rate limiting | В Program.cs нет AddRateLimiter. |

## 3. План работ (порядок = приоритет)

### Этап 1 — критичное ядро (главная гипотеза продукта)
1. **MeetingService.CreateAsync: повторная проверка занятости** (Г1, п.10.2 ТЗ)
   - Перед созданием: собрать занятость участников на [start..end], при конфликте — 409.
2. **Привести Meeting API к контракту ТЗ** (Г2)
   - POST /api/meetings/search (алиас/переименование find-slots)
   - DTO: participantIds, durationMinutes, from, to; ответ { slots: [{start,end}] }
3. **Unit-тесты Meeting Engine** (Г9, п.20.1)
   - 2/4/10 участников, пересечения, полностью занятый день, короткие окна, рабочие часы, пограничные значения, два критических примера из п.20.3.

### Этап 2 — клиент, главные экраны
4. **Экран «Календарь»**: недельный вид + события (Г5)
5. **Правая панель «Сформировать встречу»** + кнопка (Г3, Г4):
   - выбор участников (2-10), длительность (пресеты из §9.2), период (§9.3), время поиска
   - вызов search → список слотов → подтверждение → создание Meeting
6. **Раздел «Встречи»**: список встреч, статусы, respond/confirm (Г3)
7. **Списки покупок**: списки + товары + участники (Г7)
8. **«Люди»**: поиск пользователей, добавление в календарь (Г6)
9. **Уведомления**: центр уведомлений (Г8)

### Этап 3 — качество и полировка
10. Integration-тесты (п.20.2 ТЗ)
11. Rate limiting (Г11, §19)
12. Проверить формат ошибок API (Г10, §22)
13. Тёмная тема/адаптив по мере надобности

## 4. Замечания по инфраструктуре сборки (сессия DSH)

- Сборка в сессии: `TMP/TEMP` → `D:\Syncora\.dsh-build`, `AVALONIA_TELEMETRY_OPTOUT=1`, `dotnet build --no-restore` (иначе NuGet-lock и MSBuild temp падают в песочнице).
- src/Syncora.Api/Program.cs в кодировке windows-1251 (не UTF-8) — файлы созданы в VS; при правках аккуратно с кодировкой.
