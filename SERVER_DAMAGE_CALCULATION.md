# Server-Side Damage Calculation Implementation Guide

## Overview
The client now sends **SPECIAL stats** (Strength, Intelligence, Luck) and **base damage** to the server instead of pre-calculated damage. The server must calculate the final damage and critical hits based on these stats.

## Changes Made to Client

### 1. PlayerAttack.cs
- Added public properties:
  - `public float BaseDamage => attackDamage;` - Base weapon damage without stat bonuses
  - `public bool IsRangedAttack => isRangedAttack;` - Attack type (melee/ranged)

### 2. NetworkCombatSync.cs - SendAttack()
- Now retrieves CharacterStats and PlayerAttack components
- Sends SPECIAL stats to server instead of pre-calculated damage
- New parameters sent: `strength`, `intelligence`, `luck`, `baseDamage`

### 3. SocketIOManager.cs - SendPlayerAttack()
**Old signature:**
```csharp
public void SendPlayerAttack(string targetType, string targetId, float damage, string attackType, ...)
```

**New signature:**
```csharp
public void SendPlayerAttack(string targetType, string targetId, int strength, int intelligence, int luck, float baseDamage, string attackType, ...)
```

## Server-Side Implementation Required

### JSON Format Received by Server

The `player_attack` event now receives the following JSON:

```json
{
  "targetType": "player",      // or "enemy"
  "targetId": "socketId123",   // socketId (player) or enemyId (NPC)
  "attackType": "melee",       // "melee", "ranged", or "magic"

  // SPECIAL stats from attacker
  "strength": 5,               // For physical damage (melee)
  "intelligence": 8,           // For magical damage (ranged)
  "luck": 3,                   // For critical hits
  "baseDamage": 30.0,          // Base weapon damage (WITHOUT stat bonuses)

  "position": { "x": 10, "y": 0, "z": 5 },
  "direction": { "x": 0, "y": 0, "z": 1 },
  "targetPosition": { "x": 15, "y": 0, "z": 5 }
}
```

### Damage Calculation Formulas

