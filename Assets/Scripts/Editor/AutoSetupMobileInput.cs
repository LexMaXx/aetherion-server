using UnityEngine;
using UnityEditor;
using StarterAssets;

/// <summary>
/// Автоматическая настройка BattleMobileInput в BattleSceneUIManager
/// Menu: Aetherion/Setup/Auto Setup Mobile Input
/// </summary>
public class AutoSetupMobileInput : Editor
{
    [MenuItem("Aetherion/Setup/Auto Setup Mobile Input")]
    public static void AutoSetup()
    {
        Debug.Log("=== Автоматическая настройка Mobile Input ===");

        // Загружаем префабы персонажей
        GameObject paladinPrefab = Resources.Load<GameObject>("Characters/PaladinModel");
        GameObject magePrefab = Resources.Load<GameObject>("Characters/MageModel");
        GameObject archerPrefab = Resources.Load<GameObject>("Characters/ArcherModel");
        GameObject roguePrefab = Resources.Load<GameObject>("Characters/RogueModel");

        if (paladinPrefab == null || magePrefab == null || archerPrefab == null || roguePrefab == null)
        {
            EditorUtility.DisplayDialog(
                "Ошибка",
                "Не удалось загрузить префабы классов из Resources/Characters/!\n\n" +
                "Проверь что существуют:\n" +
                "- PaladinModel.prefab\n" +
                "- MageModel.prefab\n" +
                "- ArcherModel.prefab\n" +
                "- RogueModel.prefab",
                "OK"
            );
            return;
        }

        Debug.Log("✅ Префабы классов загружены");

        // Находим BattleSceneUIManager в сцене
        BattleSceneUIManager uiManager = GameObject.FindObjectOfType<BattleSceneUIManager>();

        if (uiManager == null)
        {
            EditorUtility.DisplayDialog(
                "Ошибка",
                "BattleSceneUIManager не найден в сцене!\n\n" +
                "Открой BattleScene и попробуй снова.",
                "OK"
            );
            return;
        }

        Debug.Log($"✅ BattleSceneUIManager найден: {uiManager.gameObject.name}");

        // Получаем BattleMobileInput компонент
        BattleMobileInput mobileInput = uiManager.GetComponent<BattleMobileInput>();

        if (mobileInput == null)
        {
            Debug.Log("⚠️ BattleMobileInput не найден, создаём...");
            mobileInput = uiManager.gameObject.AddComponent<BattleMobileInput>();
        }

        Debug.Log("✅ BattleMobileInput найден/создан");

        // Настраиваем через SerializedObject (чтобы работать с приватными полями)
        SerializedObject so = new SerializedObject(mobileInput);

        // Настраиваем массив classInputs
        SerializedProperty classInputsProp = so.FindProperty("classInputs");

        if (classInputsProp == null)
        {
            Debug.LogError("❌ Не найдено поле classInputs!");
            return;
        }

        // Устанавливаем размер массива
        classInputsProp.arraySize = 4;

        // Element 0 - Paladin
        SetupClassMapping(classInputsProp.GetArrayElementAtIndex(0), "Paladin", paladinPrefab);

        // Element 1 - Mage
        SetupClassMapping(classInputsProp.GetArrayElementAtIndex(1), "Mage", magePrefab);

        // Element 2 - Archer
        SetupClassMapping(classInputsProp.GetArrayElementAtIndex(2), "Archer", archerPrefab);

        // Element 3 - Rogue
        SetupClassMapping(classInputsProp.GetArrayElementAtIndex(3), "Rogue", roguePrefab);

        // Применяем изменения
        so.ApplyModifiedProperties();

        // Помечаем сцену как изменённую
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(uiManager.gameObject.scene);

        Debug.Log("=== РЕЗУЛЬТАТ ===");
        Debug.Log("✅ Paladin - настроен");
        Debug.Log("✅ Mage - настроен");
        Debug.Log("✅ Archer - настроен");
        Debug.Log("✅ Rogue - настроен");
        Debug.Log("==================");
        Debug.Log("💾 Сохрани сцену (Ctrl+S)!");

        EditorUtility.DisplayDialog(
            "Готово!",
            "Mobile Input настроен для всех 4 классов!\n\n" +
            "Paladin ✓\n" +
            "Mage ✓\n" +
            "Archer ✓\n" +
            "Rogue ✓\n\n" +
            "Сохрани сцену (Ctrl+S) и тестируй джойстик!",
            "OK"
        );
    }

    private static void SetupClassMapping(SerializedProperty element, string className, GameObject prefab)
    {
        // Получаем StarterAssetsInputs с префаба
        StarterAssetsInputs starterInputs = prefab.GetComponent<StarterAssetsInputs>();

        if (starterInputs == null)
        {
            Debug.LogWarning($"⚠️ {className}: StarterAssetsInputs не найден на префабе! Добавь компонент на {prefab.name}");
        }
        else
        {
            Debug.Log($"✅ {className}: StarterAssetsInputs найден на {prefab.name}");
        }

        // Устанавливаем className
        SerializedProperty classNameProp = element.FindPropertyRelative("className");
        if (classNameProp != null)
        {
            classNameProp.stringValue = className;
        }

        // Устанавливаем starterInputs
        SerializedProperty starterInputsProp = element.FindPropertyRelative("starterInputs");
        if (starterInputsProp != null)
        {
            starterInputsProp.objectReferenceValue = starterInputs;
        }

        Debug.Log($"✓ {className} настроен: className={className}, starterInputs={starterInputs != null}");
    }
}
