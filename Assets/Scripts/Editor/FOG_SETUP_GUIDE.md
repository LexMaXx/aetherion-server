# 🌫️ Полное руководство по настройке тумана в Unity (Aetherion)

## Типы тумана в проекте

В вашем проекте доступны **3 типа тумана**:

### 1. Unity Built-in Fog (Стандартный туман Unity)
Простой и производительный туман

### 2. TENKOKU Sky Fog (Расширенный туман)
Продвинутый туман с высотой, дистанцией и эффектами искажения

### 3. Fog of War (Туман войны)
Игровая механика для скрытия неисследованных областей

---

## 🎨 1. Unity Built-in Fog (Базовый туман)

### Где настраивать:
**Window → Rendering → Lighting → Environment → Fog**

### Основные параметры:

#### Включение тумана
```
☑ Fog (галочка)
```

#### Цвет тумана
```
Fog Color: RGB цвет
Примеры:
- Светлый утренний туман: #C8D5E0
- Густой серый туман: #808080
- Мрачный фиолетовый: #6A5A8C
- Золотой закат: #FFB870
```

#### Режимы тумана (Fog Mode):

**Linear (Линейный)**
- **Start**: Расстояние начала тумана (например: 10)
- **End**: Расстояние полного тумана (например: 100)
- **Использовать для**: Четкого контроля видимости

**Exponential (Экспоненциальный)**
- **Density**: Плотность 0.0-1.0 (рекомендую: 0.01-0.05)
- **Использовать для**: Реалистичного тумана

**Exponential Squared (Квадратичный)**
- **Density**: Плотность 0.0-1.0 (рекомендую: 0.005-0.03)
- **Использовать для**: Более густого реалистичного тумана

### Настройка через код:

```csharp
// Включить туман
RenderSettings.fog = true;

// Цвет тумана
RenderSettings.fogColor = new Color(0.5f, 0.5f, 0.5f);

// Линейный режим
RenderSettings.fogMode = FogMode.Linear;
RenderSettings.fogStartDistance = 10f;
RenderSettings.fogEndDistance = 100f;

// Экспоненциальный режим
RenderSettings.fogMode = FogMode.Exponential;
RenderSettings.fogDensity = 0.02f;

// Квадратичный режим
RenderSettings.fogMode = FogMode.ExponentialSquared;
RenderSettings.fogDensity = 0.01f;
```

---

## 🌤️ 2. TENKOKU Sky Fog (Продвинутый туман)

### Компонент: TenkokuSkyFog.cs

Этот туман работает как **Image Effect** на камере и предоставляет:
- Туман с учётом высоты (Height Fog)
- Туман горизонта
- Эффекты искажения воздуха (Heat Distortion)
- Интеграция с динамическим небом

### Установка:

#### Шаг 1: Добавить компонент на камеру
```
1. Выбрать Main Camera в иерархии
2. Add Component → Image Effects → Tenkoku → Tenkoku Fog
   или найти: TenkokuSkyFog
```

#### Шаг 2: Настроить параметры

### Основные параметры TENKOKU Fog:

#### Use Radial Distance
```
☑ Use Radial Distance
Включает расчёт тумана на основе радиального расстояния от камеры
Рекомендуется: ВКЛЮЧЕНО
```

#### Fog Horizon
```
☐ Fog Horizon
Добавляет туман на линии горизонта
Используйте для: океанских сцен, открытых пространств
```

#### Fog Skybox
```
☑ Fog Skybox
Применяет туман к skybox
Рекомендуется: ВКЛЮЧЕНО для реалистичности
```

#### Height (Высота тумана)
```
Height: 185.0
Определяет Y-координату верхней границы тумана
Настройте под ландшафт:
- Низкий туман (болота): 50-100
- Средний туман (равнины): 100-200
- Высокий туман (горы): 200-500
```

#### Height Density (Плотность по высоте)
```
Height Density: 0.00325 (Range: 0.00001 - 10.0)
Плотность тумана относительно высоты
Рекомендации:
- Лёгкая дымка: 0.001 - 0.005
- Умеренный туман: 0.005 - 0.01
- Густой туман: 0.01 - 0.05
- Очень густой: 0.05+
```

