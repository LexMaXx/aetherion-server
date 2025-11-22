using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System;

namespace AetherionMMO.Inventory
{
    /// <summary>
    /// Менеджер экипировки для MMO системы
    /// Управляет экипированными предметами и их бонусами к статам
    /// </summary>
    public class MMOEquipmentManager : MonoBehaviour
    {
        public static MMOEquipmentManager Instance { get; private set; }

        [Header("Equipment UI")]
        [SerializeField] private GameObject equipmentPanel;
        [SerializeField] private MMOEquipmentSlot weaponSlot;
        [SerializeField] private MMOEquipmentSlot armorSlot;
        [SerializeField] private MMOEquipmentSlot helmetSlot;
        [SerializeField] private MMOEquipmentSlot accessorySlot;

        [Header("Alternative UI (EquipmentSlotUI)")]
        [SerializeField] private UI.EquipmentSlotUI weaponSlotUI;
        [SerializeField] private UI.EquipmentSlotUI armorSlotUI;
        [SerializeField] private UI.EquipmentSlotUI helmetSlotUI;
        [SerializeField] private UI.EquipmentSlotUI accessorySlotUI;

        [Header("Stats Display")]
        [SerializeField] private TextMeshProUGUI totalStatsText;

        // Текущая экипировка
        private Dictionary<EquipmentSlot, ItemData> equippedItems = new Dictionary<EquipmentSlot, ItemData>();

        // События
        public event Action<EquipmentSlot, ItemData> OnEquipmentChanged;
        public event Action OnStatsUpdated;

        private string characterClass;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            Debug.Log("[MMOEquipment] Singleton initialized");
        }

        [ContextMenu("Auto-Link Equipment Slots")]
        private void AutoLinkSlots()
        {
            Debug.Log("[MMOEquipment] Auto-linking slots...");

            // Ищем новые слоты (EquipmentSlotUI)
            var newSlots = FindObjectsOfType<UI.EquipmentSlotUI>();
            Debug.Log($"[MMOEquipment] Found {newSlots.Length} EquipmentSlotUI components");

            // Ищем старые слоты (MMOEquipmentSlot)
            var oldSlots = FindObjectsOfType<MMOEquipmentSlot>();
            Debug.Log($"[MMOEquipment] Found {oldSlots.Length} MMOEquipmentSlot components");

            // Ищем просто по имени GameObject (если компонентов нет)
            GameObject[] allObjects = GameObject.FindObjectsOfType<GameObject>();
            int foundByName = 0;
            foreach (var obj in allObjects)
            {
                string name = obj.name.ToLower();
                if (name.Contains("weapon") || name.Contains("armor") || name.Contains("helmet") || name.Contains("accessory"))
                {
                    if (name.Contains("slot"))
                    {
                        foundByName++;
                        Debug.Log($"[MMOEquipment] Found GameObject by name: {obj.name}");
                    }
                }
            }
            Debug.Log($"[MMOEquipment] Found {foundByName} GameObjects with 'slot' in name");

            int linkedCount = 0;

            // Пытаемся привязать новые слоты
            foreach (var slot in newSlots)
            {
                string slotName = slot.gameObject.name.ToLower();

                if (slotName.Contains("weapon"))
                {
                    weaponSlotUI = slot;
                    linkedCount++;
                    Debug.Log($"[MMOEquipment] ✅ Linked Weapon Slot: {slot.gameObject.name}");
                }
                else if (slotName.Contains("armor") && !slotName.Contains("helmet") && !slotName.Contains("accessory"))
                {
                    armorSlotUI = slot;
                    linkedCount++;
                    Debug.Log($"[MMOEquipment] ✅ Linked Armor Slot: {slot.gameObject.name}");
                }
                else if (slotName.Contains("helmet"))
                {
                    helmetSlotUI = slot;
                    linkedCount++;
                    Debug.Log($"[MMOEquipment] ✅ Linked Helmet Slot: {slot.gameObject.name}");
                }
                else if (slotName.Contains("accessory"))
                {
                    accessorySlotUI = slot;
                    linkedCount++;
                    Debug.Log($"[MMOEquipment] ✅ Linked Accessory Slot: {slot.gameObject.name}");
                }
            }

            Debug.Log($"[MMOEquipment] ✅ Auto-linked {linkedCount} out of 4 slots!");

            if (linkedCount == 4)
            {
                Debug.Log("[MMOEquipment] ✅✅✅ All slots linked successfully! Test: K → I → Double-click item → C");
            }
            else if (linkedCount == 0)
            {
                Debug.LogError($"[MMOEquipment] ❌ No slots linked!\n\n" +
                    $"PROBLEM: No EquipmentSlotUI components found on your slots.\n\n" +
                    $"SOLUTION: Add 'EquipmentSlotUI' component to WeaponSlot, ArmorSlot, HelmetSlot, AccessorySlot GameObjects.\n\n" +
                    $"Found {foundByName} GameObjects with 'slot' in name - please add EquipmentSlotUI component to them.");
            }
            else
            {
                Debug.LogWarning($"[MMOEquipment] ⚠️ Only {linkedCount} slots linked. Check GameObject names (should contain: weapon, armor, helmet, accessory)");
            }

            #if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
            #endif
        }

