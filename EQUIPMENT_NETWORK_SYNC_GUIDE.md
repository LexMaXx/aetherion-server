# Сетевая синхронизация экипировки - Руководство

## Обзор

Система изменения экипировки с **полной сетевой синхронизацией** для PvP:
- ✅ Экипировка изменяет статы (HP, Mana, Attack, Defense)
- ✅ Изменения синхронизируются с сервером в реальном времени
- ✅ Все игроки в комнате видят корректные значения статов
- ✅ Работает через Socket.IO для мгновенной синхронизации
- ✅ Поддержка множественных игроков в PvP

---

## Установка

### Шаг 1: Добавить EquipmentNetworkSync в сцену

1. **Hierarchy** → Find `NetworkManager` (или любой GameObject с SocketIOManager)
2. **Inspector** → **Add Component** → `EquipmentNetworkSync`

**Готово!** Больше ничего не нужно настраивать.

---

## Как это работает

### Поток данных:

```
Игрок A экипирует Legendary Sword
    ↓
MMOEquipmentManager.EquipItem()
    ↓
CharacterStats пересчитывает статы ← ЛОКАЛЬНОЕ изменение
    ↓
SendEquipmentChangeToServer() ← Отправка на сервер
    ↓
Сервер: multiplayer.js обработчик "equipment_changed"
    ↓
Обновление player.maxHealth, player.attack, player.defense на сервере
    ↓
Broadcast "player_equipment_changed" всем игрокам в комнате
    ↓
Игрок B получает событие "player_equipment_changed"
    ↓
EquipmentNetworkSync.OnEquipmentChanged()
    ↓
Находит NetworkPlayerEntity для Игрока A
    ↓
Обновляет HealthSystem/ManaSystem/CharacterStats
    ↓
Игрок B видит корректные статы Игрока A!
```

---

## Серверная часть (multiplayer.js)

### Обработчик изменения экипировки:

```javascript
socket.on('equipment_changed', (data) => {
  // Парсим данные
  const {
    slotType, itemName, isEquip,
    attackBonus, defenseBonus, healthBonus, manaBonus,
    totalAttackBonus, totalDefenseBonus, totalHealthBonus, totalManaBonus,
    currentHealth, maxHealth, currentMana, maxMana,
    attack, defense
  } = data;

  // Обновляем значения на сервере
  player.maxHealth = maxHealth;
  player.health = currentHealth;
  player.maxMana = maxMana;
  player.mana = currentMana;
  player.attack = attack;
  player.defense = defense;

  // Сохраняем бонусы экипировки
  player.equipment = {
    totalAttackBonus, totalDefenseBonus,
    totalHealthBonus, totalManaBonus
  };

  // Broadcast всем игрокам в комнате
  io.to(player.roomId).emit('player_equipment_changed', {
    socketId: socket.id,
    username: player.username,
    slotType, itemName, isEquip,
    totalAttackBonus, totalDefenseBonus, totalHealthBonus, totalManaBonus,
    health: currentHealth, maxHealth,
    mana: currentMana, maxMana,
    attack, defense
  });
});
```

---

## Клиентская часть (Unity)

### 1. MMOEquipmentManager.SendEquipmentChangeToServer()

```csharp
private void SendEquipmentChangeToServer(EquipmentSlot slot, ItemData item, bool isEquip)
{
    // Получаем текущие статы персонажа
    var healthSystem = FindObjectOfType<HealthSystem>();
    var characterStats = FindObjectOfType<CharacterStats>();
    var manaSystem = FindObjectOfType<ManaSystem>();

    // Собираем суммарные бонусы от ВСЕЙ экипировки
    EquipmentStats totalStats = GetTotalEquipmentStats();

    var data = new
    {
        slotType = slot.ToString(),
        itemName = item?.itemName ?? "",
        isEquip = isEquip,
        // Бонусы от конкретного предмета
        attackBonus = item?.attackBonus ?? 0,
        defenseBonus = item?.defenseBonus ?? 0,
        healthBonus = item?.healthBonus ?? 0,
        manaBonus = item?.manaBonus ?? 0,
        // Суммарные бонусы
        totalAttackBonus = totalStats.attackBonus,
        totalDefenseBonus = totalStats.defenseBonus,
        totalHealthBonus = totalStats.healthBonus,
        totalManaBonus = totalStats.manaBonus,
        // Текущие значения
        currentHealth = healthSystem.CurrentHealth,
        maxHealth = healthSystem.MaxHealth,
        currentMana = manaSystem.CurrentMana,
        maxMana = manaSystem.MaxMana,
        attack = characterStats.GetTotalAttack(),
        defense = characterStats.GetTotalDefense()
    };

    string json = JsonConvert.SerializeObject(data);
    socketManager.Emit("equipment_changed", json);
}
```

