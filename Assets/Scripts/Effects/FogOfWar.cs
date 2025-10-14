using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Система тумана войны (Fog of War) как в стратегиях
/// Скрывает врагов за пределами радиуса видимости
/// Всё за пределами радиуса покрыто черной маской
/// </summary>
public class FogOfWar : MonoBehaviour
{
    [Header("Settings Asset")]
    [Tooltip("Глобальные настройки FogOfWar (ScriptableObject). Если не указано - используются локальные настройки ниже")]
    [SerializeField] private FogOfWarSettings globalSettings;

    [Header("Visibility Settings")]
    [SerializeField] private bool usePerceptionForRadius = true; // Использовать Perception для радиуса?
    [SerializeField] private float visibilityRadius = 60f; // Радиус видимости (если usePerceptionForRadius = false)
    [SerializeField] private bool ignoreHeight = true; // Игнорировать высоту (Y) при проверке расстояния
    [SerializeField] private float maxHeightDifference = 100f; // Максимальная разница по высоте (если ignoreHeight = false)
    [SerializeField] private float updateInterval = 0.2f; // Как часто обновлять (оптимизация)

    [Header("Fog Visual - Black Mask")]
    [SerializeField] private bool useBlackMask = true; // Использовать черную маску (как в стратегиях)
    [SerializeField] [Range(0f, 1f)] private float fogAlpha = 1f; // Прозрачность тумана (0 = прозрачный, 1 = черный)
    [SerializeField] private bool darkenBuildings = true; // Затемнять здания в тумане
    [SerializeField] private float darkenedBrightness = 0.3f; // Яркость затемненных зданий (0-1)

    [Header("Wall Detection")]
    [SerializeField] private bool checkLineOfSight = true; // Проверять линию видимости к врагам
    [SerializeField] private LayerMask wallLayers; // Слои которые блокируют обзор (стены)
    [SerializeField] private float minObstacleSize = 2f; // Минимальный размер препятствия чтобы блокировать обзор (столб/дерево < 2, стена > 2)

    private Transform player;
    private List<Enemy> allEnemies = new List<Enemy>();
    private List<BuildingRenderer> allBuildings = new List<BuildingRenderer>();
    private List<Projectile> activeProjectiles = new List<Projectile>(); // Активные снаряды для скрытия
    private float updateTimer = 0f;

    // CharacterStats integration (Perception влияет на радиус)
    private CharacterStats characterStats;

    // Black Mask System
    private FogOfWarCanvas fogCanvas;

    // Darkening System
    private Dictionary<Renderer, RendererMaterialData> rendererData = new Dictionary<Renderer, RendererMaterialData>();

    // Отслеживание состояния видимости врагов (для предотвращения мигания)
    private Dictionary<Enemy, EnemyVisibilityState> enemyVisibilityStates = new Dictionary<Enemy, EnemyVisibilityState>();

    // Структура для хранения данных рендерера здания
    private class BuildingRenderer
    {
        public Renderer renderer;
        public Transform transform;
    }

    // Структура для хранения оригинальных цветов материалов
    private class RendererMaterialData
    {
        public Color[] originalColors;
        public Color[] originalEmissionColors;
    }

    // Структура для отслеживания состояния видимости врага
    private class EnemyVisibilityState
    {
        public bool isCurrentlyVisible; // Текущее состояние видимости
        public float lastVisibleTime; // Время когда враг был виден в последний раз
        public bool wasVisible; // Был ли виден в прошлый раз
    }

    void Start()
    {
        // Загружаем глобальные настройки если они есть
        if (globalSettings != null)
        {
            ApplyGlobalSettings();
            Debug.Log("[FogOfWar] ✅ Применены глобальные настройки из ScriptableObject");
        }

        // Находим игрока
        player = transform;

        // Интеграция с CharacterStats (Perception → Vision Radius)
        characterStats = GetComponent<CharacterStats>();
        if (characterStats != null)
        {
            characterStats.OnStatsChanged += UpdateVisionRadiusFromStats;
            UpdateVisionRadiusFromStats();
            Debug.Log("[FogOfWar] ✅ Интеграция с CharacterStats активирована");
        }

        // Создаем черную маску используя Canvas (работает с любым рендер пайплайном)
        if (useBlackMask)
        {
            fogCanvas = gameObject.AddComponent<FogOfWarCanvas>();
            Debug.Log("[FogOfWar] ✅ Создан Canvas туман войны (URP/HDRP compatible)");
        }

        // Находим всех врагов в сцене
        FindAllEnemies();

        // Находим все здания для затемнения
        if (darkenBuildings)
        {
            FindAllBuildings();
        }

        Debug.Log($"[FogOfWar] Система инициализирована. Радиус видимости: {visibilityRadius}м");
    }

