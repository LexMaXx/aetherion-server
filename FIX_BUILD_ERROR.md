# FIX BUILD ERROR - TextMeshPro

## Проблема:
```
IndexOutOfRangeException: Index was outside the bounds of the array.
TMPro.TMP_MaterialManager.GetFallbackMaterial
```

## Причина:
Файл "LiberationSans SDF - Fallback.asset" повреждён или несовместим.

## РЕШЕНИЕ 1 (БЫСТРОЕ):

### В Unity Editor:
1. Window → TextMeshPro → Import TMP Essential Resources
2. Нажми "Import"
3. Дождись импорта
4. Build снова

## РЕШЕНИЕ 2 (ЕСЛИ НЕ ПОМОГЛО):

### Найди проблемный UI элемент:
1. В Unity: Edit → Project Settings → Player
2. Во вкладке "Other Settings" → отметь "Development Build"
3. Build снова
4. Посмотри в каком UI элементе ошибка

### Замени шрифт:
1. Найди все TextMeshPro компоненты использующие LiberationSans
2. Hierarchy → Поиск "t:TextMeshProUGUI"
3. Смени Font Asset на другой (например InterBold)

## РЕШЕНИЕ 3 (РАДИКАЛЬНОЕ):

### Удали и пересоздай fallback font:
```
1. Удали: Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF - Fallback.asset
2. Window → TextMeshPro → Font Asset Creator
3. Source Font File: LiberationSans.ttf
4. Generate Font Atlas
5. Save as "LiberationSans SDF - Fallback"
```

## ВРЕМЯ: 2 минуты
