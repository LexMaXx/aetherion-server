#!/bin/bash
# Ручное применение патча синхронизации снарядов

FILE="Assets/Scripts/Skills/SkillExecutor.cs"

echo "Applying projectile sync patch to $FILE..."

# Создаём backup
cp "$FILE" "$FILE.backup"

# Используем sed для вставки кода после GameObject projectile = Instantiate
sed -i '/GameObject projectile = Instantiate.*LookRotation.*direction/a\
\
        // 🚀 MULTIPLAYER SYNC: Send projectile to server for other players to see\
        string targetSocketId = "";\
        if (target != null)\
        {\
            NetworkPlayer networkTarget = target.GetComponent<NetworkPlayer>();\
            if (networkTarget != null)\
            {\
                targetSocketId = networkTarget.socketId;\
            }\
        }\
\
        SocketIOManager socketIO = SocketIOManager.Instance;\
        if (socketIO != null && socketIO.IsConnected)\
        {\
            socketIO.SendProjectileSpawned(skill.skillId, spawnPos, direction, targetSocketId);\
            Log($"🌐 Projectile synced to server: {skill.skillName}");\
        }' "$FILE"

echo "✅ Patch applied! Backup saved as $FILE.backup"
echo "Please verify the changes in Unity"
