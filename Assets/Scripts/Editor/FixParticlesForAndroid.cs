#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// Исправляет particle системы для работы на Android с URP
/// Заменяет Built-in shaders на URP-совместимые
/// </summary>
public class FixParticlesForAndroid : EditorWindow
{
    private List<ParticleSystemRenderer> foundParticles = new List<ParticleSystemRenderer>();
    private Vector2 scrollPos;
    private int fixedCount = 0;

    [MenuItem("Tools/Fix Particles for Android (URP)")]
    public static void ShowWindow()
    {
        GetWindow<FixParticlesForAndroid>("Fix Particles for Android");
    }

    void OnGUI()
    {
        GUILayout.Label("Fix Particle Systems for Android (URP)", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        EditorGUILayout.HelpBox(
            "На Android particle системы могут показывать только тени если используют Built-in shaders.\n" +
            "Этот инструмент заменит их на URP-совместимые shaders.",
            MessageType.Info
        );

        EditorGUILayout.Space();

        if (GUILayout.Button("1. Сканировать Prefabs", GUILayout.Height(30)))
        {
            ScanPrefabs();
        }

        EditorGUILayout.Space();

        if (foundParticles.Count > 0)
        {
            EditorGUILayout.LabelField($"Найдено particle систем: {foundParticles.Count}", EditorStyles.boldLabel);

            scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(200));
            foreach (var particle in foundParticles)
            {
                if (particle != null)
                {
                    EditorGUILayout.LabelField($"- {GetFullPath(particle.transform)}");
                }
            }
            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space();

            if (GUILayout.Button("2. Исправить Все Particle Системы", GUILayout.Height(40)))
            {
                FixAllParticles();
            }

            if (fixedCount > 0)
            {
                EditorGUILayout.HelpBox($"✅ Исправлено particle систем: {fixedCount}", MessageType.Info);
            }
        }
    }

    /// <summary>
    /// Сканирует все prefabs в проекте и находит particle системы
    /// </summary>
    void ScanPrefabs()
    {
        foundParticles.Clear();
        fixedCount = 0;

        // Ищем все prefabs в проекте
        string[] guids = AssetDatabase.FindAssets("t:Prefab");

        EditorUtility.DisplayProgressBar("Сканирование", "Поиск particle систем...", 0f);

        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

            if (prefab != null)
            {
                // Ищем ParticleSystemRenderer в prefab
                ParticleSystemRenderer[] renderers = prefab.GetComponentsInChildren<ParticleSystemRenderer>(true);
                foundParticles.AddRange(renderers);
            }

            EditorUtility.DisplayProgressBar("Сканирование", $"Обработано: {i + 1}/{guids.Length}", (float)(i + 1) / guids.Length);
        }

        EditorUtility.ClearProgressBar();

        Debug.Log($"[FixParticlesForAndroid] ✅ Сканирование завершено. Найдено particle систем: {foundParticles.Count}");
    }

    /// <summary>
    /// Исправляет все найденные particle системы
    /// </summary>
    void FixAllParticles()
    {
        fixedCount = 0;

        // Загружаем URP particle shader
        Shader urpParticleShader = Shader.Find("Universal Render Pipeline/Particles/Unlit");

        if (urpParticleShader == null)
        {
            EditorUtility.DisplayDialog("Ошибка", "URP Particles/Unlit shader не найден! Убедитесь что URP установлен.", "OK");
            return;
        }

        EditorUtility.DisplayProgressBar("Исправление", "Замена shaders на URP...", 0f);

        for (int i = 0; i < foundParticles.Count; i++)
        {
            ParticleSystemRenderer renderer = foundParticles[i];

            if (renderer != null && renderer.sharedMaterial != null)
            {
                Material mat = renderer.sharedMaterial;

                // Проверяем использует ли Built-in shader
                if (mat.shader.name.Contains("Particles/Standard") ||
                    mat.shader.name.Contains("Particles/Additive") ||
                    mat.shader.name.Contains("Particles/Multiply") ||
                    mat.shader.name.Contains("Particles/Alpha Blended") ||
                    mat.shader.name == "Particles/Standard Unlit")
                {
                    Debug.Log($"[FixParticlesForAndroid] 🔧 Fixing: {GetFullPath(renderer.transform)} (shader: {mat.shader.name})");

                    // Создаём новый material с URP shader
                    Material newMat = new Material(urpParticleShader);
                    newMat.name = mat.name;

                    // Копируем базовые параметры
                    if (mat.HasProperty("_MainTex"))
                    {
                        newMat.SetTexture("_BaseMap", mat.GetTexture("_MainTex"));
                    }

                    if (mat.HasProperty("_Color"))
                    {
                        newMat.SetColor("_BaseColor", mat.GetColor("_Color"));
                    }

                    // Устанавливаем blend mode
                    if (mat.shader.name.Contains("Additive"))
                    {
                        // Additive blending
                        newMat.SetFloat("_BlendOp", 0); // Add
                        newMat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
                        newMat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One);
                    }
                    else if (mat.shader.name.Contains("Multiply"))
                    {
                        // Multiply blending
                        newMat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.DstColor);
                        newMat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.Zero);
                    }
                    else
                    {
                        // Alpha blended (default)
                        newMat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
                        newMat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    }

                    // Сохраняем material
                    string prefabPath = AssetDatabase.GetAssetPath(renderer.gameObject);
                    string materialPath = prefabPath.Replace(".prefab", "_Material_URP.mat");
                    AssetDatabase.CreateAsset(newMat, materialPath);

                    // Применяем к renderer
                    renderer.sharedMaterial = newMat;

                    // Отмечаем prefab как изменённый
                    EditorUtility.SetDirty(renderer);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);

                    fixedCount++;
                }
            }

            EditorUtility.DisplayProgressBar("Исправление", $"Обработано: {i + 1}/{foundParticles.Count}", (float)(i + 1) / foundParticles.Count);
        }

        EditorUtility.ClearProgressBar();

        // Сохраняем изменения
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[FixParticlesForAndroid] ✅ Исправление завершено! Исправлено particle систем: {fixedCount}");

        EditorUtility.DisplayDialog("Готово", $"✅ Исправлено particle систем: {fixedCount}\n\nВсе particle системы теперь используют URP shaders.", "OK");
    }

    /// <summary>
    /// Получить полный путь к объекту
    /// </summary>
    string GetFullPath(Transform transform)
    {
        string path = transform.name;
        Transform current = transform.parent;

        while (current != null)
        {
            path = current.name + "/" + path;
            current = current.parent;
        }

        return path;
    }
}
#endif