    /// <summary>
    /// Применить настройки из глобального ScriptableObject
    /// </summary>
    private void ApplyGlobalSettings()
    {
        if (globalSettings == null) return;

        useBlackMask = globalSettings.useBlackMask;
        fogAlpha = globalSettings.fogAlpha;
        darkenBuildings = globalSettings.darkenBuildings;
        darkenedBrightness = globalSettings.darkenedBrightness;
        checkLineOfSight = globalSettings.checkLineOfSight;
        wallLayers = globalSettings.wallLayers;
        minObstacleSize = globalSettings.minObstacleSize;
        ignoreHeight = globalSettings.ignoreHeight;
        maxHeightDifference = globalSettings.maxHeightDifference;
        updateInterval = globalSettings.updateInterval;
    }

    /// <summary>
    /// Обновить радиус видимости на основе Perception
    /// </summary>
    private void UpdateVisionRadiusFromStats()
    {
        // Если отключено использование Perception - не обновляем
        if (!usePerceptionForRadius)
        {
            Debug.Log($"[FogOfWar] Perception отключен. Используется фиксированный радиус: {visibilityRadius}м");
            return;
        }

        if (characterStats != null)
        {
            visibilityRadius = characterStats.VisionRadius;
            Debug.Log($"[FogOfWar] Радиус видимости обновлен: {visibilityRadius}м (Perception: {characterStats.perception})");
        }
    }

    private void OnDestroy()
    {
        // Отписываемся от событий
        if (characterStats != null)
        {
            characterStats.OnStatsChanged -= UpdateVisionRadiusFromStats;
        }
    }

    void Update()
    {
        updateTimer += Time.deltaTime;

        // Обновляем видимость врагов периодически (оптимизация)
        if (updateTimer >= updateInterval)
        {
            updateTimer = 0f;
            UpdateEnemyVisibility();

            // Обновляем затемнение зданий
            if (darkenBuildings)
            {
                UpdateBuildingDarkening();
            }

            // Обновляем видимость снарядов
            UpdateProjectileVisibility();
        }

        // Синхронизируем настройки с canvas (только если изменились)
        if (fogCanvas != null)
        {
            fogCanvas.SetVisibilityRadius(visibilityRadius);
            fogCanvas.SetFogAlpha(fogAlpha);
        }
    }

    /// <summary>
    /// Найти всех врагов в сцене
    /// </summary>
    private void FindAllEnemies()
    {
        allEnemies.Clear();
        Enemy[] enemies = FindObjectsByType<Enemy>(FindObjectsSortMode.None);
        allEnemies.AddRange(enemies);

        Debug.Log($"[FogOfWar] Найдено врагов: {allEnemies.Count}");
    }

