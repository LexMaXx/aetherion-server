using UnityEngine;

/// <summary>
/// Улучшенный эффект свечения оружия (Lineage 2 style)
/// - Переливание цветов (голубой → синий → голубой)
/// - Аура вокруг оружия
/// - Летающие частицы
/// </summary>
public class WeaponGlowEffect : MonoBehaviour
{
    [Header("Glow Settings")]
    [SerializeField] private Color glowColor1 = new Color(0.4f, 0.7f, 1.0f); // Голубой
    [SerializeField] private Color glowColor2 = new Color(0.2f, 0.4f, 1.0f); // Синий
    [SerializeField] private float glowIntensity = 3.0f;
    [SerializeField] private float colorTransitionSpeed = 2.0f; // Скорость переливания

    [Header("Aura Settings")]
    [SerializeField] private float auraSize = 0.15f;
    [SerializeField] private Color auraColor = new Color(0.3f, 0.6f, 1.0f, 0.3f);

    [Header("Particle Settings")]
    [SerializeField] private int particleCount = 30; // Больше частиц
    [SerializeField] private float particleSize = 0.08f;
    [SerializeField] private float particleSpeed = 1.0f;

    private ParticleSystem electricParticles;
    private ParticleSystem auraParticles;
    private GameObject auraObject;
    private Material[] originalMaterials;
    private Renderer[] weaponRenderers;
    private bool isGlowing = false;
    private float colorTransition = 0f;

    void Start()
    {
        // Получаем все Renderer'ы оружия
        weaponRenderers = GetComponentsInChildren<Renderer>();

        // Сохраняем оригинальные материалы
        SaveOriginalMaterials();

        // Создаём системы частиц
        CreateElectricParticles();
        CreateAuraParticles();

        // По умолчанию выключено
        DeactivateGlow();
    }

    void Update()
    {
        if (isGlowing)
        {
            // Переливание цветов
            colorTransition += Time.deltaTime * colorTransitionSpeed;
            float t = Mathf.PingPong(colorTransition, 1.0f); // 0 -> 1 -> 0

            Color currentGlowColor = Color.Lerp(glowColor1, glowColor2, t);

            // Обновляем эмиссию на материалах
            foreach (Renderer rend in weaponRenderers)
            {
                foreach (Material mat in rend.materials)
                {
                    mat.SetColor("_EmissionColor", currentGlowColor * glowIntensity);
                }
            }
        }
    }

    /// <summary>
    /// Сохранить оригинальные материалы
    /// </summary>
    private void SaveOriginalMaterials()
    {
        int totalMaterials = 0;
        foreach (Renderer rend in weaponRenderers)
        {
            totalMaterials += rend.materials.Length;
        }

        originalMaterials = new Material[totalMaterials];
        int index = 0;

        foreach (Renderer rend in weaponRenderers)
        {
            foreach (Material mat in rend.materials)
            {
                originalMaterials[index] = new Material(mat);
                index++;
            }
        }
    }

