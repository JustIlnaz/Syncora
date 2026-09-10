---
name: macos-style-design
description: Визуальный дизайн интерфейса в стиле macOS (Big Sur/Sonoma/Sequoia) для приложений на Avalonia. Используй этот скилл при работе над .axaml-стилями, окнами, боковыми панелями, тулбарами, кнопками, цветовой схемой и типографикой — везде, где пользователь просит "сделать красиво", "как на маке", "нативный вид под macOS" или просто настраивает внешний вид планировщика задач. Также применяй при настройке кастомного окна (frameless/extend-client-area), скруглений, теней, blur-эффектов и системного меню.
---

# macOS-style Design для Avalonia

Скилл описывает, как визуально приблизить Avalonia-приложение к нативному macOS-виду: layout, типографика, цвета, эффекты и конкретные Avalonia API. Не про пиксель-в-пиксель копирование системных иконок Apple (это лицензионно проблематично), а про язык дизайна: сдержанность, скруглённость, воздух, приглушённые цвета, sidebar-навигация.

Для проекта **Syncora** учитывай существующую архитектуру: `src/Syncora.Api` + `src/Syncora.Client`, тесты в `tests/`, без отдельного `*.Core` проекта. Этот skill отвечает за визуальную часть `Syncora.Client` и не должен предлагать перестраивать solution ради macOS-стиля.

## 1. Общая композиция окна

Классический macOS-паттерн для productivity-приложений (Notes, Reminders, Things):

```
┌───────────────────────────────────────────────┐
│  ●●●     Заголовок / поиск           [+] [⚙]  │  ← тулбар, встроенный в title bar
├───────────┬─────────────────────────────────────┤
│  Sidebar  │              Content                │
│  (списки, │        (список задач /              │
│  теги)    │         детали задачи)               │
│           │                                      │
└───────────┴─────────────────────────────────────┘
```

- **Sidebar** — фиксированная ширина ~220–260px, полупрозрачный/чуть темнее фона контента.
- **Тулбар** объединён с заголовком окна (нет отдельной "толстой" полосы меню сверху, как в Windows).
- Кнопки закрытия/сворачивания/разворачивания (трафик-лайты) — слева, если приложение реально таргетится под macOS и запускается там.

### Кастомное окно в Avalonia

```csharp
Window window = new()
{
    ExtendClientAreaToDecorationsHint = true,
    ExtendClientAreaTitleBarHeightHint = -1,
    ExtendClientAreaChromeHints = ExtendClientAreaChromeHints.PreferSystemChrome,
    TransparencyLevelHint = new[] { WindowTransparencyLevel.Mica, WindowTransparencyLevel.AcrylicBlur }
};
```

- `PreferSystemChrome` на macOS сохранит нативные трафик-лайты вместо самодельных кнопок — это выглядит правильнее, чем рисовать круглые кнопки руками.
- Не пытайся вручную рисовать трафик-лайты для эмуляции на Windows/Linux — на не-macOS платформах используй свой минималистичный набор кнопок управления окном (просто три плоские иконки без "конфетных" цветов).

## 2. Типографика

- Системный шрифт macOS — **SF Pro** — лицензирован Apple только для приложений, распространяемых для Apple-платформ; не встраивай его файл в кроссплатформенную сборку под Windows/Linux.
- Практичное решение: `FontFamily="avares://.../Fonts#Inter"` (Inter) или `-apple-system` fallback-стек — по метрикам Inter близок к SF Pro и бесплатен (OFL).
- Если сборка реально идёт **только** под macOS — можно ссылаться на системный `.AppleSystemUIFont` через `FontFamily="San Francisco"` (Avalonia резолвит через Skia/CoreText на macOS).
- Иерархия размеров (примерно как в HIG):
  - Заголовок окна/секции: 20–22px, Semibold
  - Заголовок задачи: 15px, Medium
  - Обычный текст/описание: 13px, Regular
  - Вторичный текст (даты, теги): 11–12px, Regular, приглушённый цвет

## 3. Цвета — палитра Syncora со скриншота

Вместо стандартной macOS-синей палитры используй фирменный **мягкий lavender/purple** стиль Syncora со скриншота: светлый почти белый фон, очень светлые лавандовые поверхности, насыщенный мягкий фиолетовый акцент и приглушённый фиолетово-серый текст.

Не размазывай цвета по `.axaml`: вынеси их в semantic-ресурсы (`Styles/Colors.axaml`) и используй ключи ресурсов во Views.

Рекомендуемая базовая палитра, подобранная по визуальному языку скриншота:

