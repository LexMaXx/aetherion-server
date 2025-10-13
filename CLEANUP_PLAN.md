# 🧹 План очистки проекта Aetherion

## 📊 Статистика:
- **Editor скриптов всего**: 52
- **Оставить**: 9 полезных
- **Удалить**: 43 устаревших
- **MD файлов**: 33
- **Оставить**: 3-5 актуальных
- **Удалить**: ~28 устаревших

---

## ✅ ОСТАВИТЬ - Editor скрипты (9 шт):

### Создание ресурсов:
```
Assets/Scripts/Editor/CreateWeaponDatabase.cs
Assets/Scripts/Editor/CreateAttackSettingsAsset.cs
Assets/Scripts/Editor/CreateProjectilePrefabs.cs
Assets/Scripts/Editor/CreateWeaponPrefabs.cs
Assets/Scripts/Editor/CreateHitEffectPrefab.cs
Assets/Scripts/Editor/CreateArcherQuiverPrefab.cs
```

### Полезные утилиты:
```
Assets/Scripts/Editor/PlayerAttackEditor.cs
Assets/Scripts/Editor/TextStyleMenuEditor.cs
Assets/Scripts/Editor/SaveWeaponTransformsInPlayMode.cs
Assets/Scripts/Editor/UpdateWeaponDatabase.cs
```

---

## ❌ УДАЛИТЬ - Editor скрипты (43 шт):

### Анимации - устаревшие фиксы (24 скрипта):
```
Assets/Scripts/Editor/AddAttackAnimations.cs
Assets/Scripts/Editor/AddAttackTagToAnimators.cs
Assets/Scripts/Editor/AddSlowRunAnimation.cs
Assets/Scripts/Editor/AutoFixAnimations.cs
Assets/Scripts/Editor/CheckAnimatorStates.cs
Assets/Scripts/Editor/CleanupBlendTrees.cs
Assets/Scripts/Editor/CompletelyFixAttackRootMotion.cs
Assets/Scripts/Editor/CopyWarriorAnimsToArcher.cs
Assets/Scripts/Editor/DiagnoseAnimations.cs
Assets/Scripts/Editor/FixAllAnimationsRootMotion.cs
Assets/Scripts/Editor/FixAnimationSettings.cs
Assets/Scripts/Editor/FixAttackAnimationsRootMotion.cs
Assets/Scripts/Editor/FixMovementBlendTree.cs
Assets/Scripts/Editor/FixPaladinRunAnimation.cs
Assets/Scripts/Editor/FixRunAnimationRootMotion.cs
Assets/Scripts/Editor/ForceReplaceArcherIdle.cs
Assets/Scripts/Editor/ListAllAnimations.cs
Assets/Scripts/Editor/PROFESSIONALFixAttackAnimations.cs
Assets/Scripts/Editor/QuickFixAnimations.cs
Assets/Scripts/Editor/ReplaceArcherBattleIdle.cs
Assets/Scripts/Editor/SetupAnimatorLayers.cs
Assets/Scripts/Editor/SetupCharacterAnimations.cs
Assets/Scripts/Editor/SyncAllAnimations.cs
Assets/Scripts/Editor/SyncBlendTreesFromMage.cs
```

### Модели - одноразовые фиксы (10 скриптов):
```
Assets/Scripts/Editor/AlignCharacterModelsInScene.cs
Assets/Scripts/Editor/AssignMaskToLayers.cs
Assets/Scripts/Editor/DiagnoseCharacterModels.cs
Assets/Scripts/Editor/FixArcherAvatar.cs
Assets/Scripts/Editor/FixArcherFBXImport.cs
Assets/Scripts/Editor/FixArcherModelPivot.cs
Assets/Scripts/Editor/FixArcherPrefabActive.cs
Assets/Scripts/Editor/RecreateArcherPrefab.cs
Assets/Scripts/Editor/SetupCharacterModels.cs
Assets/Scripts/Editor/UnifyCharacterColliders.cs
```

### Разное - устаревшее (9 скриптов):
```
Assets/Scripts/Editor/CopyWeaponSettingsFromCharacterSelection.cs
Assets/Scripts/Editor/CreateUpperBodyMask.cs
Assets/Scripts/Editor/DebugArenaCamera.cs
Assets/Scripts/Editor/DebugWeaponInArena.cs
Assets/Scripts/Editor/FixAllFonts.cs
Assets/Scripts/Editor/RemoveUpperBodyLayer.cs
Assets/Scripts/Editor/SetDefaultBattleStance.cs
Assets/Scripts/Editor/SetupActionPointsUI.cs
```

---

## ✅ ОСТАВИТЬ - Документация (5 файлов):

```
SETUP_INSTRUCTIONS.md          - главная инструкция
ACTION_POINTS_SETUP.md         - система AP
FOG_OF_WAR_SYSTEM.md          - туман войны
WEAPON_GLOW_ENHANCED.md       - эффекты оружия
СИСТЕМА_ТАРГЕТИРОВАНИЯ.md      - таргетинг
```

---

## ❌ УДАЛИТЬ - Документация (~28 файлов):

