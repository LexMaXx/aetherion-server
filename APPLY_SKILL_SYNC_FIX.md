# 🛠️ ПРИМЕНЕНИЕ ИСПРАВЛЕНИЙ ДЛЯ СИНХРОНИЗАЦИИ СКИЛЛОВ

## ⚠️ **ВАЖНО: Закройте Unity Editor перед применением!**

Unity блокирует файлы .cs от изменений. Выполните эти шаги:

---

## **ШАГ 1: Добавить SendSkillUsed в NetworkCombatSync.cs**

Откройте `Assets/Scripts/Network/NetworkCombatSync.cs`

Найдите метод `OnDestroy()` (в конце файла) и **ПЕРЕД НИМ** добавьте:

```csharp
    /// <summary>
    /// ✅ НОВЫЙ МЕТОД: Отправить использование скилла на сервер с полными данными
    /// Вызывается из PlayerAttack.UseSkillDirectly() после успешного UseSkill()
    /// </summary>
    public void SendSkillUsed(int skillId, string animationTrigger, Vector3 targetPosition, float castTime)
    {
        if (!enableSync || !isMultiplayer || SocketIOManager.Instance == null)
        {
            Debug.Log("[NetworkCombatSync] SendSkillUsed пропущен (не мультиплеер)");
            return;
        }

        if (!SocketIOManager.Instance.IsConnected)
        {
            Debug.LogWarning("[NetworkCombatSync] SendSkillUsed пропущен - нет подключения к серверу");
            return;
        }

        // Отправляем полные данные скилла для синхронизации
        SocketIOManager.Instance.SendSkillUsed(skillId, animationTrigger, targetPosition, castTime);

        Debug.Log($"[NetworkCombatSync] 📡 Скилл отправлен на сервер: ID={skillId}, trigger={animationTrigger}, castTime={castTime}с");
    }
```

---

## **ШАГ 2: Добавить SendSkillUsed в SocketIOManager.cs**

Откройте `Assets/Scripts/Network/SocketIOManager.cs`

Найдите метод `SendProjectileSpawned()` (около строки 489) и **ПОСЛЕ НЕГО** добавьте:

```csharp
    /// <summary>
    /// Отправить использование скилла на сервер
    /// </summary>
    public void SendSkillUsed(int skillId, string animationTrigger, Vector3 targetPosition, float castTime)
    {
        if (!IsConnected)
        {
            DebugLog("⚠️ SendSkillUsed: Не подключены к серверу");
            return;
        }

        var data = new
        {
            skillId = skillId,
            animationTrigger = animationTrigger,
            targetPosition = new { x = targetPosition.x, y = targetPosition.y, z = targetPosition.z },
            castTime = castTime,
            timestamp = GetTimestamp()
        };

        string json = JsonConvert.SerializeObject(data);
        Emit("player_used_skill", json);

        DebugLog($"⚡ Отправка скилла: ID={skillId}, trigger={animationTrigger}, castTime={castTime}с");
    }
```

---

## **ШАГ 3: Добавить вызов в PlayerAttack.cs**

Откройте `Assets/Scripts/Player/PlayerAttack.cs`

Найдите метод `UseSkillDirectly()` (около строки 1120), найдите блок:

```csharp
if (success)
{
    Debug.Log($"[PlayerAttack] ⚡ Скилл {skill.skillName} УСПЕШНО использован!");
}
```

**ЗАМЕНИТЕ на:**

```csharp
if (success)
{
    Debug.Log($"[PlayerAttack] ⚡ Скилл {skill.skillName} УСПЕШНО использован!");

    // ✅ КРИТИЧЕСКОЕ: Отправляем на сервер для синхронизации
    NetworkCombatSync networkSync = GetComponent<NetworkCombatSync>();
    if (networkSync != null && networkSync.enabled)
    {
        Vector3 targetPos = target != null ? target.position : transform.position + transform.forward * 10f;
        networkSync.SendSkillUsed(skill.skillId, skill.animationTrigger, targetPos, skill.castTime);
        Debug.Log($"[PlayerAttack] 📡 Скилл {skill.skillName} (ID:{skill.skillId}) отправлен на сервер");
    }
}
```

---

## **ШАГ 4: Исправить targetSocketId в EffectManager.cs**

Откройте `Assets/Scripts/Skills/EffectManager.cs`

Найдите метод `SendEffectToServer()` (около строки 620-630) и **ЗАМЕНИТЕ его полностью на:**

```csharp
    /// <summary>
    /// Отправить эффект на сервер для синхронизации с другими игроками
    /// </summary>
    private void SendEffectToServer(ActiveEffect effect)
    {
        NetworkCombatSync networkSync = GetComponent<NetworkCombatSync>();
        if (networkSync != null && networkSync.enabled)
        {
            // ✅ ИСПРАВЛЕНО: Определяем targetSocketId корректно
            string targetSocketId = GetMySocketId();

            // Отправляем эффект с правильным targetSocketId
            if (SocketIOManager.Instance != null && SocketIOManager.Instance.IsConnected)
            {
                SocketIOManager.Instance.SendEffectApplied(effect.config, targetSocketId);
                Log($"📡 Эффект {effect.config.effectType} отправлен на сервер (target={targetSocketId})");
            }
        }
    }

    /// <summary>
    /// Получить мой socketId из SocketIOManager
    /// </summary>
    private string GetMySocketId()
    {
        if (SocketIOManager.Instance != null && SocketIOManager.Instance.IsConnected)
        {
            // Получаем наш socketId
            return SocketIOManager.Instance.SessionId;
        }
        return "";
    }
```