```xml
<Style.Resources>
    <!-- Main surfaces -->
    <SolidColorBrush x:Key="WindowBackground" Color="#F7F6FE" />
    <SolidColorBrush x:Key="SurfaceBackground" Color="#FFFFFF" />
    <SolidColorBrush x:Key="SidebarBackground" Color="#F0EEFC" />
    <SolidColorBrush x:Key="SurfaceSecondary" Color="#F4F2FC" />

    <!-- Text -->
    <SolidColorBrush x:Key="LabelPrimary" Color="#30264A" />
    <SolidColorBrush x:Key="LabelSecondary" Color="#756D88" />
    <SolidColorBrush x:Key="LabelMuted" Color="#9B94AB" />

    <!-- Syncora purple accent -->
    <SolidColorBrush x:Key="AccentPurple" Color="#8B70FB" />
    <SolidColorBrush x:Key="AccentPurpleDark" Color="#7656D6" />
    <SolidColorBrush x:Key="AccentPurpleLight" Color="#EAE4FF" />
    <SolidColorBrush x:Key="AccentPurpleTint" Color="#F3EFFF" />

    <!-- Borders / separators -->
    <SolidColorBrush x:Key="BorderSoft" Color="#E7E3F2" />

    <!-- Soft category colors from the reference UI -->
    <SolidColorBrush x:Key="PastelRed" Color="#F7B8C7" />
    <SolidColorBrush x:Key="PastelGreen" Color="#BFE8D0" />
    <SolidColorBrush x:Key="PastelBlue" Color="#B9DDF5" />
    <SolidColorBrush x:Key="PastelYellow" Color="#F4DFAE" />
</Style.Resources>
```

### Правила использования палитры
- Главный акцент — **фиолетовый `#8B70FB`**, а не macOS Blue. Используй его для primary-кнопок, активного пункта sidebar, selected-состояний, ссылок и ключевых интерактивных элементов.
- `AccentPurpleLight`/`AccentPurpleTint` используй как мягкую подложку selected/hover-состояний вместо сплошной яркой заливки.
- Основной фон должен оставаться очень светлым и слегка лавандовым; карточки и рабочие области — преимущественно белые.
- Не используй одновременно несколько насыщенных акцентных цветов. Категории — только приглушённые pastel-оттенки.
- Избегай чистого `#000000` для текста и чистого `#FFFFFF` для всех поверхностей подряд: интерфейс должен выглядеть мягким, воздушным и близким к референсу.
- Для hover можно использовать полупрозрачный фиолетовый слой (`AccentPurple` с низкой opacity), для selected — `AccentPurpleLight` или `AccentPurpleTint`.
- Если нужна тёмная тема, сохраняй ту же фиолетовую идентичность: не заменяй акцент на синий.

## 4. Формы, скругления, тени

- Радиус скругления карточек задач и кнопок: 8–10px (не 2–4px, как в "плоском" Fluent/Material).
- Панели/попапы: скругление 12–14px, лёгкая тень (`BoxShadow="0 2 12 0 #22000000"`), без резких границ.
- Разделители — не сплошные линии на всю ширину, а тонкие (`1px`, `#1A000000`) и часто вообще заменяются отступом/фоном вместо линии.
- Чекбоксы задач — круглые (как в Reminders), а не квадратные.

## 5. Sidebar и списки

- Пункты sidebar: иконка + текст, высота строки ~28–32px, при hover — лёгкая подсветка (`#00000008`), при выборе — акцентный полупрозрачный фон (`#0A84FF1A`) с текстом акцентного цвета, без сплошной заливки на всю ширину до края — с отступом 6–8px и скруглением 6px.
- Список задач — без "зебры" (чередующихся цветов строк), разделение через отступы и тонкие разделители.

## 6. Blur / vibrancy

Для эффекта "полупрозрачного стекла" под sidebar (как NSVisualEffectView):

```csharp
Background = Brushes.Transparent;
TransparencyLevelHint = new[] { WindowTransparencyLevel.AcrylicBlur };
```
и на самой панели — полупрозрачный `SolidColorBrush` поверх (`Opacity="0.7"`), чтобы был виден блюр фона окна. Используй экономно — весь интерфейс "стеклянным" не делай, это быстро выглядит дёшево и мешает читаемости.

## 7. Иконки

- Стиль — line-icons с толщиной обводки ~1.5px, в духе SF Symbols (не заливные "материальные" иконки).
- Не копируй сами SF Symbols как ассеты в кроссплатформенный проект — они лицензированы под экосистему Apple. Используй open-source аналоги схожего стиля (например, Lucide Icons) — визуально они близки к SF Symbols по духу (line-style, единая толщина линий).

## 8. Нативная интеграция меню (только для macOS-сборки)

Чтобы меню приложения было в системной строке меню сверху экрана (а не внутри окна), используй `NativeMenu`:

```xml
<NativeMenu.Menu>
    <NativeMenu>
        <NativeMenuItem Header="Файл">
            <NativeMenu>
                <NativeMenuItem Header="Новая задача" Gesture="Cmd+N" Command="{Binding NewTaskCommand}" />
            </NativeMenu>
        </NativeMenuItem>
    </NativeMenu>
</NativeMenu.Menu>
```
На Windows/Linux `NativeMenu` игнорируется автоматически — не нужно городить условную компиляцию, просто дублируй важные команды обычным `Menu`/тулбаром для этих платформ.

## 9. Чего избегать

- Резких прямоугольных карточек без скруглений — сразу выдаёт "не macOS".
- Множества ярких, конкурирующих акцентных цветов одновременно.
- Тяжёлых теней и градиентов "под Windows 7 Aero".
- Плотной, "инженерной" компоновки с минимальными отступами — в macOS много воздуха (padding 12–16px как база).