### Устаревшие инструкции по исправлениям:
```
ANIMATION_EVENTS_SUMMARY.md
ANIMATION_FIX_README.md
FIX_РОЗОВЫЕ_ПРЕФАБЫ.md
QUICKSTART_ACTION_POINTS.md
WEAPON_GLOW_EFFECT.md (старая версия)
WEAPON_SWITCHING_SYSTEM.md
АВТОМАТИЧЕСКИЙ_ТАЙМИНГ_АТАКИ.md
БЛОКИРОВКА_ДВИЖЕНИЯ_ВО_ВРЕМЯ_АТАКИ.md
БЫСТРАЯ_НАСТРОЙКА.md
БЫСТРАЯ_НАСТРОЙКА_СНАРЯДОВ.md
БЫСТРЫЙ_СТАРТ.md
ИЗМЕНЕНИЯ_СИСТЕМЫ_АТАКИ.md
ИНСТРУКЦИЯ_АТАКА_ВО_ВРЕМЯ_БЕГА.md
ИНСТРУКЦИЯ_ИСПРАВЛЕНИЕ_АНИМАЦИЙ.md
ИНСТРУКЦИЯ_НАСТРОЙКИ_АТАКИ.md
ИНСТРУКЦИЯ_ПЕРЕСОЗДАНИЕ_ПРЕФАБОВ.md
ИСПРАВЛЕНИЕ_ПРОМАХА_АТАКИ.md
ИСПРАВЛЕНИЕ_ЭФФЕКТОВ_СНАРЯДОВ.md
ИСПРАВЛЕНИЯ_СНАРЯДОВ.md
НАСТРОЙКА_ANIMATION_EVENTS.md
НАСТРОЙКА_СКОРОСТИ_АТАКИ_МАГА.md
НАСТРОЙКА_СКОРОСТИ_В_ANIMATOR.md
ПОЛНАЯ_БЛОКИРОВКА_ДВИЖЕНИЯ_ПРИ_АТАКЕ.md
РЕДАКТОР_НАСТРОЙКИ_АТАКИ.md
РЕШЕНИЕ_ВСЕХ_ПРОБЛЕМ_С_АНИМАЦИЯМИ.md
СИСТЕМА_СНАРЯДОВ_И_ДИСТАНЦИЙ.md
СПАВН_СНАРЯДОВ_ОТ_ОРУЖИЯ.md
УЛУЧШЕНИЕ_АТАКИ_ПОВОРОТ_К_ЦЕЛИ.md
```

---

## 📝 Команды для удаления:

### Удалить Editor скрипты (ВНИМАТЕЛЬНО!):
```bash
# 1. Создайте бэкап проекта перед удалением!
# 2. Затем выполните:

# Анимации (24 файла)
rm "Assets/Scripts/Editor/AddAttackAnimations.cs"
rm "Assets/Scripts/Editor/AddAttackTagToAnimators.cs"
rm "Assets/Scripts/Editor/AddSlowRunAnimation.cs"
rm "Assets/Scripts/Editor/AutoFixAnimations.cs"
rm "Assets/Scripts/Editor/CheckAnimatorStates.cs"
rm "Assets/Scripts/Editor/CleanupBlendTrees.cs"
rm "Assets/Scripts/Editor/CompletelyFixAttackRootMotion.cs"
rm "Assets/Scripts/Editor/CopyWarriorAnimsToArcher.cs"
rm "Assets/Scripts/Editor/DiagnoseAnimations.cs"
rm "Assets/Scripts/Editor/FixAllAnimationsRootMotion.cs"
rm "Assets/Scripts/Editor/FixAnimationSettings.cs"
rm "Assets/Scripts/Editor/FixAttackAnimationsRootMotion.cs"
rm "Assets/Scripts/Editor/FixMovementBlendTree.cs"
rm "Assets/Scripts/Editor/FixPaladinRunAnimation.cs"
rm "Assets/Scripts/Editor/FixRunAnimationRootMotion.cs"
rm "Assets/Scripts/Editor/ForceReplaceArcherIdle.cs"
rm "Assets/Scripts/Editor/ListAllAnimations.cs"
rm "Assets/Scripts/Editor/PROFESSIONALFixAttackAnimations.cs"
rm "Assets/Scripts/Editor/QuickFixAnimations.cs"
rm "Assets/Scripts/Editor/ReplaceArcherBattleIdle.cs"
rm "Assets/Scripts/Editor/SetupAnimatorLayers.cs"
rm "Assets/Scripts/Editor/SetupCharacterAnimations.cs"
rm "Assets/Scripts/Editor/SyncAllAnimations.cs"
rm "Assets/Scripts/Editor/SyncBlendTreesFromMage.cs"

# Модели (10 файлов)
rm "Assets/Scripts/Editor/AlignCharacterModelsInScene.cs"
rm "Assets/Scripts/Editor/AssignMaskToLayers.cs"
rm "Assets/Scripts/Editor/DiagnoseCharacterModels.cs"
rm "Assets/Scripts/Editor/FixArcherAvatar.cs"
rm "Assets/Scripts/Editor/FixArcherFBXImport.cs"
rm "Assets/Scripts/Editor/FixArcherModelPivot.cs"
rm "Assets/Scripts/Editor/FixArcherPrefabActive.cs"
rm "Assets/Scripts/Editor/RecreateArcherPrefab.cs"
rm "Assets/Scripts/Editor/SetupCharacterModels.cs"
rm "Assets/Scripts/Editor/UnifyCharacterColliders.cs"

# Разное (9 файлов)
rm "Assets/Scripts/Editor/CopyWeaponSettingsFromCharacterSelection.cs"
rm "Assets/Scripts/Editor/CreateUpperBodyMask.cs"
rm "Assets/Scripts/Editor/DebugArenaCamera.cs"
rm "Assets/Scripts/Editor/DebugWeaponInArena.cs"
rm "Assets/Scripts/Editor/FixAllFonts.cs"
rm "Assets/Scripts/Editor/RemoveUpperBodyLayer.cs"
rm "Assets/Scripts/Editor/SetDefaultBattleStance.cs"
rm "Assets/Scripts/Editor/SetupActionPointsUI.cs"
```

