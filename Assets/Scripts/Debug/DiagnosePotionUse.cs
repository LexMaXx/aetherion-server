using UnityEngine;
using AetherionMMO.Inventory;

/// <summary>
/// Диагностика использования зелий
/// Нажмите P чтобы увидеть информацию о зельях в инвентаре
/// Нажмите O чтобы проверить наличие HealthSystem и ManaSystem
/// </summary>
public class DiagnosePotionUse : MonoBehaviour
{
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            DiagnosePotions();
        }

        if (Input.GetKeyDown(KeyCode.O))
        {
            DiagnoseSystemComponents();
        }
    }

    [ContextMenu("Diagnose System Components")]
    public void DiagnoseSystemComponents()
    {
        Debug.Log("========================================");
        Debug.Log("🔍 ДИАГНОСТИКА КОМПОНЕНТОВ СИСТЕМ");
        Debug.Log("========================================");

        // Ищем HealthSystem
        HealthSystem healthSystem = FindObjectOfType<HealthSystem>();
        if (healthSystem != null)
        {
            Debug.Log($"✅ HealthSystem найден на объекте: {healthSystem.gameObject.name}");
            Debug.Log($"   HP: {healthSystem.CurrentHealth}/{healthSystem.MaxHealth}");
        }
        else
        {
            Debug.LogError("❌ HealthSystem НЕ НАЙДЕН в сцене!");
            Debug.LogError("   БЕЗ HealthSystem зелья HP НЕ БУДУТ УДАЛЯТЬСЯ!");
        }

        // Ищем ManaSystem
        ManaSystem manaSystem = FindObjectOfType<ManaSystem>();
        if (manaSystem != null)
        {
            Debug.Log($"✅ ManaSystem найден на объекте: {manaSystem.gameObject.name}");
            Debug.Log($"   Mana: {manaSystem.CurrentMana}/{manaSystem.MaxMana}");
        }
        else
        {
            Debug.LogError("❌ ManaSystem НЕ НАЙДЕН в сцене!");
            Debug.LogError("   БЕЗ ManaSystem зелья маны НЕ БУДУТ УДАЛЯТЬСЯ!");
        }

        Debug.Log("========================================");
    }

    [ContextMenu("Diagnose Potions")]
    public void DiagnosePotions()
    {
        Debug.Log("========================================");
        Debug.Log("🧪 ДИАГНОСТИКА ЗЕЛИЙ");
        Debug.Log("========================================");

        if (MongoInventoryManager.Instance == null)
        {
            Debug.LogError("❌ MongoInventoryManager.Instance == null!");
            return;
        }

        var slots = MongoInventoryManager.Instance.GetAllSlots();
        if (slots == null)
        {
            Debug.LogError("❌ Slots == null!");
            return;
        }

        Debug.Log($"Всего слотов: {slots.Count}");
        Debug.Log("");

        int potionCount = 0;
        for (int i = 0; i < slots.Count; i++)
        {
            var slot = slots[i];
            if (slot != null && !slot.IsEmpty())
            {
                var item = slot.GetItem();
                if (item != null && item.itemType == ItemType.Consumable)
                {
                    potionCount++;
                    Debug.Log($"🧪 Slot {i}: {item.itemName}");
                    Debug.Log($"   ItemId: {item.ItemId}");
                    Debug.Log($"   Quantity: {slot.GetQuantity()}");
                    Debug.Log($"   HealAmount: {item.healAmount}");
                    Debug.Log($"   ManaRestoreAmount: {item.manaRestoreAmount}");
                    Debug.Log("");
                }
            }
        }

        if (potionCount == 0)
        {
            Debug.LogWarning("⚠️ Нет зелий в инвентаре!");
        }
        else
        {
            Debug.Log($"✅ Найдено зелий: {potionCount}");
        }

        Debug.Log("========================================");
    }

    [ContextMenu("Test Use Health Potion")]
    public void TestUseHealthPotion()
    {
        Debug.Log("🧪 ТЕСТ: Использование Health Potion...");

        if (MongoInventoryManager.Instance == null)
        {
            Debug.LogError("❌ MongoInventoryManager.Instance == null!");
            return;
        }

        var slots = MongoInventoryManager.Instance.GetAllSlots();
        foreach (var slot in slots)
        {
            if (slot != null && !slot.IsEmpty())
            {
                var item = slot.GetItem();
                if (item != null && item.itemName == "Health Potion")
                {
                    Debug.Log($"✅ Нашли Health Potion в слоте. Quantity ДО: {slot.GetQuantity()}");
                    MongoInventoryManager.Instance.UseConsumable(item);
                    Debug.Log($"📊 Quantity ПОСЛЕ: {slot.GetQuantity()}");
                    return;
                }
            }
        }

        Debug.LogError("❌ Health Potion не найден в инвентаре!");
    }
}
