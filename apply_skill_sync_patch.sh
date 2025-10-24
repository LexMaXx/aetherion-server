#!/bin/bash
# Применение патча для синхронизации скиллов в мультиплеере

echo "🛠️ Применение патча для синхронизации скиллов..."

# ШАГ 1: NetworkCombatSync.cs - добавить SendSkillUsed
echo "📝 Шаг 1/5: Патчим NetworkCombatSync.cs..."

# Создаём временный патч-файл
cat > /tmp/networkcomba tsync_patch.txt << 'EOF'
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

EOF

# Вставляем перед OnDestroy()
sed -i '/void OnDestroy()/i \    /// <summary>\n    /// ✅ НОВЫЙ МЕТОД: Отправить использование скилла на сервер с полными данными\n    /// Вызывается из PlayerAttack.UseSkillDirectly() после успешного UseSkill()\n    /// <\/summary>\n    public void SendSkillUsed(int skillId, string animationTrigger, Vector3 targetPosition, float castTime)\n    {\n        if (!enableSync || !isMultiplayer || SocketIOManager.Instance == null)\n        {\n            Debug.Log("[NetworkCombatSync] SendSkillUsed пропущен (не мультиплеер)");\n            return;\n        }\n\n        if (!SocketIOManager.Instance.IsConnected)\n        {\n            Debug.LogWarning("[NetworkCombatSync] SendSkillUsed пропущен - нет подключения к серверу");\n            return;\n        }\n\n        // Отправляем полные данные скилла для синхронизации\n        SocketIOManager.Instance.SendSkillUsed(skillId, animationTrigger, targetPosition, castTime);\n\n        Debug.Log($"[NetworkCombatSync] 📡 Скилл отправлен на сервер: ID={skillId}, trigger={animationTrigger}, castTime={castTime}с");\n    }\n' Assets/Scripts/Network/NetworkCombatSync.cs

echo "✅ NetworkCombatSync.cs обновлён!"

echo ""
echo "🎉 Патч успешно применён!"
echo ""
echo "⚠️ ВАЖНО:"
echo "1. Откройте Unity Editor"
echo "2. Дождитесь перекомпиляции (Build Succeeded)"
echo "3. Протестируйте с 2 клиентами"
echo ""
echo "📋 Следуйте инструкциям в APPLY_SKILL_SYNC_FIX.md для остальных шагов"
