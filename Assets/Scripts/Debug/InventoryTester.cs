using UnityEngine;

/// <summary>
/// Тестер инвентаря - добавляет предметы по нажатию клавиш
/// Полезно для тестирования системы инвентаря
/// </summary>
public class InventoryTester : MonoBehaviour
{
    [Header("Test Items")]
    [Tooltip("Нажмите 1 чтобы добавить")]
    public ItemData testWeapon;

    [Tooltip("Нажмите 2 чтобы добавить")]
    public ItemData testArmor;

    [Tooltip("Нажмите 3 чтобы добавить")]
    public ItemData testHelmet;

    [Tooltip("Нажмите 4 чтобы добавить")]
    public ItemData testAccessory;

    [Tooltip("Нажмите 5 чтобы добавить")]
    public ItemData testPotion;

    [Header("Settings")]
    [Tooltip("Количество зелий для добавления")]
    public int potionQuantity = 5;

    void Update()
    {
        if (InventoryManager.Instance == null)
        {
            Debug.LogWarning("[InventoryTester] InventoryManager не найден!");
            return;
        }

        // Клавиша 1 - добавить оружие
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            if (testWeapon != null)
            {
                InventoryManager.Instance.AddItem(testWeapon, 1);
                Debug.Log($"[InventoryTester] ✅ Добавлено: {testWeapon.itemName}");
            }
            else
            {
                Debug.LogWarning("[InventoryTester] Test Weapon не назначен!");
            }
        }

        // Клавиша 2 - добавить броню
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            if (testArmor != null)
            {
                InventoryManager.Instance.AddItem(testArmor, 1);
                Debug.Log($"[InventoryTester] ✅ Добавлено: {testArmor.itemName}");
            }
            else
            {
                Debug.LogWarning("[InventoryTester] Test Armor не назначен!");
            }
        }

        // Клавиша 3 - добавить шлем
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            if (testHelmet != null)
            {
                InventoryManager.Instance.AddItem(testHelmet, 1);
                Debug.Log($"[InventoryTester] ✅ Добавлено: {testHelmet.itemName}");
            }
            else
            {
                Debug.LogWarning("[InventoryTester] Test Helmet не назначен!");
            }
        }

        // Клавиша 4 - добавить аксессуар
        if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            if (testAccessory != null)
            {
                InventoryManager.Instance.AddItem(testAccessory, 1);
                Debug.Log($"[InventoryTester] ✅ Добавлено: {testAccessory.itemName}");
            }
            else
            {
                Debug.LogWarning("[InventoryTester] Test Accessory не назначен!");
            }
        }

        // Клавиша 5 - добавить зелья
        if (Input.GetKeyDown(KeyCode.Alpha5))
        {
            if (testPotion != null)
            {
                InventoryManager.Instance.AddItem(testPotion, potionQuantity);
                Debug.Log($"[InventoryTester] ✅ Добавлено: {potionQuantity}x {testPotion.itemName}");
            }
            else
            {
                Debug.LogWarning("[InventoryTester] Test Potion не назначен!");
            }
        }

        // Клавиша C - очистить инвентарь
        if (Input.GetKeyDown(KeyCode.C))
        {
            Debug.Log("[InventoryTester] Очистка инвентаря...");
            // TODO: Добавить метод ClearInventory в InventoryManager
        }

        // Клавиша F9 - принудительная синхронизация с сервером
        if (Input.GetKeyDown(KeyCode.F9))
        {
            Debug.Log("[InventoryTester] 🔄 Принудительная синхронизация с сервером...");
            if (InventoryManager.Instance != null)
            {
                InventoryManager.Instance.SyncInventoryToServer();
            }
            else
            {
                Debug.LogError("[InventoryTester] ❌ InventoryManager.Instance == null!");
            }
        }

        // Клавиша F10 - загрузить инвентарь с сервера
        if (Input.GetKeyDown(KeyCode.F10))
        {
            Debug.Log("[InventoryTester] 📥 Загрузка инвентаря с сервера...");
            if (InventoryManager.Instance != null)
            {
                InventoryManager.Instance.LoadInventoryFromServer();
            }
            else
            {
                Debug.LogError("[InventoryTester] ❌ InventoryManager.Instance == null!");
            }
        }
    }

    void OnGUI()
    {
        // Показываем подсказки на экране
        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.fontSize = 14;
        style.normal.textColor = Color.yellow;

        GUI.Label(new Rect(10, 10, 400, 200),
            "ИНВЕНТАРЬ ТЕСТЕР:\n\n" +
            "1 - Добавить оружие\n" +
            "2 - Добавить броню\n" +
            "3 - Добавить шлем\n" +
            "4 - Добавить аксессуар\n" +
            "5 - Добавить зелья\n" +
            "I - Открыть/закрыть инвентарь\n" +
            "F9 - СИНХРОНИЗИРОВАТЬ с сервером\n" +
            "F10 - ЗАГРУЗИТЬ с сервера\n" +
            "C - Очистить инвентарь",
            style
        );
    }
}
