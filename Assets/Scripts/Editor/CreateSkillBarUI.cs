using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

/// <summary>
/// Создает UI панель скиллов для Arena Scene (3 иконки внизу справа)
/// Запуск: Tools → Aetherion → Create Skill Bar UI (Arena Scene)
/// </summary>
public class CreateSkillBarUI : Editor
{
    [MenuItem("Tools/Aetherion/Create Skill Bar UI (Arena Scene)")]
    public static void CreateSkillBar()
    {
        Debug.Log("[CreateSkillBarUI] Начинаю создание UI панели скиллов для Arena Scene...");

        // Находим или создаем Canvas
        Canvas canvas = Object.FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("Canvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
            Debug.Log("[CreateSkillBarUI] ✅ Создан новый Canvas");
        }

        // Проверяем есть ли уже SkillBar
        SkillBarUI existingBar = Object.FindObjectOfType<SkillBarUI>();
        if (existingBar != null)
        {
            if (EditorUtility.DisplayDialog(
                "Skill Bar уже существует",
                "SkillBarUI уже есть в сцене. Удалить и создать заново?",
                "Да, пересоздать",
                "Отмена"))
            {
                DestroyImmediate(existingBar.gameObject);
                Debug.Log("[CreateSkillBarUI] Старый SkillBar удалён");
            }
            else
            {
                Debug.Log("[CreateSkillBarUI] Отменено пользователем");
                return;
            }
        }

        // Создаем контейнер SkillBar
        GameObject skillBarObj = new GameObject("SkillBar");
        skillBarObj.transform.SetParent(canvas.transform, false);

        RectTransform skillBarRect = skillBarObj.AddComponent<RectTransform>();

        // Позиция: внизу справа
        // Anchors: bottom-right (1, 0)
        skillBarRect.anchorMin = new Vector2(1, 0);
        skillBarRect.anchorMax = new Vector2(1, 0);
        skillBarRect.pivot = new Vector2(1, 0);

        // Размер: 3 иконки по 80px + отступы
        float iconSize = 80f;
        float spacing = 10f;
        float totalWidth = iconSize * 3 + spacing * 2;
        float margin = 20f;

        skillBarRect.sizeDelta = new Vector2(totalWidth, iconSize);
        skillBarRect.anchoredPosition = new Vector2(-margin, margin);

        // Добавляем HorizontalLayoutGroup
        HorizontalLayoutGroup layoutGroup = skillBarObj.AddComponent<HorizontalLayoutGroup>();
        layoutGroup.spacing = spacing;
        layoutGroup.childAlignment = TextAnchor.MiddleCenter;
        layoutGroup.childControlWidth = false;
        layoutGroup.childControlHeight = false;
        layoutGroup.childForceExpandWidth = false;
        layoutGroup.childForceExpandHeight = false;

        Debug.Log($"[CreateSkillBarUI] ✅ SkillBar контейнер создан (позиция: внизу справа, отступ {margin}px)");

        // Создаем 3 слота для скиллов
        for (int i = 0; i < 3; i++)
        {
            GameObject slotObj = new GameObject($"SkillSlot_{i + 1}");
            slotObj.transform.SetParent(skillBarObj.transform, false);

            RectTransform slotRect = slotObj.AddComponent<RectTransform>();
            slotRect.sizeDelta = new Vector2(iconSize, iconSize);

            // Добавляем фон слота (темный квадрат)
            Image slotBg = slotObj.AddComponent<Image>();
            slotBg.color = new Color(0.1f, 0.1f, 0.1f, 0.8f); // Почти черный, прозрачный
            slotBg.raycastTarget = false;

            // Создаем Icon (дочерний объект)
            GameObject iconObj = new GameObject("Icon");
            iconObj.transform.SetParent(slotObj.transform, false);

            RectTransform iconRect = iconObj.AddComponent<RectTransform>();
            iconRect.anchorMin = Vector2.zero;
            iconRect.anchorMax = Vector2.one;
            iconRect.offsetMin = Vector2.zero;
            iconRect.offsetMax = Vector2.zero; // Растянуть на весь слот

            Image iconImage = iconObj.AddComponent<Image>();
            iconImage.preserveAspect = false; // Растягивать без сохранения пропорций
            iconImage.raycastTarget = false;
            iconImage.enabled = false; // По умолчанию скрыта

            // Создаем Cooldown Overlay (затемнение при кулдауне)
            GameObject cooldownObj = new GameObject("CooldownOverlay");
            cooldownObj.transform.SetParent(slotObj.transform, false);

            RectTransform cooldownRect = cooldownObj.AddComponent<RectTransform>();
            cooldownRect.anchorMin = Vector2.zero;
            cooldownRect.anchorMax = Vector2.one;
            cooldownRect.offsetMin = Vector2.zero;
            cooldownRect.offsetMax = Vector2.zero;

            Image cooldownImage = cooldownObj.AddComponent<Image>();
            cooldownImage.color = new Color(0, 0, 0, 0.6f); // Черный полупрозрачный
            cooldownImage.type = Image.Type.Filled;
            cooldownImage.fillMethod = Image.FillMethod.Radial360;
            cooldownImage.fillOrigin = (int)Image.Origin360.Top;
            cooldownImage.fillAmount = 0f; // 0 = кулдаун закончен
            cooldownImage.raycastTarget = false;

            // Создаем текст кулдауна
            GameObject cooldownTextObj = new GameObject("CooldownText");
            cooldownTextObj.transform.SetParent(slotObj.transform, false);

            RectTransform cooldownTextRect = cooldownTextObj.AddComponent<RectTransform>();
            cooldownTextRect.anchorMin = Vector2.zero;
            cooldownTextRect.anchorMax = Vector2.one;
            cooldownTextRect.offsetMin = Vector2.zero;
            cooldownTextRect.offsetMax = Vector2.zero;

            TextMeshProUGUI cooldownText = cooldownTextObj.AddComponent<TextMeshProUGUI>();
            cooldownText.text = "";
            cooldownText.fontSize = 24;
            cooldownText.fontStyle = FontStyles.Bold;
            cooldownText.color = Color.white;
            cooldownText.alignment = TextAlignmentOptions.Center;
            cooldownText.raycastTarget = false;

            // Создаем текст хоткея (1, 2, 3)
            GameObject hotkeyTextObj = new GameObject("HotkeyText");
            hotkeyTextObj.transform.SetParent(slotObj.transform, false);

            RectTransform hotkeyTextRect = hotkeyTextObj.AddComponent<RectTransform>();
            hotkeyTextRect.anchorMin = new Vector2(0, 0);
            hotkeyTextRect.anchorMax = new Vector2(1, 0);
            hotkeyTextRect.pivot = new Vector2(0.5f, 0);
            hotkeyTextRect.sizeDelta = new Vector2(0, 20);
            hotkeyTextRect.anchoredPosition = new Vector2(0, 5);

            TextMeshProUGUI hotkeyText = hotkeyTextObj.AddComponent<TextMeshProUGUI>();
            hotkeyText.text = $"{i + 1}";
            hotkeyText.fontSize = 16;
            hotkeyText.fontStyle = FontStyles.Bold;
            hotkeyText.color = new Color(1f, 1f, 0.5f, 1f); // Желтоватый
            hotkeyText.alignment = TextAlignmentOptions.Center;
            hotkeyText.raycastTarget = false;

            // Добавляем компонент SkillSlotBar
            SkillSlotBar slotBar = slotObj.AddComponent<SkillSlotBar>();

            // Используем SerializedObject для установки ссылок (правильный способ в Editor)
            SerializedObject slotBarSO = new SerializedObject(slotBar);
            slotBarSO.FindProperty("iconImage").objectReferenceValue = iconImage;
            slotBarSO.FindProperty("cooldownOverlay").objectReferenceValue = cooldownImage;
            slotBarSO.FindProperty("cooldownText").objectReferenceValue = cooldownText;
            slotBarSO.FindProperty("hotkeyText").objectReferenceValue = hotkeyText;
            slotBarSO.FindProperty("slotIndex").intValue = i;
            slotBarSO.ApplyModifiedProperties();

            Debug.Log($"[CreateSkillBarUI] ✅ Создан слот {i + 1}: Icon + Cooldown + Hotkey");
        }

        // Добавляем компонент SkillBarUI
        SkillBarUI skillBarUI = skillBarObj.AddComponent<SkillBarUI>();

        Debug.Log("[CreateSkillBarUI] ✅ Добавлен компонент SkillBarUI");

        // Выделяем созданный объект
        Selection.activeGameObject = skillBarObj;

        Debug.Log("[CreateSkillBarUI] ✅ ГОТОВО! Skill Bar создан внизу справа экрана");
        Debug.Log("[CreateSkillBarUI] Теперь добавь SkillDatabase в Inspector компонента SkillBarUI");
    }
}
