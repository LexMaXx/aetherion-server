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
            Debug.Log("[MongoInventory] ===== START =====");
            Debug.Log($"[MongoInventory] itemDatabase count: {itemDatabase?.Count ?? 0}");
            if (itemDatabase != null && itemDatabase.Count > 0)
            {
                Debug.Log($"[MongoInventory] itemDatabase items: {string.Join(", ", itemDatabase.Select(i => i.itemName))}");
            }
            else
            {
                Debug.LogError("[MongoInventory] ❌ itemDatabase is EMPTY! Items will not be found by ApplySnapshot!");
            }

            InitializeUI();
            LoadCharacterClass();
            RegisterSocketEvents();

            // НОВАЯ ЛОГИКА:
            // Проверяем подключение к серверу
            bool isConnected = SocketIOManager.Instance != null && SocketIOManager.Instance.IsConnected;

            if (isConnected)
            {
                // Если подключены к серверу - загружаем ТОЛЬКО с сервера (не из PlayerPrefs!)
                // УВЕЛИЧЕНА ЗАДЕРЖКА до 2 секунд, чтобы LoadCharacterClass() успел установить characterClass
                Debug.Log("[MongoInventory] 🌐 Подключен к серверу - загружаю инвентарь с MongoDB через 2 секунды...");
                Invoke(nameof(LoadInventoryFromServer), 2f);
            }
            else
            {
                // Если НЕ подключены - используем локальный кэш
                Debug.LogWarning("[MongoInventory] ⚠️ Нет подключения к серверу - загружаю из PlayerPrefs");
                LoadInventoryFromPlayerPrefs();
            }
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
                // FALLBACK для тестирования: используем класс из SelectedCharacterId
                string characterId = PlayerPrefs.GetString("SelectedCharacterId", "");
                if (!string.IsNullOrEmpty(characterId))
                {
                    // Если есть ID персонажа, используем его как класс
                    characterClass = characterId;
                    Debug.LogWarning($"[MongoInventory] ⚠️ Класс не найден, использую SelectedCharacterId: {characterClass}");
                }
                else
                {
                    // Последний fallback - используем "Warrior" для тестирования
                    characterClass = "Warrior";
                    Debug.LogWarning($"[MongoInventory] ⚠️ Класс персонажа не найден в PlayerPrefs! Использую тестовый класс: {characterClass}");
                    Debug.LogWarning("[MongoInventory] 💡 Для корректной работы зайдите через Character Selection!");
                }
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

            // Отправляем запрос на сервер (используем Newtonsoft.Json для правильной сериализации)
            var request = new { characterClass = characterClass };
            string json = Newtonsoft.Json.JsonConvert.SerializeObject(request);

            Debug.Log($"[MongoInventory] 📤 Отправка запроса: {json}");

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
            int oldGold = currentGold;
            Debug.Log($"[MongoInventory] 📸 ApplySnapshot() called with {snapshot.items.Count} items, gold={snapshot.gold}");
            Debug.LogWarning($"[MongoInventory] ⚠️ ВНИМАНИЕ: Золото будет перезаписано с {oldGold} на {snapshot.gold}");

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
                    }
                    else
                    {
                        Debug.LogError($"[MongoInventory] ❌ Предмет не найден в itemDatabase: {itemStack.itemName} ({itemStack.itemId})");
                    }
                }
            }

            // Обновляем золото
            currentGold = snapshot.gold;
            UpdateGoldDisplay();

            Debug.Log($"[MongoInventory] 📸 ApplySnapshot() complete. Золото изменено: {oldGold} → {currentGold}");
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
                Debug.Log($"[MongoInventory] 💵 UpdateGoldDisplay: Отображаю {currentGold} золота");
            }
            else
            {
                Debug.LogWarning("[MongoInventory] ⚠️ goldText == null! UI не обновится");
            }
        }

        /// <summary>
        /// Добавить золото
        /// </summary>
        public void AddGold(int amount)
        {
            Debug.Log($"[MongoInventory] 🔍 AddGold вызван: amount={amount}, currentGold ДО={currentGold}");

            if (amount <= 0)
            {
                Debug.LogWarning($"[MongoInventory] ⚠️ Попытка добавить некорректное количество золота: {amount}");
                return;
            }

            int oldGold = currentGold;
            currentGold += amount;
            UpdateGoldDisplay();

            Debug.Log($"[MongoInventory] 💰 Добавлено золота: +{amount}. Было: {oldGold}, Стало: {currentGold}");

            // Сохраняем на сервер
            SaveInventoryToServer();
        }

        /// <summary>
        /// Убрать золото (для покупок)
        /// </summary>
        public bool RemoveGold(int amount)
        {
            Debug.Log($"[MongoInventory] 🔍 RemoveGold вызван: amount={amount}, currentGold={currentGold}");

            if (amount <= 0)
            {
                Debug.LogWarning($"[MongoInventory] ⚠️ Попытка убрать некорректное количество золота: {amount}");
                return false;
            }

            if (currentGold < amount)
            {
                Debug.LogWarning($"[MongoInventory] ⚠️ Недостаточно золота! Требуется: {amount}, доступно: {currentGold}");
                return false;
            }

            int oldGold = currentGold;
            currentGold -= amount;
            UpdateGoldDisplay();

            Debug.Log($"[MongoInventory] 💰 Снято золота: -{amount}. Было: {oldGold}, Осталось: {currentGold}");

            // Сохраняем на сервер
            SaveInventoryToServer();

            return true;
        }

        /// <summary>
        /// Получить текущее количество золота
        /// </summary>
        public int GetGold()
        {
            return currentGold;
        }

        /// <summary>
        /// Получить все слоты инвентаря (для MerchantUI)
        /// </summary>
        public List<MMOInventorySlot> GetAllSlots()
        {
            return slots;
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
        /// Сохранить инвентарь на сервер (MongoDB)
        /// </summary>
        public void SaveInventoryToServer()
        {
            if (string.IsNullOrEmpty(characterClass))
            {
                Debug.LogWarning("[MongoInventory] ⚠️ Не могу сохранить на сервер - класс персонажа не задан");
                return;
            }

            if (SocketIOManager.Instance == null || !SocketIOManager.Instance.IsConnected)
            {
                Debug.LogWarning("[MongoInventory] ⚠️ Не подключен к серверу, сохраняю только локально");
                SaveInventoryToPlayerPrefs();
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

            // Формируем запрос (используем Newtonsoft.Json для правильной сериализации)
            var saveRequest = new
            {
                characterClass = characterClass,
                inventory = snapshot
            };

            string json = Newtonsoft.Json.JsonConvert.SerializeObject(saveRequest);

            // Отправляем на сервер
            SocketIOManager.Instance.Emit("mmo_save_inventory", json);
            Debug.Log($"[MongoInventory] 📤 Инвентарь отправлен на сервер: {snapshot.items.Count} предметов, {currentGold} золота");

            // Также сохраняем локально (backup)
            SaveInventoryToPlayerPrefs();
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

        /// <summary>
        /// Найти предмет по имени (публичный метод для MMOEquipmentManager)
        /// </summary>
        public ItemData FindItemByNamePublic(string itemName)
        {
            return FindItemByName(itemName);
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

            // СНАЧАЛА добавляем локально в UI
            if (emptySlotIndex >= 0 && emptySlotIndex < slots.Count)
            {
                slots[emptySlotIndex].SetItem(item, quantity);
                Debug.Log($"[MongoInventory] ✅ Локально установлен предмет в слот {emptySlotIndex}: {item.itemName} x{quantity}");
            }

            // ПОТОМ отправляем на сервер (если подключён)
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
                    // НЕ вызываем HandleInventoryUpdated, чтобы не перезаписать золото!
                    // Просто логируем успех
                    try
                    {
                        MMOInventoryResponse res = JsonUtility.FromJson<MMOInventoryResponse>(response);
                        if (res.success)
                        {
                            Debug.Log($"[MongoInventory] ✅ Предмет добавлен на сервере");
                        }
                        else
                        {
                            Debug.LogError($"[MongoInventory] ❌ Ошибка добавления на сервере: {res.message}");
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"[MongoInventory] ❌ Ошибка парсинга ответа: {e.Message}");
                    }
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
                    // НЕ вызываем HandleInventoryUpdated, чтобы не перезаписать золото!
                    // Просто логируем успех
                    try
                    {
                        MMOInventoryResponse res = JsonUtility.FromJson<MMOInventoryResponse>(response);
                        if (res.success)
                        {
                            Debug.Log($"[MongoInventory] ✅ Предмет перемещён на сервере");
                        }
                        else
                        {
                            Debug.LogError($"[MongoInventory] ❌ Ошибка перемещения на сервере: {res.message}");
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"[MongoInventory] ❌ Ошибка парсинга ответа: {e.Message}");
                    }
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
        /// Удалить предмет из слота по индексу
        /// </summary>
        public void RemoveItem(int slotIndex, int quantity = 0)
        {
            Debug.Log($"[MongoInventory] 🗑️ Удаление предмета из слота {slotIndex}");

            // ВАЖНО: Проверяем валидность слота
            if (slotIndex < 0 || slotIndex >= slots.Count)
            {
                Debug.LogError($"[MongoInventory] ❌ Недопустимый индекс слота: {slotIndex}");
                return;
            }

            // ВАЖНО: Проверяем что слот не пустой
            if (slots[slotIndex].IsEmpty())
            {
                Debug.LogWarning($"[MongoInventory] ⚠️ Слот {slotIndex} уже пуст!");
                return;
            }

            // Получаем имя предмета для логирования
            string itemName = slots[slotIndex].GetItem()?.itemName ?? "Unknown";

            if (SocketIOManager.Instance != null && SocketIOManager.Instance.IsConnected)
            {
                var request = new RemoveItemRequest
                {
                    characterClass = characterClass,
                    slotIndex = slotIndex,
                    quantity = quantity // 0 = удалить всё
                };

                string json = JsonUtility.ToJson(request);

                // КРИТИЧЕСКИ ВАЖНО: Удаляем предмет из UI СРАЗУ, не дожидаясь ответа сервера
                // Это предотвращает race condition при двойном клике
                Debug.Log($"[MongoInventory] 🗑️ Локально удаляю {itemName} из слота {slotIndex}...");
                slots[slotIndex].Clear();
                Debug.Log($"[MongoInventory] ✅ UI обновлён, слот {slotIndex} очищен");

                SocketIOManager.Instance.EmitCustomEvent("mmo_remove_item", json, (response) =>
                {
                    // НЕ вызываем HandleInventoryUpdated, чтобы не перезаписать золото!
                    // Просто логируем успех
                    try
                    {
                        MMOInventoryResponse res = JsonUtility.FromJson<MMOInventoryResponse>(response);
                        if (res.success)
                        {
                            Debug.Log($"[MongoInventory] ✅ Предмет {itemName} удалён на сервере");
                        }
                        else
                        {
                            Debug.LogError($"[MongoInventory] ❌ Ошибка удаления на сервере: {res.message}");
                            // TODO: В случае ошибки можно восстановить предмет в UI из snapshot
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"[MongoInventory] ❌ Ошибка парсинга ответа: {e.Message}");
                    }
                });
            }
            else
            {
                // РЕЖИМ БЕЗ СЕРВЕРА: Локальное удаление
                Debug.LogWarning("[MongoInventory] ⚠️ Сервер не подключен - локальное удаление");

                slots[slotIndex].Clear();
                Debug.Log($"[MongoInventory] ✅ Локально удалено {itemName} из слота {slotIndex}");

                // Автосохранение в PlayerPrefs
                SaveInventoryToPlayerPrefs();
            }
        }

        /// <summary>
        /// Удалить предмет из слота (альтернативное имя для совместимости)
        /// </summary>
        private void RemoveItemFromSlot(int slotIndex)
        {
            RemoveItem(slotIndex, 0);
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

        /// <summary>
        /// Удалить предмет из инвентаря
        /// </summary>
        public void RemoveItem(ItemData item, int quantity = 1)
        {
            if (item == null)
            {
                Debug.LogError("[MongoInventory] ❌ Cannot remove null item!");
                return;
            }

            Debug.Log($"[MongoInventory] 🗑️ Removing {quantity}x {item.itemName}");

            // Ищем предмет в инвентаре
            for (int i = 0; i < slots.Count; i++)
            {
                MMOInventorySlot slot = slots[i];
                if (!slot.IsEmpty() && slot.GetItem().ItemId == item.ItemId)
                {
                    int currentQuantity = slot.GetQuantity();

                    if (currentQuantity <= quantity)
                    {
                        // Удаляем весь стак
                        RemoveItemFromSlot(i);
                        Debug.Log($"[MongoInventory] ✅ Removed all {currentQuantity}x {item.itemName} from slot {i}");
                        return;
                    }
                    else
                    {
                        // Уменьшаем количество
                        int newQuantity = currentQuantity - quantity;
                        slot.SetItem(item, newQuantity);
                        Debug.Log($"[MongoInventory] ✅ Reduced {item.itemName} quantity: {currentQuantity} → {newQuantity}");

                        // Синхронизируем с сервером
                        if (SocketIOManager.Instance != null && SocketIOManager.Instance.IsConnected)
                        {
                            var request = new UpdateQuantityRequest
                            {
                                characterClass = characterClass,
                                slotIndex = i,
                                newQuantity = newQuantity
                            };

                            string json = JsonUtility.ToJson(request);
                            SocketIOManager.Instance.EmitCustomEvent("mmo_update_quantity", json, (response) =>
                            {
                                Debug.Log($"[MongoInventory] Quantity synced");
                            });
                        }
                        return;
                    }
                }
            }

            Debug.LogWarning($"[MongoInventory] ⚠️ Item not found in inventory: {item.itemName}");
        }

        /// <summary>
        /// Использовать расходуемый предмет (зелье)
        /// </summary>
        public void UseConsumable(ItemData item)
        {
            if (item == null || item.itemType != ItemType.Consumable)
            {
                Debug.LogWarning($"[MongoInventory] Cannot use item: {item?.itemName ?? "null"}");
                return;
            }

            Debug.Log($"[MongoInventory] 🍾 Using consumable: {item.itemName}");
            Debug.Log($"[MongoInventory] 📊 Item stats: healAmount={item.healAmount}, manaRestoreAmount={item.manaRestoreAmount}");
            Debug.Log($"[MongoInventory] 📊 Item ItemId: {item.ItemId}");

            // Применяем эффект зелья
            bool effectApplied = false;

            if (item.healAmount > 0)
            {
                Debug.Log($"[MongoInventory] 🔍 Searching for HealthSystem...");
                // Восстанавливаем HP через HealthSystem
                HealthSystem healthSystem = FindObjectOfType<HealthSystem>();
                if (healthSystem != null)
                {
                    Debug.Log($"[MongoInventory] ✅ HealthSystem found! HP before: {healthSystem.CurrentHealth}/{healthSystem.MaxHealth}");
                    healthSystem.Heal(item.healAmount);
                    Debug.Log($"[MongoInventory] ✅ Healed {item.healAmount} HP. HP after: {healthSystem.CurrentHealth}/{healthSystem.MaxHealth}");
                    effectApplied = true;

                    // Отправляем на сервер для синхронизации
                    SendPotionUseToServer("health", item.healAmount, healthSystem.CurrentHealth, healthSystem.MaxHealth);
                }
                else
                {
                    Debug.LogError("[MongoInventory] ❌ HealthSystem not found! Зелье НЕ БУДЕТ удалено!");
                }
            }
            else
            {
                Debug.Log($"[MongoInventory] ⚠️ healAmount = 0, пропускаем восстановление HP");
            }

            if (item.manaRestoreAmount > 0)
            {
                Debug.Log($"[MongoInventory] 🔍 Searching for ManaSystem...");
                // Восстанавливаем ману через ManaSystem
                ManaSystem manaSystem = FindObjectOfType<ManaSystem>();
                if (manaSystem != null)
                {
                    Debug.Log($"[MongoInventory] ✅ ManaSystem found! Mana before: {manaSystem.CurrentMana}/{manaSystem.MaxMana}");
                    manaSystem.RestoreMana(item.manaRestoreAmount);
                    Debug.Log($"[MongoInventory] ✅ Restored {item.manaRestoreAmount} Mana. Mana after: {manaSystem.CurrentMana}/{manaSystem.MaxMana}");
                    effectApplied = true;

                    // Отправляем на сервер для синхронизации
                    SendPotionUseToServer("mana", item.manaRestoreAmount, manaSystem.CurrentMana, manaSystem.MaxMana);
                }
                else
                {
                    Debug.LogError("[MongoInventory] ❌ ManaSystem not found! Зелье НЕ БУДЕТ удалено!");
                }
            }
            else
            {
                Debug.Log($"[MongoInventory] ⚠️ manaRestoreAmount = 0, пропускаем восстановление маны");
            }

            Debug.Log($"[MongoInventory] 📊 effectApplied = {effectApplied}");

            // ИЗМЕНЕНИЕ ЛОГИКИ: Удаляем зелье ВСЕГДА, даже если HealthSystem/ManaSystem не найдены
            // Это предотвращает "зависание" зелий в инвентаре
            // Если системы не найдены - это проблема настройки сцены, но зелье должно исчезнуть
            if (item.healAmount > 0 || item.manaRestoreAmount > 0)
            {
                Debug.Log($"[MongoInventory] 🗑️ Вызываю RemoveItem({item.itemName}, 1)...");
                // Удаляем 1 предмет из инвентаря
                RemoveItem(item, 1);
                Debug.Log($"[MongoInventory] ✅ RemoveItem завершён");

                if (!effectApplied)
                {
                    Debug.LogWarning($"[MongoInventory] ⚠️ Зелье {item.itemName} удалено, но эффект НЕ применён!");
                    Debug.LogWarning($"[MongoInventory] ⚠️ Причина: HealthSystem или ManaSystem не найдены в сцене!");
                }
            }
            else
            {
                Debug.LogError($"[MongoInventory] ❌ Зелье {item.itemName} имеет healAmount=0 и manaRestoreAmount=0!");
                Debug.LogError($"[MongoInventory] ❌ Это ошибка конфигурации ItemData! Проверьте настройки зелья в Inspector!");
            }
        }

        /// <summary>
        /// Отправить использование зелья на сервер для синхронизации
        /// </summary>
        private void SendPotionUseToServer(string potionType, float restoreAmount, float currentValue, float maxValue)
        {
            var socketManager = FindObjectOfType<SocketIOManager>();
            if (socketManager != null && socketManager.IsConnected)
            {
                var data = new
                {
                    potionType = potionType, // "health" или "mana"
                    restoreAmount = restoreAmount,
                    currentValue = currentValue,
                    maxValue = maxValue
                };

                string json = Newtonsoft.Json.JsonConvert.SerializeObject(data);
                socketManager.Emit("use_potion", json);
                Debug.Log($"[MongoInventory] 📡 Sent potion use to server: {potionType} +{restoreAmount} (now: {currentValue}/{maxValue})");
            }
            else
            {
                Debug.LogWarning("[MongoInventory] ⚠️ Not connected to server, potion effect local only");
            }
        }
    }

    /// <summary>
    /// Запрос на обновление количества предмета
    /// </summary>
    [Serializable]
    public class UpdateQuantityRequest
    {
        public string characterClass;
        public int slotIndex;
        public int newQuantity;
    }
}
