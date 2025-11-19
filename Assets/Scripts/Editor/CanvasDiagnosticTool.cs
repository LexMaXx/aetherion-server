using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Инструмент для диагностики проблем с Canvas и UI кликами
/// Помогает найти что блокирует клики по skill buttons
/// </summary>
public class CanvasDiagnosticTool : EditorWindow
{
    private Vector2 scrollPosition;
    private List<CanvasInfo> canvasInfoList = new List<CanvasInfo>();
    private bool autoRefresh = true;

    [System.Serializable]
    private class CanvasInfo
    {
        public Canvas canvas;
        public string name;
        public int sortingOrder;
        public bool hasGraphicRaycaster;
        public bool hasCanvasGroup;
        public bool canvasGroupBlocksRaycasts;
        public bool canvasGroupInteractable;
        public int buttonCount;
        public List<string> buttonNames = new List<string>();
        public string path;
    }

    [MenuItem("Aetherion/Canvas Diagnostic Tool")]
    public static void ShowWindow()
    {
        GetWindow<CanvasDiagnosticTool>("Canvas Diagnostic");
    }

    void OnEnable()
    {
        RefreshCanvasList();
    }

    void OnGUI()
    {
        GUILayout.Label("Canvas Diagnostic Tool", EditorStyles.boldLabel);
        GUILayout.Label("Проверка Canvas hierarchy и блокировок кликов", EditorStyles.helpBox);

        EditorGUILayout.Space();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("🔄 Обновить", GUILayout.Height(30)))
        {
            RefreshCanvasList();
        }
        autoRefresh = GUILayout.Toggle(autoRefresh, "Auto Refresh", GUILayout.Width(100));
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();

        // Сводка проблем
        DrawProblemSummary();

        EditorGUILayout.Space();
        GUILayout.Label($"Найдено Canvas: {canvasInfoList.Count}", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        // Список Canvas
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        foreach (var info in canvasInfoList.OrderByDescending(c => c.sortingOrder))
        {
            DrawCanvasInfo(info);
            EditorGUILayout.Space(10);
        }

        EditorGUILayout.EndScrollView();

        // Auto-refresh в Play Mode
        if (autoRefresh && Application.isPlaying)
        {
            RefreshCanvasList();
            Repaint();
        }
    }

    private void DrawProblemSummary()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        GUILayout.Label("⚠️ ВОЗМОЖНЫЕ ПРОБЛЕМЫ", EditorStyles.boldLabel);

        // Проверка 1: Canvas без GraphicRaycaster
        var canvasWithoutRaycaster = canvasInfoList.Where(c => !c.hasGraphicRaycaster && c.buttonCount > 0).ToList();
        if (canvasWithoutRaycaster.Count > 0)
        {
            EditorGUILayout.HelpBox($"❌ {canvasWithoutRaycaster.Count} Canvas с кнопками БЕЗ GraphicRaycaster!", MessageType.Error);
            foreach (var c in canvasWithoutRaycaster)
            {
                EditorGUILayout.LabelField($"  → {c.name}");
            }
        }

        // Проверка 2: CanvasGroup блокирует клики
        var canvasWithBlockedRaycasts = canvasInfoList.Where(c => c.hasCanvasGroup && !c.canvasGroupBlocksRaycasts && c.buttonCount > 0).ToList();
        if (canvasWithBlockedRaycasts.Count > 0)
        {
            EditorGUILayout.HelpBox($"⚠️ {canvasWithBlockedRaycasts.Count} Canvas с blocksRaycasts = false", MessageType.Warning);
            foreach (var c in canvasWithBlockedRaycasts)
            {
                EditorGUILayout.LabelField($"  → {c.name}");
            }
        }

        // Проверка 3: CanvasGroup не interactable
        var canvasNotInteractable = canvasInfoList.Where(c => c.hasCanvasGroup && !c.canvasGroupInteractable && c.buttonCount > 0).ToList();
        if (canvasNotInteractable.Count > 0)
        {
            EditorGUILayout.HelpBox($"⚠️ {canvasNotInteractable.Count} Canvas с interactable = false", MessageType.Warning);
            foreach (var c in canvasNotInteractable)
            {
                EditorGUILayout.LabelField($"  → {c.name}");
            }
        }

        // Проверка 4: Перекрытие Canvas (высокий sortingOrder перекрывает низкий)
        var skillBarCanvas = canvasInfoList.FirstOrDefault(c => c.name.ToLower().Contains("skill"));
        if (skillBarCanvas != null)
        {
            var overlappingCanvas = canvasInfoList.Where(c => c.sortingOrder > skillBarCanvas.sortingOrder).ToList();
            if (overlappingCanvas.Count > 0)
            {
                EditorGUILayout.HelpBox($"⚠️ {overlappingCanvas.Count} Canvas с ВЫШЕ sortingOrder чем Skill Bar (могут блокировать клики)", MessageType.Warning);
                foreach (var c in overlappingCanvas)
                {
                    EditorGUILayout.LabelField($"  → {c.name} (Order: {c.sortingOrder})");
                }
            }
        }