        void Start()
        {
            LoadCharacterClass();

            // Автоматически находим totalStatsText если не назначен
            if (totalStatsText == null)
            {
                // Вариант 1: Ищем по имени "StatsPanel"
                GameObject statsPanel = GameObject.Find("StatsPanel");
                if (statsPanel == null && equipmentPanel != null)
                {
                    // Вариант 2: Ищем в EquipmentPanel
                    statsPanel = equipmentPanel.transform.Find("StatsPanel")?.gameObject;
                }
                if (statsPanel == null && equipmentPanel != null)
                {
                    // Вариант 3: Ищем любой объект с "Stats" или "Bonus" в имени
                    foreach (Transform child in equipmentPanel.transform)
                    {
                        if (child.name.ToLower().Contains("stats") || child.name.ToLower().Contains("bonus"))
                        {
                            statsPanel = child.gameObject;
                            Debug.Log($"[MMOEquipment] Found stats panel by name: {child.name}");
                            break;
                        }
                    }
                }

                if (statsPanel != null)
                {
                    // Ищем TextMeshProUGUI в самом объекте или в детях
                    totalStatsText = statsPanel.GetComponent<TextMeshProUGUI>();
                    if (totalStatsText == null)
                    {
                        totalStatsText = statsPanel.GetComponentInChildren<TextMeshProUGUI>();
                    }

                    if (totalStatsText != null)
                    {
                        Debug.Log($"[MMOEquipment] ✅ Auto-found totalStatsText in: {statsPanel.name}");
                    }
                    else
                    {
                        Debug.LogWarning($"[MMOEquipment] Panel {statsPanel.name} found but has no TextMeshProUGUI!");
                    }
                }

                // Вариант 4: Ищем любой TextMeshPro с текстом содержащим "bonus" или числа "+0"
                if (totalStatsText == null && equipmentPanel != null)
                {
                    var allTexts = equipmentPanel.GetComponentsInChildren<TextMeshProUGUI>(true);
                    foreach (var text in allTexts)
                    {
                        string content = text.text.ToLower();
                        if (content.Contains("bonus") || content.Contains("+0") || content.Contains("+15"))
                        {
                            totalStatsText = text;
                            Debug.Log($"[MMOEquipment] ✅ Found totalStatsText by content: {text.gameObject.name}");
                            break;
                        }
                    }
                }

                if (totalStatsText == null)
                {
                    Debug.LogError("[MMOEquipment] ❌ Failed to find totalStatsText automatically! Please assign manually in Inspector.");
                }
            }

            InitializeEquipmentSlots();

            // ОТКЛЮЧЕНО: Управление теперь в EquipmentUI
            // if (equipmentPanel != null)
            // {
            //     equipmentPanel.SetActive(false);
            // }

            // Подписываемся на событие изменения статов
            OnStatsUpdated += ApplyStatsToCharacter;

            // Загружаем экипировку с сервера
            LoadEquipmentFromServer();
        }