#### Fog Color
```
Fog Color: RGBA
Цвет тумана (работает с Unity Fog Color)
```

#### Heat Distortion (Искажение воздуха)

**Heat Speed**
```
Heat Spd: 4.0
Скорость эффекта искажения воздуха
```

**Heat Scale**
```
Heat Scale: 2.0
Масштаб эффекта искажения
```

**Heat Distance**
```
Heat Distance: 0.01
Интенсивность эффекта искажения
Используйте для: пустынь, горячих областей
```

### Настройка через код:

```csharp
using Tenkoku.Effects;

// Получить компонент
TenkokuSkyFog skyFog = Camera.main.GetComponent<TenkokuSkyFog>();

if (skyFog != null)
{
    // Основные настройки
    skyFog.useRadialDistance = true;
    skyFog.fogHorizon = false;
    skyFog.fogSkybox = true;

    // Высота и плотность
    skyFog.height = 185f;
    skyFog.heightDensity = 0.00325f;

    // Цвет
    skyFog.fogColor = new Color(0.8f, 0.8f, 0.85f, 1f);

    // Heat distortion
    skyFog.heatSpd = 4f;
    skyFog.heatScale = 2f;
    skyFog.heatDistance = 0.01f;
}
```

---

## 🎮 Рекомендуемые пресеты

### Пресет 1: Лёгкая утренняя дымка
```
Unity Fog:
- Fog: ON
- Mode: Exponential
- Density: 0.01
- Color: #C8D5E0 (светло-голубой)

TENKOKU Fog:
- Height: 150
- Height Density: 0.002
- Fog Skybox: ON
- Fog Horizon: OFF
```

### Пресет 2: Густой лесной туман
```
Unity Fog:
- Fog: ON
- Mode: Exponential Squared
- Density: 0.03
- Color: #8A9BA0 (серо-зелёный)

TENKOKU Fog:
- Height: 100
- Height Density: 0.008
- Fog Skybox: ON
- Fog Horizon: ON
```

### Пресет 3: Мистический фиолетовый туман
```
Unity Fog:
- Fog: ON
- Mode: Exponential
- Density: 0.025
- Color: #6A5A8C (фиолетовый)

TENKOKU Fog:
- Height: 200
- Height Density: 0.005
- Fog Skybox: ON
- Fog Horizon: ON
```

### Пресет 4: Пустынная жара
```
Unity Fog:
- Fog: ON
- Mode: Linear
- Start: 20
- End: 300
- Color: #FFE8C0 (песочный)

TENKOKU Fog:
- Height: 300
- Height Density: 0.001
- Heat Spd: 8.0
- Heat Scale: 4.0
- Heat Distance: 0.02
```

### Пресет 5: Ночной туман
```
Unity Fog:
- Fog: ON
- Mode: Exponential Squared
- Density: 0.02
- Color: #1A2530 (тёмно-синий)

TENKOKU Fog:
- Height: 120
- Height Density: 0.006
- Fog Skybox: ON
- Fog Horizon: ON
```

---

## 🛠️ Интеграция с GraphicsSettingsManager

Если вы хотите добавить настройки тумана в меню графики:

### Добавить в GraphicsSettingsManager.cs:

```csharp
[Header("Fog Settings")]
[SerializeField] private Toggle fogToggle;
[SerializeField] private Slider fogDensitySlider;
[SerializeField] private TMP_Text fogDensityText;

private TenkokuSkyFog tenkokuFog;

private void InitializeFogSettings()
{
    tenkokuFog = Camera.main.GetComponent<TenkokuSkyFog>();

    if (fogToggle != null)
    {
        fogToggle.isOn = RenderSettings.fog;
        fogToggle.onValueChanged.AddListener(OnFogToggleChanged);
    }

    if (fogDensitySlider != null)
    {
        fogDensitySlider.minValue = 0f;
        fogDensitySlider.maxValue = 0.1f;
        fogDensitySlider.value = RenderSettings.fogDensity;
        fogDensitySlider.onValueChanged.AddListener(OnFogDensityChanged);
    }
}

private void OnFogToggleChanged(bool enabled)
{
    RenderSettings.fog = enabled;
    if (tenkokuFog != null)
    {
        tenkokuFog.enabled = enabled;
    }
    PlayerPrefs.SetInt("FogEnabled", enabled ? 1 : 0);
}

private void OnFogDensityChanged(float value)
{
    RenderSettings.fogDensity = value;
    if (tenkokuFog != null)
    {
        tenkokuFog.heightDensity = value * 3f; // Коэффициент для TENKOKU
    }

    if (fogDensityText != null)
    {
        fogDensityText.text = $"{(value * 100f):F1}%";
    }

    PlayerPrefs.SetFloat("FogDensity", value);
}
```

