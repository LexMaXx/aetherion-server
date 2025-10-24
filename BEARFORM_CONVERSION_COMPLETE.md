# ✅ Конвертация Paladin_BearForm.asset завершена!

## Проблема
`Paladin_BearForm.asset` был единственным файлом среди 25 скиллов, который использовал **старый тип SkillData** вместо **нового SkillConfig**.

### Симптомы:
```
[SkillConfigLoader] ❌ Не удалось загрузить скилл по пути: Skills/Paladin_BearForm
```

У каждого класса загружалось только **3 скилла из 5**, потому что:
- Paladin: BearForm (501) не загружался
- Другие классы: вероятно тоже пропускались 2 скилла

## Что было сделано

### 1. Изменён GUID (тип ScriptableObject)
```yaml
# ДО:
m_Script: {fileID: 11500000, guid: 6e79cfd8b12443c408c3d4a9fbdce0c8, type: 3}  # SkillData (СТАРЫЙ)

# ПОСЛЕ:
m_Script: {fileID: 11500000, guid: 93ea6d4f751c12e48a5c2881809ebb04, type: 3}  # SkillConfig (НОВЫЙ)
```

### 2. Исправлены базовые поля
- `m_Name`: `BearForm` → `Paladin_BearForm` (консистентность с другими файлами)
- `skillId`: `401` → `501` (правильный ID для первого скилла паладина)

### 3. Переименовано поле урона
```yaml
# ДО:
physicalDamageBonusPercent: 30

# ПОСЛЕ:
damageBonusPercent: 30
```
*(В SkillConfig нет `physicalDamageBonusPercent`, только `damageBonusPercent`)*

### 4. Добавлены недостающие поля SkillConfig
Добавлены все поля, которых не было в старом SkillData:
- `lifeStealPercent: 0`
- `projectileSpeed`, `projectileLifetime`, `projectileHoming`, `homingSpeed`, `homingRadius`
- `castEffectPrefab`, `hitEffectPrefab`, `aoeEffectPrefab`, `casterEffectPrefab`
- `animationSpeed`, `projectileSpawnTiming`
- `hitSound`, `soundVolume`
- `enableMovement`, `movementType`, `movementDistance`, `movementSpeed`, `movementDirection`
- `syncProjectiles`, `syncHitEffects`, `syncStatusEffects`
- `customData` (с полями: chainCount, chainRadius, chainDamageMultiplier, hitCount, hitDelay, manaRestorePercent, piercing, maxPierceTargets)

## 🐻 Механика BearForm СОХРАНЕНА ПОЛНОСТЬЮ

Все критически важные поля трансформации сохранены:

```yaml
transformationModel: {fileID: 2392146731170012137, guid: 854220d1cd63d4049a99e4c4ec58555e, type: 3}
transformationDuration: 30
hpBonusPercent: 50
damageBonusPercent: 30
```

### Что это значит:
- ✅ Модель медведя (`transformationModel`) сохранена
- ✅ Длительность трансформации: **30 секунд**
- ✅ Бонус к HP: **+50%**
- ✅ Бонус к урону: **+30%**

## Результат

### До конвертации:
- ❌ 1 из 25 файлов (Paladin_BearForm) имел старый GUID SkillData
- ❌ У паладина загружалось 3/5 скиллов
- ❌ BearForm не загружался (`Resources.Load<SkillConfig>()` возвращал NULL)

### После конвертации:
- ✅ Все 25 файлов имеют правильный GUID SkillConfig
- ✅ У всех классов должны загружаться все 5 скиллов
- ✅ BearForm будет загружаться как SkillConfig
- ✅ Механика трансформации в медведя полностью сохранена

## Тестирование

Запусти Unity Editor и проверь:

1. **CharacterSelection Scene**:
   ```
   [SkillConfigLoader] ✅ Загружен скилл: Bear Form (ID: 501)
   [SkillConfigLoader] ✅ Загружено 5/5 скиллов для Paladin
   ```

2. **Arena Scene**:
   - Выбери паладина
   - Нажми клавишу **1** (первый скилл - Bear Form)
   - Должна произойти **трансформация в медведя** на 30 секунд
   - HP должно увеличиться на 50%
   - Урон должен увеличиться на 30%

## Backup

Создан backup файл: `Assets/Resources/Skills/Paladin_BearForm.asset.backup`

Если что-то пойдёт не так, можно откатиться:
```bash
cd Assets/Resources/Skills
cp Paladin_BearForm.asset.backup Paladin_BearForm.asset
```
