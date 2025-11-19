using UnityEngine;
using UnityEditor;
using TMPro;

/// <summary>
/// Editor скрипт для быстрого применения золотого стиля через меню Unity
/// </summary>
public class TextStyleMenuEditor : MonoBehaviour
{
    [MenuItem("Tools/Fantasy MMO/Apply Aetherion Style To All Texts")]
    public static void ApplyAetherionStyleToAll()
    {
        TextMeshProUGUI[] allTexts = FindObjectsOfType<TextMeshProUGUI>(true);
        int count = 0;

        foreach (var text in allTexts)
        {
            ApplyAetherionStyleToText(text);
            EditorUtility.SetDirty(text.gameObject);
            count++;
        }

        Debug.Log($"✅ Aetherion стиль применен к {count} текстовым элементам!");
        EditorUtility.DisplayDialog("Успех", $"Aetherion стиль применен к {count} текстам", "OK");
    }

    [MenuItem("Tools/Fantasy MMO/Apply Golden Style To All Texts")]
    public static void ApplyGoldenStyleToAll()
    {
        TextMeshProUGUI[] allTexts = FindObjectsOfType<TextMeshProUGUI>(true);
        int count = 0;

        foreach (var text in allTexts)
        {
            ApplyGoldenStyleToText(text);
            EditorUtility.SetDirty(text.gameObject);
            count++;
        }

        Debug.Log($"✅ Золотой стиль применен к {count} текстовым элементам!");
        EditorUtility.DisplayDialog("Успех", $"Золотой стиль применен к {count} текстам", "OK");
    }

    [MenuItem("Tools/Fantasy MMO/Apply Golden Style To Selected")]
    public static void ApplyGoldenStyleToSelected()
    {
        if (Selection.activeGameObject == null)
        {
            EditorUtility.DisplayDialog("Ошибка", "Выберите объект с TextMeshProUGUI!", "OK");
            return;
        }

        TextMeshProUGUI[] texts = Selection.activeGameObject.GetComponentsInChildren<TextMeshProUGUI>(true);

        if (texts.Length == 0)
        {
            EditorUtility.DisplayDialog("Ошибка", "На выбранном объекте нет TextMeshProUGUI компонентов!", "OK");
            return;
        }

        foreach (var text in texts)
        {
            ApplyGoldenStyleToText(text);
            EditorUtility.SetDirty(text.gameObject);
        }

        Debug.Log($"✅ Золотой стиль применен к {texts.Length} выбранным текстам!");
        EditorUtility.DisplayDialog("Успех", $"Золотой стиль применен к {texts.Length} текстам", "OK");
    }

    private static void ApplyAetherionStyleToText(TextMeshProUGUI tmp)
    {
        // Применяем шрифт Cinzel Decorative
        TMP_FontAsset cinzelFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/CinzelDecorative SDF.asset");
        if (cinzelFont != null)
        {
            tmp.font = cinzelFont;
        }

        // Цвета для металлического эффекта
        Color highlightGold = new Color(1f, 0.95f, 0.7f, 1f); // Яркий блик
        Color deepGold = new Color(0.55f, 0.42f, 0.12f, 1f); // Темное золото
        Color midGold = new Color(0.83f, 0.68f, 0.23f, 1f); // Средний золотой

        // Базовый цвет
        tmp.color = midGold;

        // Металлический градиент (светлый сверху, темный снизу)
        VertexGradient gradient = new VertexGradient(
            highlightGold, // верх - яркий блик
            highlightGold,
            deepGold,      // низ - темное золото для рельефа
            deepGold
        );
        tmp.colorGradient = gradient;

        // Темный контур для 3D эффекта
        tmp.fontStyle = FontStyles.Bold;
        tmp.outlineColor = new Color(0.1f, 0.06f, 0.02f, 1f);
        tmp.outlineWidth = 0.25f;
        tmp.richText = true;
    }

    private static void ApplyGoldenStyleToText(TextMeshProUGUI tmp)
    {
        // Применяем шрифт Cinzel Decorative (загружаем из Assets)
        TMP_FontAsset cinzelFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/CinzelDecorative SDF.asset");
        if (cinzelFont != null)
        {
            tmp.font = cinzelFont;
        }
        else
        {
            Debug.LogWarning("Cinzel Decorative шрифт не найден по пути: Assets/Fonts/CinzelDecorative SDF.asset");
        }

        // Базовый золотой цвет
        tmp.color = new Color(0.83f, 0.68f, 0.23f, 1f); // #D4AF37

        // Градиент
        VertexGradient gradient = new VertexGradient(
            new Color(0.98f, 0.84f, 0.47f, 1f), // верх - светлое золото
            new Color(0.98f, 0.84f, 0.47f, 1f),
            new Color(0.65f, 0.48f, 0.15f, 1f), // низ - бронза
            new Color(0.65f, 0.48f, 0.15f, 1f)
        );
        tmp.colorGradient = gradient;

        // Контур
        tmp.fontStyle = FontStyles.Bold;
        tmp.outlineColor = new Color(0.15f, 0.1f, 0.05f, 1f);
        tmp.outlineWidth = 0.2f;

        // Включаем Rich Text
        tmp.richText = true;
    }

    [MenuItem("Tools/Fantasy MMO/Create Golden Text Material")]
    public static void CreateGoldenMaterial()
    {
        // Создаем материал
        Material material = new Material(Shader.Find("TextMeshPro/Distance Field"));
        material.name = "GoldenTextMaterial";

        // Базовые настройки
        material.SetColor("_FaceColor", new Color(0.83f, 0.68f, 0.23f, 1f)); // #D4AF37

        // Сохраняем материал
        string path = "Assets/UI/GoldenTextMaterial.mat";
        AssetDatabase.CreateAsset(material, path);
        AssetDatabase.SaveAssets();

        Debug.Log($"✅ Материал создан: {path}");
        EditorUtility.DisplayDialog("Успех", "Золотой материал создан в Assets/UI/", "OK");

        // Выделяем созданный материал
        Selection.activeObject = material;
    }
}
