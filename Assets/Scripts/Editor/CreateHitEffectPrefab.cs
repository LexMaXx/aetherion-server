using UnityEngine;
using UnityEditor;

public class CreateHitEffectPrefab : MonoBehaviour
{
    [MenuItem("Tools/Create Hit Effect Prefab")]
    static void CreateHitEffect()
    {
        // Создаем GameObject для эффекта
        GameObject hitEffect = new GameObject("HitEffect");

        // Добавляем Particle System
        ParticleSystem ps = hitEffect.AddComponent<ParticleSystem>();

        // Настройка основных параметров
        var main = ps.main;
        main.duration = 0.5f;
        main.startLifetime = 0.3f;
        main.startSpeed = 5f;
        main.startSize = 0.5f;
        main.startColor = new Color(1f, 0.8f, 0.3f, 1f); // Оранжево-желтый
        main.maxParticles = 20;
        main.loop = false;
        main.playOnAwake = true;

        // Emission - взрывной выброс частиц
        var emission = ps.emission;
        emission.rateOverTime = 0;
        emission.SetBursts(new ParticleSystem.Burst[] {
            new ParticleSystem.Burst(0.0f, 15, 25, 1, 0.01f)
        });

        // Shape - сфера для взрывного эффекта
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.1f;

        // Color over Lifetime - затухание
        var col = ps.colorOverLifetime;
        col.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(Color.white, 0.0f),
                new GradientColorKey(new Color(1f, 0.5f, 0f), 0.5f),
                new GradientColorKey(Color.red, 1.0f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(1.0f, 0.0f),
                new GradientAlphaKey(0.8f, 0.5f),
                new GradientAlphaKey(0.0f, 1.0f)
            }
        );
        col.color = new ParticleSystem.MinMaxGradient(grad);

        // Size over Lifetime - уменьшение размера
        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve();
        sizeCurve.AddKey(0.0f, 1.0f);
        sizeCurve.AddKey(1.0f, 0.0f);
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1.0f, sizeCurve);

        // Renderer настройки
        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.material = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Particle.mat");

        // Добавляем AutoDestroy скрипт
        var autoDestroy = hitEffect.AddComponent<AutoDestroyParticleSystem>();

        // Создаем папку Effects если её нет
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs/Effects"))
        {
            if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
            {
                AssetDatabase.CreateFolder("Assets", "Prefabs");
            }
            AssetDatabase.CreateFolder("Assets/Prefabs", "Effects");
        }

        // Сохраняем prefab
        string prefabPath = "Assets/Prefabs/Effects/HitEffect.prefab";
        PrefabUtility.SaveAsPrefabAsset(hitEffect, prefabPath);

        Debug.Log($"✅ Hit Effect prefab создан: {prefabPath}");

        // Удаляем временный объект
        DestroyImmediate(hitEffect);

        AssetDatabase.Refresh();
    }
}