        void OnDestroy()
        {
            // Отписываемся от событий
            OnStatsUpdated -= ApplyStatsToCharacter;
        }

        void Update()
        {
            // ОТКЛЮЧЕНО: Управление теперь в EquipmentUI
            // Клавиша C - открыть/закрыть экипировку
            // if (Input.GetKeyDown(KeyCode.C))
            // {
            //     ToggleEquipmentPanel();
            // }
        }

        /// <summary>
        /// Применить бонусы экипировки к персонажу
        /// </summary>
        private void ApplyStatsToCharacter()
        {
            var characterStats = FindObjectOfType<CharacterStats>();
            if (characterStats != null)
            {
                EquipmentStats stats = GetTotalEquipmentStats();
                characterStats.ApplyEquipmentBonuses(stats);
                Debug.Log($"[MMOEquipment] Equipment bonuses applied to CharacterStats");
            }
            else
            {
                Debug.LogWarning("[MMOEquipment] CharacterStats not found in scene!");
            }
        }

        /// <summary>
        /// Загрузить класс персонажа
        /// </summary>
        private void LoadCharacterClass()
        {
            characterClass = PlayerPrefs.GetString("SelectedCharacterClass", "");
            Debug.Log($"[MMOEquipment] Character class: {characterClass}");
        }

        /// <summary>
        /// Инициализация слотов экипировки
        /// </summary>
        private void InitializeEquipmentSlots()
        {
            // Старые слоты (MMOEquipmentSlot)
            if (weaponSlot != null) weaponSlot.Initialize(EquipmentSlot.Weapon, this);
            if (armorSlot != null) armorSlot.Initialize(EquipmentSlot.Armor, this);
            if (helmetSlot != null) helmetSlot.Initialize(EquipmentSlot.Helmet, this);
            if (accessorySlot != null) accessorySlot.Initialize(EquipmentSlot.Accessory, this);

            // Новые слоты (EquipmentSlotUI) - используют EquipmentUI для инициализации
            // Найдем EquipmentUI если он есть
            var equipmentUI = FindObjectOfType<UI.EquipmentUI>();
            if (equipmentUI != null)
            {
                if (weaponSlotUI != null) weaponSlotUI.Initialize(EquipmentSlot.Weapon, equipmentUI);
                if (armorSlotUI != null) armorSlotUI.Initialize(EquipmentSlot.Armor, equipmentUI);
                if (helmetSlotUI != null) helmetSlotUI.Initialize(EquipmentSlot.Helmet, equipmentUI);
                if (accessorySlotUI != null) accessorySlotUI.Initialize(EquipmentSlot.Accessory, equipmentUI);
                Debug.Log("[MMOEquipment] New equipment slots (EquipmentSlotUI) initialized");
            }
            else
            {
                Debug.LogWarning("[MMOEquipment] EquipmentUI not found - new slots won't be initialized");
            }

            Debug.Log("[MMOEquipment] Equipment slots initialized");
        }

        /// <summary>
        /// Переключить панель экипировки
        /// </summary>
        public void ToggleEquipmentPanel()
        {
            if (equipmentPanel != null)
            {
                bool newState = !equipmentPanel.activeSelf;
                equipmentPanel.SetActive(newState);
                Debug.Log($"[MMOEquipment] Panel {(newState ? "opened" : "closed")}");
            }
        }

