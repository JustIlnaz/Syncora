---
name: avalonia-mvvm-dev
description: Правила разработки desktop-приложений на Avalonia UI (.NET), включая планировщики задач / todo-приложения. Используй этот скилл ВСЕГДА, когда работаешь с файлами .axaml, .cs в проекте Avalonia, ViewModel'ями, биндингами, стилями Avalonia, хранением данных задач (SQLite/LiteDB/JSON), командами и DI-контейнером. Обязательно применяй при создании новых Views/ViewModels, добавлении фич (напоминания, категории, drag&drop, повторяющиеся задачи), рефакторинге биндингов или исправлении багов с UI-потоком.
---

# Avalonia MVVM Dev — правила разработки

Скилл описывает архитектуру и практики для desktop-приложения на Avalonia (планировщик задач), чтобы агент не изобретал WPF-паттерны там, где Avalonia работает иначе, и не ломал производительность/кроссплатформенность.

## 1. Архитектура и структура проекта

Используй архитектуру именно проекта **Syncora**. Отдельных проектов `*.Core` и `*.Data` не создавай и не предлагай. Клиент общается с сервером только через REST API:

```
Syncora/
├── src/
│   ├── Syncora.Api/             # ASP.NET Core Web API
│   │   ├── Controllers/         # REST endpoints
│   │   ├── Services/            # бизнес-логика и прикладные сервисы
│   │   ├── Data/                # DbContext, конфигурация EF Core
│   │   ├── Models/              # серверные/domain-модели
│   │   └── ...
│   └── Syncora.Client/          # Avalonia desktop client
│       ├── Dependencies/
│       ├── Assets/
│       ├── Services/            # REST-клиенты и клиентские сервисы
│       ├── ViewModels/
│       ├── Views/
│       ├── Styles/
│       ├── App.axaml
│       └── ...
├── tests/
│   ├── Syncora.Api.Tests/
│   └── Syncora.IntegrationTests/
├── docker-compose.yml
└── Syncora.sln
```

Поток данных:
**Avalonia Client → REST API → Services → EF Core / DbContext → PostgreSQL**.

Правила архитектуры:
- **Не создавай `Syncora.Core`, `Syncora.Data` или любые другие отдельные `.Core`/`.Data` проекты**, если пользователь явно не попросил изменить архитектуру.
- Серверная бизнес-логика и доступ к данным находятся в `Syncora.Api`.
- `Syncora.Client` отвечает за UI, ViewModel'и и вызовы REST API; он не подключается напрямую к PostgreSQL/EF Core.
- Клиент и API должны быть слабо связаны через DTO/контракты HTTP, а не через общую библиотеку `Core`.
- Тесты находятся в `tests/` и разделены на unit-тесты API и integration-тесты.

## 2. MVVM: используй CommunityToolkit.Mvvm

Не пиши biolerplate вручную и не тяни ReactiveUI без необходимости — для планировщика задач достаточно `CommunityToolkit.Mvvm`:

```csharp
public partial class TaskItemViewModel : ObservableObject
{
    [ObservableProperty]
    private string title = string.Empty;

    [ObservableProperty]
    private bool isCompleted;

    [ObservableProperty]
    private DateTimeOffset? dueDate;

    [RelayCommand]
    private void ToggleComplete() => IsCompleted = !IsCompleted;

    [RelayCommand(CanExecute = nameof(CanDelete))]
    private async Task DeleteAsync() => await _repository.DeleteAsync(Id);

    private bool CanDelete() => !IsCompleted;
}
```

Правила:
- ViewModel НЕ должна знать про Avalonia-типы (Control, Window, Visual). Диалоги/навигация — только через абстракции (`IDialogService`, `INavigationService`), которые реализуются в App-слое.
- Используй ReactiveUI только если реально нужны реактивные цепочки (`WhenAnyValue`, комбинирование потоков событий) — для обычного todo-приложения это избыточно.

## 3. Биндинги — включай Compiled Bindings

Это критично для производительности и для отлова ошибок на этапе компиляции, а не в рантайме:

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             x:Class="TaskPlanner.App.Views.TaskListView"
             x:DataType="vm:TaskListViewModel">
    <ListBox ItemsSource="{Binding Tasks}">
        <ListBox.ItemTemplate>
            <DataTemplate DataType="vm:TaskItemViewModel">
                <CheckBox IsChecked="{Binding IsCompleted}" Content="{Binding Title}" />
            </DataTemplate>
        </ListBox.ItemTemplate>
    </ListBox>