    /// <summary>
    /// Обновить видимость врагов
    /// </summary>
    private void UpdateEnemyVisibility()
    {
        if (player == null)
        {
            Debug.LogWarning("[FogOfWar] Игрок не найден!");
            return;
        }

        // Проверяем каждого врага
        foreach (Enemy enemy in allEnemies)
        {
            if (enemy == null) continue;

            // Создаем состояние для нового врага
            if (!enemyVisibilityStates.ContainsKey(enemy))
            {
                enemyVisibilityStates[enemy] = new EnemyVisibilityState
                {
                    isCurrentlyVisible = false,
                    lastVisibleTime = 0f,
                    wasVisible = false
                };
            }

            EnemyVisibilityState state = enemyVisibilityStates[enemy];

            // Вычисляем расстояние до игрока
            Vector3 playerPos = player.position;
            Vector3 enemyPos = enemy.transform.position;

            float distance;
            if (ignoreHeight)
            {
                // ИГНОРИРУЕМ высоту (только X,Z) - враги на любой высоте видны
                playerPos.y = 0;
                enemyPos.y = 0;
                distance = Vector3.Distance(playerPos, enemyPos);
            }
            else
            {
                // УЧИТЫВАЕМ высоту (X,Y,Z) но с ограничением
                distance = Vector3.Distance(player.position, enemy.transform.position);

                // Проверяем разницу по высоте
                float heightDifference = Mathf.Abs(player.position.y - enemy.transform.position.y);
                if (heightDifference > maxHeightDifference)
                {
                    // Слишком высоко/низко - не виден
                    distance = float.MaxValue;
                }
            }

            // Проверяем расстояние и линию видимости
            bool shouldBeVisible = distance <= visibilityRadius;

            // Проверяем есть ли стена между игроком и врагом
            if (shouldBeVisible && checkLineOfSight)
            {
                shouldBeVisible = HasLineOfSight(player.position, enemy.transform.position);
            }

            // Обновляем состояние
            if (shouldBeVisible)
            {
                // Враг должен быть виден - сразу показываем
                state.lastVisibleTime = Time.time;
                if (!state.isCurrentlyVisible)
                {
                    state.isCurrentlyVisible = true;
                    SetEnemyVisibility(enemy, true);
                }
            }
            else
            {
                // Враг не должен быть виден
                // Добавляем небольшую задержку перед скрытием (0.15 секунды)
                // Это предотвращает мигание на границе видимости
                float timeSinceVisible = Time.time - state.lastVisibleTime;
                if (state.isCurrentlyVisible && timeSinceVisible > 0.15f)
                {
                    state.isCurrentlyVisible = false;
                    SetEnemyVisibility(enemy, false);
                }
            }

            state.wasVisible = shouldBeVisible;
        }
    }

    /// <summary>
    /// Установить видимость врага (включая NetworkPlayer для PvP)
    /// </summary>
    private void SetEnemyVisibility(Enemy enemy, bool visible)
    {
        if (enemy == null) return;

        // ВАЖНО: ТЕПЕРЬ СКРЫВАЕМ NetworkPlayer (для PvP Fog of War как в Dota/Warcraft)
        NetworkPlayer networkPlayer = enemy.GetComponent<NetworkPlayer>();
        if (networkPlayer != null)
        {
            // Это сетевой игрок - применяем Fog of War!
            Debug.Log($"[FogOfWar] NetworkPlayer {networkPlayer.username}: visible={visible}");

            // Скрываем/показываем визуал игрока
            Renderer[] renderers = networkPlayer.GetComponentsInChildren<Renderer>();
            foreach (Renderer rend in renderers)
            {
                rend.enabled = visible;
            }

            // ВАЖНО: Также скрываем/показываем nameplate
            // ИСПРАВЛЕНО: Используем GetNameplate() для получения ссылки
            GameObject nameplate = networkPlayer.GetNameplate();
            if (nameplate != null)
            {
                nameplate.SetActive(visible);
                Debug.Log($"[FogOfWar] Nameplate для {networkPlayer.username}: visible={visible}");
            }
            else
            {
                Debug.LogWarning($"[FogOfWar] Nameplate не найден для {networkPlayer.username}");
            }

            // ВАЖНО: НЕ отключаем коллайдер NetworkPlayer!
            // Иначе через них можно будет ходить
            // Коллайдер остаётся активным, но враг невидим (как в Dota)

            return;
        }

        // Обычный NPC враг
        Renderer[] npcRenderers = enemy.GetComponentsInChildren<Renderer>();
        foreach (Renderer rend in npcRenderers)
        {
            rend.enabled = visible;
        }

        // Для NPC можно отключить коллайдер (оптимизация)
        Collider collider = enemy.GetComponent<Collider>();
        if (collider != null)
        {
            collider.enabled = visible;
        }
    }