        /// <summary>
        /// Экипировать предмет
        /// </summary>
        public void EquipItem(ItemData item)
        {
            if (item == null || !item.isEquippable)
            {
                Debug.LogWarning($"[MMOEquipment] Cannot equip item: {item?.itemName ?? "null"}");
                return;
            }

            Debug.Log($"[MMOEquipment] Equipping: {item.itemName} to slot {item.equipmentSlot}");

            // Снимаем текущий предмет если есть
            if (equippedItems.ContainsKey(item.equipmentSlot))
            {
                UnequipItem(item.equipmentSlot);
            }

            // Экипируем новый предмет
            equippedItems[item.equipmentSlot] = item;

            // Обновляем UI слота
            UpdateEquipmentSlotUI(item.equipmentSlot, item);

            // Удаляем предмет из инвентаря
            MongoInventoryManager.Instance?.RemoveItem(item, 1);

            // Синхронизируем с сервером
            SyncEquipmentToServer();

            // Обновляем статы
            UpdateTotalStats();

            // Отправляем изменение экипировки всем игрокам в PvP
            SendEquipmentChangeToServer(item.equipmentSlot, item, true);

            // Вызываем событие
            OnEquipmentChanged?.Invoke(item.equipmentSlot, item);

            Debug.Log($"[MMOEquipment] ✅ Equipped: {item.itemName}");
        }

        /// <summary>
        /// Снять предмет
        /// </summary>
        public void UnequipItem(EquipmentSlot slot)
        {
            if (!equippedItems.ContainsKey(slot))
            {
                Debug.LogWarning($"[MMOEquipment] No item in slot {slot}");
                return;
            }

            ItemData item = equippedItems[slot];
            Debug.Log($"[MMOEquipment] Unequipping: {item.itemName} from slot {slot}");

            // Убираем из экипировки
            equippedItems.Remove(slot);

            // Обновляем UI слота
            UpdateEquipmentSlotUI(slot, null);

            // Возвращаем предмет в инвентарь
            MongoInventoryManager.Instance?.AddItem(item, 1);

            // Синхронизируем с сервером
            SyncEquipmentToServer();

            // Обновляем статы
            UpdateTotalStats();

            // Отправляем изменение экипировки всем игрокам в PvP
            SendEquipmentChangeToServer(slot, item, false);

            // Вызываем событие
            OnEquipmentChanged?.Invoke(slot, null);

            Debug.Log($"[MMOEquipment] ✅ Unequipped: {item.itemName}");
        }

        /// <summary>
        /// Обновить UI слота экипировки
        /// </summary>
        private void UpdateEquipmentSlotUI(EquipmentSlot slot, ItemData item)
        {
            // Пытаемся обновить старый тип слотов (MMOEquipmentSlot)
            MMOEquipmentSlot uiSlot = GetEquipmentSlotUI(slot);
            if (uiSlot != null)
            {
                Debug.Log($"[MMOEquipment] Updating MMO UI slot {slot} with {item?.itemName ?? "null"}");
                uiSlot.SetItem(item);
                return;
            }

            // Пытаемся обновить новый тип слотов (EquipmentSlotUI)
            UI.EquipmentSlotUI newSlot = GetEquipmentSlotUINew(slot);
            if (newSlot != null)
            {
                Debug.Log($"[MMOEquipment] Updating new UI slot {slot} with {item?.itemName ?? "null"}");
                newSlot.SetItem(item);
                return;
            }

            Debug.LogWarning($"[MMOEquipment] ❌ UI slot {slot} is NULL! Cannot update UI. Check Inspector references.");
        }

        /// <summary>
        /// Получить новый UI слот (EquipmentSlotUI)
        /// </summary>
        private UI.EquipmentSlotUI GetEquipmentSlotUINew(EquipmentSlot slot)
        {
            return slot switch
            {
                EquipmentSlot.Weapon => weaponSlotUI,
                EquipmentSlot.Armor => armorSlotUI,
                EquipmentSlot.Helmet => helmetSlotUI,
                EquipmentSlot.Accessory => accessorySlotUI,
                _ => null
            };
        }

        /// <summary>
        /// Получить UI слот по типу
        /// </summary>
        private MMOEquipmentSlot GetEquipmentSlotUI(EquipmentSlot slot)
        {
            return slot switch
            {
                EquipmentSlot.Weapon => weaponSlot,
                EquipmentSlot.Armor => armorSlot,
                EquipmentSlot.Helmet => helmetSlot,
                EquipmentSlot.Accessory => accessorySlot,
                _ => null
            };
        }