### Удалить .meta файлы тоже:
```bash
# Удалит все .meta файлы для удалённых скриптов
find Assets/Scripts/Editor/ -name "*.cs.meta" -type f | while read file; do
  base="${file%.meta}"
  if [ ! -f "$base" ]; then
    rm "$file"
  fi
done
```

### Удалить устаревшую документацию:
```bash
# Удаляем старые MD файлы
rm ANIMATION_EVENTS_SUMMARY.md
rm ANIMATION_FIX_README.md
rm FIX_РОЗОВЫЕ_ПРЕФАБЫ.md
rm QUICKSTART_ACTION_POINTS.md
rm WEAPON_GLOW_EFFECT.md
rm WEAPON_SWITCHING_SYSTEM.md
rm АВТОМАТИЧЕСКИЙ_ТАЙМИНГ_АТАКИ.md
rm БЛОКИРОВКА_ДВИЖЕНИЯ_ВО_ВРЕМЯ_АТАКИ.md
rm БЫСТРАЯ_НАСТРОЙКА.md
rm БЫСТРАЯ_НАСТРОЙКА_СНАРЯДОВ.md
rm БЫСТРЫЙ_СТАРТ.md
rm ИЗМЕНЕНИЯ_СИСТЕМЫ_АТАКИ.md
rm ИНСТРУКЦИЯ_АТАКА_ВО_ВРЕМЯ_БЕГА.md
rm ИНСТРУКЦИЯ_ИСПРАВЛЕНИЕ_АНИМАЦИЙ.md
rm ИНСТРУКЦИЯ_НАСТРОЙКИ_АТАКИ.md
rm ИНСТРУКЦИЯ_ПЕРЕСОЗДАНИЕ_ПРЕФАБОВ.md
rm ИСПРАВЛЕНИЕ_ПРОМАХА_АТАКИ.md
rm ИСПРАВЛЕНИЕ_ЭФФЕКТОВ_СНАРЯДОВ.md
rm ИСПРАВЛЕНИЯ_СНАРЯДОВ.md
rm НАСТРОЙКА_ANIMATION_EVENTS.md
rm НАСТРОЙКА_СКОРОСТИ_АТАКИ_МАГА.md
rm НАСТРОЙКА_СКОРОСТИ_В_ANIMATOR.md
rm ПОЛНАЯ_БЛОКИРОВКА_ДВИЖЕНИЯ_ПРИ_АТАКЕ.md
rm РЕДАКТОР_НАСТРОЙКИ_АТАКИ.md
rm РЕШЕНИЕ_ВСЕХ_ПРОБЛЕМ_С_АНИМАЦИЯМИ.md
rm СИСТЕМА_СНАРЯДОВ_И_ДИСТАНЦИЙ.md
rm СПАВН_СНАРЯДОВ_ОТ_ОРУЖИЯ.md
rm УЛУЧШЕНИЕ_АТАКИ_ПОВОРОТ_К_ЦЕЛИ.md
```

---

## ✅ Результат после очистки:

### Editor скрипты: 52 → 9 (сокращение на 83%)
### Документация: 33 → 5 (сокращение на 85%)

**Меню Tools будет чистым:**
```
Tools/
├── Create Weapon Database
├── Create Attack Settings Asset
├── Create Hit Effect Prefab
├── Update Weapon Database
└── Projectiles/
    ├── Create All Projectile Prefabs
    ├── 1. Create Arrow (Archer)
    ├── 2. Create Fireball (Mage)
    └── 3. Create Soul Shards (Rogue)
```

---

## ⚠️ ВАЖНО ПЕРЕД УДАЛЕНИЕМ:

1. **Создайте бэкап проекта!**
2. Закройте Unity перед удалением
3. Удалите файлы
4. Откройте Unity - он автоматически удалит .meta файлы
5. Проверьте что проект компилируется без ошибок

---

**Готово!** 🎉 Проект станет намного чище и понятнее.