    /// <summary>
    /// Проверить есть ли прямая видимость между двумя точками
    /// </summary>
    private bool HasLineOfSight(Vector3 from, Vector3 to)
    {
        // ИСПРАВЛЕНО: Луч идёт от центра игрока к центру врага
        Vector3 fromEyeLevel = from + Vector3.up * 1.5f; // Уровень глаз игрока
        Vector3 toEyeLevel = to + Vector3.up * 1.5f;     // Центр врага

        Vector3 direction = toEyeLevel - fromEyeLevel;
        float distance = direction.magnitude;

        // Raycast от игрока к врагу
        RaycastHit hit;
        if (Physics.Raycast(fromEyeLevel, direction.normalized, out hit, distance, wallLayers))
        {
            // ИСПРАВЛЕНО: Проверяем что луч НЕ попал во врага
            // Враг не должен блокировать видимость сам себе!
            Enemy hitEnemy = hit.collider.GetComponent<Enemy>();
            if (hitEnemy != null || hit.collider.GetComponentInParent<Enemy>() != null)
            {
                // Луч попал во врага - это нормально, враг виден
                return true;
            }

            // Проверяем размер препятствия
            Renderer obstacleRenderer = hit.collider.GetComponent<Renderer>();
            if (obstacleRenderer != null)
            {
                // Вычисляем размер препятствия
                float obstacleSize = Mathf.Max(
                    obstacleRenderer.bounds.size.x,
                    obstacleRenderer.bounds.size.z
                );

                // Если препятствие большое (стена) - блокирует обзор
                // Если маленькое (столб/дерево) - не блокирует
                if (obstacleSize >= minObstacleSize)
                {
                    Debug.Log($"[FogOfWar] Луч заблокирован: {hit.collider.name} (размер: {obstacleSize}м)");
                    return false; // Большое препятствие - не видно
                }
                else
                {
                    return true; // Маленькое препятствие - видно
                }
            }

            // Если нет Renderer - считаем стеной
            Debug.Log($"[FogOfWar] Луч заблокирован: {hit.collider.name} (нет Renderer)");
            return false;
        }

        // Нет препятствий - видно
        return true;
    }

    /// <summary>
    /// Добавить нового врага в список (если спавнится динамически)
    /// </summary>
    public void RegisterEnemy(Enemy enemy)
    {
        if (!allEnemies.Contains(enemy))
        {
            allEnemies.Add(enemy);
            Debug.Log($"[FogOfWar] Зарегистрирован новый враг: {enemy.GetEnemyName()}");
        }
    }

    /// <summary>
    /// Удалить врага из списка (когда умирает)
    /// </summary>
    public void UnregisterEnemy(Enemy enemy)
    {
        if (allEnemies.Contains(enemy))
        {
            allEnemies.Remove(enemy);
            Debug.Log($"[FogOfWar] Враг удален из списка: {enemy.GetEnemyName()}");
        }
    }

    /// <summary>
    /// Обновить список врагов (если враги спавнятся/удаляются)
    /// </summary>
    public void RefreshEnemies()
    {
        FindAllEnemies();
    }

    /// <summary>
    /// Проверить виден ли враг игроку (в радиусе и без стен)
    /// </summary>
    public bool IsEnemyVisible(Enemy enemy)
    {
        if (enemy == null || player == null) return false;

        // Проверяем расстояние
        Vector3 playerPos = player.position;
        Vector3 enemyPos = enemy.transform.position;

        float distance;
        if (ignoreHeight)
        {
            // ИГНОРИРУЕМ высоту (только X,Z)
            playerPos.y = 0;
            enemyPos.y = 0;
            distance = Vector3.Distance(playerPos, enemyPos);
        }
        else
        {
            // УЧИТЫВАЕМ высоту (X,Y,Z) с ограничением
            distance = Vector3.Distance(player.position, enemy.transform.position);

            // Проверяем разницу по высоте
            float heightDifference = Mathf.Abs(player.position.y - enemy.transform.position.y);
            if (heightDifference > maxHeightDifference)
            {
                return false; // Слишком высоко/низко
            }
        }

        // Если за пределами радиуса - не виден
        if (distance > visibilityRadius)
            return false;

        // Проверяем линию видимости (стены)
        if (checkLineOfSight)
        {
            return HasLineOfSight(player.position, enemy.transform.position);
        }

        return true;
    }