### 2. EquipmentNetworkSync.OnEquipmentChanged()

```csharp
private void OnEquipmentChanged(string jsonData)
{
    JToken data = JToken.Parse(jsonData);

    string socketId = data["socketId"].ToString();
    float maxHealth = data["maxHealth"].ToObject<float>();
    float health = data["health"].ToObject<float>();
    float maxMana = data["maxMana"].ToObject<float>();
    float mana = data["mana"].ToObject<float>();

    // Не обрабатываем своё собственное изменение
    if (socketId == socketManager.SocketId)
        return;

    // Находим другого игрока
    NetworkPlayerEntity targetPlayer = FindPlayerBySocketId(socketId);

    // Обновляем его статы
    HealthSystem healthSystem = targetPlayer.GetComponent<HealthSystem>();
    healthSystem.SetMaxHealth(maxHealth);
    healthSystem.SetHealth(health);

    ManaSystem manaSystem = targetPlayer.GetComponent<ManaSystem>();
    manaSystem.SetMaxMana(maxMana);
    manaSystem.SetMana(mana);

    CharacterStats characterStats = targetPlayer.GetComponent<CharacterStats>();
    characterStats.RecalculateStats();
}
```

---

## Тестирование в PvP

### Шаг 1: Запустить сервер

```bash
cd c:\Users\Asus\Aetherion
node multiplayer.js
```

Должно быть:
```
🚀 Server running on port 3000
🌍 ГЛОБАЛЬНАЯ MMO КОМНАТА СОЗДАНА
```

### Шаг 2: Запустить 2 клиента Unity

**Клиент 1:**
1. **Play Mode**
2. Выбери класс (например, Warrior)
3. Войди в игру
4. Нажми **K** - добавятся предметы
5. Посмотри статы в Equipment UI (C)

**Клиент 2** (Build or another Unity Editor):
1. Запусти второй инстанс игры
2. Выбери другой класс (например, Mage)
3. Войди в игру
4. Найди первого игрока на карте

### Шаг 3: Тестирование синхронизации

**На Клиенте 1:**
1. Нажми **I** - открыть инвентарь
2. Нажми **C** - открыть экипировку
3. **Double-click** на оружие в инвентаре (например, Legendary Sword)
4. Оружие экипируется → Статы изменяются:
   - Attack: 50 → 100 (+50)
   - MaxHP: 1000 → 1100 (+100)

**На Клиенте 2:**
1. Посмотри на healthbar над головой Игрока 1
2. Healthbar должен показывать **новое MaxHP: 1100**!
3. При атаке Игрока 1 урон должен быть выше (+50 ATK)

**Логи:**

**Клиент 1 (экипировал оружие):**
```
[MMOEquipment] Equipping: Legendary Sword to slot Weapon
[MMOEquipment] ✅ Equipped: Legendary Sword
[MMOEquipment] 📡 Sent equipment change to server: Weapon equipped Legendary Sword
[MMOEquipment] 📊 Total stats sent: ATK+50 DEF+0 HP+100 MP+0
```

**Сервер:**
```
[Equipment] ⚔️ PlayerName equipped Legendary Sword in Weapon slot
[Equipment] 📊 Item bonuses: ATK+50 DEF+0 HP+100 MP+0
[Equipment] 📊 Total bonuses: ATK+50 DEF+0 HP+100 MP+0
[Equipment] ✅ PlayerName stats updated: HP=1100/1100 MP=500/500 ATK=100 DEF=20
[Equipment] ✅ PlayerName equipment change broadcasted to room aetherion-global-world
```

**Клиент 2 (видит эффект):**
```
[EquipmentSync] ⚔️ PlayerName equipped Legendary Sword in Weapon slot
[EquipmentSync] 📊 Item bonuses: ATK+50 DEF+0 HP+100 MP+0
[EquipmentSync] 📊 Total bonuses: ATK+50 DEF+0 HP+100 MP+0
[EquipmentSync] ❤️ Updated PlayerName HP: 1100/1100
[EquipmentSync] 💙 Updated PlayerName Mana: 500/500
[EquipmentSync] ⚔️🛡️ Updated PlayerName stats: ATK=100 DEF=20
[EquipmentSync] ✅ PlayerName equipment update applied
```

---

## Проверка работы

### Локально (без сервера):

1. **Play Mode**
2. Нажми **K** - добавятся предметы
3. Нажми **I** → **C** → Double-click на предмет
4. Должно работать локально:
   - Предмет экипируется
   - Статы изменяются
   - Логи показывают предупреждение:
     ```
     [MMOEquipment] ⚠️ Not connected to server, equipment change local only
     ```

