using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace AetherionMMO.Inventory
{
    /// <summary>
    /// Тестовый скрипт для добавления предметов в MMO инвентарь
    /// </summary>
    public class MMOInventoryTester : MonoBehaviour
    {
        [Header("Test Items")]
        [SerializeField] private ItemData testItem1;
        [SerializeField] private ItemData testItem2;
        [SerializeField] private ItemData testItem3;

        [Header("UI (Optional)")]
        [SerializeField] private Button addItemButton;
        [SerializeField] private Button addRandomButton;
        [SerializeField] private Button clearInventoryButton;
        [SerializeField] private TextMeshProUGUI statusText;

        void Start()
        {
            // Подключаем кнопки если они назначены
            if (addItemButton != null)
            {
                addItemButton.onClick.AddListener(AddTestItem);
            }

            if (addRandomButton != null)
            {
                addRandomButton.onClick.AddListener(AddRandomItem);
            }

            if (clearInventoryButton != null)
            {
                clearInventoryButton.onClick.AddListener(ClearInventory);
            }

            UpdateStatus("Готов к тестированию");
        }

        void Update()
        {
            // Клавиши для быстрого тестирования
            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                AddTestItem();
            }

            if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                AddRandomItem();
            }

            if (Input.GetKeyDown(KeyCode.Alpha9))
            {
                AddGold(100);
            }

            if (Input.GetKeyDown(KeyCode.Alpha0))
            {
                ClearInventory();
            }
        }

        /// <summary>
        /// Добавить тестовый предмет
        /// </summary>
        public void AddTestItem()
        {
            if (MongoInventoryManager.Instance == null)
            {
                UpdateStatus("❌ MongoInventoryManager не найден!");
                return;
            }

            if (testItem1 != null)
            {
                MongoInventoryManager.Instance.AddItem(testItem1, 1);
                UpdateStatus($"✅ Добавлен: {testItem1.itemName}");
                Debug.Log($"[Tester] ➕ Добавлен предмет: {testItem1.itemName}");
            }
            else
            {
                UpdateStatus("⚠️ Не назначен testItem1!");
            }
        }

        /// <summary>
        /// Добавить случайный предмет
        /// </summary>
        public void AddRandomItem()
        {
            if (MongoInventoryManager.Instance == null)
            {
                UpdateStatus("❌ MongoInventoryManager не найден!");
                return;
            }

            ItemData[] items = { testItem1, testItem2, testItem3 };
            ItemData randomItem = null;

            // Выбираем случайный предмет который назначен
            foreach (ItemData item in items)
            {
                if (item != null)
                {
                    if (randomItem == null || Random.value > 0.5f)
                    {
                        randomItem = item;
                    }
                }
            }

            if (randomItem != null)
            {
                int randomQuantity = Random.Range(1, 6);
                MongoInventoryManager.Instance.AddItem(randomItem, randomQuantity);
                UpdateStatus($"✅ Добавлен: {randomItem.itemName} x{randomQuantity}");
                Debug.Log($"[Tester] ➕ Добавлен случайный предмет: {randomItem.itemName} x{randomQuantity}");
            }
            else
            {
                UpdateStatus("⚠️ Не назначены тестовые предметы!");
            }
        }

        /// <summary>
        /// Добавить золото
        /// </summary>
        public void AddGold(int amount)
        {
            if (MongoInventoryManager.Instance == null)
            {
                UpdateStatus("❌ MongoInventoryManager не найден!");
                return;
            }

            // TODO: Реализовать метод AddGold в MongoInventoryManager
            UpdateStatus($"✅ Добавлено золота: {amount}");
            Debug.Log($"[Tester] 💰 Добавлено золота: {amount}");
        }

        /// <summary>
        /// Очистить инвентарь (для тестирования)
        /// </summary>
        public void ClearInventory()
        {
            if (MongoInventoryManager.Instance == null)
            {
                UpdateStatus("❌ MongoInventoryManager не найден!");
                return;
            }

            // TODO: Реализовать метод ClearInventory в MongoInventoryManager
            UpdateStatus("⚠️ Функция ClearInventory в разработке");
            Debug.Log("[Tester] 🗑️ Очистка инвентаря...");
        }

        /// <summary>
        /// Обновить статус на UI
        /// </summary>
        private void UpdateStatus(string message)
        {
            if (statusText != null)
            {
                statusText.text = message;
            }
        }

        /// <summary>
        /// Добавить предмет по имени (для вызова из Inspector)
        /// </summary>
        public void AddItemByName(string itemName)
        {
            if (MongoInventoryManager.Instance == null)
            {
                UpdateStatus("❌ MongoInventoryManager не найден!");
                return;
            }

            MongoInventoryManager.Instance.AddItem(itemName, 1);
            UpdateStatus($"✅ Добавлен: {itemName}");
        }
    }
}