    /// <summary>
    /// Проверить виден ли враг игроку (перегрузка для Transform)
    /// </summary>
    public bool IsEnemyVisible(Transform enemyTransform)
    {
        if (enemyTransform == null) return false;

        Enemy enemy = enemyTransform.GetComponent<Enemy>();
        if (enemy != null)
        {
            return IsEnemyVisible(enemy);
        }

        // Если нет компонента Enemy, проверяем только расстояние и стены
        Vector3 playerPos = player.position;
        Vector3 enemyPos = enemyTransform.position;

        float distance;
        if (ignoreHeight)
        {
            // ИГНОРИРУЕМ высоту (только X,Z)
            playerPos.y = 0;
            enemyPos.y = 0;
            distance = Vector3.Distance(playerPos, enemyPos);
        }
        else
        {
            // УЧИТЫВАЕМ высоту (X,Y,Z) с ограничением
            distance = Vector3.Distance(player.position, enemyTransform.position);

            // Проверяем разницу по высоте
            float heightDifference = Mathf.Abs(player.position.y - enemyTransform.position.y);
            if (heightDifference > maxHeightDifference)
            {
                return false; // Слишком высоко/низко
            }
        }

        if (distance > visibilityRadius)
            return false;

        if (checkLineOfSight)
        {
            return HasLineOfSight(player.position, enemyTransform.position);
        }

        return true;
    }

    /// <summary>
    /// Найти все здания в сцене (всё кроме врагов и игрока)
    /// </summary>
    private void FindAllBuildings()
    {
        allBuildings.Clear();

        // Находим все объекты с Renderer
        Renderer[] allRenderers = FindObjectsByType<Renderer>(FindObjectsSortMode.None);

        foreach (Renderer rend in allRenderers)
        {
            // Пропускаем врагов
            if (rend.GetComponentInParent<Enemy>() != null)
                continue;

            // Пропускаем игрока и его оружие
            if (rend.transform.IsChildOf(player))
                continue;

            // Пропускаем UI элементы
            if (rend.GetComponent<Canvas>() != null)
                continue;

            // Пропускаем Terrain
            if (rend.GetComponent<Terrain>() != null)
                continue;

            // Добавляем как здание
            BuildingRenderer building = new BuildingRenderer
            {
                renderer = rend,
                transform = rend.transform
            };
            allBuildings.Add(building);

            // Сохраняем оригинальные цвета материалов
            if (!rendererData.ContainsKey(rend))
            {
                Material[] materials = rend.materials;
                RendererMaterialData data = new RendererMaterialData();
                data.originalColors = new Color[materials.Length];
                data.originalEmissionColors = new Color[materials.Length];

                for (int i = 0; i < materials.Length; i++)
                {
                    if (materials[i].HasProperty("_Color"))
                    {
                        data.originalColors[i] = materials[i].color;
                    }
                    if (materials[i].HasProperty("_EmissionColor"))
                    {
                        data.originalEmissionColors[i] = materials[i].GetColor("_EmissionColor");
                    }
                }

                rendererData[rend] = data;
            }
        }

        Debug.Log($"[FogOfWar] Найдено зданий для затемнения: {allBuildings.Count}");
    }

    /// <summary>
    /// Обновить затемнение зданий
    /// </summary>
    private void UpdateBuildingDarkening()
    {
        if (player == null) return;

        foreach (BuildingRenderer building in allBuildings)
        {
            if (building.renderer == null || building.transform == null) continue;
            if (!rendererData.ContainsKey(building.renderer)) continue;

            // Вычисляем расстояние до игрока
            Vector3 playerPos = player.position;
            Vector3 buildingPos = building.transform.position;

            float distance;
            if (ignoreHeight)
            {
                // ИГНОРИРУЕМ высоту (только X,Z)
                playerPos.y = 0;
                buildingPos.y = 0;
                distance = Vector3.Distance(playerPos, buildingPos);
            }
            else
            {
                // УЧИТЫВАЕМ высоту (X,Y,Z)
                distance = Vector3.Distance(player.position, building.transform.position);
            }

            // Вычисляем фактор затемнения
            float darkenFactor;
            if (distance <= visibilityRadius)
            {
                // Внутри радиуса - нормальная яркость
                darkenFactor = 0f;
            }
            else if (distance <= visibilityRadius + 10f)
            {
                // Переходная зона - плавное затемнение
                float t = (distance - visibilityRadius) / 10f;
                darkenFactor = Mathf.Lerp(0f, 1f, t);
            }
            else
            {
                // За пределами - полное затемнение
                darkenFactor = 1f;
            }

            // Применяем затемнение
            ApplyDarkeningToRenderer(building.renderer, darkenFactor);
        }
    }