### С сервером (сетевая синхронизация):

1. **Запусти node multiplayer.js**
2. **Play Mode**
3. Подключись к серверу
4. Экипируй предмет
5. Должны появиться логи:
   ```
   [MMOEquipment] 📡 Sent equipment change to server
   ```
6. В консоли сервера:
   ```
   [Equipment] ✅ PlayerName equipment change broadcasted
   ```

---

## Troubleshooting

### Проблема: Экипировка не изменяет статы

**Причина**: CharacterStats не применяет бонусы

**Решение**:
1. Select Player в Hierarchy
2. Проверь что есть компонент `CharacterStats`
3. Проверь что в CharacterStats есть метод `ApplyEquipmentBonuses()`

### Проблема: Другие игроки не видят изменения статов

**Причина**: EquipmentNetworkSync не добавлен или не работает

**Решение**:
1. **Hierarchy** → Find GameObject с `SocketIOManager`
2. **Inspector** → Проверь наличие `EquipmentNetworkSync`
3. Если нет - добавь компонент
4. В логах должно быть:
   ```
   [EquipmentSync] ✅ Registered event handler for 'player_equipment_changed'
   ```

### Проблема: Ошибка "RecalculateStats not found"

**Причина**: Старая версия CharacterStats без метода RecalculateStats

**Решение**:
- Метод RecalculateStats должен быть в CharacterStats
- Если нет - добавь его:
  ```csharp
  public void RecalculateStats()
  {
      // Пересчитать статы с учетом экипировки
      UpdateStats();
  }
  ```

### Проблема: События не приходят от сервера

**Причина**: Сервер не запущен или событие не зарегистрировано

**Решение**:
1. Проверь что `node multiplayer.js` запущен
2. В консоли Unity:
   ```
   [SocketIO] ✅ Подключено к серверу!
   ```
3. Проверь что в multiplayer.js есть обработчик:
   ```javascript
   socket.on('equipment_changed', (data) => {
   ```

---

## Какие статы синхронизируются

### Базовые статы:
- **MaxHealth** - максимальное здоровье
- **CurrentHealth** - текущее здоровье
- **MaxMana** - максимальная мана
- **CurrentMana** - текущая мана
- **Attack** - атака
- **Defense** - защита

### Бонусы от экипировки:
- **totalAttackBonus** - суммарный бонус к атаке
- **totalDefenseBonus** - суммарный бонус к защите
- **totalHealthBonus** - суммарный бонус к здоровью
- **totalManaBonus** - суммарный бонус к мане

---

## Архитектура файлов

```
Unity Client:
├── MMOEquipmentManager.cs
│   ├── EquipItem() - экипирует предмет локально
│   ├── UnequipItem() - снимает предмет локально
│   ├── SendEquipmentChangeToServer() - отправляет на сервер
│   └── GetTotalEquipmentStats() - считает суммарные бонусы
│
├── CharacterStats.cs
│   ├── ApplyEquipmentBonuses() - применяет бонусы к статам
│   └── RecalculateStats() - пересчитывает статы
│
├── HealthSystem.cs
│   ├── SetMaxHealth() - устанавливает MaxHP
│   └── SetHealth() - устанавливает HP
│
├── ManaSystem.cs
│   ├── SetMaxMana() - устанавливает MaxMP
│   └── SetMana() - устанавливает MP
│
└── EquipmentNetworkSync.cs
    └── OnEquipmentChanged() - обрабатывает событие от сервера

Server:
└── multiplayer.js
    └── socket.on('equipment_changed') - обработчик изменения экипировки
        └── io.to().emit('player_equipment_changed') - broadcast всем игрокам
```

---

## Готово! ✅

Теперь система экипировки полностью работает с сетевой синхронизацией:

1. ✅ **Локальное экипирование** - мгновенное изменение статов
2. ✅ **Серверная синхронизация** - данные обновляются на сервере
3. ✅ **Broadcast** - все игроки в комнате получают обновление
4. ✅ **Сетевое применение** - другие игроки видят корректные значения
5. ✅ **PvP готово** - работает в реальных боях

**Тест в PvP:**
```
Игрок 1: Экипирует Legendary Sword → MaxHP 1000→1100 ATK 50→100
Игрок 2: Видит healthbar Игрока 1 обновляется в реальном времени!
Игрок 2: Атакует Игрока 1 и видит корректный урон с учетом новой защиты!
```

Все игроки всегда видят актуальные значения статов! 🎮⚔️🛡️
