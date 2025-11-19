# Список анимаций Mixamo для MMO персонажа

## Основные движения (необходимо скачать):

### 1. **Idle (Стойка)**
- ✅ **Standing Idle** - базовая стойка (уже есть)
- ⬇ **Idle** - обычная idle анимация
- ⬇ **Breathing Idle** - idle с дыханием (более живая)

### 2. **Ходьба (Walk) - ОБЯЗАТЕЛЬНО**
- ⬇ **Walking** - обычная ходьба вперед
- ⬇ **Walking Backwards** - ходьба назад
- ⬇ **Left Strafe Walking** - ходьба влево
- ⬇ **Right Strafe Walking** - ходьба вправо

### 3. **Бег (Run)**
- ✅ **Sword And Shield Run** - бег вперед (уже есть)
- ✅ **Standing Run Back** - бег назад (уже есть)
- ✅ **Left Strafe** - бег влево (уже есть)
- ✅ **Right Strafe** - бег вправо (уже есть)

### 4. **Боевые стойки**
- ✅ **Sword And Shield Idle** - боевая стойка воина (уже есть)
- ✅ **2hand Idle** - стойка двуручного оружия (уже есть)
- ✅ **Standing Draw Arrow** - стойка лучника (уже есть)
- ✅ **Idle_TwoHanded** - стойка мага (уже есть)

### 5. **Дополнительно (для будущего)**
- ⬇ **Jump** - прыжок
- ⬇ **Falling Idle** - падение
- ⬇ **Hard Landing** - приземление
- ⬇ **Roll** - перекат
- ⬇ **Death** - смерть (несколько вариантов)

## Настройки скачивания с Mixamo:

1. **Character**: Используй того же персонажа что и раньше (или Y Bot)
2. **In Place**: ✅ **ВКЛЮЧЕНО** (Without Root Motion)
3. **Frame Rate**: 30 FPS
4. **Format**: FBX for Unity (.fbx)
5. **Skin**: With Skin

## Куда сохранять:
`Assets/Animations/` - все FBX файлы в эту папку

## После скачивания:
1. Запусти **Tools > Auto Fix All Animations** - исправит Root Motion
2. Запусти **Tools > Setup Character Animations** - настроит Animator Controllers
3. Добавь новые анимации в Blend Tree (используй скрипт ниже)