        // Проверка 5: Поиск Action Points UI
        var actionPointsCanvas = canvasInfoList.FirstOrDefault(c => c.name.ToLower().Contains("action") || c.path.ToLower().Contains("action"));
        if (actionPointsCanvas != null)
        {
            if (skillBarCanvas != null && actionPointsCanvas.sortingOrder > skillBarCanvas.sortingOrder)
            {
                EditorGUILayout.HelpBox($"❌ ПРОБЛЕМА: Action Points Canvas (Order: {actionPointsCanvas.sortingOrder}) перекрывает Skill Bar (Order: {skillBarCanvas.sortingOrder})!", MessageType.Error);
            }
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawCanvasInfo(CanvasInfo info)
    {
        Color bgColor = GUI.backgroundColor;

        // Подсветка проблемных Canvas
        if (info.buttonCount > 0 && !info.hasGraphicRaycaster)
        {
            GUI.backgroundColor = new Color(1f, 0.5f, 0.5f); // Красный
        }
        else if (info.name.ToLower().Contains("skill"))
        {
            GUI.backgroundColor = new Color(0.5f, 1f, 0.5f); // Зеленый - Skill Bar
        }
        else if (info.name.ToLower().Contains("action"))
        {
            GUI.backgroundColor = new Color(1f, 1f, 0.5f); // Желтый - Action Points
        }

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        GUI.backgroundColor = bgColor;

        // Заголовок
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField($"📋 {info.name}", EditorStyles.boldLabel);
        if (GUILayout.Button("Select", GUILayout.Width(60)))
        {
            Selection.activeGameObject = info.canvas.gameObject;
            EditorGUIUtility.PingObject(info.canvas.gameObject);
        }
        EditorGUILayout.EndHorizontal();

        // Детали
        EditorGUI.indentLevel++;
        EditorGUILayout.LabelField("Path:", info.path);
        EditorGUILayout.LabelField("Sorting Order:", info.sortingOrder.ToString());

        // GraphicRaycaster
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("GraphicRaycaster:", GUILayout.Width(150));
        if (info.hasGraphicRaycaster)
        {
            EditorGUILayout.LabelField("✅ Есть", EditorStyles.boldLabel);
        }
        else
        {
            EditorGUILayout.LabelField("❌ Отсутствует", EditorStyles.boldLabel);
            if (GUILayout.Button("Добавить", GUILayout.Width(80)))
            {
                info.canvas.gameObject.AddComponent<GraphicRaycaster>();
                RefreshCanvasList();
            }
        }
        EditorGUILayout.EndHorizontal();

        // CanvasGroup
        if (info.hasCanvasGroup)
        {
            EditorGUILayout.LabelField("CanvasGroup:");
            EditorGUI.indentLevel++;
            EditorGUILayout.LabelField("Blocks Raycasts:", info.canvasGroupBlocksRaycasts ? "✅ true" : "❌ false");
            EditorGUILayout.LabelField("Interactable:", info.canvasGroupInteractable ? "✅ true" : "❌ false");
            EditorGUI.indentLevel--;
        }

        // Кнопки
        if (info.buttonCount > 0)
        {
            EditorGUILayout.LabelField($"Buttons: {info.buttonCount}");
            if (info.buttonNames.Count > 0)
            {
                EditorGUI.indentLevel++;
                foreach (var btnName in info.buttonNames.Take(5)) // Показываем первые 5
                {
                    EditorGUILayout.LabelField($"→ {btnName}");
                }
                if (info.buttonNames.Count > 5)
                {
                    EditorGUILayout.LabelField($"... и ещё {info.buttonNames.Count - 5}");
                }
                EditorGUI.indentLevel--;
            }
        }

        EditorGUI.indentLevel--;
        EditorGUILayout.EndVertical();
    }

    private void RefreshCanvasList()
    {
        canvasInfoList.Clear();

        // Находим все Canvas в сцене
        Canvas[] allCanvas = FindObjectsOfType<Canvas>(true);

        foreach (var canvas in allCanvas)
        {
            var info = new CanvasInfo
            {
                canvas = canvas,
                name = canvas.gameObject.name,
                sortingOrder = canvas.sortingOrder,
                hasGraphicRaycaster = canvas.GetComponent<GraphicRaycaster>() != null,
                path = GetGameObjectPath(canvas.gameObject)
            };

            // CanvasGroup
            var canvasGroup = canvas.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                info.hasCanvasGroup = true;
                info.canvasGroupBlocksRaycasts = canvasGroup.blocksRaycasts;
                info.canvasGroupInteractable = canvasGroup.interactable;
            }

            // Кнопки
            Button[] buttons = canvas.GetComponentsInChildren<Button>(true);
            info.buttonCount = buttons.Length;
            foreach (var btn in buttons)
            {
                info.buttonNames.Add(btn.gameObject.name);
            }

            canvasInfoList.Add(info);
        }

        Debug.Log($"[CanvasDiagnostic] Найдено {canvasInfoList.Count} Canvas в сцене");
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
