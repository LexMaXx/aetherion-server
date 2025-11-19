using UnityEngine;

/// <summary>
/// Улучшенный тестер инвентаря с автоматической диагностикой и настройкой
/// Автоматически проверяет и настраивает систему для корректного сохранения
/// </summary>
public class ImprovedInventoryTester : MonoBehaviour
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

    [Tooltip("Количество зелий для добавления")]
    public int potionQuantity = 5;

    [Header("Auto Setup Settings")]
    [Tooltip("Автоматически устанавливать класс персонажа для тестирования")]
    public bool autoSetupCharacterClass = true;

    [Tooltip("Класс персонажа по умолчанию для тестирования")]
    public string defaultCharacterClass = "Warrior";

    [Tooltip("Автоматически подключаться к серверу при старте")]
    public bool autoConnectSocket = true;

    [Header("Debug Settings")]
    [Tooltip("Показывать детальные логи")]
    public bool verboseLogging = true;

    private bool hasSetupRun = false;
    private float setupCheckTime = 2f; // Проверка через 2 секунды после старта

    void Start()
    {
        if (autoSetupCharacterClass)
        {
            SetupCharacterClass();
        }

        if (autoConnectSocket)
        {
            EnsureSocketConnected();
        }
    }

    /// <summary>
    /// Устанавливает класс персонажа для тестирования
    /// </summary>
    void SetupCharacterClass()
    {
        string currentClass = PlayerPrefs.GetString("SelectedCharacterClass", "");
        if (string.IsNullOrEmpty(currentClass))
        {
            PlayerPrefs.SetString("SelectedCharacterClass", defaultCharacterClass);
            PlayerPrefs.Save();
            Debug.Log($"[ImprovedInventoryTester] ✅ Установлен тестовый класс: {defaultCharacterClass}");
        }
        else
        {
            if (verboseLogging)
                Debug.Log($"[ImprovedInventoryTester] ℹ️ Класс уже установлен: {currentClass}");
        }
    }

    /// <summary>
    /// Убеждается что SocketIO подключен
    /// </summary>
    void EnsureSocketConnected()
    {
        if (SocketIOManager.Instance != null && !SocketIOManager.Instance.IsConnected)
        {
            // Проверяем наличие JWT токена
            string token = PlayerPrefs.GetString("UserToken", "");

            if (string.IsNullOrEmpty(token))
            {
                Debug.LogWarning("[ImprovedInventoryTester] ⚠️ JWT токен не найден в PlayerPrefs");
                Debug.LogWarning("[ImprovedInventoryTester] 💡 Пройдите через LoginScene для авторизации");
                return;
            }

            Debug.Log("[ImprovedInventoryTester] 🔌 Подключаем SocketIO...");
            SocketIOManager.Instance.Connect(token, (success) =>
            {
                if (success)
                {
                    Debug.Log("[ImprovedInventoryTester] ✅ SocketIO подключен успешно!");
                }
                else
                {
                    Debug.LogError("[ImprovedInventoryTester] ❌ Не удалось подключиться к SocketIO");
                }
            });
        }
    }

    void Update()
    {
        // Первая проверка системы через 2 секунды после старта
        if (!hasSetupRun && Time.time > setupCheckTime)
        {
            CheckSystemReady();
            hasSetupRun = true;
        }

        // Клавиша 1 - добавить оружие
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            AddItemSafely(testWeapon, 1, "Weapon");
        }

        // Клавиша 2 - добавить броню
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            AddItemSafely(testArmor, 1, "Armor");
        }

        // Клавиша 3 - добавить шлем
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            AddItemSafely(testHelmet, 1, "Helmet");
        }

        // Клавиша 4 - добавить аксессуар
        if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            AddItemSafely(testAccessory, 1, "Accessory");
        }

        // Клавиша 5 - добавить зелья
        if (Input.GetKeyDown(KeyCode.Alpha5))
        {
            AddItemSafely(testPotion, potionQuantity, "Potion");
        }

        // Клавиша F9 - принудительная синхронизация
        if (Input.GetKeyDown(KeyCode.F9))
        {
            ForceSyncInventory();
        }

        // Клавиша F10 - загрузить с сервера
        if (Input.GetKeyDown(KeyCode.F10))
        {
            ForceLoadInventory();
        }

        // Клавиша F8 - повторная проверка системы
        if (Input.GetKeyDown(KeyCode.F8))
        {
            CheckSystemReady();
        }

        // Клавиша C - очистить инвентарь (TODO)
        if (Input.GetKeyDown(KeyCode.C))
        {
            Debug.Log("[ImprovedInventoryTester] ℹ️ Очистка инвентаря пока не реализована");
        }
    }

    /// <summary>
    /// Проверяет готовность всех систем для сохранения инвентаря
    /// </summary>
    void CheckSystemReady()
    {
        Debug.Log("═══════════════════════════════════════════");
        Debug.Log("[ImprovedInventoryTester] 🔍 ДИАГНОСТИКА СИСТЕМЫ:");
        Debug.Log("═══════════════════════════════════════════");

        bool allOk = true;

        // Проверка 1: InventoryManager
        if (InventoryManager.Instance == null)
        {
            Debug.LogError("❌ КРИТИЧНО: InventoryManager.Instance == null");
            Debug.LogError("   💡 Убедитесь что InventoryManager существует в сцене");
            allOk = false;
        }
        else
        {
            Debug.Log("✅ InventoryManager готов");
        }

        // Проверка 2: SocketIOManager
        if (SocketIOManager.Instance == null)
        {
            Debug.LogError("❌ КРИТИЧНО: SocketIOManager.Instance == null");
            Debug.LogError("   💡 Убедитесь что SocketIOManager существует в сцене");
            allOk = false;
        }
        else
        {
            if (!SocketIOManager.Instance.IsConnected)
            {
                Debug.LogWarning("⚠️ ВНИМАНИЕ: SocketIO не подключен");
                Debug.LogWarning($"   SocketId: {SocketIOManager.Instance.SocketId ?? "null"}");
                Debug.LogWarning("   💡 Предметы будут добавлены, но НЕ сохранены в БД");
                Debug.LogWarning("   💡 Нажмите F9 после подключения для ручной синхронизации");

                if (autoConnectSocket)
                {
                    Debug.Log("   🔌 Попытка подключения...");

                    // Проверяем наличие JWT токена
                    string token = PlayerPrefs.GetString("UserToken", "");

                    if (!string.IsNullOrEmpty(token))
                    {
                        SocketIOManager.Instance.Connect(token, (success) =>
                        {
                            if (success)
                            {
                                Debug.Log("   ✅ Подключение успешно!");
                            }
                            else
                            {
                                Debug.LogError("   ❌ Подключение не удалось!");
                            }
                        });
                    }
                    else
                    {
                        Debug.LogWarning("   ⚠️ JWT токен не найден - пройдите через LoginScene");
                    }
                }

                allOk = false;
            }
            else
            {
                Debug.Log($"✅ SocketIO подключен (SocketId: {SocketIOManager.Instance.SocketId})");
            }
        }

        // Проверка 3: Класс персонажа
        string characterClass = PlayerPrefs.GetString("SelectedCharacterClass", "");
        if (string.IsNullOrEmpty(characterClass))
        {
            characterClass = PlayerPrefs.GetString("SelectedClass", "");
        }

        if (string.IsNullOrEmpty(characterClass))
        {
            Debug.LogError("❌ КРИТИЧНО: Класс персонажа не установлен");
            Debug.LogError("   💡 PlayerPrefs не содержит 'SelectedCharacterClass'");
            Debug.LogError("   💡 Сохранение в БД невозможно");
            allOk = false;
        }
        else
        {
            Debug.Log($"✅ Класс персонажа: {characterClass}");
        }

        // Итоговый статус
        Debug.Log("═══════════════════════════════════════════");
        if (allOk)
        {
            Debug.Log("✅✅✅ ВСЕ СИСТЕМЫ ГОТОВЫ - АВТОСОХРАНЕНИЕ РАБОТАЕТ");
        }
        else
        {
            Debug.LogWarning("⚠️⚠️⚠️ ЕСТЬ ПРОБЛЕМЫ - АВТОСОХРАНЕНИЕ НЕ РАБОТАЕТ");
            Debug.LogWarning("💡 Исправьте ошибки выше или используйте F9 для ручной синхронизации");
        }
        Debug.Log("═══════════════════════════════════════════");
    }

    /// <summary>
    /// Безопасно добавляет предмет с проверками
    /// </summary>
    void AddItemSafely(ItemData item, int quantity, string itemType)
    {
        if (item == null)
        {
            Debug.LogWarning($"[ImprovedInventoryTester] ⚠️ Test {itemType} не назначен в Inspector!");
            Debug.LogWarning($"   💡 Перетащите ItemData в поле test{itemType} в Inspector");
            return;
        }

        if (InventoryManager.Instance == null)
        {
            Debug.LogError("[ImprovedInventoryTester] ❌ InventoryManager не найден!");
            return;
        }

        // Проверяем готовность SocketIO ПЕРЕД добавлением
        bool socketReady = SocketIOManager.Instance != null && SocketIOManager.Instance.IsConnected;
        string characterClass = PlayerPrefs.GetString("SelectedCharacterClass", "");
        if (string.IsNullOrEmpty(characterClass))
        {
            characterClass = PlayerPrefs.GetString("SelectedClass", "");
        }
        bool classReady = !string.IsNullOrEmpty(characterClass);

        // Предупреждаем если сохранение не сработает
        if (!socketReady)
        {
            Debug.LogWarning("═══════════════════════════════════════════");
            Debug.LogWarning("[ImprovedInventoryTester] ⚠️ ВНИМАНИЕ:");
            Debug.LogWarning("   SocketIO не подключен!");
            Debug.LogWarning("   Предмет будет добавлен в UI, но НЕ сохранён в MongoDB!");
            Debug.LogWarning("   💡 Нажмите F9 после подключения для ручной синхронизации");
            Debug.LogWarning("═══════════════════════════════════════════");
        }

        if (!classReady)
        {
            Debug.LogError("═══════════════════════════════════════════");
            Debug.LogError("[ImprovedInventoryTester] ❌ КРИТИЧНО:");
            Debug.LogError("   Класс персонажа не установлен!");
            Debug.LogError("   Сохранение в БД невозможно!");
            Debug.LogError("   💡 Установите autoSetupCharacterClass = true в Inspector");
            Debug.LogError("═══════════════════════════════════════════");
        }

        // Добавляем предмет
        bool added = InventoryManager.Instance.AddItem(item, quantity);

        if (added)
        {
            Debug.Log($"[ImprovedInventoryTester] ✅ Добавлено в UI: {quantity}x {item.itemName}");

            if (socketReady && classReady)
            {
                Debug.Log($"[ImprovedInventoryTester] 📤 Автосохранение запущено... (смотрите логи InventoryManager)");
            }
            else
            {
                Debug.LogWarning($"[ImprovedInventoryTester] ⚠️ Автосохранение НЕ запустится (нет подключения или класса)");
            }
        }
        else
        {
            Debug.LogError($"[ImprovedInventoryTester] ❌ Не удалось добавить {item.itemName}");
            Debug.LogError($"   Причина: Инвентарь полон (40/40 слотов)");
        }
    }

    /// <summary>
    /// Принудительная синхронизация с сервером
    /// </summary>
    void ForceSyncInventory()
    {
        Debug.Log("═══════════════════════════════════════════");
        Debug.Log("[ImprovedInventoryTester] 🔄 ПРИНУДИТЕЛЬНАЯ СИНХРОНИЗАЦИЯ");
        Debug.Log("═══════════════════════════════════════════");

        if (InventoryManager.Instance == null)
        {
            Debug.LogError("[ImprovedInventoryTester] ❌ InventoryManager не найден!");
            return;
        }

        if (SocketIOManager.Instance == null || !SocketIOManager.Instance.IsConnected)
        {
            Debug.LogError("[ImprovedInventoryTester] ❌ SocketIO не подключен!");
            Debug.LogError("   💡 Подключитесь к серверу и попробуйте снова");
            return;
        }

        string characterClass = PlayerPrefs.GetString("SelectedCharacterClass", "");
        if (string.IsNullOrEmpty(characterClass))
        {
            characterClass = PlayerPrefs.GetString("SelectedClass", "");
        }

        if (string.IsNullOrEmpty(characterClass))
        {
            Debug.LogError("[ImprovedInventoryTester] ❌ Класс персонажа не установлен!");
            return;
        }

        Debug.Log($"[ImprovedInventoryTester] 📤 Отправка данных на сервер...");
        Debug.Log($"   Класс: {characterClass}");
        Debug.Log($"   SocketId: {SocketIOManager.Instance.SocketId}");

        InventoryManager.Instance.SyncInventoryToServer();

        Debug.Log("[ImprovedInventoryTester] ⏳ Ожидание ответа от сервера...");
        Debug.Log("   Проверьте логи InventoryManager для подтверждения");
    }

    /// <summary>
    /// Загрузка инвентаря с сервера
    /// </summary>
    void ForceLoadInventory()
    {
        Debug.Log("═══════════════════════════════════════════");
        Debug.Log("[ImprovedInventoryTester] 📥 ЗАГРУЗКА С СЕРВЕРА");
        Debug.Log("═══════════════════════════════════════════");

        if (InventoryManager.Instance == null)
        {
            Debug.LogError("[ImprovedInventoryTester] ❌ InventoryManager не найден!");
            return;
        }

        if (SocketIOManager.Instance == null || !SocketIOManager.Instance.IsConnected)
        {
            Debug.LogError("[ImprovedInventoryTester] ❌ SocketIO не подключен!");
            Debug.LogError("   💡 Подключитесь к серверу и попробуйте снова");
            return;
        }

        string characterClass = PlayerPrefs.GetString("SelectedCharacterClass", "");
        if (string.IsNullOrEmpty(characterClass))
        {
            characterClass = PlayerPrefs.GetString("SelectedClass", "");
        }

        if (string.IsNullOrEmpty(characterClass))
        {
            Debug.LogError("[ImprovedInventoryTester] ❌ Класс персонажа не установлен!");
            return;
        }

        Debug.Log($"[ImprovedInventoryTester] 📥 Запрос данных с сервера...");
        Debug.Log($"   Класс: {characterClass}");
        Debug.Log($"   SocketId: {SocketIOManager.Instance.SocketId}");

        InventoryManager.Instance.LoadInventoryFromServer();

        Debug.Log("[ImprovedInventoryTester] ⏳ Ожидание ответа от сервера...");
        Debug.Log("   Проверьте логи InventoryManager для подтверждения");
    }

    /// <summary>
    /// Отображает UI с инструкциями и статусом
    /// </summary>
    void OnGUI()
    {
        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.fontSize = 14;

        // Определяем статус системы
        bool socketReady = SocketIOManager.Instance != null && SocketIOManager.Instance.IsConnected;
        string characterClass = PlayerPrefs.GetString("SelectedCharacterClass", "");
        if (string.IsNullOrEmpty(characterClass))
        {
            characterClass = PlayerPrefs.GetString("SelectedClass", "НЕ УСТАНОВЛЕН");
        }
        bool classReady = characterClass != "НЕ УСТАНОВЛЕН";

        // Цвет зависит от статуса
        if (socketReady && classReady)
        {
            style.normal.textColor = Color.green;
        }
        else if (socketReady || classReady)
        {
            style.normal.textColor = Color.yellow;
        }
        else
        {
            style.normal.textColor = Color.red;
        }

        string status;
        if (socketReady && classReady)
        {
            status = "✅ ГОТОВО - АВТОСОХРАНЕНИЕ РАБОТАЕТ";
        }
        else if (!socketReady && !classReady)
        {
            status = "❌ НЕ ГОТОВО - НЕТ ПОДКЛЮЧЕНИЯ И КЛАССА";
        }
        else if (!socketReady)
        {
            status = "⚠️ ВНИМАНИЕ - НЕТ ПОДКЛЮЧЕНИЯ К СЕРВЕРУ";
        }
        else
        {
            status = "⚠️ ВНИМАНИЕ - КЛАСС НЕ УСТАНОВЛЕН";
        }

        GUI.Label(new Rect(10, 10, 600, 300),
            $"IMPROVED INVENTORY TESTER:\n\n" +
            $"Статус: {status}\n" +
            $"Класс: {characterClass}\n" +
            $"Socket: {(socketReady ? "✅ Подключен" : "❌ Не подключен")}\n\n" +
            $"═══════════════════════════════════════════\n" +
            $"ДОБАВИТЬ ПРЕДМЕТЫ:\n" +
            $"1 - Оружие\n" +
            $"2 - Броня\n" +
            $"3 - Шлем\n" +
            $"4 - Аксессуар\n" +
            $"5 - Зелья (x{potionQuantity})\n\n" +
            $"УПРАВЛЕНИЕ:\n" +
            $"I - Открыть/закрыть инвентарь\n" +
            $"F8 - Проверить систему\n" +
            $"F9 - ПРИНУДИТЕЛЬНАЯ СИНХРОНИЗАЦИЯ\n" +
            $"F10 - ЗАГРУЗИТЬ С СЕРВЕРА",
            style
        );
    }
}
