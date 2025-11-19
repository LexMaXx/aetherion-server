using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Автоматическое исправление проблем с кликами по skill buttons
/// Исправляет Canvas sorting order, добавляет GraphicRaycaster, настраивает CanvasGroup
/// </summary>
public class FixSkillButtonClicks : EditorWindow
{
    [MenuItem("Aetherion/Fix Skill Button Clicks")]
    public static void ShowWindow()
    {
        GetWindow<FixSkillButtonClicks>("Fix Skill Clicks");
    }

    void OnGUI()
    {
        GUILayout.Label("Fix Skill Button Clicks", EditorStyles.boldLabel);
        GUILayout.Label("Автоматическое исправление проблем с UI кликами", EditorStyles.helpBox);

        EditorGUILayout.Space(20);

        if (GUILayout.Button("🔧 FIX 1: Добавить GraphicRaycaster ко всем Canvas", GUILayout.Height(40)))
        {
            FixMissingGraphicRaycasters();
        }

        EditorGUILayout.Space(10);

        if (GUILayout.Button("🔧 FIX 2: Исправить Canvas Sorting Order", GUILayout.Height(40)))
        {
            FixCanvasSortingOrder();
        }

        EditorGUILayout.Space(10);

        if (GUILayout.Button("🔧 FIX 3: Исправить CanvasGroup settings", GUILayout.Height(40)))
        {
            FixCanvasGroups();
        }

        EditorGUILayout.Space(10);

        if (GUILayout.Button("🔧 FIX 4: Включить raycastTarget на всех кнопках", GUILayout.Height(40)))
        {
            FixButtonRaycastTargets();
        }

        EditorGUILayout.Space(20);

        if (GUILayout.Button("✅ FIX ALL - Исправить ВСЁ автоматически", GUILayout.Height(50)))
        {
            FixAll();
        }

        EditorGUILayout.Space(20);

        EditorGUILayout.HelpBox("Рекомендуется:\n1. Сначала запустить Canvas Diagnostic Tool для диагностики\n2. Затем использовать FIX ALL для автоматического исправления", MessageType.Info);
    }

    private void FixAll()
    {
        Debug.Log("=== STARTING AUTO-FIX ===");
        FixMissingGraphicRaycasters();
        FixCanvasSortingOrder();
        FixCanvasGroups();
        FixButtonRaycastTargets();
        Debug.Log("=== AUTO-FIX COMPLETE ===");
        EditorUtility.DisplayDialog("Готово!", "Все проблемы исправлены!\n\nПроверьте Unity Console для деталей.", "OK");
    }

    /// <summary>
    /// FIX 1: Добавить GraphicRaycaster ко всем Canvas
    /// </summary>
    private void FixMissingGraphicRaycasters()
    {
        Canvas[] allCanvas = FindObjectsOfType<Canvas>(true);
        int fixedCount = 0;

        foreach (var canvas in allCanvas)
        {
            if (canvas.GetComponent<GraphicRaycaster>() == null)
            {
                canvas.gameObject.AddComponent<GraphicRaycaster>();
                Debug.Log($"[FixSkillClicks] ✅ Добавлен GraphicRaycaster к Canvas: {canvas.name}");
                fixedCount++;
            }
        }

        Debug.Log($"[FixSkillClicks] FIX 1 Complete: Добавлено {fixedCount} GraphicRaycaster");
        EditorUtility.SetDirty(GameObject.Find("Canvas"));
    }

