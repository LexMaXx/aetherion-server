using UnityEngine;
using UnityEditor;

/// <summary>
/// Диагностический инструмент для проверки кнопок скиллов в ArenaScene
/// Tools > Aetherion > Diagnose Skill Buttons
/// </summary>
public class DiagnoseSkillButtons : EditorWindow
{
    [MenuItem("Tools/Aetherion/Diagnose Skill Buttons")]
    public static void ShowWindow()
    {
        GetWindow<DiagnoseSkillButtons>("Skill Buttons Diagnostic");
    }

    private Vector2 scrollPosition;

    void OnGUI()
    {
        GUILayout.Label("Диагностика кнопок скиллов", EditorStyles.boldLabel);
        GUILayout.Space(10);

        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("Запустите игру (Play Mode) для диагностики!", MessageType.Warning);
            return;
        }

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        // Ищем все SkillButton в сцене
        SkillButton[] skillButtons = FindObjectsOfType<SkillButton>();

        GUILayout.Label($"Найдено SkillButton компонентов: {skillButtons.Length}", EditorStyles.boldLabel);
        GUILayout.Space(10);

        if (skillButtons.Length == 0)
        {
            EditorGUILayout.HelpBox("❌ Не найдено ни одного SkillButton! Проверьте что у вас есть UI кнопки скиллов в сцене.", MessageType.Error);
        }
        else
        {
            foreach (SkillButton button in skillButtons)
            {
                GUILayout.BeginVertical(EditorStyles.helpBox);

                GUILayout.Label($"Кнопка: {button.gameObject.name}", EditorStyles.boldLabel);

                // Проверяем компоненты
                UnityEngine.UI.Button uiButton = button.GetComponent<UnityEngine.UI.Button>();
                UnityEngine.UI.Image image = button.GetComponent<UnityEngine.UI.Image>();
                Canvas canvas = button.GetComponentInParent<Canvas>();

                GUILayout.Label($"  • GameObject Active: {(button.gameObject.activeSelf ? "✅" : "❌")}");
                GUILayout.Label($"  • Button Component: {(uiButton != null ? "✅" : "❌")}");

                if (uiButton != null)
                {
                    GUILayout.Label($"    - Interactable: {(uiButton.interactable ? "✅" : "❌")}");
                    GUILayout.Label($"    - OnClick Listeners: {uiButton.onClick.GetPersistentEventCount()}");
                }

                GUILayout.Label($"  • Image Component: {(image != null ? "✅" : "❌")}");

                if (image != null)
                {
                    GUILayout.Label($"    - Raycast Target: {(image.raycastTarget ? "✅" : "❌")}");
                }

                GUILayout.Label($"  • В Canvas: {(canvas != null ? "✅" : "❌")}");

                if (canvas != null)
                {
                    GUILayout.Label($"    - Canvas Enabled: {(canvas.enabled ? "✅" : "❌")}");
                    GUILayout.Label($"    - Render Mode: {canvas.renderMode}");

                    UnityEngine.UI.GraphicRaycaster raycaster = canvas.GetComponent<UnityEngine.UI.GraphicRaycaster>();
                    GUILayout.Label($"    - GraphicRaycaster: {(raycaster != null ? "✅" : "❌")}");
                }

                // Проверяем EventSystem
                UnityEngine.EventSystems.EventSystem eventSystem = FindObjectOfType<UnityEngine.EventSystems.EventSystem>();
                GUILayout.Label($"  • EventSystem в сцене: {(eventSystem != null ? "✅" : "❌")}");

                // Проверяем текущий скилл
                SkillData currentSkill = button.GetSkill();
                GUILayout.Label($"  • Текущий скилл: {(currentSkill != null ? currentSkill.skillName : "ПУСТО")}");

                GUILayout.Space(5);

                // Кнопка для теста
                if (GUILayout.Button("🧪 Тестировать UseSkill()"))
                {
                    Debug.Log($"[DiagnoseSkillButtons] Тестируем кнопку {button.gameObject.name}...");
                    button.UseSkill();
                }

                GUILayout.EndVertical();
                GUILayout.Space(5);
            }
        }

        GUILayout.Space(20);

        // Проверяем игрока
        GUILayout.Label("Проверка игрока:", EditorStyles.boldLabel);
        GameObject player = GameObject.FindGameObjectWithTag("Player");

        if (player == null)
        {
            EditorGUILayout.HelpBox("❌ Игрок с тегом 'Player' не найден!", MessageType.Error);
        }
        else
        {
            GUILayout.Label($"✅ Игрок найден: {player.name}");

            SkillExecutor executor = player.GetComponentInChildren<SkillExecutor>();
            SkillManager manager = player.GetComponentInChildren<SkillManager>();

            GUILayout.Label($"  • SkillExecutor: {(executor != null ? "✅" : "❌")}");
            GUILayout.Label($"  • SkillManager: {(manager != null ? "✅" : "❌")}");

            if (executor != null)
            {
                GUILayout.Label($"  • Экипировано скиллов: {executor.equippedSkills.Count}");

                for (int i = 0; i < executor.equippedSkills.Count; i++)
                {
                    if (executor.equippedSkills[i] != null)
                    {
                        float cooldown = executor.GetCooldown(i);
                        string cooldownText = cooldown > 0 ? $"(CD: {cooldown:F1}s)" : "";
                        GUILayout.Label($"    - Слот {i}: {executor.equippedSkills[i].skillName} {cooldownText}");
                    }
                    else
                    {
                        GUILayout.Label($"    - Слот {i}: ПУСТО");
                    }
                }
            }
        }

        EditorGUILayout.EndScrollView();
    }
}
