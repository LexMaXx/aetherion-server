# Исправление: Маг не получает опыт и золото

## Проблема
**Маг НЕ получает опыт и золото** при убийстве врагов, в то время как Лучник и Воин получают нормально.

## Корень проблемы

**MageModel.prefab НЕ ИМЕЕТ компонента CharacterStats!**

### Почему это критично?

1. **BattleSceneManager.SetupLevelingSystem()** (строка 956-1077) ищет CharacterStats на префабе
2. Если CharacterStats НЕ найден → **LevelingSystem НЕ ДОБАВЛЯЕТСЯ** (строка 992-995)
3. Без LevelingSystem → **опыт не добавляется**
4. Без CharacterStats → **золото не добавляется** (MongoInventoryManager требует CharacterStats)

### Сравнение префабов:

```
✅ ArcherModel.prefab   → ИМЕЕТ CharacterStats → работает
✅ WarriorModel.prefab  → ИМЕЕТ CharacterStats → работает
❌ MageModel.prefab     → НЕ ИМЕЕТ CharacterStats → НЕ работает!
```

## Решение

### Вариант 1: Через Unity Editor Tool (Автоматически) ⭐ РЕКОМЕНДУЕТСЯ

1. Откройте Unity Editor
2. В меню выберите: **Tools → Aetherion → Fix Mage Prefab (Add CharacterStats)**
3. Нажмите **"Да, исправить"**
4. Скрипт автоматически:
   - Добавит компонент `CharacterStats` к MageModel.prefab
   - Установит MageStats preset
   - Установит StatsFormulas
   - Сохранит изменения

### Вариант 2: Вручную через Inspector

1. Откройте Unity Editor
2. Найдите `Assets/Resources/Characters/MageModel.prefab`
3. Выберите **ROOT объект** префаба (MageModel)
4. В Inspector нажмите **Add Component**
5. Найдите и добавьте **CharacterStats**
6. В компоненте CharacterStats установите:
   - **Class Preset:** `Assets/Resources/ClassStats/MageStats.asset`
   - **Formulas:** `Assets/Resources/StatsFormulas.asset`
7. Сохраните префаб

## Как это работает?

### Последовательность инициализации:

```
1. BattleSceneManager.SetupCharacter()
   ↓
2. SetupLevelingSystem() (строка 956)
   ↓
3. Ищет CharacterStats на префабе (строка 975-988)
   ↓
4. Если НЕ найден → ERROR и RETURN (строка 992-995)
   ❌ LevelingSystem НЕ добавляется!
   ↓
5. Если найден → AddComponent<LevelingSystem>() (строка 1027)
   ✅ Система прокачки работает!
```

### Логи при ошибке:

```
[BattleSceneManager] === SetupLevelingSystem для MageModel ===
[BattleSceneManager] CharacterStats не на Model, ищем в родительском объекте...
[BattleSceneManager] CharacterStats не на родителе, ищем в детях...
[BattleSceneManager] ❌ CharacterStats не найден нигде! LevelingSystem требует CharacterStats.
[BattleSceneManager] ❌ Проверьте префаб MagePlayer - должен иметь CharacterStats компонент!
```

### Логи после исправления:

```
[BattleSceneManager] === SetupLevelingSystem для MageModel ===
[BattleSceneManager] ✓ CharacterStats найден на: MageModel
[BattleSceneManager] ⭐ ДОБАВЛЕН LevelingSystem на MageModel
[BattleSceneManager] ✅ LevelingSystem настроен (Level: 1, MaxLevel: 20)
[EnemyRewardSystem] ✅ ОПЫТ ВЫДАН! +30 XP игроку MagePlayer
[EnemyRewardSystem] ✅ Выдано 15 золота игроку MagePlayer
```

## Проверка после исправления

1. Откройте Unity Editor
2. Запустите BattleScene с магом
3. Убейте любого врага
4. Откройте Console (Ctrl+Shift+C)
5. Должны появиться логи:

```
[EnemyRewardSystem] 🎯 Попытка выдать опыт игроку MagePlayer...
[EnemyRewardSystem] ✅ LevelingSystem найден через GetComponent
[EnemyRewardSystem] ✅ ОПЫТ ВЫДАН! +30 XP игроку MagePlayer
[EnemyRewardSystem] ✅ Выдано 15 золота игроку MagePlayer
```

## Дополнительно

### Если хотите проверить другие классы:

Запустите в PowerShell из корня проекта:

```powershell
# Проверить все префабы персонажей на наличие CharacterStats
$guid = "57c0fe220acbf3a4db825a37d02bfa33"  # GUID CharacterStats.cs
Get-ChildItem "Assets\Resources\Characters\*Model.prefab" | ForEach-Object {
    $count = (Select-String -Path $_.FullName -Pattern $guid).Count
    if ($count -gt 0) {
        Write-Host "✅ $($_.Name) - CharacterStats найден"
    } else {
        Write-Host "❌ $($_.Name) - CharacterStats ОТСУТСТВУЕТ!"
    }
}
```

### Если проблема повторится:

1. Проверьте что изменения в префабе сохранились
2. Перезапустите Unity Editor
3. Проверьте Console на наличие логов от BattleSceneManager

## Связанные файлы

- [BattleSceneManager.cs:956-1077](Assets/Scripts/Battle/BattleSceneManager.cs) - SetupLevelingSystem()
- [CharacterStats.cs](Assets/Scripts/Stats/CharacterStats.cs) - система характеристик
- [LevelingSystem.cs](Assets/Scripts/Stats/LevelingSystem.cs) - система прокачки
- [FixMagePrefab.cs](Assets/Scripts/Editor/FixMagePrefab.cs) - Editor инструмент исправления
- [MageModel.prefab](Assets/Resources/Characters/MageModel.prefab) - префаб мага