    /// <summary>
    /// Применить затемнение к renderer
    /// </summary>
    private void ApplyDarkeningToRenderer(Renderer rend, float darkenFactor)
    {
        if (!rendererData.ContainsKey(rend)) return;

        RendererMaterialData data = rendererData[rend];
        Material[] materials = rend.materials;

        for (int i = 0; i < materials.Length; i++)
        {
            if (i >= data.originalColors.Length) continue;

            // Интерполируем между оригинальным и затемненным цветом
            Color originalColor = data.originalColors[i];
            Color targetColor = originalColor * darkenedBrightness;
            Color finalColor = Color.Lerp(originalColor, targetColor, darkenFactor);

            if (materials[i].HasProperty("_Color"))
            {
                materials[i].color = finalColor;
            }

            // Затемняем emission
            if (i < data.originalEmissionColors.Length && materials[i].HasProperty("_EmissionColor"))
            {
                Color originalEmission = data.originalEmissionColors[i];
                Color targetEmission = originalEmission * darkenedBrightness;
                Color finalEmission = Color.Lerp(originalEmission, targetEmission, darkenFactor);
                materials[i].SetColor("_EmissionColor", finalEmission);
            }
        }

        rend.materials = materials;
    }

    /// <summary>
    /// Обновить видимость снарядов (скрывать снаряды за пределами FoW)
    /// </summary>
    private void UpdateProjectileVisibility()
    {
        // Находим все активные снаряды
        activeProjectiles.Clear();
        Projectile[] projectiles = FindObjectsByType<Projectile>(FindObjectsSortMode.None);
        activeProjectiles.AddRange(projectiles);

        foreach (Projectile projectile in activeProjectiles)
        {
            if (projectile == null) continue;

            // Проверяем расстояние снаряда до игрока
            float distance = Vector3.Distance(player.position, projectile.transform.position);

            // Снаряд виден только если он в радиусе видимости
            bool shouldBeVisible = distance <= visibilityRadius;

            // Скрываем/показываем снаряд
            Renderer[] renderers = projectile.GetComponentsInChildren<Renderer>();
            foreach (Renderer rend in renderers)
            {
                rend.enabled = shouldBeVisible;
            }

            // Также скрываем TrailRenderer если есть
            TrailRenderer trail = projectile.GetComponent<TrailRenderer>();
            if (trail != null)
            {
                trail.enabled = shouldBeVisible;
            }

            // КРИТИЧЕСКИ ВАЖНО: Скрываем СВЕТ от снаряда (свет виден за пределами FoW!)
            Light[] lights = projectile.GetComponentsInChildren<Light>();
            foreach (Light light in lights)
            {
                light.enabled = shouldBeVisible;
            }
        }
    }

    /// <summary>
    /// Визуализация радиуса видимости в редакторе
    /// </summary>
    void OnDrawGizmos()
    {
        if (player == null) return;

        // Рисуем зеленый круг - радиус видимости
        Gizmos.color = new Color(0, 1, 0, 0.3f);
        DrawCircle(player.position, visibilityRadius, 64);

        // Рисуем желтую линию границы
        Gizmos.color = Color.yellow;
        DrawCircle(player.position, visibilityRadius, 64, true);
    }

    /// <summary>
    /// Нарисовать круг в Gizmos
    /// </summary>
    private void DrawCircle(Vector3 center, float radius, int segments, bool wireOnly = false)
    {
        float angleStep = 360f / segments;
        Vector3 prevPoint = center + new Vector3(radius, 0, 0);

        for (int i = 1; i <= segments; i++)
        {
            float angle = i * angleStep * Mathf.Deg2Rad;
            Vector3 newPoint = center + new Vector3(
                Mathf.Cos(angle) * radius,
                0,
                Mathf.Sin(angle) * radius
            );

            Gizmos.DrawLine(prevPoint, newPoint);

            if (!wireOnly && i > 1)
            {
                Gizmos.DrawLine(center, prevPoint);
                Gizmos.DrawLine(center, newPoint);
            }

            prevPoint = newPoint;
        }
    }
}