---

## 🎯 Производительность

### Влияние на FPS:

**Unity Built-in Fog:**
- Очень лёгкий (почти без затрат)
- Поддерживается GPU
- Рекомендуется для мобильных устройств

**TENKOKU Sky Fog:**
- Средняя-высокая нагрузка
- Image Effect (post-processing)
- Рекомендуется для PC/консолей
- На мобильных: отключить heat distortion

### Оптимизация для Android:

```csharp
private void ApplyPlatformDefaults()
{
    if (Application.isMobilePlatform)
    {
        // Используем только Unity Fog на мобильных
        TenkokuSkyFog tenkokuFog = Camera.main.GetComponent<TenkokuSkyFog>();
        if (tenkokuFog != null)
        {
            tenkokuFog.enabled = false;
        }

        // Простой Exponential fog
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Exponential;
        RenderSettings.fogDensity = 0.015f;

        Debug.Log("[GraphicsSettings] Мобильный режим: использован лёгкий туман");
    }
}
```

---

## 🧪 Быстрое тестирование

### Тест 1: Проверка Unity Fog
1. Window → Rendering → Lighting → Environment
2. Включить Fog
3. Режим: Exponential, Density: 0.02
4. Цвет: Серый
5. Запустить сцену - должен быть виден туман

### Тест 2: Проверка TENKOKU Fog
1. Main Camera → Add Component → TenkokuSkyFog
2. Height: 150, Height Density: 0.005
3. Запустить сцену - должен быть высотный туман
4. Поднять камеру выше Height - туман должен исчезнуть

### Тест 3: Комбинация
1. Включить Unity Fog (Exponential, 0.01)
2. Добавить TenkokuSkyFog (Height: 180, Density: 0.003)
3. Запустить сцену - должен быть объёмный реалистичный туман

---

## 📋 Чеклист настройки тумана

- [ ] Unity Fog включен в Lighting Settings
- [ ] Выбран подходящий Fog Mode (Linear/Exponential/Exponential Squared)
- [ ] Настроен Fog Color под освещение сцены
- [ ] Density/Start/End настроены под масштаб мира
- [ ] (Опционально) TenkokuSkyFog добавлен на Main Camera
- [ ] Height настроен под рельеф ландшафта
- [ ] Height Density настроена для желаемой видимости
- [ ] Fog Skybox включен для реалистичности
- [ ] Проверена производительность (FPS)
- [ ] Сохранены настройки в пресет

---

## 🔗 Дополнительные ресурсы

### Файлы в проекте:
- **TenkokuSkyFog.cs**: `Assets/TENKOKU - DYNAMIC SKY/SCRIPTS/TenkokuSkyFog.cs`
- **Fog Prefabs**: `Assets/TENKOKU - DYNAMIC SKY/EFFECTS/fxFog.prefab`
- **Fog Shader**: `Assets/TENKOKU - DYNAMIC SKY/SHADERS/Tenkoku_FX_Fog.shader`

### Unity Documentation:
- [Unity Fog](https://docs.unity3d.com/Manual/lighting-fog.html)
- [RenderSettings](https://docs.unity3d.com/ScriptReference/RenderSettings.html)

---

**Версия**: 1.0
**Дата**: 2025-11-13
**Совместимость**: Unity 2021.3+, URP
**Статус**: ✅ Готово к использованию