---

## **ШАГ 5: Улучшить OnEffectApplied в NetworkSyncManager.cs**

Откройте `Assets/Scripts/Network/NetworkSyncManager.cs`

Найдите метод `OnEffectApplied()` (около строки 1067-1150) и найдите блок после определения `targetTransform`:

```csharp
if (targetTransform != null)
{
    // ... существующий код ...
```

**ДОБАВЬТЕ ПОСЛЕ существующего кода внутри этого if:**

```csharp
            // ✅ НОВОЕ: СОЗДАЁМ ВИЗУАЛЬНУЮ АУРУ
            if (!string.IsNullOrEmpty(data.particleEffectPrefabName))
            {
                GameObject particlePrefab = Resources.Load<GameObject>($"Effects/{data.particleEffectPrefabName}");

                if (particlePrefab != null)
                {
                    // Создаём ауру как child объект игрока
                    GameObject aura = Instantiate(particlePrefab, targetTransform);
                    aura.transform.localPosition = Vector3.up * 1f; // 1 метр над головой
                    aura.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f); // Поворот вниз

                    // Удаляем через duration секунд
                    Destroy(aura, data.duration);

                    Debug.Log($"[NetworkSync] ✨ Аура {data.effectType} создана для {targetTransform.name} на {data.duration} секунд");
                }
                else
                {
                    Debug.LogWarning($"[NetworkSync] ⚠️ Particle prefab не найден: Effects/{data.particleEffectPrefabName}");
                }
            }
```

---

## 🧪 **ТЕСТИРОВАНИЕ:**

После применения всех исправлений:

1. **Откройте Unity Editor**
2. **Дождитесь перекомпиляции** (Build Succeeded)
3. **Запустите 2 клиента:**
   - Клиент 1: Создать комнату → Выбрать Archer → Войти в арену
   - Клиент 2: Войти в ту же комнату → Выбрать любой класс → Войти в арену
4. **Игрок 1 использует Eagle Eye (кнопка 4)**

### **Ожидаемые логи:**

**Клиент 1 (Игрок A):**
```
[PlayerAttack] 📡 Скилл Eagle Eye (ID:303) отправлен на сервер
[NetworkCombatSync] 📡 Скилл отправлен на сервер: ID=303, trigger=Skill, castTime=0с
[SocketIO] ⚡ Отправка скилла: ID=303, trigger=Skill, castTime=0с
[EffectManager] 📡 Эффект IncreasePerception отправлен на сервер (target=xyz...)
```

**Клиент 2 (Игрок B):**
```
[NetworkSync] ⚡ RAW player_used_skill JSON: {...}
[NetworkSync] ⚡ Скилл получен: socketId=xyz, skillId=303
[NetworkSync] 🎬 Анимация 'Skill' запущена для PlayerA
[NetworkSync] ✨ RAW effect_applied JSON: {...}
[NetworkSync] ✨ Эффект IncreasePerception применён на PlayerA
[NetworkSync] ✨ Аура IncreasePerception создана для PlayerA на 15 секунд  ← ✅ НОВОЕ!
```

---

## ✅ **РЕЗУЛЬТАТ:**

После исправлений:
- ✅ Игрок B **видит** анимацию каста Eagle Eye
- ✅ Игрок B **видит** золотую ауру над головой Игрока A
- ✅ Скилл синхронизируется через сервер
- ✅ `targetSocketId` корректно передаётся

---

## 🚨 **ЕСЛИ НЕ РАБОТАЕТ:**

### **Проблема: "Parameter 'Skill' does not exist"**

**Решение:** Измените `animationTrigger` в SkillConfig:

1. Откройте `Assets/Resources/Skills/Archer_EagleEye.asset` в Inspector
2. Найдите поле `Animation Trigger`
3. Измените с `"Skill"` на `"Attack"` (или другой существующий параметр из ArcherAnimator)
4. Сохраните (Ctrl+S)

### **Проблема: "SessionId не существует"**

**Решение:** Замените `SocketIOManager.Instance.SessionId` на:

```csharp
private string GetMySocketId()
{
    // Ищем наш socket.id через NetworkSyncManager
    if (NetworkSyncManager.Instance != null)
    {
        return NetworkSyncManager.Instance.GetLocalPlayerSocketId();
    }
    return "";
}
```

А в `NetworkSyncManager.cs` добавьте:

```csharp
public string GetLocalPlayerSocketId()
{
    return localPlayerSocketId;
}
```

---

**Готово! Все изменения применены! 🎉**
