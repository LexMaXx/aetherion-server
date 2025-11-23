using UnityEngine;
using Newtonsoft.Json.Linq;

/// <summary>
/// Синхронизация изменений экипировки через сеть
/// Обрабатывает событие player_equipment_changed от сервера и обновляет статы других игроков
/// </summary>
public class EquipmentNetworkSync : MonoBehaviour
{
    private SocketIOManager socketManager;

    void Start()
    {
        socketManager = FindObjectOfType<SocketIOManager>();
        if (socketManager == null)
        {
            Debug.LogError("[EquipmentSync] ❌ SocketIOManager not found!");
            return;
        }

        // Регистрируем обработчик события
        socketManager.On("player_equipment_changed", OnEquipmentChanged);
        Debug.Log("[EquipmentSync] ✅ Registered event handler for 'player_equipment_changed'");
    }

    /// <summary>
    /// Обработчик события изменения экипировки другим игроком
    /// </summary>
    private void OnEquipmentChanged(string jsonData)
    {
        try
        {
            // Парсим JSON
            JToken data = JToken.Parse(jsonData);

            string socketId = data["socketId"]?.ToString();
            string username = data["username"]?.ToString();
            string slotType = data["slotType"]?.ToString();
            string itemName = data["itemName"]?.ToString();
            bool isEquip = data["isEquip"]?.ToObject<bool>() ?? false;

            // Бонусы от конкретного предмета
            int attackBonus = data["attackBonus"]?.ToObject<int>() ?? 0;
            int defenseBonus = data["defenseBonus"]?.ToObject<int>() ?? 0;
            int healthBonus = data["healthBonus"]?.ToObject<int>() ?? 0;
            int manaBonus = data["manaBonus"]?.ToObject<int>() ?? 0;

            // Суммарные бонусы от всей экипировки
            int totalAttackBonus = data["totalAttackBonus"]?.ToObject<int>() ?? 0;
            int totalDefenseBonus = data["totalDefenseBonus"]?.ToObject<int>() ?? 0;
            int totalHealthBonus = data["totalHealthBonus"]?.ToObject<int>() ?? 0;
            int totalManaBonus = data["totalManaBonus"]?.ToObject<int>() ?? 0;

            // Текущие значения
            float health = data["health"]?.ToObject<float>() ?? 0f;
            float maxHealth = data["maxHealth"]?.ToObject<float>() ?? 0f;
            float mana = data["mana"]?.ToObject<float>() ?? 0f;
            float maxMana = data["maxMana"]?.ToObject<float>() ?? 0f;
            int attack = data["attack"]?.ToObject<int>() ?? 0;
            int defense = data["defense"]?.ToObject<int>() ?? 0;

            Debug.Log($"[EquipmentSync] ⚔️ {username} {(isEquip ? "equipped" : "unequipped")} {itemName} in {slotType} slot");
            Debug.Log($"[EquipmentSync] 📊 Item bonuses: ATK+{attackBonus} DEF+{defenseBonus} HP+{healthBonus} MP+{manaBonus}");
            Debug.Log($"[EquipmentSync] 📊 Total bonuses: ATK+{totalAttackBonus} DEF+{totalDefenseBonus} HP+{totalHealthBonus} MP+{totalManaBonus}");

            // Не обрабатываем свою собственную экипировку
            if (socketId == socketManager.SocketId)
            {
                Debug.Log($"[EquipmentSync] ⏭️ Skipping own equipment change");
                return;
            }

            // Находим NetworkPlayerEntity по socketId
            NetworkPlayerEntity[] allPlayers = FindObjectsOfType<NetworkPlayerEntity>();
            NetworkPlayerEntity targetPlayer = null;

            foreach (var player in allPlayers)
            {
                // Получаем NetworkPlayer компонент для доступа к socketId
                NetworkPlayer netPlayer = player.GetComponent<NetworkPlayer>();
                if (netPlayer != null && netPlayer.socketId == socketId)
                {
                    targetPlayer = player;
                    break;
                }
            }

            if (targetPlayer == null)
            {
                Debug.LogWarning($"[EquipmentSync] ⚠️ Player not found: {socketId}");
                return;
            }

            // Обновляем HP и Mana
            HealthSystem healthSystem = targetPlayer.GetComponent<HealthSystem>();
            if (healthSystem != null)
            {
                healthSystem.SetMaxHealth(maxHealth);
                // Сохраняем текущий процент HP
                float currentHealthPercent = healthSystem.CurrentHealth / healthSystem.MaxHealth;
                // Применяем новое значение здоровья от сервера
                healthSystem.SetHealth(health);

                Debug.Log($"[EquipmentSync] ❤️ Updated {username} HP: {health}/{maxHealth}");
            }
            else
            {
                Debug.LogWarning($"[EquipmentSync] ⚠️ HealthSystem not found on {username}");
            }

            ManaSystem manaSystem = targetPlayer.GetComponent<ManaSystem>();
            if (manaSystem != null)
            {
                manaSystem.SetMaxMana(maxMana);
                // Применяем новое значение маны от сервера
                manaSystem.SetMana(mana);

                Debug.Log($"[EquipmentSync] 💙 Updated {username} Mana: {mana}/{maxMana}");
            }
            else
            {
                Debug.LogWarning($"[EquipmentSync] ⚠️ ManaSystem not found on {username}");
            }

            // Обновляем атаку и защиту через CharacterStats
            CharacterStats characterStats = targetPlayer.GetComponent<CharacterStats>();
            if (characterStats != null)
            {
                // CharacterStats должен пересчитать статы с учетом бонусов экипировки
                // Вызываем обновление статов
                characterStats.RecalculateStats();

                Debug.Log($"[EquipmentSync] ⚔️🛡️ Updated {username} stats: ATK={attack} DEF={defense}");
            }
            else
            {
                Debug.LogWarning($"[EquipmentSync] ⚠️ CharacterStats not found on {username}");
            }

            Debug.Log($"[EquipmentSync] ✅ {username} equipment update applied");

        }
        catch (System.Exception e)
        {
            Debug.LogError($"[EquipmentSync] ❌ Error processing player_equipment_changed: {e.Message}");
        }
    }

    void OnDestroy()
    {
        // Отписки не требуется - SocketIOManager сам управляет подписками
        // При уничтожении SocketIOManager все подписки удаляются автоматически
    }
}
