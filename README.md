# Шаурма Магнат: Путь к миллиарду

Браузерная игра-симулятор шаурмичной для платформы Яндекс Игры (Unity → WebGL, ПК и телефон).
Студия Valour Games. Автор: Изюмов Артём, ИБ 125.

## Быстрый запуск

1. Установить Unity Hub и Unity 6 (проект собирался на **6000.6.3f1**, подходит любая 6000.x)
   с модулем **WebGL Build Support**.
2. Клонировать репозиторий и открыть папку в Unity Hub → *Add project from disk*.
   Зависимости (только `com.unity.ugui` и встроенные модули) Unity скачает сам по `Packages/manifest.json`.
3. В меню редактора: **Valour Games → Собрать игру** — создаётся сцена `Assets/_Game/Scenes/Main.unity`.
4. Нажать **Play** (окно Game: 1920×1080 или 16:9). В редакторе SDK нет: реклама и покупки
   выдаются сразу, сохранение — локальное.

### Сборка для Яндекс Игр

1. **Valour Games → Применить настройки WebGL** (шаблон `YandexGames`, Brotli + decompression fallback).
2. **Valour Games → Сделать WebGL-билд** → папка `Builds/WebGL`.
3. Запаковать *содержимое* папки (`index.html` в корне + `Build/`) в zip и загрузить в черновик консоли.
   Что настроить в консоли — см. `Assets/_Game/README.txt`, раздел «Что сделать в консоли».

## Структура

```
Assets/
  _Game/
    Scripts/
      Data/       баланс (GameData), профиль и сохранение (SaveData, SaveSystem), мета (Meta), сюжет
      Gameplay/   рабочий день: DayRun (partial: Kitchen, Events, Tutor, Panels, Banner, Seats), Customer
      Core/       Game (partial: экраны меню, карты, магазина, пасса, сотрудников, лидерборда), загрузка
      Platform/   Platform + YandexBridge (SDK Яндекс Игр), Payments (покупки)
      UI/         UIKit, Icons, Juice, Scenery, ShawarmaView … — интерфейс строится из кода
      Audio/      Sfx — звуки генерируются кодом
    Plugins/WebGL/YandexGames.jslib   JS-мост C# ↔ SDK
    Resources/Fonts/                  шрифт с кириллицей (Liberation Sans, SIL OFL)
    Editor/ValourGamesMenu.cs         меню сборки (в игру не попадает)
  WebGLTemplates/YandexGames/         страница-шаблон: /sdk.js, YaGames.init()
Packages/manifest.json                зависимости
ProjectSettings/ProjectVersion.txt    версия Unity
tools/lint.py                         проверка стиля
.githooks/pre-commit                  запуск проверки перед коммитом
```

Слои и правила зависимостей описаны в разделе 3 «Архитектура»: Представление (UI) → Логика (Core, Gameplay)
→ Данные (Data); о SDK знает только слой Platform.

## Правила ведения репозитория

**Ветки.** `main` — всегда собираемая версия. Каждая версия или задача — отдельная ветка от `main`:
`feature/vX.YY-кратко` — новая функциональность, `fix/vX.YY-кратко` — исправления,
`chore/…` — настройка проекта, `refactor/…` — переработка без изменения поведения.
Ветка вливается в `main` через merge `--no-ff`, версия помечается тегом `vX.YY`.

**Коммиты** — по Conventional Commits, описание по-русски:
`тип(область): что сделано`, где тип — `feat`, `fix`, `refactor`, `style`, `docs`, `chore`,
область — `data`, `gameplay`, `core`, `platform`, `ui`, `repo`. В теле — список изменений.

**Задачи.** Задача формулируется как «что видит игрок / какая ошибка» + критерий готовности;
номер версии в имени ветки связывает задачу, ветку и запись в `CHANGELOG.md`.

**Стиль кода** задан в `.editorconfig` (UTF-8, LF, 4 пробела, скобки Allman, PascalCase для типов
и методов, `_camelCase` для приватных полей). Проверка:

```
python3 tools/lint.py              # весь Assets/
git config core.hooksPath .githooks  # включить проверку перед каждым коммитом
```

IDE (Visual Studio с Tools for Unity или Rider) применяет `.editorconfig` при форматировании.

## История

Изменения по версиям — в `CHANGELOG.md`. Версии v0.3–v0.9 велись до создания репозитория
и сохранились только в описании изменений; в репозитории история начинается с v0.10.
