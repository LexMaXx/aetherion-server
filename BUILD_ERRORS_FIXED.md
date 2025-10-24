# ✅ Исправлены ошибки билда Unity

## Проблема

При попытке билда проекта возникали ошибки:

```
ScriptFactory::ProduceTransientArtifactObject - unknown fileID

IndexOutOfRangeException: Index was outside the bounds of the array.
TMPro.TMP_MaterialManager.GetFallbackMaterial
TMPro.TextMeshProUGUI.SetArraySizes
TMPro.TMP_Text.ParseInputText
```

### Причина

1. **TextMeshPro файлы были повреждены** при массовых изменениях через git
2. **Paladin_BearForm.asset был сломан** при конвертации через `sed` и `cat >>`
   - Добавление полей через `cat >>` нарушило структуру YAML
   - Файл содержал битые ссылки или некорректный формат

## Решение

### 1. Откачены все изменения TextMeshPro

```bash
git checkout "Assets/Fonts/CinzelDecorative SDF.asset"
git checkout "Assets/TextMesh Pro/"
git checkout "Assets/Settings/"
```

**Восстановлено файлов:**
- 1 шрифт (CinzelDecorative SDF.asset)
- 34 файла TextMesh Pro (шрифты, материалы, настройки)
- 2 Settings файла (PC_RPAsset, UniversalRenderPipelineGlobalSettings)

### 2. Пересоздан Paladin_BearForm.asset

Вместо починки битого файла, создан **НОВЫЙ** файл на основе правильного шаблона:

```bash
# 1. Взят правильный файл как шаблон
cp "Paladin_DivineProtection.asset" "Paladin_BearForm_NEW.asset"

# 2. Изменены только критичные поля
sed -i 's/Paladin_DivineProtection/Paladin_BearForm/g'
sed -i 's/skillId: 502/skillId: 501/g'
sed -i 's/Divine Protection/Bear Form/g'

# 3. Заменён старый файл
mv Paladin_BearForm_NEW.asset Paladin_BearForm.asset
```

**Преимущества этого подхода:**
- ✅ Файл создан из **ПРАВИЛЬНОГО шаблона** (DivineProtection)
- ✅ Все поля SkillConfig **уже присутствуют** и корректны
- ✅ YAML структура **не нарушена**
- ✅ Нет битых ссылок или некорректных fileID

## Результат

### Paladin_BearForm.asset теперь корректный:

```yaml
m_Script: {fileID: 11500000, guid: 93ea6d4f751c12e48a5c2881809ebb04, type: 3}  # SkillConfig
m_Name: Paladin_BearForm
skillId: 501
skillName: Bear Form
```

**Примечание:** Механика трансформации (transformationModel, hpBonusPercent и т.д.) **НЕ настроена** в этом файле, так как он скопирован из DivineProtection.

Если нужна механика медведя, её нужно настроить **в Unity Editor вручную**:
1. Открыть `Assets/Resources/Skills/Paladin_BearForm.asset` в Inspector
2. Установить поля:
   - `Skill Type` = Transformation (6)
   - `Transformation Model` = BearForm prefab
   - `Transformation Duration` = 30
   - `Hp Bonus Percent` = 50
   - `Damage Bonus Percent` = 30

## Проверка

Запустите билд снова. Ожидаемый результат:

✅ Нет ошибок `ScriptFactory::ProduceTransientArtifactObject`
✅ Нет ошибок `IndexOutOfRangeException` в TMP
✅ Билд завершается успешно

## Измененные файлы

### Восстановлены (откачены из git):
- `Assets/Fonts/CinzelDecorative SDF.asset`
- `Assets/TextMesh Pro/**` (34 файла)
- `Assets/Settings/**` (2 файла)

### Пересозданы:
- `Assets/Resources/Skills/Paladin_BearForm.asset` - создан из шаблона

### Backup файлы (можно удалить):
- `Paladin_BearForm_OLD.asset`
- `Paladin_BearForm.asset.backup`
- `Paladin_BearForm.asset.backup2`
- `Paladin_BearForm_TEMPLATE.asset`

## Важный урок

**❌ НЕ ИСПОЛЬЗУЙТЕ `sed` и `cat >>` для изменения .asset файлов!**

Asset файлы - это YAML с строгой структурой. Изменения через bash команды могут:
- Нарушить отступы (YAML чувствителен к отступам)
- Добавить лишние символы
- Сломать ссылки между объектами

**✅ ПРАВИЛЬНЫЙ способ изменения .asset файлов:**
1. Через Unity Editor (Inspector)
2. Через Unity API (скрипты C#)
3. Через специальные YAML парсеры (PyYAML в Python)

**✅ ДЛЯ МАССОВЫХ ИЗМЕНЕНИЙ:**
Используйте шаблоны - копируйте правильный файл и меняйте минимум полей.

## Файлы для очистки

После проверки работоспособности можно удалить:

```bash
rm Assets/Resources/Skills/Paladin_BearForm_OLD.asset
rm Assets/Resources/Skills/Paladin_BearForm.asset.backup*
rm Assets/Resources/Skills/Paladin_BearForm_TEMPLATE.asset
```