</UserControl>
```

- Всегда указывай `x:DataType` на UserControl/Window и на каждом `DataTemplate`.
- В `.csproj` включи `<AvaloniaUseCompiledBindingsByDefault>true</AvaloniaUseCompiledBindingsByDefault>` — тогда обычные `{Binding}` без x:DataType будут падать на компиляции, а не молча вставлять null в рантайме.
- Не обращайся к `DataContext` через код-behind, если можно обойтись биндингом или Behavior (Avalonia.Xaml.Interactivity).

## 4. Данные и хранение

Для планировщика задач:
- Простой вариант — **LiteDB** (embedded NoSQL, не требует миграций, отлично ложится на модель TaskItem).
- Более "энтерпрайзный" — **SQLite + EF Core** (`Microsoft.EntityFrameworkCore.Sqlite`), если нужны сложные запросы/связи (категории, теги, повторяющиеся задачи).
- Не используй `System.Text.Json` в файл на диске как единственное хранилище, если задач может быть много и нужны конкурентные обновления — файл целиком перезаписывается, легко словить порчу данных при краше.

Путь к данным — платформонезависимый:
```csharp
var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
var dbPath = Path.Combine(appData, "TaskPlanner", "tasks.db");
```
Никогда не хардкодь `C:\...` или `~/Library/...` напрямую.

## 5. UI-поток и асинхронность

- Загрузка/сохранение задач — всегда `async/await`, никогда не блокируй UI-поток `.Result`/`.Wait()`.
- Если нужно обновить коллекцию из фонового потока — используй `Dispatcher.UIThread.Post(...)` или `Dispatcher.UIThread.InvokeAsync(...)`.
- `ObservableCollection<T>` нельзя мутировать из не-UI потока напрямую — либо диспетчеризация, либо `Avalonia.Collections.AvaloniaList<T>` с осторожностью.

## 6. Напоминания и системные уведомления

Avalonia не имеет единого кроссплатформенного API нотификаций "из коробки" на уровне ОС:
- Для уведомлений **внутри окна** используй `Avalonia.Controls.Notifications` (`WindowNotificationManager`).
- Для **системных** нотификаций (трей/уведомления ОС) нужны платформенные обёртки:
  - Windows: `Microsoft.Toolkit.Uwp.Notifications` или прямой Win32 toast API.
  - macOS: `NSUserNotificationCenter` через P/Invoke или нативный биндинг.
  - Linux: `libnotify` через D-Bus.
- Инкапсулируй это за `INotificationService`/клиентским сервисом в `Syncora.Client` и делай платформенные реализации в клиентском слое с `#if WINDOWS`/`RuntimeInformation.IsOSPlatform(...)`, чтобы ViewModel не занималась платформенной логикой.

## 7. Drag & Drop и переупорядочивание задач

- Для drag&drop между списками/колонками (например, Kanban-доска "To Do / In Progress / Done") используй встроенный `Avalonia.Input.DragDrop` API, а не сторонние библиотеки — он кроссплатформенный.
- Для reorder внутри одного списка проще реализовать через `PointerPressed/PointerMoved` + `AllowDrop`, храня индекс перетаскиваемого элемента во ViewModel, а не в code-behind.

## 8. Стили и темы

- Используй `ControlTheme` вместо WPF-style `Style x:Key`, где возможно — это нативный Avalonia-подход, лучше поддерживает Selectors и Classes.
- Светлая/тёмная тема — через `Application.Current.RequestedThemeVariant` и `ThemeVariant.Light/Dark`, не изобретай ручное переключение ResourceDictionary.
- Общие цвета/отступы выноси в `Styles/Colors.axaml`, `Styles/Sizes.axaml` — не хардкодь hex-цвета в каждом View.

## 9. Тестирование

- ViewModel'и покрывай unit-тестами (xUnit + Moq для `ITaskRepository`) — они не зависят от Avalonia, поэтому тестируются как обычный C#.
- Для UI-тестов (если нужны) — `Avalonia.Headless` + `Avalonia.Headless.XUnit`.

## 10. Частые ошибки, которых нужно избегать

- Забытый `x:DataType` → биндинги молча не работают в Release, но работают в Debug из-за reflection fallback.
- Подписка на события (`PropertyChanged`, `CollectionChanged`) без отписки → утечки памяти при закрытии вкладок/окон задач.
- Прямой вызов `new Window().ShowDialog()` из ViewModel — ломает тестируемость; используй `IDialogService`.
- Использование `Task.Run` для операций с БД "на всякий случай" — сначала делай реально асинхронный I/O (`SqliteConnection.OpenAsync`), а не оборачивай синхронный код в поток.
