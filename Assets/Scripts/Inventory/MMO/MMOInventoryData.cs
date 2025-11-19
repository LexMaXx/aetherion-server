using System;
using System.Collections.Generic;
using UnityEngine;

namespace AetherionMMO.Inventory
{
    /// <summary>
    /// Данные предмета в слоте (для MongoDB)
    /// </summary>
    [Serializable]
    public class MMOItemStack
    {
        public string itemId;           // GUID предмета
        public string itemName;         // Название (fallback)
        public int quantity;            // Количество
        public int slotIndex;           // Индекс слота (0-39)
        public long timestamp;          // Время добавления

        public MMOItemStack()
        {
            itemId = "";
            itemName = "";
            quantity = 0;
            slotIndex = -1;
            timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        }

        public MMOItemStack(string itemId, string itemName, int quantity, int slotIndex)
        {
            this.itemId = itemId;
            this.itemName = itemName;
            this.quantity = quantity;
            this.slotIndex = slotIndex;
            this.timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        }
    }

    /// <summary>
    /// Данные экипировки (для MongoDB)
    /// </summary>
    [Serializable]
    public class MMOEquipmentData
    {
        public string weapon;
        public string armor;
        public string helmet;
        public string accessory;
        public string ring1;
        public string ring2;
        public string trinket1;
        public string trinket2;

        public MMOEquipmentData()
        {
            weapon = "";
            armor = "";
            helmet = "";
            accessory = "";
            ring1 = "";
            ring2 = "";
            trinket1 = "";
            trinket2 = "";
        }
    }

    /// <summary>
    /// Полные данные инвентаря (JSON для MongoDB)
    /// </summary>
    [Serializable]
    public class MMOInventorySnapshot
    {
        public List<MMOItemStack> items;
        public MMOEquipmentData equipment;
        public int gold;
        public long lastModified;

        public MMOInventorySnapshot()
        {
            items = new List<MMOItemStack>();
            equipment = new MMOEquipmentData();
            gold = 0;
            lastModified = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        }
    }

    /// <summary>
    /// Запрос на добавление предмета
    /// </summary>
    [Serializable]
    public class AddItemRequest
    {
        public string characterClass;
        public string itemId;
        public string itemName;
        public int quantity;
        public int slotIndex; // -1 = автовыбор
    }

    /// <summary>
    /// Запрос на перемещение предмета (drag-drop)
    /// </summary>
    [Serializable]
    public class MoveItemRequest
    {
        public string characterClass;
        public int fromSlot;
        public int toSlot;
    }

    /// <summary>
    /// Запрос на удаление предмета
    /// </summary>
    [Serializable]
    public class RemoveItemRequest
    {
        public string characterClass;
        public int slotIndex;
        public int quantity; // 0 = удалить всё
    }

    /// <summary>
    /// Ответ от сервера
    /// </summary>
    [Serializable]
    public class MMOInventoryResponse
    {
        public bool success;
        public string message;
        public MMOInventorySnapshot snapshot; // Новое состояние инвентаря
    }

    /// <summary>
    /// Обёртка для сохранения snapshot в PlayerPrefs
    /// (JsonUtility требует обёртку для сложных объектов)
    /// </summary>
    [Serializable]
    public class MMOInventorySnapshotWrapper
    {
        public MMOInventorySnapshot snapshot;
    }
}