    /// <summary>
    /// Создать систему частиц электричества
    /// </summary>
    private void CreateElectricParticles()
    {
        GameObject particleObj = new GameObject("ElectricParticles");
        particleObj.transform.SetParent(transform);
        particleObj.transform.localPosition = Vector3.zero;

        electricParticles = particleObj.AddComponent<ParticleSystem>();

        // ВАЖНО: Останавливаем систему перед настройкой
        electricParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = electricParticles.main;
        main.duration = 1.0f;
        main.loop = true;
        main.startLifetime = 0.5f;
        main.startSpeed = particleSpeed;
        main.startSize = particleSize;
        main.startColor = new Color(0.6f, 0.8f, 1.0f, 1.0f);
        main.maxParticles = particleCount;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;

        var emission = electricParticles.emission;
        emission.rateOverTime = particleCount * 3;

        var shape = electricParticles.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.4f;

        // Gradient - переливание голубой -> синий -> белый
        var col = electricParticles.colorOverLifetime;
        col.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(new Color(0.9f, 0.95f, 1.0f), 0.0f), // Светло-голубой
                new GradientColorKey(new Color(0.4f, 0.7f, 1.0f), 0.3f),  // Голубой
                new GradientColorKey(new Color(0.2f, 0.5f, 1.0f), 0.7f),  // Синий
                new GradientColorKey(new Color(0.8f, 0.9f, 1.0f), 1.0f)   // Белый
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(1.0f, 0.0f),
                new GradientAlphaKey(0.8f, 0.5f),
                new GradientAlphaKey(0.0f, 1.0f)
            }
        );
        col.color = new ParticleSystem.MinMaxGradient(grad);

        var sizeOverLifetime = electricParticles.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve();
        sizeCurve.AddKey(0.0f, 0.3f);
        sizeCurve.AddKey(0.5f, 1.0f);
        sizeCurve.AddKey(1.0f, 0.0f);
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1.0f, sizeCurve);

        var renderer = electricParticles.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        // Use Mobile/Particles/Additive - available in all Unity versions
        Shader particleShader = Shader.Find("Mobile/Particles/Additive");
        if (particleShader == null)
        {
            particleShader = Shader.Find("Particles/Additive");
        }
        if (particleShader != null)
        {
            renderer.material = new Material(particleShader);
            renderer.material.SetColor("_TintColor", new Color(0.5f, 0.8f, 1.0f, 0.8f));
        }
        else
        {
            Debug.LogWarning("[WeaponGlowEffect] Particle shader not found, using default");
        }

        Debug.Log("[WeaponGlowEffect] ✨ Создана система частиц электричества");
    }

    /// <summary>
    /// Создать систему частиц для ауры
    /// </summary>
    private void CreateAuraParticles()
    {
        GameObject auraObj = new GameObject("AuraParticles");
        auraObj.transform.SetParent(transform);
        auraObj.transform.localPosition = Vector3.zero;

        auraParticles = auraObj.AddComponent<ParticleSystem>();

        // ВАЖНО: Останавливаем систему перед настройкой
        auraParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = auraParticles.main;
        main.duration = 1.0f;
        main.loop = true;
        main.startLifetime = 0.8f;
        main.startSpeed = 0.2f;
        main.startSize = auraSize;
        main.startColor = auraColor;
        main.maxParticles = 50;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;

        var emission = auraParticles.emission;
        emission.rateOverTime = 60;

        var shape = auraParticles.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.35f;

        // Аура - мягкое свечение
        var col = auraParticles.colorOverLifetime;
        col.enabled = true;
        Gradient auraGrad = new Gradient();
        auraGrad.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(new Color(0.5f, 0.8f, 1.0f), 0.0f),
                new GradientColorKey(new Color(0.3f, 0.6f, 1.0f), 0.5f),
                new GradientColorKey(new Color(0.4f, 0.7f, 1.0f), 1.0f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0.0f, 0.0f),
                new GradientAlphaKey(0.4f, 0.3f),
                new GradientAlphaKey(0.0f, 1.0f)
            }
        );
        col.color = new ParticleSystem.MinMaxGradient(auraGrad);

        var sizeOverLifetime = auraParticles.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve auraSizeCurve = new AnimationCurve();
        auraSizeCurve.AddKey(0.0f, 0.5f);
        auraSizeCurve.AddKey(0.5f, 1.0f);
        auraSizeCurve.AddKey(1.0f, 1.2f);
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1.0f, auraSizeCurve);

        var renderer = auraParticles.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        // Use Mobile/Particles/Additive - available in all Unity versions
        Shader auraShader = Shader.Find("Mobile/Particles/Additive");
        if (auraShader == null)
        {
            auraShader = Shader.Find("Particles/Additive");
        }
        if (auraShader != null)
        {
            renderer.material = new Material(auraShader);
            renderer.material.SetColor("_TintColor", new Color(0.4f, 0.7f, 1.0f, 0.3f));
        }
        else
        {
            Debug.LogWarning("[WeaponGlowEffect] Aura shader not found, using default");
        }

        Debug.Log("[WeaponGlowEffect] 🌟 Создана система частиц ауры");
    }

    /// <summary>
    /// Активировать эффект свечения
    /// </summary>
    public void ActivateGlow()
    {
        if (isGlowing) return;
        isGlowing = true;
        colorTransition = 0f;

        // Включаем эмиссию на материалах
        foreach (Renderer rend in weaponRenderers)
        {
            foreach (Material mat in rend.materials)
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", glowColor1 * glowIntensity);

                // Добавляем голубой оттенок
                Color originalColor = mat.color;
                mat.color = originalColor + new Color(0.1f, 0.2f, 0.3f, 0);
            }
        }

        // Запускаем частицы
        if (electricParticles != null)
        {
            electricParticles.Play();
        }

        if (auraParticles != null)
        {
            auraParticles.Play();
        }

        Debug.Log("[WeaponGlowEffect] ⚡ Эффект свечения активирован (с переливанием и аурой)");
    }

    /// <summary>
    /// Деактивировать эффект свечения
    /// </summary>
    public void DeactivateGlow()
    {
        if (!isGlowing && originalMaterials == null) return;
        isGlowing = false;

        // Восстанавливаем оригинальные материалы
        if (originalMaterials != null && weaponRenderers != null)
        {
            int index = 0;
            foreach (Renderer rend in weaponRenderers)
            {
                Material[] mats = new Material[rend.materials.Length];
                for (int i = 0; i < mats.Length; i++)
                {
                    if (index < originalMaterials.Length)
                    {
                        mats[i] = new Material(originalMaterials[index]);
                        index++;
                    }
                }
                rend.materials = mats;
            }
        }

        // Останавливаем частицы
        if (electricParticles != null)
        {
            electricParticles.Stop();
        }

        if (auraParticles != null)
        {
            auraParticles.Stop();
        }

        Debug.Log("[WeaponGlowEffect] 💤 Эффект свечения деактивирован");
    }

    void OnDestroy()
    {
        if (electricParticles != null)
        {
            Destroy(electricParticles.gameObject);
        }

        if (auraParticles != null)
        {
            Destroy(auraParticles.gameObject);
        }
    }
}
