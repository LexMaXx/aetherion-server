# Debug Console Setup Guide

## Что это?
In-game debug console для просмотра логов в .exe билдах игры. Позволяет видеть все Debug.Log сообщения прямо в игре без Unity Editor.

## Быстрая установка

### Способ 1: Через меню Unity (рекомендуется)
1. Откройте сцену, где нужен debug console (ArenaScene, GameScene и т.д.)
2. В меню Unity выберите: `Tools > Debug > Add Debug Console to Scene`
3. Готово! DebugConsole добавлен в сцену

### Способ 2: Вручную
1. Создайте пустой GameObject в сцене
2. Добавьте компонент `DebugConsole`
3. Готово!

### Способ 3: Создать Prefab (для использования в нескольких сценах)
1. В меню Unity: `Tools > Debug > Create DebugConsole Prefab`
2. Prefab будет создан в `Assets/Prefabs/Debug/DebugConsole.prefab`
3. Перетащите prefab в любую сцену

## Использование

### Основные возможности
- **F12** - показать/скрыть консоль
- Консоль автоматически захватывает все Debug.Log(), Debug.LogWarning(), Debug.LogError()
- Работает в Unity Editor и в .exe билдах
- Сохраняет до 500 последних логов
- Цветовая индикация:
  - Белый - обычные логи
  - Желтый - предупреждения
  - Красный - ошибки

### Кнопки в консоли
- **Clear Logs** - очистить все логи
- **Logs** - показать/скрыть обычные логи
- **Warnings** - показать/скрыть предупреждения
- **Errors** - показать/скрыть ошибки

### Настройки в Inspector

```
Toggle Key: F12 (клавиша для открытия/закрытия)
Max Log Count: 500 (максимум логов в памяти)
Show On Start: false (показывать при запуске)
Capture Stack Traces: true (захватывать stack traces для ошибок)

Show Logs: true (показывать Debug.Log)
Show Warnings: true (показывать Debug.LogWarning)
Show Errors: true (показывать Debug.LogError)
Filter Tag: "" (фильтр по тегу, например "[NetworkSync]")
```

## Фильтрация логов

### По типу
Отключите ненужные типы логов через Inspector:
- `Show Logs = false` - скрыть обычные логи
- `Show Warnings = false` - скрыть предупреждения
- `Show Errors = false` - скрыть ошибки

### По тегу
Установите Filter Tag для показа только определенных логов:
```csharp
// В Inspector установите Filter Tag = "[NetworkSync]"
// Будут показаны только логи содержащие [NetworkSync]
```

Или программно:
```csharp
FindObjectOfType<DebugConsole>().SetFilterTag("[NetworkSync]");
```

## Для разработчиков

### Использование в коде
```csharp
// Обычные логи - будут видны в консоли
Debug.Log("[NetworkSync] Player spawned");
Debug.LogWarning("[NetworkSync] Connection timeout");
Debug.LogError("[NetworkSync] Failed to sync");

// Очистить логи программно
FindObjectOfType<DebugConsole>().ClearLogs();

// Показать/скрыть консоль программно
FindObjectOfType<DebugConsole>().SetVisibility(true);

// Установить фильтр
FindObjectOfType<DebugConsole>().SetFilterTag("[NetworkSync]");
```

### Рекомендации для мультиплеера
Добавьте DebugConsole в следующие сцены:
- ✅ **ArenaScene** - для отладки мультиплеера
- ✅ **GameScene** - для отладки геймплея
- ✅ **CharacterSelectionScene** - для отладки выбора персонажа
- ⚠️ **LoginScene** - опционально

### Тестирование в .exe билде
1. Соберите игру: `File > Build Settings > Build`
2. Запустите .exe файл
3. Нажмите **F12** для открытия консоли
4. Все Debug.Log сообщения будут видны!

## Отладка проблем с мультиплеером

### Пример: Проверка почему игроки не видят друг друга
1. Запустите 2 клиента (2 .exe)
2. Войдите в одну комнату
3. Нажмите F12 на обоих клиентах
4. Ищите логи:
   - `[NetworkSync]` - синхронизация игроков
   - `[SimpleWS]` - WebSocket соединение
   - `[RoomManager]` - управление комнатами

### Фильтрация для мультиплеера
Установите Filter Tag для просмотра только сетевых логов:
- `[NetworkSync]` - синхронизация
- `[SimpleWS]` - WebSocket
- `[RoomManager]` - комнаты
- `[ArenaManager]` - арена

## Production

### Отключение для релиза
Если не хотите консоль в финальной версии:

**Вариант 1**: Удалите DebugConsole из сцены
```
Tools > Debug > Remove Debug Console from Scene
```

**Вариант 2**: Отключите GameObject с DebugConsole

**Вариант 3**: Добавьте условную компиляцию:
```csharp
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    // DebugConsole активен только в Editor и Development билдах
#endif
```

### Рекомендации
- ✅ Оставьте консоль для **Development Build** (для тестирования)
- ❌ Отключите для **Release Build** (финальная версия для игроков)

## Troubleshooting

### Консоль не открывается
- Проверьте что GameObject с DebugConsole активен в иерархии
- Проверьте что клавиша не занята другим скриптом
- Попробуйте изменить Toggle Key на другую клавишу

### Логи не появляются
- Проверьте фильтры (Show Logs, Show Warnings, Show Errors)
- Проверьте Filter Tag (должен быть пустым или соответствовать вашим логам)
- Убедитесь что Max Log Count > 0

### UI не видно
- Проверьте что Canvas Sorting Order = 9999 (поверх всех UI)
- Проверьте что нет других Canvas с higher sorting order

## Технические детали

### Производительность
- Хранит максимум 500 логов в памяти (configurable)
- UI обновляется только когда консоль открыта
- Не влияет на производительность когда закрыта
- Используется StringBuilder для оптимизации

### Совместимость
- Unity 2022.3 или новее
- Работает на всех платформах (Windows, macOS, Linux, WebGL)
- Не требует дополнительных зависимостей
- Использует встроенный UI системы Unity

## Контакты
Если нужна помощь с настройкой или возникли проблемы - пишите в лог ошибки!
