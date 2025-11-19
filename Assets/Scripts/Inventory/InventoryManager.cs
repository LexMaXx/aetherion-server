using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Менеджер инвентаря (Singleton)
/// Управляет всеми предметами, экипировкой и UI
/// </summary>
public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }

    [Header("Inventory Settings")]
    [Tooltip("Максимальное количество слотов инвентаря")]
    [SerializeField] private int maxInventorySlots = 40;

    [Header("UI References")]
    [Tooltip("Главная панель инвентаря")]
    [SerializeField] private GameObject inventoryPanel;

    [Tooltip("Контейнер для слотов инвентаря (Grid Layout Group)")]
    [SerializeField] private Transform inventorySlotsContainer;

    [Tooltip("Контейнер для слотов экипировки")]
    [SerializeField] private Transform equipmentSlotsContainer;

    [Tooltip("Префаб слота инвентаря")]
    [SerializeField] private GameObject inventorySlotPrefab;

    [Tooltip("Tooltip для отображения информации о предмете")]
    [SerializeField] private GameObject itemTooltip;

    [SerializeField] private TextMeshProUGUI tooltipNameText;
    [SerializeField] private TextMeshProUGUI tooltipDescriptionText;

    [Header("Equipment Slots")]
    [SerializeField] private EquipmentSlotUI weaponSlot;
    [SerializeField] private EquipmentSlotUI armorSlot;
    [SerializeField] private EquipmentSlotUI helmetSlot;
    [SerializeField] private EquipmentSlotUI accessorySlot;

    // Runtime данные
    private List<InventorySlot> inventorySlots = new List<InventorySlot>();
    private bool isInventoryOpen = false;
    private bool isLoadingFromServer = false; // Флаг для предотвращения автосинхронизации во время загрузки
    private bool isWaitingServerInventoryResponse = false;
    private bool hasLoadedFromServerOnce = false;

    // НОВОЕ: Словарь предметов для быстрого поиска по GUID
    private Dictionary<string, ItemData> itemDatabaseById = new Dictionary<string, ItemData>();
    private Dictionary<string, ItemData> itemDatabaseByName = new Dictionary<string, ItemData>(); // Для обратной совместимости
    private bool isDatabaseInitialized = false;

    // Кэш данных инвентаря для восстановления после смены сцены
    private List<ItemStackData> cachedInventoryItems = new List<ItemStackData>();
    private EquipmentData cachedEquipment = new EquipmentData();

    // Pending sync состояния
    private bool hasPendingSync = false;
    private string pendingSyncJson = "";
    private float nextPendingSyncTime = 0f;
    private const float pendingSyncRetryInterval = 2f;

    private string lastLoadedCharacterClass = "";
    private bool wasSocketConnected = false;
    private float nextAutoLoadAttemptTime = 0f;
    private const float autoLoadRetryDelay = 2f;

    // Буфер данных, полученных раньше, чем готов UI
    private string pendingInventoryJson = "";
    private bool hasPendingInventoryJson = false;

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

        // Подписываемся на событие смены сцены
        SceneManager.sceneLoaded += OnSceneLoaded;

        InitializeInventory();
    }

    void OnDestroy()
    {
        // Отписываемся от события при уничтожении
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    /// <summary>
    /// Обработчик загрузки сцены - переинициализирует UI
    /// </summary>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"[InventoryManager] 🔄 Сцена загружена: {scene.name}");

        // Сохраняем текущий инвентарь перед переинициализацией
        CacheCurrentInventory();

        // Очищаем старые ссылки (они могут указывать на уничтоженные объекты)
        inventorySlots.Clear();

        // Ищем новые UI элементы в текущей сцене
        StartCoroutine(ReinitializeUIDelayed());
    }

    /// <summary>
    /// Кэширует текущий инвентарь перед переинициализацией
    /// </summary>
    private void CacheCurrentInventory()
    {
        cachedInventoryItems.Clear();

        foreach (InventorySlot slot in inventorySlots)
        {
            // Проверяем что слот не уничтожен
            if (slot != null && !slot.IsEmpty())
            {
                ItemData item = slot.GetItem();
                if (item != null)
                {
                    cachedInventoryItems.Add(new ItemStackData
                    {
                        itemName = item.itemName,
                        quantity = slot.GetQuantity()
                    });
                }
            }
        }

        // Кэшируем экипировку
        if (weaponSlot != null && !weaponSlot.IsEmpty())
            cachedEquipment.weapon = weaponSlot.GetEquippedItem()?.itemName ?? "";
        if (armorSlot != null && !armorSlot.IsEmpty())
            cachedEquipment.armor = armorSlot.GetEquippedItem()?.itemName ?? "";
        if (helmetSlot != null && !helmetSlot.IsEmpty())
            cachedEquipment.helmet = helmetSlot.GetEquippedItem()?.itemName ?? "";
        if (accessorySlot != null && !accessorySlot.IsEmpty())
            cachedEquipment.accessory = accessorySlot.GetEquippedItem()?.itemName ?? "";

        Debug.Log($"[InventoryManager] 💾 Закэшировано {cachedInventoryItems.Count} предметов");
    }

    /// <summary>
    /// Переинициализирует UI с задержкой
    /// </summary>
    private System.Collections.IEnumerator ReinitializeUIDelayed()
    {
        // Ждём один кадр чтобы все объекты в сцене инициализировались
        yield return null;

        ReinitializeUI();
    }

    /// <summary>
    /// Ищет и привязывает UI элементы в новой сцене
    /// </summary>
    private void ReinitializeUI()
    {
        Debug.Log("[InventoryManager] 🔍 Поиск UI элементов в новой сцене...");

        // Ищем InventoryCanvas в сцене
        GameObject inventoryCanvas = GameObject.Find("InventoryCanvas");
        if (inventoryCanvas == null)
        {
            Debug.LogWarning("[InventoryManager] ⚠️ InventoryCanvas не найден в сцене");
            return;
        }

        // Ищем InventoryPanel
        Transform panel = inventoryCanvas.transform.Find("InventoryPanel");
        if (panel != null)
        {
            inventoryPanel = panel.gameObject;
            inventoryPanel.SetActive(false);
            Debug.Log("[InventoryManager] ✅ Найден InventoryPanel");

            // Ищем контейнер слотов
            Transform container = panel.Find("InventorySlotsContainer");
            if (container != null)
            {
                inventorySlotsContainer = container;
                Debug.Log("[InventoryManager] ✅ Найден InventorySlotsContainer");
            }
            else
            {
                Debug.LogError("[InventoryManager] ❌ InventorySlotsContainer не найден!");
            }

            // Ищем tooltip (опционально)
            Transform tooltip = panel.Find("ItemTooltip");
            if (tooltip != null)
            {
                itemTooltip = tooltip.gameObject;
                tooltipNameText = tooltip.Find("NameText")?.GetComponent<TextMeshProUGUI>();
                tooltipDescriptionText = tooltip.Find("DescriptionText")?.GetComponent<TextMeshProUGUI>();
            }
        }
        else
        {
            Debug.LogError("[InventoryManager] ❌ InventoryPanel не найден в InventoryCanvas!");
            return;
        }

        // Пересоздаём слоты инвентаря
        if (inventorySlotPrefab != null && inventorySlotsContainer != null)
        {
            // Очищаем контейнер от старых слотов (на случай если они там есть)
            foreach (Transform child in inventorySlotsContainer)
            {
                Destroy(child.gameObject);
            }

            // Создаём новые слоты
            inventorySlots.Clear();

            // КРИТИЧНО: Сбрасываем флаг автозагрузки при пересоздании слотов
            // Это позволяет каждой новой сцене заново запросить данные с сервера
            hasLoadedFromServerOnce = false;
            Debug.Log("[InventoryManager] 🔄 Сброс hasLoadedFromServerOnce при пересоздании слотов");
            for (int i = 0; i < maxInventorySlots; i++)
            {
                GameObject slotObj = Instantiate(inventorySlotPrefab, inventorySlotsContainer);
                InventorySlot slot = slotObj.GetComponent<InventorySlot>();
                if (slot != null)
                {
                    inventorySlots.Add(slot);
                }
            }

            Debug.Log($"[InventoryManager] ✅ Создано {inventorySlots.Count} слотов инвентаря в новой сцене");

            // Восстанавливаем кэшированный инвентарь
            RestoreCachedInventory();

            // Пытаемся автоматически загрузить данные с сервера, как только UI готов
            TryAutoLoadInventory();

            // Если данные с сервера пришли раньше, применяем их сейчас
            TryApplyPendingInventoryJson();
        }
        else
        {
            Debug.LogError("[InventoryManager] ❌ inventorySlotPrefab не назначен! Назначьте его в Inspector.");
        }
    }

    /// <summary>
    /// Восстанавливает инвентарь из кэша после смены сцены
    /// ВАЖНО: Кэш показывает предметы ВРЕМЕННО, пока не загрузятся данные с сервера
    /// Сервер автоматически перезапишет кэш актуальными данными через TryAutoLoadInventory()
    /// </summary>
    private void RestoreCachedInventory()
    {
        if (cachedInventoryItems.Count == 0)
        {
            Debug.Log("[InventoryManager] 📦 Кэш пуст, нечего восстанавливать");
            return;
        }

        Debug.Log($"[InventoryManager] 📦 Восстанавливаем {cachedInventoryItems.Count} предметов из кэша (временно, до загрузки с сервера)");

        isLoadingFromServer = true; // Отключаем автосинхронизацию

        int restored = 0;
        foreach (ItemStackData itemData in cachedInventoryItems)
        {
            ItemData item = FindItemByName(itemData.itemName);
            if (item != null)
            {
                if (AddItem(item, itemData.quantity))
                {
                    restored++;
                }
            }
        }

        // Восстанавливаем экипировку
        LoadEquipmentFromData(cachedEquipment);

        isLoadingFromServer = false;

        Debug.Log($"[InventoryManager] ✅ Восстановлено {restored}/{cachedInventoryItems.Count} предметов из кэша");

        // НЕ очищаем кэш! Он будет перезаписан данными с сервера через LoadInventoryFromJson()
        // cachedInventoryItems.Clear(); // Закомментировано - кэш очистится при следующей смене сцены
        // cachedEquipment = new EquipmentData();
    }

    void Start()
    {
        // ИСПРАВЛЕНИЕ: Сбрасываем флаг автозагрузки при старте сцены
        // Это позволяет инвентарю загрузиться автоматически при каждом запуске
        hasLoadedFromServerOnce = false;
        Debug.Log("[InventoryManager] 🔄 Start: Сброшен флаг hasLoadedFromServerOnce - разрешена автозагрузка");

        // Скрываем инвентарь при старте
        if (inventoryPanel != null)
        {
            inventoryPanel.SetActive(false);
        }

        // Скрываем tooltip
        if (itemTooltip != null)
        {
            itemTooltip.SetActive(false);
        }
    }

    void Update()
    {
        // Открытие/закрытие инвентаря по клавише I
        if (Input.GetKeyDown(KeyCode.I))
        {
            ToggleInventory();
        }

        // Скрываем tooltip при клике
        if (Input.GetMouseButtonDown(0) && itemTooltip != null && itemTooltip.activeSelf)
        {
            HideItemTooltip();
        }

        // Пытаемся автоматически загрузить данные с сервера, как только появится подключение и UI готов
        TryAutoLoadInventory();

        // Если были отложенные изменения, пробуем снова отправить их на сервер
        TryFlushPendingSync();

        MonitorSocketConnectionState();
    }

    /// <summary>
    /// Инициализация инвентаря - создание слотов
    /// </summary>
    private void InitializeInventory()
    {
        if (inventorySlotPrefab == null || inventorySlotsContainer == null)
        {
            Debug.LogError("[InventoryManager] ❌ Inventory slot prefab or container not assigned!");
            return;
        }

        // Создаём слоты инвентаря
        for (int i = 0; i < maxInventorySlots; i++)
        {
            GameObject slotObj = Instantiate(inventorySlotPrefab, inventorySlotsContainer);
            InventorySlot slot = slotObj.GetComponent<InventorySlot>();

            if (slot != null)
            {
                inventorySlots.Add(slot);
            }
        }

        Debug.Log($"[InventoryManager] ✅ Created {inventorySlots.Count} inventory slots");
    }

    /// <summary>
    /// Открыть/закрыть инвентарь
    /// </summary>
    public void ToggleInventory()
    {
        isInventoryOpen = !isInventoryOpen;

        if (inventoryPanel != null)
        {
            inventoryPanel.SetActive(isInventoryOpen);
        }

        Debug.Log($"[InventoryManager] Inventory {(isInventoryOpen ? "opened" : "closed")}");
    }

    /// <summary>
    /// Открыть инвентарь
    /// </summary>
    public void OpenInventory()
    {
        isInventoryOpen = true;
        if (inventoryPanel != null)
        {
            inventoryPanel.SetActive(true);
        }
    }

    /// <summary>
    /// Закрыть инвентарь
    /// </summary>
    public void CloseInventory()
    {
        isInventoryOpen = false;
        if (inventoryPanel != null)
        {
            inventoryPanel.SetActive(false);
        }
        HideItemTooltip();
    }

    /// <summary>
    /// Добавить предмет в инвентарь
    /// </summary>
    public bool AddItem(ItemData item, int quantity = 1)
    {
        if (item == null) return false;

        Debug.Log($"[InventoryManager] Adding {quantity}x {item.itemName}");

        // Если предмет стакается - пробуем добавить к существующему
        if (item.isStackable)
        {
            foreach (InventorySlot slot in inventorySlots)
            {
                if (!slot.IsEmpty() && slot.GetItem() == item)
                {
                    if (slot.AddQuantity(quantity))
                    {
                        Debug.Log($"[InventoryManager] ✅ Added to existing stack");
                        AutoSyncInventory(); // Синхронизация с сервером
                        return true;
                    }
                }
            }
        }

        // Ищем пустой слот
        int emptySlotIndex = -1;
        for (int i = 0; i < inventorySlots.Count; i++)
        {
            if (inventorySlots[i].IsEmpty())
            {
                emptySlotIndex = i;
                break;
            }
        }

        if (emptySlotIndex >= 0)
        {
            Debug.Log($"[InventoryManager] 🔍 Found empty slot at index {emptySlotIndex}");
            Debug.Log($"[InventoryManager] 🔵 ПЕРЕД SetItem: slot={inventorySlots[emptySlotIndex]}, item={item.itemName}, qty={quantity}");

            inventorySlots[emptySlotIndex].SetItem(item, quantity);

            Debug.Log($"[InventoryManager] 🔵 ПОСЛЕ SetItem: slot.IsEmpty()={inventorySlots[emptySlotIndex].IsEmpty()}, slot.GetItem()={inventorySlots[emptySlotIndex].GetItem()?.itemName}");
            Debug.Log($"[InventoryManager] ✅ Added to new slot (index {emptySlotIndex})");
            AutoSyncInventory(); // Синхронизация с сервером
            return true;
        }

        Debug.LogWarning($"[InventoryManager] ⚠️ Inventory is full! Total slots: {inventorySlots.Count}");
        return false;
    }

    /// <summary>
    /// Удалить предмет из инвентаря
    /// </summary>
    public bool RemoveItem(ItemData item, int quantity = 1)
    {
        if (item == null) return false;

        foreach (InventorySlot slot in inventorySlots)
        {
            if (!slot.IsEmpty() && slot.GetItem() == item)
            {
                slot.RemoveQuantity(quantity);
                Debug.Log($"[InventoryManager] ✅ Removed {quantity}x {item.itemName}");
                AutoSyncInventory(); // Синхронизация с сервером
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Экипировать предмет
    /// </summary>
    public void EquipItem(ItemData item, InventorySlot fromSlot)
    {
        if (item == null || !item.isEquippable) return;

        EquipmentSlotUI targetSlot = GetEquipmentSlot(item.equipmentSlot);

        if (targetSlot != null)
        {
            if (targetSlot.EquipItem(item))
            {
                // Убираем из инвентаря
                fromSlot.RemoveQuantity(1);
                Debug.Log($"[InventoryManager] ✅ Equipped {item.itemName}");

                // НОВОЕ: Обновляем бонусы экипировки в CharacterStats
                UpdateCharacterStatsFromEquipment();

                // Синхронизация с сервером
                AutoSyncInventory();
            }
        }
    }

    /// <summary>
    /// Обновить бонусы CharacterStats от экипировки
    /// </summary>
    private void UpdateCharacterStatsFromEquipment()
    {
        // Найти локального игрока
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            Debug.LogWarning("[InventoryManager] Player not found! Cannot update CharacterStats.");
            return;
        }

        CharacterStats stats = player.GetComponent<CharacterStats>();
        if (stats != null)
        {
            stats.UpdateEquipmentBonuses();
            Debug.Log("[InventoryManager] ✅ CharacterStats updated from equipment");
        }
        else
        {
            Debug.LogWarning("[InventoryManager] CharacterStats not found on Player!");
        }
    }

    /// <summary>
    /// Использовать предмет (зелья и тд)
    /// ОБНОВЛЕНО: Теперь работает с HealthSystem и ManaSystem!
    /// </summary>
    public void UseItem(ItemData item, InventorySlot fromSlot)
    {
        if (item == null) return;

        if (item.itemType == ItemType.Consumable)
        {
            Debug.Log($"[InventoryManager] 🧪 Using consumable: {item.itemName}");

            // Найти локального игрока
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player == null)
            {
                Debug.LogError("[InventoryManager] ❌ Player not found! Cannot use consumable.");
                return;
            }

            bool itemUsed = false;

            // Применяем эффект восстановления HP
            if (item.healAmount > 0)
            {
                HealthSystem healthSystem = player.GetComponent<HealthSystem>();
                if (healthSystem != null)
                {
                    healthSystem.Heal(item.healAmount);
                    Debug.Log($"[InventoryManager] ✅ Restored {item.healAmount} HP");
                    itemUsed = true;
                }
                else
                {
                    Debug.LogWarning("[InventoryManager] ⚠️ HealthSystem not found on Player!");
                }
            }

            // Применяем эффект восстановления Mana
            if (item.manaRestoreAmount > 0)
            {
                ManaSystem manaSystem = player.GetComponent<ManaSystem>();
                if (manaSystem != null)
                {
                    manaSystem.RestoreMana(item.manaRestoreAmount);
                    Debug.Log($"[InventoryManager] ✅ Restored {item.manaRestoreAmount} Mana");
                    itemUsed = true;
                }
                else
                {
                    Debug.LogWarning("[InventoryManager] ⚠️ ManaSystem not found on Player!");
                }
            }

            // Убираем предмет из инвентаря ТОЛЬКО если он был успешно использован
            if (itemUsed)
            {
                fromSlot.RemoveQuantity(1);
                Debug.Log($"[InventoryManager] 🗑️ Removed {item.itemName} from inventory");

                // Синхронизация с сервером
                AutoSyncInventory();
            }
            else
            {
                Debug.LogWarning($"[InventoryManager] ⚠️ {item.itemName} не был использован (нет эффектов или компонентов)");
            }
        }
    }

    /// <summary>
    /// Получить слот экипировки по типу
    /// </summary>
    private EquipmentSlotUI GetEquipmentSlot(EquipmentSlot slotType)
    {
        switch (slotType)
        {
            case EquipmentSlot.Weapon: return weaponSlot;
            case EquipmentSlot.Armor: return armorSlot;
            case EquipmentSlot.Helmet: return helmetSlot;
            case EquipmentSlot.Accessory: return accessorySlot;
            default: return null;
        }
    }

    /// <summary>
    /// Показать tooltip с информацией о предмете
    /// </summary>
    public void ShowItemTooltip(ItemData item, Vector3 position)
    {
        if (item == null || itemTooltip == null) return;

        itemTooltip.SetActive(true);
        itemTooltip.transform.position = position + new Vector3(100, 0, 0); // Смещаем вправо

        if (tooltipNameText != null)
        {
            tooltipNameText.text = item.itemName;
        }

        if (tooltipDescriptionText != null)
        {
            tooltipDescriptionText.text = item.GetFullDescription();
        }
    }

    /// <summary>
    /// Скрыть tooltip
    /// </summary>
    public void HideItemTooltip()
    {
        if (itemTooltip != null)
        {
            itemTooltip.SetActive(false);
        }
    }

    /// <summary>
    /// Получить все экипированные предметы
    /// </summary>
    public Dictionary<EquipmentSlot, ItemData> GetEquippedItems()
    {
        Dictionary<EquipmentSlot, ItemData> equipped = new Dictionary<EquipmentSlot, ItemData>();

        if (weaponSlot != null && !weaponSlot.IsEmpty())
            equipped[EquipmentSlot.Weapon] = weaponSlot.GetEquippedItem();

        if (armorSlot != null && !armorSlot.IsEmpty())
            equipped[EquipmentSlot.Armor] = armorSlot.GetEquippedItem();

        if (helmetSlot != null && !helmetSlot.IsEmpty())
            equipped[EquipmentSlot.Helmet] = helmetSlot.GetEquippedItem();

        if (accessorySlot != null && !accessorySlot.IsEmpty())
            equipped[EquipmentSlot.Accessory] = accessorySlot.GetEquippedItem();

        return equipped;
    }

    /// <summary>
    /// Подсчитать бонусы от экипировки
    /// </summary>
    public (int attack, int defense, int health, int mana) GetTotalEquipmentBonuses()
    {
        int totalAttack = 0;
        int totalDefense = 0;
        int totalHealth = 0;
        int totalMana = 0;

        var equippedItems = GetEquippedItems();
        foreach (var item in equippedItems.Values)
        {
            totalAttack += item.attackBonus;
            totalDefense += item.defenseBonus;
            totalHealth += item.healthBonus;
            totalMana += item.manaBonus;
        }

        return (totalAttack, totalDefense, totalHealth, totalMana);
    }

    // ═══════════════════════════════════════════
    // СЕРИАЛИЗАЦИЯ ДЛЯ СОХРАНЕНИЯ В MONGODB
    // ═══════════════════════════════════════════

    /// <summary>
    /// Сериализовать инвентарь для отправки на сервер (MongoDB)
    /// Возвращает JSON строку
    /// </summary>
    public string SerializeInventory()
    {
        InventoryData data = new InventoryData();

        // Сериализуем предметы инвентаря
        foreach (InventorySlot slot in inventorySlots)
        {
            if (!slot.IsEmpty())
            {
                ItemData item = slot.GetItem();
                data.items.Add(new ItemStackData
                {
                    itemId = item.ItemId,        // НОВОЕ: Сохраняем GUID
                    itemName = item.itemName,    // Старое: Для обратной совместимости
                    quantity = slot.GetQuantity()
                });
            }
        }

        // Сериализуем экипировку
        if (weaponSlot != null && !weaponSlot.IsEmpty())
        {
            ItemData weaponItem = weaponSlot.GetEquippedItem();
            data.equipment.weaponId = weaponItem.ItemId;    // НОВОЕ: GUID
            data.equipment.weapon = weaponItem.itemName;    // Старое
        }

        if (armorSlot != null && !armorSlot.IsEmpty())
        {
            ItemData armorItem = armorSlot.GetEquippedItem();
            data.equipment.armorId = armorItem.ItemId;
            data.equipment.armor = armorItem.itemName;
        }

        if (helmetSlot != null && !helmetSlot.IsEmpty())
        {
            ItemData helmetItem = helmetSlot.GetEquippedItem();
            data.equipment.helmetId = helmetItem.ItemId;
            data.equipment.helmet = helmetItem.itemName;
        }

        if (accessorySlot != null && !accessorySlot.IsEmpty())
        {
            ItemData accessoryItem = accessorySlot.GetEquippedItem();
            data.equipment.accessoryId = accessoryItem.ItemId;
            data.equipment.accessory = accessoryItem.itemName;
        }

        string json = JsonUtility.ToJson(data);
        Debug.Log($"[InventoryManager] 📦 Инвентарь сериализован: {data.items.Count} предметов (с GUID), экипировка: {data.equipment}");
        return json;
    }

    /// <summary>
    /// Загрузить инвентарь из JSON (из MongoDB)
    /// </summary>
    public void LoadInventoryFromJson(string json)
    {
        if (string.IsNullOrEmpty(json))
        {
            Debug.LogWarning("[InventoryManager] ⚠️ JSON пустой, инвентарь не загружен");
            return;
        }

        // ДИАГНОСТИКА: Проверяем состояние слотов
        Debug.Log($"[InventoryManager] 📋 ДИАГНОСТИКА: inventorySlots.Count = {inventorySlots.Count}");
        if (inventorySlots.Count == 0)
        {
            Debug.LogError("[InventoryManager] ❌ КРИТИЧНО: Нет слотов инвентаря! Сохраняем JSON и попробуем повторно после инициализации UI.");
            pendingInventoryJson = json;
            hasPendingInventoryJson = true;
            return;
        }

        try
        {
            Debug.Log($"[InventoryManager] 🟢 STEP 1: Начинаем try блок LoadInventoryFromJson");

            // Отключаем автосинхронизацию во время загрузки
            isLoadingFromServer = true;
            Debug.Log($"[InventoryManager] 🟢 STEP 2: isLoadingFromServer = true");

            Debug.Log($"[InventoryManager] 🟢 STEP 3: Начинаем парсинг JSON...");
            InventoryData data = JsonUtility.FromJson<InventoryData>(json);
            Debug.Log($"[InventoryManager] 🟢 STEP 4: Парсинг успешен! data = {(data != null ? "NOT NULL" : "NULL")}");

            if (data == null)
            {
                Debug.LogError($"[InventoryManager] ❌ data is NULL after parsing!");
                return;
            }

            Debug.Log($"[InventoryManager] 🟢 STEP 5: data.items = {(data.items != null ? "NOT NULL" : "NULL")}, Count = {data.items?.Count ?? 0}");

            // КРИТИЧНО: Очищаем инвентарь перед загрузкой чтобы избежать дубликатов
            Debug.Log($"[InventoryManager] 🟢 STEP 6: Очищаем инвентарь перед загрузкой...");
            ClearInventory(); // Очищаем слоты перед загрузкой данных с сервера
            Debug.Log($"[InventoryManager] 🟢 STEP 6.1: Инвентарь очищен, слотов: {inventorySlots.Count}");

            Debug.Log($"[InventoryManager] 🟢 STEP 7: Начинаем foreach loop для {data.items.Count} предметов...");

            // КРИТИЧНО: Инициализируем базу данных предметов перед загрузкой
            InitializeItemDatabase();

            // Загружаем предметы
            int loadedCount = 0;
            int loopIndex = 0;
            foreach (ItemStackData itemData in data.items)
            {
                loopIndex++;
                Debug.Log($"[InventoryManager] 🟢 LOOP {loopIndex}: Обрабатываем предмет: itemId={itemData.itemId}, itemName={itemData.itemName} x{itemData.quantity}");

                try
                {
                    // НОВОЕ: Сначала пытаемся найти по GUID, затем по имени (fallback)
                    ItemData item = null;

                    if (!string.IsNullOrEmpty(itemData.itemId))
                    {
                        item = FindItemById(itemData.itemId);
                        Debug.Log($"[InventoryManager] 🟢 LOOP {loopIndex}: FindItemById({itemData.itemId}) вернул: {(item != null ? item.itemName : "NULL")}");
                    }

                    // Fallback: Если не нашли по GUID, ищем по имени (обратная совместимость)
                    if (item == null && !string.IsNullOrEmpty(itemData.itemName))
                    {
                        item = FindItemByName(itemData.itemName);
                        Debug.Log($"[InventoryManager] 🟡 FALLBACK: FindItemByName({itemData.itemName}) вернул: {(item != null ? item.itemName : "NULL")}");
                    }

                    if (item != null)
                    {
                        bool added = AddItem(item, itemData.quantity);
                        Debug.Log($"[InventoryManager] 🟢 LOOP {loopIndex}: AddItem вернул: {added}");

                        if (added)
                        {
                            loadedCount++;
                            Debug.Log($"[InventoryManager] ✅ Загружен: {item.itemName} (ID: {item.ItemId}) x{itemData.quantity} (icon: {(item.icon != null ? "YES" : "NO")})");
                        }
                        else
                        {
                            Debug.LogError($"[InventoryManager] ❌ Не удалось добавить: {item.itemName} (инвентарь полон?)");
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"[InventoryManager] ⚠️ Предмет не найден! itemId='{itemData.itemId}', itemName='{itemData.itemName}'");
                    }
                }
                catch (System.Exception loopEx)
                {
                    Debug.LogError($"[InventoryManager] ❌ Ошибка в цикле загрузки предмета {loopIndex}: {loopEx.Message}\n{loopEx.StackTrace}");
                }
            }

            Debug.Log($"[InventoryManager] 🟢 STEP 8: Цикл foreach завершён, loadedCount = {loadedCount}");

            // КРИТИЧНО: Очищаем экипировку перед загрузкой
            Debug.Log($"[InventoryManager] 🟢 STEP 8.5: Очищаем экипировку перед загрузкой...");
            ClearEquipment();

            // Загружаем экипировку
            Debug.Log($"[InventoryManager] 🟢 STEP 9: Загружаем экипировку...");
            LoadEquipmentFromData(data.equipment);
            Debug.Log($"[InventoryManager] 🟢 STEP 10: Экипировка загружена");

            Debug.Log($"[InventoryManager] ✅ Инвентарь загружен: {loadedCount}/{data.items.Count} предметов в {inventorySlots.Count} слотов");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[InventoryManager] ❌ Ошибка загрузки инвентаря: {e.Message}\n{e.StackTrace}");
        }
        finally
        {
            // Включаем автосинхронизацию обратно
            isLoadingFromServer = false;
        }
    }

    /// <summary>
    /// Загрузить экипировку из данных
    /// </summary>
    private void LoadEquipmentFromData(EquipmentData equipmentData)
    {
        // НОВОЕ: Сначала пытаемся загрузить по GUID, потом fallback на имя

        // Weapon
        ItemData weapon = null;
        if (!string.IsNullOrEmpty(equipmentData.weaponId))
            weapon = FindItemById(equipmentData.weaponId);
        if (weapon == null && !string.IsNullOrEmpty(equipmentData.weapon))
            weapon = FindItemByName(equipmentData.weapon);
        if (weapon != null && weaponSlot != null)
            weaponSlot.EquipItem(weapon);

        // Armor
        ItemData armor = null;
        if (!string.IsNullOrEmpty(equipmentData.armorId))
            armor = FindItemById(equipmentData.armorId);
        if (armor == null && !string.IsNullOrEmpty(equipmentData.armor))
            armor = FindItemByName(equipmentData.armor);
        if (armor != null && armorSlot != null)
            armorSlot.EquipItem(armor);

        // Helmet
        ItemData helmet = null;
        if (!string.IsNullOrEmpty(equipmentData.helmetId))
            helmet = FindItemById(equipmentData.helmetId);
        if (helmet == null && !string.IsNullOrEmpty(equipmentData.helmet))
            helmet = FindItemByName(equipmentData.helmet);
        if (helmet != null && helmetSlot != null)
            helmetSlot.EquipItem(helmet);

        // Accessory
        ItemData accessory = null;
        if (!string.IsNullOrEmpty(equipmentData.accessoryId))
            accessory = FindItemById(equipmentData.accessoryId);
        if (accessory == null && !string.IsNullOrEmpty(equipmentData.accessory))
            accessory = FindItemByName(equipmentData.accessory);
        if (accessory != null && accessorySlot != null)
            accessorySlot.EquipItem(accessory);

        // Обновляем CharacterStats после загрузки экипировки
        UpdateCharacterStatsFromEquipment();
    }

    /// <summary>
    /// Инициализировать базу данных предметов (один раз при старте)
    /// </summary>
    private void InitializeItemDatabase()
    {
        if (isDatabaseInitialized) return;

        Debug.Log("[InventoryManager] 🗂️ Инициализация базы данных предметов...");

        // Загружаем все ItemData из Resources ОДИН РАЗ
        ItemData[] allItems = Resources.LoadAll<ItemData>("Data/Items");
        Debug.Log($"[InventoryManager] 📦 Загружено {allItems.Length} предметов из Resources/Data/Items");

        itemDatabaseById.Clear();
        itemDatabaseByName.Clear();

        foreach (ItemData item in allItems)
        {
            // Добавляем в словарь по GUID
            string guid = item.ItemId; // Вызов свойства автоматически создаст GUID если его нет
            if (!itemDatabaseById.ContainsKey(guid))
            {
                itemDatabaseById[guid] = item;
                Debug.Log($"[InventoryManager]   ✅ {item.itemName} → ID: {guid.Substring(0, 8)}...");
            }
            else
            {
                Debug.LogError($"[InventoryManager]   ❌ ДУБЛИКАТ GUID! {item.itemName} имеет тот же ID что и {itemDatabaseById[guid].itemName}");
            }

            // Также добавляем по имени (для обратной совместимости)
            if (!itemDatabaseByName.ContainsKey(item.itemName))
            {
                itemDatabaseByName[item.itemName] = item;
            }
            else
            {
                Debug.LogWarning($"[InventoryManager]   ⚠️ ДУБЛИКАТ ИМЕНИ! Несколько предметов с именем '{item.itemName}'");
            }
        }

        isDatabaseInitialized = true;
        Debug.Log($"[InventoryManager] ✅ База данных готова: {itemDatabaseById.Count} предметов");
    }

    /// <summary>
    /// Найти предмет по GUID (НОВЫЙ МЕТОД)
    /// </summary>
    private ItemData FindItemById(string itemId)
    {
        InitializeItemDatabase();

        if (itemDatabaseById.TryGetValue(itemId, out ItemData item))
        {
            return item;
        }

        Debug.LogError($"[InventoryManager] ❌ Предмет с ID '{itemId}' не найден в базе!");
        return null;
    }

    /// <summary>
    /// Найти предмет по имени (СТАРЫЙ МЕТОД - для обратной совместимости)
    /// </summary>
    private ItemData FindItemByName(string itemName)
    {
        InitializeItemDatabase();

        if (itemDatabaseByName.TryGetValue(itemName, out ItemData item))
        {
            Debug.Log($"[InventoryManager] ✅ Найден '{itemName}' по имени (ID: {item.ItemId.Substring(0, 8)}...)");
            return item;
        }

        Debug.LogError($"[InventoryManager] ❌ Предмет '{itemName}' не найден в базе!");
        return null;
    }

    /// <summary>
    /// Очистить инвентарь
    /// </summary>
    private void ClearInventory()
    {
        foreach (InventorySlot slot in inventorySlots)
        {
            slot.ClearSlot();
        }
    }

    /// <summary>
    /// Очистить экипировку
    /// </summary>
    private void ClearEquipment()
    {
        if (weaponSlot != null)
        {
            weaponSlot.ClearSlot();
        }

        if (armorSlot != null)
        {
            armorSlot.ClearSlot();
        }

        if (helmetSlot != null)
        {
            helmetSlot.ClearSlot();
        }

        if (accessorySlot != null)
        {
            accessorySlot.ClearSlot();
        }

        Debug.Log("[InventoryManager] 🧹 Экипировка очищена");
    }

    // ═══════════════════════════════════════════
    // SERVER SYNC METHODS
    // ═══════════════════════════════════════════

    /// <summary>
    /// Публичный метод для ручной синхронизации инвентаря
    /// Вызывается из EquipmentSlotUI и других внешних классов
    /// </summary>
    public void SyncInventoryToServer()
    {
        AutoSyncInventory();
    }

    /// <summary>
    /// Автоматическая синхронизация инвентаря с сервером
    /// Вызывается после каждого изменения инвентаря
    /// </summary>
    private void AutoSyncInventory()
    {
        // Не синхронизируем если загружаем с сервера (избегаем бесконечного цикла)
        if (isLoadingFromServer)
        {
            Debug.Log("[InventoryManager] 🔄 AutoSync: Пропускаем (загрузка с сервера)");
            return;
        }

        string inventoryJson = SerializeInventory();
        if (TrySendInventoryToServer(inventoryJson, out string failureReason))
        {
            // ИСПРАВЛЕНИЕ: НЕ сбрасываем pending здесь!
            // Сброс произойдёт в callback TrySendInventoryToServer() при success=true
            Debug.Log("[InventoryManager] 📤 AutoSync: Отправка успешна, ждём подтверждения от сервера...");
        }
        else
        {
            QueuePendingSync(inventoryJson, failureReason);
        }
    }

    /// <summary>
    /// Загрузить инвентарь с сервера при входе в игру
    /// Вызывается из NetworkLevelingSync или при подключении
    /// </summary>
    public void LoadInventoryFromServer()
    {
        if (SocketIOManager.Instance == null || !SocketIOManager.Instance.IsConnected)
        {
            Debug.LogWarning("[InventoryManager] ⚠️ LoadFromServer: Не подключен к серверу");
            return;
        }

        if (!TryGetCharacterClass(out string characterClass))
        {
            Debug.LogError("[InventoryManager] ❌ LoadFromServer: Класс персонажа не найден (ожидаем SelectedCharacterClass или SelectedClass)!");
            return;
        }

        if (isWaitingServerInventoryResponse)
        {
            Debug.LogWarning("[InventoryManager] ⚠️ Уже ожидаем ответ от сервера, повторный запрос пропущен");
            return;
        }

        Debug.Log($"[InventoryManager] 📥📥📥 ЗАГРУЗКА ИНВЕНТАРЯ С СЕРВЕРА:");
        Debug.Log($"[InventoryManager]   - CharacterClass: {characterClass}");
        Debug.Log($"[InventoryManager]   - SocketId: {SocketIOManager.Instance?.SocketId}");
        Debug.Log($"[InventoryManager]   - IsConnected: {SocketIOManager.Instance?.IsConnected}");

        isWaitingServerInventoryResponse = true;

        SocketIOManager.Instance.LoadInventory(characterClass, (inventoryJson) =>
        {
            isWaitingServerInventoryResponse = false;

            if (!string.IsNullOrEmpty(inventoryJson))
            {
                Debug.Log($"[InventoryManager] 📦 Получен JSON инвентаря: {inventoryJson.Length} символов");
                Debug.Log($"[InventoryManager] 📦 ПОЛНЫЙ JSON: {inventoryJson}");
                LoadInventoryFromJson(inventoryJson);
                Debug.Log($"[InventoryManager] ✅✅✅ УСПЕХ! Инвентарь загружен с сервера и применён!");
                Debug.Log($"[InventoryManager] 📊 Итоговое состояние UI: inventorySlots.Count={inventorySlots.Count}, занято слотов={GetOccupiedSlotsCount()}");

                // После успешной загрузки пробуем синхронизировать отложенные изменения
                TryFlushPendingSync();

                hasLoadedFromServerOnce = true;
                lastLoadedCharacterClass = characterClass;
            }
            else
            {
                Debug.LogWarning($"[InventoryManager] ⚠️ Инвентарь пустой или не найден на сервере (новый персонаж?)");

                // КРИТИЧНО: Пустой инвентарь - это ВАЛИДНЫЙ ответ!
                // Устанавливаем флаг загрузки, чтобы не запрашивать повторно
                hasLoadedFromServerOnce = true;
                lastLoadedCharacterClass = characterClass;

                Debug.Log($"[InventoryManager] ✅ Пустой инвентарь принят как валидное состояние для персонажа '{characterClass}'");
            }
        });
    }

    /// <summary>
    /// Возвращает true, если класс персонажа найден в PlayerPrefs
    /// </summary>
    private bool TryGetCharacterClass(out string characterClass)
    {
        characterClass = PlayerPrefs.GetString("SelectedCharacterClass", "");
        if (string.IsNullOrEmpty(characterClass))
        {
            characterClass = PlayerPrefs.GetString("SelectedClass", "");
        }
        return !string.IsNullOrEmpty(characterClass);
    }

    /// <summary>
    /// Возвращает количество занятых слотов в инвентаре
    /// </summary>
    private int GetOccupiedSlotsCount()
    {
        int count = 0;
        foreach (var slot in inventorySlots)
        {
            if (slot != null && slot.GetItem() != null)
            {
                count++;
            }
        }
        return count;
    }

    /// <summary>
    /// Пытается отправить JSON инвентаря на сервер
    /// </summary>
    private bool TrySendInventoryToServer(string inventoryJson, out string failureReason)
    {
        failureReason = "";

        if (!TryGetCharacterClass(out string characterClass))
        {
            failureReason = "Класс персонажа отсутствует в PlayerPrefs";
            Debug.LogError("[InventoryManager] ❌ AutoSync: Класс персонажа не найден (SelectedCharacterClass / SelectedClass)!");
            return false;
        }

        if (SocketIOManager.Instance == null)
        {
            failureReason = "SocketIOManager.Instance == null";
            Debug.LogError("[InventoryManager] ❌ AutoSync: SocketIOManager.Instance == null!");
            return false;
        }

        if (!SocketIOManager.Instance.IsConnected)
        {
            failureReason = "Socket.IO не подключен";
            Debug.LogWarning("[InventoryManager] ❌ AutoSync: Не подключен к серверу! IsConnected=false");
            return false;
        }

        Debug.Log("[InventoryManager] 🔄 AutoSync: Начинаем синхронизацию...");
        Debug.Log($"[InventoryManager] 📤 AutoSync: Отправляем инвентарь для класса '{characterClass}'");
        Debug.Log($"[InventoryManager] 📦 AutoSync: JSON длина = {inventoryJson.Length} символов");
        Debug.Log($"[InventoryManager] 🔍 AutoSync: JSON preview: {inventoryJson.Substring(0, Mathf.Min(200, inventoryJson.Length))}...");

        SocketIOManager.Instance.SyncInventory(characterClass, inventoryJson, (success) =>
        {
            if (success)
            {
                Debug.Log($"[InventoryManager] ✅✅✅ УСПЕХ! Инвентарь сохранён в MongoDB!");

                // КРИТИЧНО: Сбрасываем pending ТОЛЬКО после успешной синхронизации
                hasPendingSync = false;
                pendingSyncJson = "";
                Debug.Log($"[InventoryManager] ✅ Pending sync сброшен после успеха");
            }
            else
            {
                Debug.LogError($"[InventoryManager] ❌❌❌ ОШИБКА! Инвентарь НЕ сохранён!");

                // КРИТИЧНО: При ошибке ПОВТОРНО ставим в очередь
                QueuePendingSync(inventoryJson, "Сервер вернул success=false");
            }
        });

        Debug.Log($"[InventoryManager] 📤 Событие inventory_sync отправлено на сервер, ждём inventory_synced...");

        // ИСПРАВЛЕНИЕ: Возвращаем true (отправка успешна), но НЕ сбрасываем pending здесь!
        // Сброс pending произойдёт в callback при success=true
        return true;
    }

    /// <summary>
    /// Сохраняет JSON, который нужно отправить когда сервер будет готов
    /// </summary>
    private void QueuePendingSync(string inventoryJson, string reason)
    {
        pendingSyncJson = inventoryJson;
        hasPendingSync = true;
        nextPendingSyncTime = Time.time + pendingSyncRetryInterval;
        Debug.LogWarning($"[InventoryManager] ⏳ AutoSync отложен: {reason}. Повтор через {pendingSyncRetryInterval}с");
    }

    /// <summary>
    /// Пытается снова отправить инвентарь, если до этого сервер был недоступен
    /// </summary>
    private void TryFlushPendingSync()
    {
        if (!hasPendingSync || isLoadingFromServer)
            return;

        if (Time.time < nextPendingSyncTime)
            return;

        if (TrySendInventoryToServer(pendingSyncJson, out string failureReason))
        {
            hasPendingSync = false;
            pendingSyncJson = "";
            Debug.Log("[InventoryManager] ✅ Pending sync успешно отправлен");
        }
        else
        {
            nextPendingSyncTime = Time.time + pendingSyncRetryInterval;
            Debug.LogWarning($"[InventoryManager] ⚠️ Pending sync всё ещё ждёт: {failureReason}");
        }
    }

    /// <summary>
    /// Автоматическая загрузка инвентаря при входе в игру (когда всё готово)
    /// </summary>
    private void TryAutoLoadInventory()
    {
        if (isLoadingFromServer || isWaitingServerInventoryResponse)
            return;

        if (inventorySlots.Count == 0)
            return;

        if (!TryGetCharacterClass(out string currentClass))
            return;

        if (lastLoadedCharacterClass != currentClass)
        {
            Debug.Log($"[InventoryManager] 🔄 AutoLoad: обнаружен новый класс персонажа '{currentClass}', перезагружаем данные");
            hasLoadedFromServerOnce = false;
        }

        if (hasLoadedFromServerOnce)
            return;

        if (Time.time < nextAutoLoadAttemptTime)
            return;

        if (SocketIOManager.Instance == null || !SocketIOManager.Instance.IsConnected)
        {
            // Пробуем снова через 2 секунды
            nextAutoLoadAttemptTime = Time.time + autoLoadRetryDelay;
            return;
        }

        // ИЗМЕНЕНО: Убрали проверку CurrentRoomId - инвентарь должен загружаться всегда при подключении
        // Инвентарь - это персональные данные персонажа, не зависят от комнаты

        Debug.Log("[InventoryManager] 📥📥📥 AutoLoad: обнаружено подключение к серверу, запрашиваем данные инвентаря...");
        LoadInventoryFromServer();
    }

    /// <summary>
    /// Следим за состоянием подключения Socket.IO, чтобы повторно загружать инвентарь после переподключения
    /// </summary>
    private void MonitorSocketConnectionState()
    {
        bool currentlyConnected = SocketIOManager.Instance != null && SocketIOManager.Instance.IsConnected;

        if (!currentlyConnected && wasSocketConnected)
        {
            Debug.LogWarning("[InventoryManager] ⚠️ Подключение к серверу потеряно — разрешаем повторную загрузку инвентаря");
            hasLoadedFromServerOnce = false;
            isWaitingServerInventoryResponse = false;
            nextAutoLoadAttemptTime = Time.time;
        }

        wasSocketConnected = currentlyConnected;
    }

    /// <summary>
    /// Если JSON инвентаря пришёл до готовности UI, пробуем применить его позже
    /// </summary>
    private void TryApplyPendingInventoryJson()
    {
        if (!hasPendingInventoryJson)
            return;

        if (inventorySlots.Count == 0)
            return;

        string json = pendingInventoryJson;
        hasPendingInventoryJson = false;
        pendingInventoryJson = "";

        Debug.Log("[InventoryManager] 🔁 Применяем сохранённый JSON инвентаря после инициализации UI");
        LoadInventoryFromJson(json);

        // КРИТИЧНО: Принудительно обновляем UI всех слотов после загрузки
        Debug.Log("[InventoryManager] 🔄 Принудительное обновление UI всех слотов...");
        StartCoroutine(ForceRefreshAllSlots());
    }

    /// <summary>
    /// Принудительно обновляет UI всех слотов (с задержкой в 1 кадр)
    /// </summary>
    private System.Collections.IEnumerator ForceRefreshAllSlots()
    {
        yield return null; // Ждём 1 кадр чтобы UI точно обновился

        Debug.Log($"[InventoryManager] 🔄 ForceRefresh: проверяем {inventorySlots.Count} слотов");
        int refreshedCount = 0;

        foreach (var slot in inventorySlots)
        {
            if (slot != null && !slot.IsEmpty())
            {
                var item = slot.GetItem();
                var quantity = slot.GetQuantity();

                // Принудительно обновляем слот
                slot.SetItem(item, quantity);
                refreshedCount++;

                Debug.Log($"[InventoryManager] 🔄 Refreshed slot: {item.itemName} x{quantity}");
            }
        }

        Debug.Log($"[InventoryManager] ✅ ForceRefresh завершён: обновлено {refreshedCount} слотов");
    }

}

// ═══════════════════════════════════════════
// КЛАССЫ ДЛЯ СЕРИАЛИЗАЦИИ
// ═══════════════════════════════════════════

[System.Serializable]
public class InventoryData
{
    public List<ItemStackData> items = new List<ItemStackData>();
    public EquipmentData equipment = new EquipmentData();
}

[System.Serializable]
public class ItemStackData
{
    public string itemId = "";      // НОВОЕ: GUID предмета (приоритет)
    public string itemName = "";    // Старое: Имя предмета (для обратной совместимости)
    public int quantity;
}

[System.Serializable]
public class EquipmentData
{
    // НОВОЕ: GUID предметов (приоритет)
    public string weaponId = "";
    public string armorId = "";
    public string helmetId = "";
    public string accessoryId = "";

    // Старое: Имена предметов (для обратной совместимости)
    public string weapon = "";
    public string armor = "";
    public string helmet = "";
    public string accessory = "";
}
