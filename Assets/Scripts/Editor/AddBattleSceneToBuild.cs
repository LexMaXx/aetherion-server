#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Editor утилита для добавления BattleScene в Build Settings
/// </summary>
public class AddBattleSceneToBuild : EditorWindow
{
    [MenuItem("Aetherion/Add BattleScene to Build")]
    public static void AddBattleSceneToBuildSettings()
    {
        Debug.Log("[AddBattleSceneToBuild] Добавляем BattleScene в Build Settings...");

        // Путь к BattleScene
        string battleScenePath = "Assets/Scenes/BattleScene.unity";

        // Проверяем существует ли файл
        if (!System.IO.File.Exists(battleScenePath))
        {
            Debug.LogError($"[AddBattleSceneToBuild] ❌ Сцена не найдена: {battleScenePath}");
            EditorUtility.DisplayDialog(
                "Ошибка!",
                $"Сцена не найдена:\n{battleScenePath}",
                "OK"
            );
            return;
        }

        // Получаем текущие сцены в Build Settings
        List<EditorBuildSettingsScene> scenes = EditorBuildSettings.scenes.ToList();

        // Проверяем есть ли уже BattleScene
        bool alreadyExists = scenes.Any(s => s.path == battleScenePath);

        if (alreadyExists)
        {
            Debug.Log("[AddBattleSceneToBuild] ⚠️ BattleScene уже есть в Build Settings");
            EditorUtility.DisplayDialog(
                "Уже добавлена!",
                "BattleScene уже находится в Build Settings.",
                "OK"
            );
            return;
        }

        // Добавляем BattleScene
        EditorBuildSettingsScene newScene = new EditorBuildSettingsScene(battleScenePath, true);
        scenes.Add(newScene);

        // Обновляем Build Settings
        EditorBuildSettings.scenes = scenes.ToArray();

        Debug.Log("[AddBattleSceneToBuild] ✅ BattleScene добавлена в Build Settings!");

        // Показываем список всех сцен
        Debug.Log("[AddBattleSceneToBuild] 📋 Сцены в Build Settings:");
        for (int i = 0; i < scenes.Count; i++)
        {
            string status = scenes[i].enabled ? "✓" : "✗";
            Debug.Log($"  {status} [{i}] {scenes[i].path}");
        }

        EditorUtility.DisplayDialog(
            "Готово!",
            $"BattleScene добавлена в Build Settings!\n\nТеперь сцена будет загружаться корректно.\n\nВсего сцен в билде: {scenes.Count}",
            "OK"
        );
    }

    [MenuItem("Aetherion/Show Build Scenes List")]
    public static void ShowBuildScenes()
    {
        Debug.Log("[Build Settings] 📋 Список всех сцен:");

        EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;

        if (scenes.Length == 0)
        {
            Debug.LogWarning("[Build Settings] ⚠️ Нет сцен в Build Settings!");
            return;
        }

        for (int i = 0; i < scenes.Length; i++)
        {
            string status = scenes[i].enabled ? "✓ ВКЛЮЧЕНА" : "✗ ВЫКЛЮЧЕНА";
            Debug.Log($"  [{i}] {status} - {scenes[i].path}");
        }

        EditorUtility.DisplayDialog(
            "Build Scenes",
            $"В Build Settings {scenes.Length} сцен.\n\nПроверьте Console для деталей.",
            "OK"
        );
    }
}
#endif
