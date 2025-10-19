# 🔧 ИНСТРУКЦИЯ ПО ОБНОВЛЕНИЮ СЕРВЕРА

## ✨ Добавить обработчик визуальных эффектов

Эти изменения нужно добавить на сервер (Node.js на Render.com).

### 📁 Файл: `multiplayer.js` (или где у вас обработчики Socket.IO)

Добавьте этот код в секцию обработчиков событий:

```javascript
// ═══════════════════════════════════════════════════════════════
// ВИЗУАЛЬНЫЕ ЭФФЕКТЫ (взрывы, ауры, горение, баффы и т.д.)
// ═══════════════════════════════════════════════════════════════

socket.on('visual_effect_spawned', (data) => {
  const room = getRoomBySocketId(socket.id);
  if (!room) {
    console.log('[visual_effect_spawned] Игрок не в комнате:', socket.id);
    return;
  }

  console.log(`[visual_effect_spawned] ${socket.id} создал эффект:`, {
    type: data.effectType,
    prefab: data.effectPrefabName,
    targetSocketId: data.targetSocketId || 'world'
  });

  // Рассылаем ВСЕМ игрокам в комнате (включая отправителя для отладки)
  io.to(room.roomId).emit('visual_effect_spawned', {
    socketId: socket.id,
    effectType: data.effectType,
    effectPrefabName: data.effectPrefabName,
    position: data.position,
    rotation: data.rotation,
    targetSocketId: data.targetSocketId || '',
    duration: data.duration || 0,
    timestamp: Date.now()
  });

  console.log(`[visual_effect_spawned] Эффект разослан всем игрокам в комнате ${room.roomId}`);
});
```

### 🔍 Где добавить этот код:

Найдите в вашем файле секцию с другими обработчиками событий, например:
- `socket.on('player_update', ...)`
- `socket.on('player_skill', ...)`
- `socket.on('projectile_spawned', ...)`

И добавьте новый обработчик `visual_effect_spawned` рядом с ними.

---

## ✅ Проверка после деплоя

После того как вы задеплоите изменения на Render.com:

1. Запустите игру в Unity
2. Используйте любой скилл (Fireball, Hammer, Lightning)
3. Проверьте логи в Unity Console - должны быть сообщения:
   ```
   [SocketIO] ✨ Отправка визуального эффекта: type=explosion, prefab=FireballExplosion...
   [NetworkSync] ✨ Визуальный эффект получен: type=explosion, prefab=FireballExplosion...
   ```
4. Проверьте логи сервера на Render.com - должны быть сообщения о получении и рассылке эффектов

---

## 📦 Какие типы эффектов поддерживаются:

- `explosion` - взрывы снарядов (Fireball, Lightning, Hammer)
- `aoe` - AOE эффекты (Ice Nova, Meteor)
- `skill_hit` - эффекты попадания скиллов
- `heal` - эффекты лечения
- `transformation` - эффекты трансформации (дым/магия)
- `buff` - баффы (IncreaseAttack, Shield и т.д.)
- `debuff` - дебаффы (Stun, Root, Slow и т.д.)
- `dot` - DOT эффекты (Burn, Poison, Bleed)

---

## 🚀 После обновления сервера:

Все визуальные эффекты будут автоматически синхронизироваться между игроками в реал-тайме!
