using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace AetherionMMO.Inventory
{
    /// <summary>
    /// MMO-style менеджер инвентаря с полной синхронизацией MongoDB
    /// Вдохновлён World of Warcraft
    /// </summary>
    public class MongoInventoryManager : MonoBehaviour
    {
        public static MongoInventoryManager Instance { get; private set; }

        [Header("Inventory Settings")]
        [SerializeField] private int maxSlots = 40;
        [SerializeField] private int rowSize = 8; // 8 слотов в ряд (как в WoW)

        [Header("UI References")]
        [SerializeField] private GameObject inventoryPanel;
        [SerializeField] private Transform slotsContainer;
        [SerializeField] private GameObject slotPrefab;
        [SerializeField] private TextMeshProUGUI goldText;
        [SerializeField] private Button closeButton;

        [Header("Item Database")]
        [SerializeField] private List<ItemData> itemDatabase = new List<ItemData>();

        [Header("Drag & Drop")]
        [SerializeField] private GameObject dragPreviewPrefab;

        // Внутренние данные
        private List<MMOInventorySlot> slots = new List<MMOInventorySlot>();
        private MMOInventorySnapshot currentSnapshot;
        private bool isOpen = false;
        private bool isLoadingFromServer = false;
        private string characterClass = "";
        private int currentGold = 0;

        // Drag & Drop
        private MMOInventorySlot draggedSlot = null;
        private GameObject dragPreview = null;

        void Awake()
        {
            // Singleton pattern
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            Debug.Log("[MongoInventory] ✅ Singleton initialized");
        }

        void Start()
        {
            InitializeUI();
            LoadCharacterClass();
            RegisterSocketEvents();

            // НОВАЯ ЛОГИКА:
            // 1. Сначала загружаем из PlayerPrefs (offline кэш)
            LoadInventoryFromPlayerPrefs();

            // 2. Потом пробуем синхронизировать с сервером (если подключен)
            Invoke(nameof(LoadInventoryFromServer), 1f);
        }

        void Update()
        {
            // Открыть/закрыть инвентарь клавишей I или B
            if (Input.GetKeyDown(KeyCode.I) || Input.GetKeyDown(KeyCode.B))
            {
                ToggleInventory();
            }
        }

        void OnDestroy()
        {
            UnregisterSocketEvents();
        }

        void OnApplicationQuit()
        {
            // Сохраняем инвентарь перед выходом
            SaveInventoryToPlayerPrefs();
            Debug.Log("[MongoInventory] 💾 Инвентарь сохранён перед выходом из приложения");
        }

        /// <summary>
        /// Инициализация UI
        /// </summary>
        private void InitializeUI()
        {
            if (inventoryPanel != null)
            {
                inventoryPanel.SetActive(false);
            }

            if (closeButton != null)
            {
                closeButton.onClick.AddListener(CloseInventory);
            }

            CreateSlots();
            UpdateGoldDisplay();
        }

        /// <summary>
        /// Создание слотов инвентаря
        /// </summary>
        private void CreateSlots()
        {
            if (slotsContainer == null || slotPrefab == null)
            {
                Debug.LogError("[MongoInventory] ❌ SlotsContainer или SlotPrefab не назначены!");
                return;
            }

            // Очищаем существующие слоты
            foreach (Transform child in slotsContainer)
            {
                Destroy(child.gameObject);
            }
            slots.Clear();

            // Создаём новые слоты
            for (int i = 0; i < maxSlots; i++)
            {
                GameObject slotObj = Instantiate(slotPrefab, slotsContainer);
                MMOInventorySlot slot = slotObj.GetComponent<MMOInventorySlot>();

                if (slot != null)
                {
                    slot.Initialize(i, this);
                    slots.Add(slot);
                }
                else
                {
                    Debug.LogError($"[MongoInventory] ❌ Prefab не содержит MMOInventorySlot компонент!");
                }
            }

            Debug.Log($"[MongoInventory] ✅ Создано {slots.Count} слотов");
        }

        /// <summary>
        /// Загрузить класс персонажа из PlayerPrefs
        /// </summary>
        private void LoadCharacterClass()
        {
            characterClass = PlayerPrefs.GetString("SelectedCharacterClass", "");
            if (string.IsNullOrEmpty(characterClass))
            {
                characterClass = PlayerPrefs.GetString("SelectedClass", "");
            }

            if (string.IsNullOrEmpty(characterClass))
            {
                Debug.LogWarning("[MongoInventory] ⚠️ Класс персонажа не найден в PlayerPrefs!");
            }
            else
            {
                Debug.Log($"[MongoInventory] 📋 Класс персонажа: {characterClass}");
            }
        }

        /// <summary>
        /// Регистрация Socket.IO событий
        /// </summary>
        private void RegisterSocketEvents()
        {
            if (SocketIOManager.Instance == null)
            {
                Debug.LogWarning("[MongoInventory] ⚠️ SocketIOManager не найден!");
                return;
            }

            // Будем регистрировать события для новых эндпоинтов
            // mmo_inventory_loaded, mmo_inventory_updated и т.д.
        }

        /// <summary>
        /// Отмена регистрации Socket.IO событий
        /// </summary>
        private void UnregisterSocketEvents()
        {
            // Отписываемся от событий
        }

        /// <summary>
        /// Открыть/закрыть инвентарь
        /// </summary>
        public void ToggleInventory()
        {
            if (isOpen)
            {
                CloseInventory();
            }
            else
            {
                OpenInventory();
            }
        }

        /// <summary>
        /// Открыть инвентарь
        /// </summary>
        public void OpenInventory()
        {
            if (inventoryPanel != null)
            {
                inventoryPanel.SetActive(true);
                isOpen = true;
                Debug.Log("[MongoInventory] 📂 Инвентарь открыт");
            }
        }

        /// <summary>
        /// Закрыть инвентарь
        /// </summary>
        public void CloseInventory()
        {
            if (inventoryPanel != null)
            {
                inventoryPanel.SetActive(false);
                isOpen = false;
                Debug.Log("[MongoInventory] 📁 Инвентарь закрыт");
            }
        }

        /// <summary>
        /// Загрузить инвентарь с сервера
        /// </summary>
        public void LoadInventoryFromServer()
        {
            if (SocketIOManager.Instance == null || !SocketIOManager.Instance.IsConnected)
            {
                Debug.LogWarning("[MongoInventory] ⚠️ Не подключен к серверу!");
                Invoke(nameof(LoadInventoryFromServer), 2f); // Retry
                return;
            }

            if (string.IsNullOrEmpty(characterClass))
            {
                Debug.LogError("[MongoInventory] ❌ Класс персонажа не задан!");
                return;
            }

            Debug.Log($"[MongoInventory] 📥 Загрузка инвентаря для {characterClass}...");

            isLoadingFromServer = true;

            // Отправляем запрос на сервер
            var request = new { characterClass = characterClass };
            string json = JsonUtility.ToJson(request);

            SocketIOManager.Instance.EmitCustomEvent("mmo_load_inventory", json, (response) =>
            {
                isLoadingFromServer = false;
                HandleInventoryLoaded(response);
            });
        }

        /// <summary>
        /// Обработка загруженного инвентаря
        /// </summary>
        private void HandleInventoryLoaded(string jsonResponse)
        {
            try
            {
                MMOInventoryResponse response = JsonUtility.FromJson<MMOInventoryResponse>(jsonResponse);

                if (!response.success)
                {
                    Debug.LogError($"[MongoInventory] ❌ Ошибка загрузки: {response.message}");
                    return;
                }

                currentSnapshot = response.snapshot;
                ApplySnapshot(currentSnapshot);

                Debug.Log($"[MongoInventory] ✅ Инвентарь загружен: {currentSnapshot.items.Count} предметов, {currentSnapshot.gold} золота");
            }
            catch (Exception e)
            {
                Debug.LogError($"[MongoInventory] ❌ Ошибка парсинга: {e.Message}");
            }
        }

        /// <summary>
        /// Применить snapshot к UI
        /// </summary>
        private void ApplySnapshot(MMOInventorySnapshot snapshot)
        {
            // Очищаем все слоты
            ClearAllSlots();

            // Заполняем слоты из snapshot
            foreach (MMOItemStack itemStack in snapshot.items)
            {
                if (itemStack.slotIndex >= 0 && itemStack.slotIndex < slots.Count)
                {
                    ItemData itemData = FindItemById(itemStack.itemId);
                    if (itemData == null)
                    {
                        itemData = FindItemByName(itemStack.itemName);
                    }

                    if (itemData != null)
                    {
                        slots[itemStack.slotIndex].SetItem(itemData, itemStack.quantity);
                        Debug.Log($"[MongoInventory] ✅ Установлен предмет в слот {itemStack.slotIndex}: {itemData.itemName} x{itemStack.quantity}, icon={itemData.icon?.name ?? "NULL"}");
                    }
                    else
                    {
                        Debug.LogWarning($"[MongoInventory] ⚠️ Предмет не найден: {itemStack.itemName} ({itemStack.itemId})");
                    }
                }
            }

            // Обновляем золото
            currentGold = snapshot.gold;
            UpdateGoldDisplay();
        }

        /// <summary>
        /// Очистить все слоты
        /// </summary>
        private void ClearAllSlots()
        {
            foreach (MMOInventorySlot slot in slots)
            {
                slot.Clear();
            }
        }

        /// <summary>
        /// Обновить отображение золота
        /// </summary>
        private void UpdateGoldDisplay()
        {
            if (goldText != null)
            {
                goldText.text = $"{currentGold:N0}";
            }
        }

        /// <summary>
        /// Сохранить инвентарь в PlayerPrefs (для offline режима)
        /// </summary>
        public void SaveInventoryToPlayerPrefs()
        {
            if (string.IsNullOrEmpty(characterClass))
            {
                Debug.LogWarning("[MongoInventory] ⚠️ Не могу сохранить - класс персонажа не задан");
                return;
            }

            // Создаём snapshot текущего состояния
            var snapshot = new MMOInventorySnapshot
            {
                items = new List<MMOItemStack>(),
                equipment = new MMOEquipmentData(),
                gold = currentGold,
                lastModified = System.DateTimeOffset.Now.ToUnixTimeMilliseconds()
            };

            // Собираем предметы из слотов
            for (int i = 0; i < slots.Count; i++)
            {
                if (!slots[i].IsEmpty())
                {
                    var item = slots[i].CurrentItem;
                    var quantity = slots[i].CurrentQuantity;

                    snapshot.items.Add(new MMOItemStack
                    {
                        itemId = item.ItemId,
                        itemName = item.itemName,
                        quantity = quantity,
                        slotIndex = i,
                        timestamp = System.DateTimeOffset.Now.ToUnixTimeMilliseconds()
                    });
                }
            }

            // Сериализуем в JSON
            string json = JsonUtility.ToJson(new MMOInventorySnapshotWrapper { snapshot = snapshot });
            string key = $"Inventory_{characterClass}";

            PlayerPrefs.SetString(key, json);
            PlayerPrefs.Save();

            Debug.Log($"[MongoInventory] 💾 Инвентарь сохранён локально: {snapshot.items.Count} предметов");
        }

        /// <summary>
        /// Загрузить инвентарь из PlayerPrefs (для offline режима)
        /// </summary>
        public void LoadInventoryFromPlayerPrefs()
        {
            if (string.IsNullOrEmpty(characterClass))
            {
                Debug.LogWarning("[MongoInventory] ⚠️ Не могу загрузить - класс персонажа не задан");
                return;
            }

            string key = $"Inventory_{characterClass}";

            if (!PlayerPrefs.HasKey(key))
            {
                Debug.Log($"[MongoInventory] ℹ️ Нет сохранённого инвентаря для {characterClass}");
                return;
            }

            string json = PlayerPrefs.GetString(key);

            try
            {
                var wrapper = JsonUtility.FromJson<MMOInventorySnapshotWrapper>(json);
                if (wrapper != null && wrapper.snapshot != null)
                {
                    ApplySnapshot(wrapper.snapshot);
                    currentSnapshot = wrapper.snapshot;
                    Debug.Log($"[MongoInventory] 📂 Инвентарь загружен из PlayerPrefs: {wrapper.snapshot.items.Count} предметов");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[MongoInventory] ❌ Ошибка загрузки из PlayerPrefs: {e.Message}");
            }
        }

        /// <summary>
        /// Найти предмет по ID
        /// </summary>
        private ItemData FindItemById(string itemId)
        {
            return itemDatabase.FirstOrDefault(item => item.ItemId == itemId);
        }

        /// <summary>
        /// Найти предмет по имени (fallback)
        /// </summary>
        private ItemData FindItemByName(string itemName)
        {
            return itemDatabase.FirstOrDefault(item => item.itemName == itemName);
        }

        // ═══════════════════════════════════════════
        // PUBLIC API ДЛЯ ВЗАИМОДЕЙСТВИЯ
        // ═══════════════════════════════════════════

        /// <summary>
        /// Добавить предмет в инвентарь
        /// </summary>
        public void AddItem(string itemName, int quantity = 1)
        {
            ItemData item = FindItemByName(itemName);
            if (item == null)
            {
                Debug.LogError($"[MongoInventory] ❌ Предмет не найден: {itemName}");
                return;
            }

            AddItem(item, quantity);
        }

        /// <summary>
        /// Добавить предмет в инвентарь
        /// </summary>
        public void AddItem(ItemData item, int quantity = 1)
        {
            Debug.Log($"[MongoInventory] 🔥 AddItem() called with item={item?.itemName ?? "NULL"}, quantity={quantity}");

            if (item == null)
            {
                Debug.LogError("[MongoInventory] ❌ Item is null!");
                return;
            }

            // Ищем пустой слот
            int emptySlotIndex = FindEmptySlot();
            Debug.Log($"[MongoInventory] 🔍 FindEmptySlot() returned: {emptySlotIndex}");

            if (emptySlotIndex == -1)
            {
                Debug.LogWarning("[MongoInventory] ⚠️ Инвентарь полон!");
                return;
            }

            Debug.Log($"[MongoInventory] 📤 Добавление предмета: {item.itemName} x{quantity} в слот {emptySlotIndex}");

            // ВАЖНО: Проверяем подключение к серверу
            bool hasSocketManager = SocketIOManager.Instance != null;
            bool isConnected = hasSocketManager && SocketIOManager.Instance.IsConnected;

            Debug.Log($"[MongoInventory] 🌐 SocketIOManager.Instance: {(hasSocketManager ? "EXISTS" : "NULL")}");
            Debug.Log($"[MongoInventory] 🔌 IsConnected: {isConnected}");

            if (hasSocketManager && isConnected)
            {
                // Отправляем запрос на сервер
                var request = new AddItemRequest
                {
                    characterClass = characterClass,
                    itemId = item.ItemId,
                    itemName = item.itemName,
                    quantity = quantity,
                    slotIndex = emptySlotIndex
                };

                string json = JsonUtility.ToJson(request);
                Debug.Log($"[MongoInventory] 📋 Request JSON: {json}");

                SocketIOManager.Instance.EmitCustomEvent("mmo_add_item", json, (response) =>
                {
                    Debug.Log($"[MongoInventory] 📥 Response received from server");
                    HandleInventoryUpdated(response);
                });

                Debug.Log($"[MongoInventory] 📡 Запрос отправлен на сервер");
            }
            else
            {
                // РЕЖИМ БЕЗ СЕРВЕРА: Обновляем UI локально для тестирования
                Debug.LogWarning("[MongoInventory] ⚠️ Сервер не подключен - локальное обновление UI");
                Debug.Log($"[MongoInventory] 📊 Slots count: {slots.Count}, emptySlotIndex: {emptySlotIndex}");

                if (emptySlotIndex >= 0 && emptySlotIndex < slots.Count)
                {
                    Debug.Log($"[MongoInventory] 🎯 Setting item in slot {emptySlotIndex}...");
                    slots[emptySlotIndex].SetItem(item, quantity);
                    Debug.Log($"[MongoInventory] ✅ Локально установлен предмет в слот {emptySlotIndex}: {item.itemName} x{quantity}");

                    // Автосохранение в PlayerPrefs
                    SaveInventoryToPlayerPrefs();
                }
                else
                {
                    Debug.LogError($"[MongoInventory] ❌ Invalid slot index! emptySlotIndex={emptySlotIndex}, slots.Count={slots.Count}");
                }
            }
        }

        /// <summary>
        /// Переместить предмет (drag-drop)
        /// </summary>
        public void MoveItem(int fromSlot, int toSlot)
        {
            if (fromSlot == toSlot)
                return;

            Debug.Log($"[MongoInventory] 🔄 Перемещение предмета: слот {fromSlot} → {toSlot}");

            if (SocketIOManager.Instance != null && SocketIOManager.Instance.IsConnected)
            {
                var request = new MoveItemRequest
                {
                    characterClass = characterClass,
                    fromSlot = fromSlot,
                    toSlot = toSlot
                };

                string json = JsonUtility.ToJson(request);

                SocketIOManager.Instance.EmitCustomEvent("mmo_move_item", json, (response) =>
                {
                    HandleInventoryUpdated(response);
                });
            }
            else
            {
                // РЕЖИМ БЕЗ СЕРВЕРА: Локальное перемещение
                Debug.LogWarning("[MongoInventory] ⚠️ Сервер не подключен - локальное перемещение");

                if (fromSlot >= 0 && fromSlot < slots.Count && toSlot >= 0 && toSlot < slots.Count)
                {
                    // Меняем местами предметы
                    var fromItem = slots[fromSlot].CurrentItem;
                    var fromQty = slots[fromSlot].CurrentQuantity;
                    var toItem = slots[toSlot].CurrentItem;
                    var toQty = slots[toSlot].CurrentQuantity;

                    slots[fromSlot].SetItem(toItem, toQty);
                    slots[toSlot].SetItem(fromItem, fromQty);

                    Debug.Log($"[MongoInventory] ✅ Локально перемещено: {fromSlot} ↔ {toSlot}");

                    // Автосохранение в PlayerPrefs
                    SaveInventoryToPlayerPrefs();
                }
            }
        }

        /// <summary>
        /// Удалить предмет
        /// </summary>
        public void RemoveItem(int slotIndex, int quantity = 0)
        {
            Debug.Log($"[MongoInventory] 🗑️ Удаление предмета из слота {slotIndex}");

            if (SocketIOManager.Instance != null && SocketIOManager.Instance.IsConnected)
            {
                var request = new RemoveItemRequest
                {
                    characterClass = characterClass,
                    slotIndex = slotIndex,
                    quantity = quantity // 0 = удалить всё
                };

                string json = JsonUtility.ToJson(request);

                SocketIOManager.Instance.EmitCustomEvent("mmo_remove_item", json, (response) =>
                {
                    HandleInventoryUpdated(response);
                });
            }
            else
            {
                // РЕЖИМ БЕЗ СЕРВЕРА: Локальное удаление
                Debug.LogWarning("[MongoInventory] ⚠️ Сервер не подключен - локальное удаление");

                if (slotIndex >= 0 && slotIndex < slots.Count)
                {
                    slots[slotIndex].Clear();
                    Debug.Log($"[MongoInventory] ✅ Локально удалено из слота {slotIndex}");

                    // Автосохранение в PlayerPrefs
                    SaveInventoryToPlayerPrefs();
                }
            }
        }

        /// <summary>
        /// Обработка обновления инвентаря
        /// </summary>
        private void HandleInventoryUpdated(string jsonResponse)
        {
            try
            {
                MMOInventoryResponse response = JsonUtility.FromJson<MMOInventoryResponse>(jsonResponse);

                if (!response.success)
                {
                    Debug.LogError($"[MongoInventory] ❌ Ошибка обновления: {response.message}");
                    return;
                }

                currentSnapshot = response.snapshot;
                ApplySnapshot(currentSnapshot);

                Debug.Log($"[MongoInventory] ✅ Инвентарь обновлён успешно");
            }
            catch (Exception e)
            {
                Debug.LogError($"[MongoInventory] ❌ Ошибка парсинга: {e.Message}");
            }
        }

        /// <summary>
        /// Найти пустой слот
        /// </summary>
        private int FindEmptySlot()
        {
            for (int i = 0; i < slots.Count; i++)
            {
                if (slots[i].IsEmpty())
                {
                    return i;
                }
            }
            return -1;
        }

        /// <summary>
        /// Начать перетаскивание
        /// </summary>
        public void StartDrag(MMOInventorySlot slot)
        {
            if (slot.IsEmpty())
                return;

            draggedSlot = slot;

            // Создаём визуальный preview
            if (dragPreviewPrefab != null)
            {
                dragPreview = Instantiate(dragPreviewPrefab, transform);
                // Настроить preview (иконку, количество и т.д.)
            }

            Debug.Log($"[MongoInventory] 🖱️ Начато перетаскивание из слота {slot.SlotIndex}");
        }

        /// <summary>
        /// Завершить перетаскивание
        /// </summary>
        public void EndDrag(MMOInventorySlot targetSlot)
        {
            if (draggedSlot == null)
                return;

            if (targetSlot != null)
            {
                MoveItem(draggedSlot.SlotIndex, targetSlot.SlotIndex);
            }

            // Удаляем preview
            if (dragPreview != null)
            {
                Destroy(dragPreview);
                dragPreview = null;
            }

            draggedSlot = null;
        }

        /// <summary>
        /// Получить слот по индексу
        /// </summary>
        public MMOInventorySlot GetSlot(int index)
        {
            if (index >= 0 && index < slots.Count)
            {
                return slots[index];
            }
            return null;
        }
    }
}
