using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Простой HUD с отображением основных характеристик (всегда на экране)
/// </summary>
public class SimpleStatsHUD : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private bool showHUD = true;
    [SerializeField] private int fontSize = 16;

    private Text hudText;
    private CharacterStats characterStats;
    private LevelingSystem levelingSystem;
    private HealthSystem healthSystem;
    private ManaSystem manaSystem;
    private ActionPointsSystem actionPointsSystem;

    void Start()
    {
        CreateHUD();
        // Не ищем сразу - персонаж генерируется динамически!
    }

    void Update()
    {
        // Если ещё не нашли системы - ищем каждый кадр (персонаж создаётся динамически)
        if (characterStats == null)
        {
            FindPlayerSystems();
        }

        if (showHUD && hudText != null && characterStats != null)
        {
            UpdateHUD();
        }
        else if (showHUD && hudText != null && characterStats == null)
        {
            // Показываем сообщение пока персонаж не загрузился
            hudText.text = "Loading character...";
        }

        // Toggle HUD с клавишей H
        if (Input.GetKeyDown(KeyCode.H))
        {
            showHUD = !showHUD;
            if (hudText != null)
            {
                hudText.gameObject.SetActive(showHUD);
            }
        }
    }

    private void FindPlayerSystems()
    {
        // Ищем персонажа по тегу
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            // Пробуем найти по компонентам
            CharacterStats[] allStats = FindObjectsByType<CharacterStats>(FindObjectsSortMode.None);
            if (allStats.Length > 0)
            {
                player = allStats[0].gameObject;
                Debug.Log("[SimpleStatsHUD] ✅ Персонаж найден через CharacterStats");
            }
            else
            {
                return; // Персонаж ещё не создан
            }
        }

        characterStats = player.GetComponentInChildren<CharacterStats>();
        levelingSystem = player.GetComponentInChildren<LevelingSystem>();
        healthSystem = player.GetComponentInChildren<HealthSystem>();
        manaSystem = player.GetComponentInChildren<ManaSystem>();
        actionPointsSystem = player.GetComponentInChildren<ActionPointsSystem>();

        if (characterStats != null)
        {
            Debug.Log($"[SimpleStatsHUD] ✅ Системы персонажа найдены! S:{characterStats.strength} P:{characterStats.perception} E:{characterStats.endurance}");
        }
    }

    private void UpdateHUD()
    {
        string text = "";

        // Уровень
        if (levelingSystem != null)
        {
            text += $"<b>LVL {levelingSystem.CurrentLevel}</b>  ";
            text += $"EXP: {levelingSystem.CurrentExperience}/{levelingSystem.GetExperienceForNextLevel()}";
            if (levelingSystem.AvailableStatPoints > 0)
            {
                text += $"  <color=yellow>★ {levelingSystem.AvailableStatPoints} points</color>";
            }
            text += "\n";
        }

        // HP
        if (healthSystem != null)
        {
            float hpPercent = healthSystem.HealthPercent * 100f;
            string hpColor = hpPercent > 50 ? "green" : (hpPercent > 25 ? "yellow" : "red");
            text += $"<color={hpColor}>HP:</color> {healthSystem.CurrentHealth:F0}/{healthSystem.MaxHealth:F0}  ";
        }

        // MP
        if (manaSystem != null)
        {
            text += $"<color=cyan>MP:</color> {manaSystem.CurrentMana:F0}/{manaSystem.MaxMana:F0}  ";
        }

        // AP (показываем current/max)
        if (actionPointsSystem != null)
        {
            int currentAP = actionPointsSystem.GetCurrentPoints();
            int maxAP = actionPointsSystem.GetMaxPoints();
            text += $"<color=yellow>AP:</color> {currentAP}/{maxAP}";
        }

        text += "\n";

        // SPECIAL в одну строку
        text += $"<size={fontSize - 2}>";
        text += $"S:{characterStats.strength} ";
        text += $"P:{characterStats.perception} ";
        text += $"E:{characterStats.endurance} ";
        text += $"W:{characterStats.wisdom} ";
        text += $"I:{characterStats.intelligence} ";
        text += $"A:{characterStats.agility} ";
        text += $"L:{characterStats.luck}";
        text += "</size>\n";

        // Доп. инфо
        text += $"<size={fontSize - 3}>";
        text += $"Vision: {characterStats.VisionRadius:F0}m  ";
        text += $"Crit: {characterStats.CritChance:F1}%";
        text += "</size>";

        text += "\n<size=10><i>Press C for details, H to toggle HUD</i></size>";

        hudText.text = text;
    }

    private void CreateHUD()
    {
        Debug.Log("[SimpleStatsHUD] 🔧 Начинаем создание HUD...");

        // Находим или создаём Canvas
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.Log("[SimpleStatsHUD] Canvas не найден, создаём новый...");
            GameObject canvasObj = new GameObject("StatsHUD_Canvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100; // Поверх всего!

            CanvasScaler scaler = canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            Debug.Log("[SimpleStatsHUD] ✅ Canvas создан с sortingOrder = 100");
        }
        else
        {
            Debug.Log($"[SimpleStatsHUD] Canvas найден: {canvas.name}, sortingOrder = {canvas.sortingOrder}");
            canvas.sortingOrder = Mathf.Max(canvas.sortingOrder, 100); // Убедимся что поверх всего
        }

        // Создаём HUD текст
        GameObject hudObj = new GameObject("StatsHUD");
        hudObj.transform.SetParent(canvas.transform, false);
        Debug.Log("[SimpleStatsHUD] HUD объект создан и прикреплён к Canvas");

        RectTransform hudRect = hudObj.AddComponent<RectTransform>();
        hudRect.anchorMin = new Vector2(0, 1); // Левый верхний угол
        hudRect.anchorMax = new Vector2(0, 1);
        hudRect.pivot = new Vector2(0, 1);
        hudRect.anchoredPosition = new Vector2(10, -10); // Отступ от края
        hudRect.sizeDelta = new Vector2(600, 120);
        Debug.Log($"[SimpleStatsHUD] RectTransform настроен: position={hudRect.anchoredPosition}, size={hudRect.sizeDelta}");

        hudText = hudObj.AddComponent<Text>();

        // Используем LegacyRuntime.ttf вместо устаревшего Arial.ttf
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null)
        {
            Debug.LogWarning("[SimpleStatsHUD] LegacyRuntime.ttf не найден, пробуем загрузить из Resources...");
            font = Resources.Load<Font>("Arial");
        }
        if (font != null)
        {
            hudText.font = font;
            Debug.Log($"[SimpleStatsHUD] Шрифт загружен: {font.name}");
        }
        else
        {
            Debug.LogError("[SimpleStatsHUD] Не удалось загрузить шрифт! Текст может не отображаться.");
        }

        hudText.fontSize = 18; // Увеличиваем размер для видимости
        hudText.color = Color.yellow; // ЯРКИЙ ЖЁЛТЫЙ для видимости!
        hudText.alignment = TextAnchor.UpperLeft;
        hudText.supportRichText = true;
        hudText.text = "=== STATS HUD ===\nLoading character...\nPress H to toggle";
        Debug.Log($"[SimpleStatsHUD] Text компонент создан: fontSize={hudText.fontSize}, color={hudText.color}, text='{hudText.text}'");

        // Добавляем Outline вместо Shadow для лучшей читаемости
        Outline outline = hudObj.AddComponent<Outline>();
        outline.effectColor = Color.black;
        outline.effectDistance = new Vector2(2, -2);
        Debug.Log("[SimpleStatsHUD] Outline добавлен для контрастности");

        Debug.Log("[SimpleStatsHUD] ✅ HUD создан полностью! Текст должен быть виден ЯРКО-ЖЁЛТЫМ в левом верхнем углу");
    }
}
