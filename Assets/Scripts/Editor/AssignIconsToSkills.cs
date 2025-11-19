using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// Автоматическое назначение иконок скиллам по имени
/// Использование: Tools → Aetherion → Assign Icons to Skills
/// </summary>
public class AssignIconsToSkills : EditorWindow
{
    [MenuItem("Tools/Aetherion/Assign Icons to Skills")]
    public static void AssignIcons()
    {
        Debug.Log("[AssignIcons] Начинаю поиск скиллов и иконок...");

        // Найти все SkillData
        string[] skillGuids = AssetDatabase.FindAssets("t:SkillData");
        int assignedCount = 0;
        int skippedCount = 0;

        foreach (string guid in skillGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            SkillData skill = AssetDatabase.LoadAssetAtPath<SkillData>(path);

            if (skill == null) continue;

            // Если иконка уже назначена, пропускаем
            if (skill.icon != null)
            {
                skippedCount++;
                continue;
            }

            // Ищем иконку по имени скилла
            string iconSearchPattern = skill.skillName.Replace(" ", "");
            string[] iconGuids = AssetDatabase.FindAssets($"{iconSearchPattern} t:Sprite");

            if (iconGuids.Length > 0)
            {
                string iconPath = AssetDatabase.GUIDToAssetPath(iconGuids[0]);
                Sprite icon = AssetDatabase.LoadAssetAtPath<Sprite>(iconPath);

                if (icon != null)
                {
                    // Назначаем иконку через SerializedObject
                    SerializedObject so = new SerializedObject(skill);
                    so.FindProperty("icon").objectReferenceValue = icon;
                    so.ApplyModifiedProperties();

                    assignedCount++;
                    Debug.Log($"[AssignIcons] ✅ {skill.skillName} → {icon.name}");
                }
            }
            else
            {
                Debug.LogWarning($"[AssignIcons] ⚠️ Иконка для {skill.skillName} не найдена! Ищу: {iconSearchPattern}");
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[AssignIcons] ✅ Готово! Назначено: {assignedCount}, Пропущено (уже есть): {skippedCount}");
        EditorUtility.DisplayDialog("Готово!",
            $"Назначено иконок: {assignedCount}\nПропущено: {skippedCount}",
            "OK");
    }
}