    /// <summary>
    /// FIX 2: Исправить Canvas Sorting Order
    /// Skill Bar должен быть ВЫШЕ Action Points UI
    /// </summary>
    private void FixCanvasSortingOrder()
    {
        Canvas[] allCanvas = FindObjectsOfType<Canvas>(true);

        // Находим важные Canvas
        Canvas skillBarCanvas = null;
        Canvas actionPointsCanvas = null;
        Canvas joystickCanvas = null;

        foreach (var canvas in allCanvas)
        {
            string name = canvas.gameObject.name.ToLower();
            string path = GetGameObjectPath(canvas.gameObject).ToLower();

            if (name.Contains("skill") || path.Contains("skillbar"))
            {
                skillBarCanvas = canvas;
            }
            else if (name.Contains("action") || path.Contains("actionpoint"))
            {
                actionPointsCanvas = canvas;
            }
            else if (name.Contains("joystick") || path.Contains("joystick"))
            {
                joystickCanvas = canvas;
            }
        }

        // Устанавливаем правильный порядок
        // Joystick (lowest) < Action Points < Skill Bar (highest)

        if (joystickCanvas != null)
        {
            joystickCanvas.sortingOrder = 10;
            Debug.Log($"[FixSkillClicks] ✅ Joystick Canvas sorting order = 10");
            EditorUtility.SetDirty(joystickCanvas);
        }

        if (actionPointsCanvas != null)
        {
            actionPointsCanvas.sortingOrder = 50;
            Debug.Log($"[FixSkillClicks] ✅ Action Points Canvas sorting order = 50");
            EditorUtility.SetDirty(actionPointsCanvas);
        }

        if (skillBarCanvas != null)
        {
            skillBarCanvas.sortingOrder = 100; // ВЫСШИЙ приоритет для skill bar
            Debug.Log($"[FixSkillClicks] ✅ Skill Bar Canvas sorting order = 100 (highest)");
            EditorUtility.SetDirty(skillBarCanvas);
        }

        Debug.Log("[FixSkillClicks] FIX 2 Complete: Canvas sorting order исправлен");
    }

    /// <summary>
    /// FIX 3: Исправить CanvasGroup settings
    /// </summary>
    private void FixCanvasGroups()
    {
        Canvas[] allCanvas = FindObjectsOfType<Canvas>(true);
        int fixedCount = 0;

        foreach (var canvas in allCanvas)
        {
            // Ищем кнопки в этом Canvas
            Button[] buttons = canvas.GetComponentsInChildren<Button>(true);
            if (buttons.Length == 0) continue; // Пропускаем Canvas без кнопок

            CanvasGroup canvasGroup = canvas.GetComponent<CanvasGroup>();

            // Если есть CanvasGroup - исправляем настройки
            if (canvasGroup != null)
            {
                if (!canvasGroup.blocksRaycasts || !canvasGroup.interactable)
                {
                    canvasGroup.blocksRaycasts = true;
                    canvasGroup.interactable = true;
                    Debug.Log($"[FixSkillClicks] ✅ Исправлен CanvasGroup на Canvas: {canvas.name}");
                    EditorUtility.SetDirty(canvasGroup);
                    fixedCount++;
                }
            }
        }

        Debug.Log($"[FixSkillClicks] FIX 3 Complete: Исправлено {fixedCount} CanvasGroup");
    }

    /// <summary>
    /// FIX 4: Включить raycastTarget на всех Image компонентах кнопок
    /// </summary>
    private void FixButtonRaycastTargets()
    {
        Button[] allButtons = FindObjectsOfType<Button>(true);
        int fixedCount = 0;

        foreach (var button in allButtons)
        {
            // Проверяем интерактивность кнопки
            if (!button.interactable)
            {
                button.interactable = true;
                Debug.Log($"[FixSkillClicks] ✅ Включен interactable на кнопке: {button.name}");
                EditorUtility.SetDirty(button);
                fixedCount++;
            }

            // Проверяем raycastTarget на Image
            Image buttonImage = button.GetComponent<Image>();
            if (buttonImage != null && !buttonImage.raycastTarget)
            {
                buttonImage.raycastTarget = true;
                Debug.Log($"[FixSkillClicks] ✅ Включен raycastTarget на Image: {button.name}");
                EditorUtility.SetDirty(buttonImage);
                fixedCount++;
            }

            // Проверяем наличие слушателя onClick
            if (button.onClick.GetPersistentEventCount() == 0)
            {
                Debug.LogWarning($"[FixSkillClicks] ⚠️ Кнопка БЕЗ onClick listener: {button.name}");
            }
        }

        Debug.Log($"[FixSkillClicks] FIX 4 Complete: Исправлено {fixedCount} кнопок");
    }

    private string GetGameObjectPath(GameObject obj)
    {
        string path = obj.name;
        Transform parent = obj.transform.parent;
        while (parent != null)
        {
            path = parent.name + "/" + path;
            parent = parent.parent;
        }
        return path;
    }
}
