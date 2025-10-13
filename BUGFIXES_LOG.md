# 🐛 Журнал исправления багов - Aetherion

## 📅 10.10.2025 - Исправление ошибок консоли

---

### ✅ Исправление #1: ParticleSystem duration warning

**Проблема:**
```
Setting the duration while system is still playing is not supported.
Please wait until the system has stopped and all particles have expired
or call Stop with ParticleSystemStopBehavior.StopEmittingAndClear.
```

**Причина:**
При создании ParticleSystem компонент автоматически запускается (играет).
Попытка изменить `main.duration` на играющей системе вызывала ошибку.

**Решение:**
Добавлен вызов `Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear)`
сразу после создания ParticleSystem, перед настройкой параметров.

**Изменённый файл:**
- `Assets/Scripts/Effects/WeaponGlowEffect.cs`
  - Строка 107: `CreateElectricParticles()`
  - Строка 173: `CreateAuraParticles()`

**Код исправления:**
```csharp
electricParticles = particleObj.AddComponent<ParticleSystem>();

// ВАЖНО: Останавливаем систему перед настройкой
electricParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

var main = electricParticles.main;
main.duration = 1.0f; // Теперь безопасно
```

---

### ✅ Исправление #2: CharacterController.Move на неактивном контроллере

**Проблема:**
```
CharacterController.Move called on inactive controller
```

**Причина:**
Во время атаки система `PlayerAttack.cs` отключает CharacterController
(`characterController.enabled = false`) для блокировки движения.
Но скрипты `PlayerController.cs` и `MixamoPlayerController.cs` продолжали
вызывать `Move()`, что вызывало ошибку.

**Решение:**
Добавлены проверки `characterController.enabled` перед каждым вызовом `Move()`.

**Изменённые файлы:**
1. `Assets/Scripts/Player/PlayerController.cs` (строка 118)
2. `Assets/Scripts/Player/MixamoPlayerController.cs` (строки 108, 171)

**Код исправления:**
```csharp
// Применяем движение (только если контроллер активен)
if (characterController != null && characterController.enabled)
{
    characterController.Move(movement);
}
```

---

## 📊 Статистика исправлений

| Категория | Количество |
|-----------|------------|
| Ошибки ParticleSystem | 4 (2 для Electric + 2 для Aura) |
| Ошибки CharacterController | 9+ |
| **Всего исправлено** | **13+** |
| Изменённых файлов | 3 |
| Добавлено строк кода | ~15 |

---

## ✅ Результат

После исправлений:
- ✅ Консоль Unity чистая (нет ошибок)
- ✅ Эффекты оружия работают корректно
- ✅ Блокировка движения при атаке работает без ошибок
- ✅ Проект компилируется успешно

---

**Протестировано:** ✅
**Коммит:** Bugfix - ParticleSystem duration и CharacterController.Move errors
**Дата:** 10.10.2025