        /// <summary>
        /// Рассчитать суммарные бонусы от экипировки
        /// </summary>
        public EquipmentStats GetTotalEquipmentStats()
        {
            EquipmentStats stats = new EquipmentStats();

            foreach (var kvp in equippedItems)
            {
                ItemData item = kvp.Value;
                stats.attackBonus += item.attackBonus;
                stats.defenseBonus += item.defenseBonus;
                stats.healthBonus += item.healthBonus;
                stats.manaBonus += item.manaBonus;
            }

            return stats;
        }

        /// <summary>
        /// Обновить отображение общих статов
        /// </summary>
        private void UpdateTotalStats()
        {
            EquipmentStats stats = GetTotalEquipmentStats();

            if (totalStatsText != null)
            {
                // Версия БЕЗ эмодзи (если шрифт не поддерживает)
                string statsString = $"<b>Equipment Bonuses:</b>\n\n" +
                    $"<color=#FF6B6B>Attack: +{stats.attackBonus}</color>\n" +
                    $"<color=#6BB6FF>Defense: +{stats.defenseBonus}</color>\n" +
                    $"<color=#FF4D4D>Health: +{stats.healthBonus}</color>\n" +
                    $"<color=#4DA6FF>Mana: +{stats.manaBonus}</color>";

                // Версия С эмодзи (раскомментируйте если шрифт поддерживает)
                // string statsString = $"<b>Equipment Bonuses:</b>\n\n" +
                //     $"<color=#FF6B6B>⚔️ Attack: +{stats.attackBonus}</color>\n" +
                //     $"<color=#6BB6FF>🛡️ Defense: +{stats.defenseBonus}</color>\n" +
                //     $"<color=#FF4D4D>❤️ Health: +{stats.healthBonus}</color>\n" +
                //     $"<color=#4DA6FF>✨ Mana: +{stats.manaBonus}</color>";

                totalStatsText.text = statsString;
                Debug.Log($"[MMOEquipment] Stats text set to: {statsString}");
            }
            else
            {
                Debug.LogError("[MMOEquipment] ❌ totalStatsText is NULL! Cannot update stats display.");
            }

            OnStatsUpdated?.Invoke();
            Debug.Log($"[MMOEquipment] Stats updated: ATK+{stats.attackBonus} DEF+{stats.defenseBonus} HP+{stats.healthBonus} MP+{stats.manaBonus}");
        }

        /// <summary>
        /// Загрузить экипировку с сервера
        /// </summary>
        private void LoadEquipmentFromServer()
        {
            if (string.IsNullOrEmpty(characterClass))
            {
                Debug.LogError("[MMOEquipment] Character class not set!");
                return;
            }

            if (SocketIOManager.Instance == null || !SocketIOManager.Instance.IsConnected)
            {
                Debug.LogWarning("[MMOEquipment] Not connected to server");
                return;
            }

            Debug.Log($"[MMOEquipment] Loading equipment for {characterClass}...");

            var request = new { characterClass = characterClass };
            string json = JsonUtility.ToJson(request);

            SocketIOManager.Instance.EmitCustomEvent("mmo_load_equipment", json, (response) =>
            {
                Debug.Log($"[MMOEquipment] Equipment loaded: {response}");
                // TODO: Parse and apply equipment from server
            });
        }

        /// <summary>
        /// Синхронизировать экипировку с сервером
        /// </summary>
        private void SyncEquipmentToServer()
        {
            if (string.IsNullOrEmpty(characterClass))
            {
                Debug.LogError("[MMOEquipment] Character class not set!");
                return;
            }

            if (SocketIOManager.Instance == null || !SocketIOManager.Instance.IsConnected)
            {
                Debug.LogWarning("[MMOEquipment] Not connected to server");
                return;
            }

            // Собираем данные экипировки
            var equipmentData = new EquipmentSyncData
            {
                characterClass = characterClass,
                weapon = equippedItems.ContainsKey(EquipmentSlot.Weapon) ? equippedItems[EquipmentSlot.Weapon].itemName : "",
                armor = equippedItems.ContainsKey(EquipmentSlot.Armor) ? equippedItems[EquipmentSlot.Armor].itemName : "",
                helmet = equippedItems.ContainsKey(EquipmentSlot.Helmet) ? equippedItems[EquipmentSlot.Helmet].itemName : "",
                accessory = equippedItems.ContainsKey(EquipmentSlot.Accessory) ? equippedItems[EquipmentSlot.Accessory].itemName : ""
            };

            string json = JsonUtility.ToJson(equipmentData);
            Debug.Log($"[MMOEquipment] Syncing equipment: {json}");

            SocketIOManager.Instance.EmitCustomEvent("mmo_update_equipment", json, (response) =>
            {
                Debug.Log($"[MMOEquipment] Equipment synced: {response}");
            });
        }

