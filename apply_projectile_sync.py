#!/usr/bin/env python3
"""
Скрипт для автоматического применения патча синхронизации снарядов
"""

import os
import re

def apply_projectile_sync_fix():
    """Применяет исправления синхронизации снарядов"""

    # Путь к SkillExecutor.cs
    skill_executor_path = "Assets/Scripts/Skills/SkillExecutor.cs"

    print("[1/4] Чтение SkillExecutor.cs...")
    with open(skill_executor_path, 'r', encoding='utf-8') as f:
        content = f.read()

    # Проверка что патч ещё не применён
    if "🚀 НОВОЕ: Синхронизация снаряда в мультиплеере" in content:
        print("⚠️  Патч уже применён к SkillExecutor.cs")
        return

    print("[2/4] Применение патча к LaunchProjectile()...")

    # Находим место для вставки (после создания снаряда)
    pattern = r'(GameObject projectile = Instantiate\(skill\.projectilePrefab, spawnPos, Quaternion\.LookRotation\(direction\)\);)\s*\n\s*(// Try CelestialProjectile first)'

    replacement = r'''\1

        // 🚀 НОВОЕ: Синхронизация снаряда в мультиплеере
        string targetSocketId = "";
        if (target != null)
        {
            NetworkPlayer networkTarget = target.GetComponent<NetworkPlayer>();
            if (networkTarget != null)
            {
                targetSocketId = networkTarget.socketId;
            }
        }

        // Отправляем на сервер для отображения у других игроков
        SocketIOManager socketIO = SocketIOManager.Instance;
        if (socketIO != null && socketIO.IsConnected)
        {
            socketIO.SendProjectileSpawned(skill.skillId, spawnPos, direction, targetSocketId);
            Log($"🌐 Снаряд отправлен на сервер: {skill.skillName}");
        }

        \2'''

    content = re.sub(pattern, replacement, content)

    print("[3/4] Изменение SpawnEffect для синхронизации...")

    # Изменяем вызов SpawnEffect в LaunchProjectile
    content = content.replace(
        'SpawnEffect(skill.castEffectPrefab, spawnPos, Quaternion.identity);',
        'SpawnEffect(skill.castEffectPrefab, spawnPos, Quaternion.identity, true); // true = синхронизировать'
    )

    # Изменяем сигнатуру метода SpawnEffect
    content = content.replace(
        'private void SpawnEffect(GameObject effectPrefab, Vector3 position, Quaternion rotation)',
        'private void SpawnEffect(GameObject effectPrefab, Vector3 position, Quaternion rotation, bool syncToNetwork = false)'
    )

    # Добавляем логику синхронизации в SpawnEffect
    spawn_effect_pattern = r'(private void SpawnEffect\(GameObject effectPrefab, Vector3 position, Quaternion rotation, bool syncToNetwork = false\)\s*\{\s*if \(effectPrefab == null\) return;\s*GameObject effect = Instantiate\(effectPrefab, position, rotation\);)'

    spawn_effect_replacement = r'''\1

        // 🌐 Синхронизация в мультиплеере
        if (syncToNetwork)
        {
            SocketIOManager socketIO = SocketIOManager.Instance;
            if (socketIO != null && socketIO.IsConnected)
            {
                socketIO.SendVisualEffect(
                    "cast",
                    effectPrefab.name,
                    position,
                    rotation,
                    "", // targetSocketId пусто для эффектов каста
                    3f, // duration
                    null
                );
                Log($"🎨 Эффект каста отправлен на сервер: {effectPrefab.name}");
            }
        }'''

    content = re.sub(spawn_effect_pattern, spawn_effect_replacement, content)

    print("[4/4] Сохранение изменений...")
    with open(skill_executor_path, 'w', encoding='utf-8') as f:
        f.write(content)

    print("✅ SkillExecutor.cs успешно обновлён!")
    print("\n📋 Следующие шаги:")
    print("1. Добавить обработку projectile_spawned в NetworkSyncManager.cs")
    print("2. Добавить метод DisableDamage() в Projectile.cs")
    print("3. Протестировать в мультиплеере")
    print("\nСм. PROJECTILE_SYNC_FIX.md для деталей")

if __name__ == "__main__":
    os.chdir(os.path.dirname(os.path.abspath(__file__)))
    apply_projectile_sync_fix()
