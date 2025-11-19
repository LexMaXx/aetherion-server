#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;

/// <summary>
/// Editor утилита для автоматического обновления настроек GameSceneManager
/// Запускается один раз чтобы обновить arenaSceneName на "BattleScene"
/// </summary>
public class UpdateGameSceneSettings : EditorWindow
{
    [MenuItem("Aetherion/Fix GameScene Settings")]
    public static void FixGameSceneSettings()
    {
        Debug.Log("[UpdateGameSceneSettings] Начинаем обновление GameScene...");

        // Путь к GameScene
        string scenePath = "Assets/Scenes/GameScene.unity";

        // Проверяем существует ли сцена
        if (!System.IO.File.Exists(scenePath))
        {
            Debug.LogError($"[UpdateGameSceneSettings] Сцена не найдена: {scenePath}");
            return;
        }

        // Открываем сцену
        Scene gameScene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

        // Ищем GameSceneManager
        GameSceneManager[] managers = GameObject.FindObjectsOfType<GameSceneManager>();

        if (managers.Length == 0)
        {
            Debug.LogError("[UpdateGameSceneSettings] GameSceneManager не найден в сцене!");
            return;
        }

        // Обновляем каждый найденный менеджер
        foreach (GameSceneManager manager in managers)
        {
            // Получаем SerializedObject для изменения приватных полей
            SerializedObject so = new SerializedObject(manager);

            // Находим поле arenaSceneName
            SerializedProperty arenaSceneNameProp = so.FindProperty("arenaSceneName");

            if (arenaSceneNameProp != null)
            {
                string oldValue = arenaSceneNameProp.stringValue;
                arenaSceneNameProp.stringValue = "BattleScene";
                so.ApplyModifiedProperties();

                Debug.Log($"[UpdateGameSceneSettings] ✅ Обновлено: '{oldValue}' → 'BattleScene'");
            }
            else
            {
                Debug.LogWarning("[UpdateGameSceneSettings] ⚠️ Поле arenaSceneName не найдено!");
            }
        }

        // Сохраняем сцену
        EditorSceneManager.SaveScene(gameScene);
        Debug.Log("[UpdateGameSceneSettings] ✅✅✅ GameScene сохранена с новыми настройками!");

        EditorUtility.DisplayDialog(
            "Готово!",
            "GameScene обновлена!\n\nТеперь кнопка Arena будет открывать BattleScene.",
            "OK"
        );
    }
}
#endif