        /// <summary>
        /// Отправить изменение экипировки на сервер для PvP синхронизации
        /// </summary>
        private void SendEquipmentChangeToServer(EquipmentSlot slot, ItemData item, bool isEquip)
        {
            var socketManager = FindObjectOfType<SocketIOManager>();
            if (socketManager == null || !socketManager.IsConnected)
            {
                Debug.LogWarning("[MMOEquipment] ⚠️ Not connected to server, equipment change local only");
                return;
            }

            // Получаем текущие статы персонажа после изменения экипировки
            var healthSystem = FindObjectOfType<HealthSystem>();
            var characterStats = FindObjectOfType<CharacterStats>();
            var manaSystem = FindObjectOfType<ManaSystem>();

            // Собираем суммарные бонусы от ВСЕЙ экипировки
            EquipmentStats totalStats = GetTotalEquipmentStats();

            var data = new
            {
                slotType = slot.ToString(),
                itemName = item?.itemName ?? "",
                isEquip = isEquip,
                // Бонусы от конкретного предмета
                attackBonus = item?.attackBonus ?? 0,
                defenseBonus = item?.defenseBonus ?? 0,
                healthBonus = item?.healthBonus ?? 0,
                manaBonus = item?.manaBonus ?? 0,
                // Суммарные бонусы от ВСЕЙ экипировки
                totalAttackBonus = totalStats.attackBonus,
                totalDefenseBonus = totalStats.defenseBonus,
                totalHealthBonus = totalStats.healthBonus,
                totalManaBonus = totalStats.manaBonus,
                // Текущие значения персонажа (с учетом экипировки)
                currentHealth = healthSystem != null ? healthSystem.CurrentHealth : 0f,
                maxHealth = healthSystem != null ? healthSystem.MaxHealth : 0f,
                currentMana = manaSystem != null ? manaSystem.CurrentMana : 0f,
                maxMana = manaSystem != null ? manaSystem.MaxMana : 0f,
                attack = characterStats != null ? (int)characterStats.physicalDamage : 0,
                defense = characterStats != null ? (int)characterStats.physicalDefense : 0
            };

            string json = Newtonsoft.Json.JsonConvert.SerializeObject(data);
            socketManager.Emit("equipment_changed", json);

            Debug.Log($"[MMOEquipment] 📡 Sent equipment change to server: {slot} {(isEquip ? "equipped" : "unequipped")} {item?.itemName ?? "null"}");
            Debug.Log($"[MMOEquipment] 📊 Total stats sent: ATK+{totalStats.attackBonus} DEF+{totalStats.defenseBonus} HP+{totalStats.healthBonus} MP+{totalStats.manaBonus}");
        }

        /// <summary>
        /// Получить экипированный предмет
        /// </summary>
        public ItemData GetEquippedItem(EquipmentSlot slot)
        {
            return equippedItems.ContainsKey(slot) ? equippedItems[slot] : null;
        }
    }

    /// <summary>
    /// Структура для хранения суммарных бонусов от экипировки
    /// </summary>
    [Serializable]
    public class EquipmentStats
    {
        public int attackBonus = 0;
        public int defenseBonus = 0;
        public int healthBonus = 0;
        public int manaBonus = 0;
    }

    /// <summary>
    /// Данные для синхронизации экипировки с сервером
    /// </summary>
    [Serializable]
    public class EquipmentSyncData
    {
        public string characterClass;
        public string weapon;
        public string armor;
        public string helmet;
        public string accessory;
    }
}