The server MUST implement these formulas (matching Unity's StatsFormulas.cs):

#### 1. Physical Damage (Melee Attacks)
```javascript
// For attackType === "melee"
const strengthDamageBonus = 5; // Damage bonus per Strength point
const finalDamage = baseDamage + (strength * strengthDamageBonus);
```

**Example:**
- Base damage: 30
- Strength: 5
- Final damage: 30 + (5 * 5) = **55**

#### 2. Magical Damage (Ranged Attacks)
```javascript
// For attackType === "ranged" or "magic"
const intelligenceDamageBonus = 5; // Damage bonus per Intelligence point
const finalDamage = baseDamage + (intelligence * intelligenceDamageBonus);
```

**Example:**
- Base damage: 40
- Intelligence: 8
- Final damage: 40 + (8 * 5) = **80**

#### 3. Critical Hit Calculation
```javascript
const baseCritChance = 5;      // 5% base crit chance
const luckCritBonus = 3;       // 3% crit chance per Luck point
const critDamageMultiplier = 2; // 2x damage on crit

// Calculate crit chance
const critChance = baseCritChance + (luck * luckCritBonus);

// Roll for crit
const randomRoll = Math.random() * 100; // 0-100
const isCrit = randomRoll < critChance;

// Apply crit if successful
if (isCrit) {
  finalDamage = finalDamage * critDamageMultiplier;
}
```

**Example:**
- Luck: 3
- Crit chance: 5 + (3 * 3) = **14%**
- If crit: damage × 2

### Complete Server-Side Handler Example

```javascript
// In your Socket.IO server (multiplayer.js or server.js)

socket.on('player_attack', (data) => {
  try {
    const parsedData = JSON.parse(data);
    const {
      targetType,
      targetId,
      attackType,
      strength,
      intelligence,
      luck,
      baseDamage,
      position,
      direction,
      targetPosition
    } = parsedData;

    // Get attacker's socketId
    const attackerId = socket.id;

    console.log(`[ATTACK] ${attackerId} attacks ${targetType} ${targetId}`);
    console.log(`  SPECIAL: STR=${strength}, INT=${intelligence}, LUCK=${luck}`);
    console.log(`  Base Damage: ${baseDamage}`);

    // STEP 1: Calculate damage based on attack type
    let finalDamage = baseDamage;

    if (attackType === 'melee') {
      // Physical damage - use Strength
      const strengthDamageBonus = 5;
      finalDamage = baseDamage + (strength * strengthDamageBonus);
      console.log(`  Physical damage: ${baseDamage} + (${strength} × 5) = ${finalDamage}`);
    } else if (attackType === 'ranged' || attackType === 'magic') {
      // Magical damage - use Intelligence
      const intelligenceDamageBonus = 5;
      finalDamage = baseDamage + (intelligence * intelligenceDamageBonus);
      console.log(`  Magical damage: ${baseDamage} + (${intelligence} × 5) = ${finalDamage}`);
    }

    // STEP 2: Roll for critical hit
    const baseCritChance = 5;
    const luckCritBonus = 3;
    const critDamageMultiplier = 2;

    const critChance = baseCritChance + (luck * luckCritBonus);
    const randomRoll = Math.random() * 100;
    const isCrit = randomRoll < critChance;

    if (isCrit) {
      finalDamage = finalDamage * critDamageMultiplier;
      console.log(`  🌟 CRITICAL HIT! Damage × ${critDamageMultiplier} = ${finalDamage}`);
    }

    // STEP 3: Apply damage to target
    if (targetType === 'player') {
      // Attack on another player
      const targetSocket = io.sockets.sockets.get(targetId);

      if (targetSocket) {
        // Send damage event to victim
        targetSocket.emit('enemy_damaged_by_server', JSON.stringify({
          attackerId: attackerId,
          damage: finalDamage,
          isCrit: isCrit,
          attackType: attackType
        }));

        console.log(`  ✅ Damage ${finalDamage} sent to player ${targetId}`);
      } else {
        console.log(`  ⚠️ Target player ${targetId} not found`);
      }
    } else if (targetType === 'enemy') {
      // Attack on NPC enemy
      const room = Array.from(socket.rooms).find(r => r !== socket.id);

      if (room) {
        // Broadcast to all players in room (including attacker)
        io.to(room).emit('enemy_damaged_by_server', JSON.stringify({
          attackerId: attackerId,
          enemyId: targetId,
          damage: finalDamage,
          isCrit: isCrit,
          attackType: attackType
        }));

        console.log(`  ✅ NPC damage ${finalDamage} broadcast to room ${room}`);
      }
    }

  } catch (error) {
    console.error('[player_attack] Error:', error);
  }
});
```

## Client-Side Handler (Already Implemented)

The client already has handlers for `enemy_damaged_by_server` in NetworkSyncManager.cs.

## Testing Checklist

### Test Case 1: Physical Damage (Melee)
- **Warrior** with Strength = 5, Base Damage = 30
- Expected: 30 + (5 × 5) = **55 damage**
- Verify in server logs and client damage numbers

### Test Case 2: Magical Damage (Ranged)
- **Mage** with Intelligence = 8, Base Damage = 40
- Expected: 40 + (8 × 5) = **80 damage**
- Verify in server logs and client damage numbers

### Test Case 3: Critical Hits
- **Rogue** with Luck = 10
- Crit chance: 5 + (10 × 3) = **35%**
- Test 10 attacks, expect ~3-4 crits with 2x damage

### Test Case 4: Different Classes
- **Paladin**: Strength-based melee (low damage, high defense)
- **Archer**: Intelligence-based ranged (physical arrows, but uses INT formula)
- **Rogue**: Intelligence-based ranged (soul shards, high luck)

## Formulas Reference (from Unity)

These values are defined in `Assets/Scripts/Stats/StatsFormulas.cs`:

```csharp
public float strengthDamageBonus = 5f;    // Damage per Strength
public float intelligenceDamageBonus = 5f; // Damage per Intelligence
public float baseCritChance = 5f;         // Base 5% crit chance
public float luckCritBonus = 3f;          // +3% per Luck
public float critDamageMultiplier = 2f;   // 2x damage on crit
```

**IMPORTANT:** Server formulas MUST match these values exactly!

## Migration Notes

### Breaking Changes
- Old `player_attack` events with `damage` field will NO LONGER WORK
- Server MUST be updated to use new JSON format
- Server MUST implement damage calculation formulas

### Backward Compatibility
If you need backward compatibility:
1. Check if `strength` field exists in data
2. If yes: use new calculation
3. If no: use old `damage` field (fallback)

```javascript
if (parsedData.strength !== undefined) {
  // New system - calculate damage
  finalDamage = calculateDamage(parsedData);
} else {
  // Old system - use pre-calculated damage
  finalDamage = parsedData.damage;
}
```

## Security Benefits

### Before (Insecure)
- Client calculated damage locally
- Client sent final damage to server
- ❌ **Cheating possible:** Client could send fake damage values

### After (Secure)
- Client sends SPECIAL stats + base damage
- Server calculates final damage
- ✅ **Cheating prevented:** Server validates and calculates damage
- ✅ **Server-authoritative:** All damage calculations on server

## Next Steps

1. **Update server handler** for `player_attack` event
2. **Implement damage formulas** (physical, magical, crit)
3. **Test with different classes** (Warrior, Mage, Rogue, Paladin, Archer)
4. **Verify crit rolls** work correctly
5. **Monitor server logs** for damage calculations
6. **Test multiplayer PvP** (player vs player attacks)
7. **Test PvE** (player vs NPC enemy attacks)

## Support

If you need help implementing server-side calculations, refer to:
- This documentation
- [StatsFormulas.cs](Assets/Scripts/Stats/StatsFormulas.cs) - Unity formulas
- [CharacterStats.cs](Assets/Scripts/Stats/CharacterStats.cs) - Stat calculations
- [NetworkCombatSync.cs](Assets/Scripts/Network/NetworkCombatSync.cs) - Client attack sender

---

**Last Updated:** 2025-10-14
**Version:** 1.0
**Related Commit:** "FEATURE: Server-side damage calculation - send SPECIAL stats"
