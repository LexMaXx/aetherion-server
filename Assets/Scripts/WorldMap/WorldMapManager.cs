using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Управление 3D мировой картой
/// Размещается на сцене WorldMapScene
/// Управляет камерой, локациями на terrain
/// </summary>
public class WorldMapManager : MonoBehaviour
{
    public static WorldMapManager Instance { get; private set; }

    [Header("Player Reference")]
    [Tooltip("Персонаж игрока на карте мира (можно оставить пустым - спавнится автоматически)")]
    [SerializeField] private GameObject playerCharacter;

    [Tooltip("Позиция спавна персонажа на карте")]
    [SerializeField] private Vector3 playerSpawnPosition = new Vector3(250, 1, 250);

    [Header("Camera Settings")]
    [Tooltip("Камера, следующая за игроком")]
    [SerializeField] private Camera worldMapCamera;

    [Tooltip("Высота камеры над игроком")]
    [SerializeField] private float cameraHeight = 20f;

    [Tooltip("Угол наклона камеры (градусы)")]
    [SerializeField] private float cameraAngle = 60f;

    [Tooltip("Дистанция камеры от игрока")]
    [SerializeField] private float cameraDistance = 15f;

    [Tooltip("Скорость следования камеры")]
    [SerializeField] private float cameraFollowSpeed = 5f;

    [Header("Locations")]
    [Tooltip("Список всех локаций на карте")]
    [SerializeField] private List<LocationData> allLocations = new List<LocationData>();

    [Tooltip("Prefab для 3D маркера локации")]
    [SerializeField] private GameObject locationMarkerPrefab;

    [Tooltip("Радиус взаимодействия с локацией")]
    [SerializeField] private float interactionRadius = 3f;

    [Header("UI")]
    [Tooltip("Canvas для UI подсказок")]
    [SerializeField] private Canvas uiCanvas;

    // Runtime переменные
    private List<WorldMapLocationMarker> spawnedMarkers = new List<WorldMapLocationMarker>();
    private WorldMapLocationMarker nearestMarker;
    private Vector3 cameraOffset;

    void Awake()
    {
        // Singleton
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        // КРИТИЧЕСКОЕ: Проверяем наличие GameProgressManager
        if (GameProgressManager.Instance == null)
        {
            Debug.LogWarning("[WorldMapManager] ⚠️ GameProgressManager не найден!");
            Debug.LogWarning("[WorldMapManager] 🔧 Создаю GameProgressManager автоматически...");

            GameObject gpmObj = new GameObject("GameProgressManager");
            gpmObj.AddComponent<GameProgressManager>();

            Debug.Log("[WorldMapManager] ✅ GameProgressManager создан автоматически");
        }

        InitializeMap();
        SetupCamera();
        SpawnLocationMarkers();

        Debug.Log("[WorldMapManager] ✅ 3D карта мира инициализирована");
    }

    void LateUpdate()
    {
        UpdateCameraPosition();
        CheckNearestLocation();
    }

    /// <summary>
    /// Инициализация карты
    /// </summary>
    private void InitializeMap()
    {
        // Проверяем наличие персонажа
        if (playerCharacter == null)
        {
            // КРИТИЧЕСКОЕ ИЗМЕНЕНИЕ: НЕ ищем Player в сцене!
            // ВСЕГДА спавним персонажа из GameProgressManager
            // Иначе может заспавниться случайный Capsule с тегом Player!

            Debug.Log("[WorldMapManager] 🔍 Персонаж не назначен в Inspector - спавним из сохранения...");
            SpawnSelectedCharacter();
        }
        else
        {
            // Персонаж назначен в Inspector вручную
            Debug.Log($"[WorldMapManager] ✅ Персонаж назначен вручную: {playerCharacter.name}");
        }

        // Проверяем наличие камеры
        if (worldMapCamera == null)
        {
            worldMapCamera = Camera.main;
            Debug.Log("[WorldMapManager] Камера найдена автоматически");
        }
    }

    /// <summary>
    /// Спавн выбранного персонажа из GameProgressManager
    /// </summary>
    private void SpawnSelectedCharacter()
    {
        if (GameProgressManager.Instance == null)
        {
            Debug.LogError("[WorldMapManager] ❌ GameProgressManager не найден!");
            Debug.LogError("[WorldMapManager] 💡 Создайте пустой GameObject → Add Component → GameProgressManager");
            return;
        }

        // Получаем сохранённый класс персонажа
        string savedCharacterName = GameProgressManager.Instance.GetSelectedCharacterPrefab()?.name;

        Debug.Log($"[WorldMapManager] 🔍 Проверка сохранённого персонажа...");
        Debug.Log($"[WorldMapManager] 📋 Saved character: {savedCharacterName ?? "НЕТ"}");
        Debug.Log($"[WorldMapManager] 📋 PlayerPrefs SelectedCharacterClass: {PlayerPrefs.GetString("SelectedCharacterClass", "НЕТ")}");

        GameObject characterPrefab = GameProgressManager.Instance.GetSelectedCharacterPrefab();

        if (characterPrefab != null)
        {
            // Спавним персонажа
            playerCharacter = Instantiate(characterPrefab, playerSpawnPosition, Quaternion.identity);
            playerCharacter.tag = "Player";
            playerCharacter.name = $"Player (WorldMap) - {characterPrefab.name}";

            // Добавляем WorldMapPlayerController если нет
            if (playerCharacter.GetComponent<WorldMapPlayerController>() == null)
            {
                playerCharacter.AddComponent<WorldMapPlayerController>();
            }

            // Добавляем CharacterController если нет
            if (playerCharacter.GetComponent<CharacterController>() == null)
            {
                CharacterController cc = playerCharacter.AddComponent<CharacterController>();
                cc.height = 2f;
                cc.radius = 0.5f;
                cc.center = new Vector3(0, 1, 0);
            }

            Debug.Log($"[WorldMapManager] ✅ Персонаж '{characterPrefab.name}' заспавнен на карте мира");
        }
        else
        {
            Debug.LogError("[WorldMapManager] ❌ Персонаж не выбран!");
            Debug.LogError("[WorldMapManager] 💡 Чтобы персонаж появился на карте:");
            Debug.LogError("[WorldMapManager] 1. Зайдите в BattleScene");
            Debug.LogError("[WorldMapManager] 2. Выберите класс персонажа");
            Debug.LogError("[WorldMapManager] 3. Персонаж автоматически зарегистрируется");
            Debug.LogError("[WorldMapManager] 4. Теперь можно переходить на карту мира");
        }
    }

    /// <summary>
    /// Настройка камеры
    /// </summary>
    private void SetupCamera()
    {
        if (worldMapCamera == null || playerCharacter == null)
            return;

        // Вычисляем смещение камеры
        float radAngle = cameraAngle * Mathf.Deg2Rad;
        cameraOffset = new Vector3(0, cameraHeight, -cameraDistance);

        // Устанавливаем начальную позицию
        worldMapCamera.transform.position = playerCharacter.transform.position + cameraOffset;
        worldMapCamera.transform.LookAt(playerCharacter.transform.position);
    }

    /// <summary>
    /// Обновление позиции камеры (следование за игроком)
    /// </summary>
    private void UpdateCameraPosition()
    {
        if (worldMapCamera == null || playerCharacter == null)
            return;

        // Целевая позиция камеры
        Vector3 targetPosition = playerCharacter.transform.position + cameraOffset;

        // Плавное следование
        worldMapCamera.transform.position = Vector3.Lerp(
            worldMapCamera.transform.position,
            targetPosition,
            Time.deltaTime * cameraFollowSpeed
        );

        // Смотрим на игрока
        Vector3 lookTarget = playerCharacter.transform.position + Vector3.up * 1f;
        worldMapCamera.transform.LookAt(lookTarget);
    }

    /// <summary>
    /// Создание 3D маркеров локаций на terrain
    /// </summary>
    private void SpawnLocationMarkers()
    {
        if (locationMarkerPrefab == null)
        {
            Debug.LogError("[WorldMapManager] ❌ LocationMarkerPrefab не назначен!");
            return;
        }

        foreach (LocationData location in allLocations)
        {
            if (location == null)
                continue;

            // Создаём маркер
            GameObject markerObj = Instantiate(locationMarkerPrefab, transform);

            // Конвертируем 2D позицию карты в 3D позицию на terrain
            Vector3 worldPosition = ConvertMapPositionToWorldPosition(location.mapPosition);
            markerObj.transform.position = worldPosition;

            // Настраиваем компонент маркера
            WorldMapLocationMarker marker = markerObj.GetComponent<WorldMapLocationMarker>();
            if (marker != null)
            {
                marker.Initialize(location);
                spawnedMarkers.Add(marker);
            }

            markerObj.name = $"LocationMarker_{location.locationName}";

            Debug.Log($"[WorldMapManager] Создан маркер: {location.locationName} at {worldPosition}");
        }
    }

    /// <summary>
    /// Конвертация 2D позиции карты (0-1) в 3D мировые координаты
    /// </summary>
    private Vector3 ConvertMapPositionToWorldPosition(Vector2 mapPosition)
    {
        // Получаем размер terrain
        Terrain terrain = Terrain.activeTerrain;
        if (terrain != null)
        {
            TerrainData terrainData = terrain.terrainData;
            Vector3 terrainSize = terrainData.size;
            Vector3 terrainPosition = terrain.transform.position;

            // Конвертируем нормализованные координаты (0-1) в мировые
            float worldX = terrainPosition.x + (mapPosition.x * terrainSize.x);
            float worldZ = terrainPosition.z + (mapPosition.y * terrainSize.z);

            // Получаем высоту terrain в этой точке
            float relativeX = mapPosition.x;
            float relativeZ = mapPosition.y;
            float height = terrain.SampleHeight(new Vector3(worldX, 0, worldZ));

            return new Vector3(worldX, height + 0.5f, worldZ); // +0.5f чтобы маркер был над поверхностью
        }
        else
        {
            // Если terrain нет, используем простые координаты
            Debug.LogWarning("[WorldMapManager] Terrain не найден, используются простые координаты");
            return new Vector3(mapPosition.x * 100f, 0, mapPosition.y * 100f);
        }
    }

    /// <summary>
    /// Проверка ближайшей локации к игроку
    /// </summary>
    private void CheckNearestLocation()
    {
        if (playerCharacter == null)
            return;

        WorldMapLocationMarker previousNearest = nearestMarker;
        nearestMarker = null;
        float nearestDistance = interactionRadius;

        Vector3 playerPos = playerCharacter.transform.position;

        // Находим ближайший маркер
        foreach (WorldMapLocationMarker marker in spawnedMarkers)
        {
            if (marker == null)
                continue;

            if (!marker.IsUnlocked())
                continue;

            float distance = Vector3.Distance(playerPos, marker.transform.position);

            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestMarker = marker;
            }
        }

        // Уведомляем маркеры о статусе "ближайший"
        if (previousNearest != nearestMarker)
        {
            if (previousNearest != null)
            {
                previousNearest.SetHighlighted(false);
                Debug.Log($"[WorldMapManager] 📍 Отошли от локации: {previousNearest.GetLocationData().locationName}");
            }

            if (nearestMarker != null)
            {
                nearestMarker.SetHighlighted(true);
                Debug.Log($"[WorldMapManager] 📍 Приблизились к локации: {nearestMarker.GetLocationData().locationName} (дистанция: {nearestDistance:F2})");
            }
        }
    }

    /// <summary>
    /// Получить ближайший маркер локации
    /// </summary>
    public WorldMapLocationMarker GetNearestMarker()
    {
        return nearestMarker;
    }

    /// <summary>
    /// Переход в локацию
    /// </summary>
    public void TravelToLocation(LocationData location)
    {
        if (location == null)
            return;

        Debug.Log($"[WorldMapManager] Переход в локацию: {location.locationName}");

        // ВАЖНО: Сохраняем текущую позицию персонажа на карте мира
        SavePlayerPosition();

        // Сохраняем прогресс
        if (GameProgressManager.Instance != null)
        {
            GameProgressManager.Instance.SetTargetLocation(location.sceneName);
            GameProgressManager.Instance.MarkLocationAsVisited(location.sceneName);
        }

        // Загружаем сцену
        if (SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.Instance.LoadScene(location.sceneName);
        }
        else
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(location.sceneName);
        }
    }

    /// <summary>
    /// Разблокировать локацию
    /// </summary>
    public void UnlockLocation(string sceneName)
    {
        if (GameProgressManager.Instance != null)
        {
            GameProgressManager.Instance.UnlockLocation(sceneName);
        }

        // Обновляем маркеры
        foreach (WorldMapLocationMarker marker in spawnedMarkers)
        {
            marker.UpdateLockedStatus();
        }

        Debug.Log($"[WorldMapManager] ✅ Локация '{sceneName}' разблокирована");
    }

    /// <summary>
    /// Возврат в последнюю локацию
    /// </summary>
    public void ReturnToLastLocation()
    {
        if (GameProgressManager.Instance != null)
        {
            string lastLocation = GameProgressManager.Instance.GetLastLocation();

            if (!string.IsNullOrEmpty(lastLocation))
            {
                Debug.Log($"[WorldMapManager] Возврат в: {lastLocation}");

                if (SceneTransitionManager.Instance != null)
                {
                    SceneTransitionManager.Instance.LoadScene(lastLocation);
                }
                else
                {
                    UnityEngine.SceneManagement.SceneManager.LoadScene(lastLocation);
                }
                return;
            }
        }

        // Fallback
        Debug.LogWarning("[WorldMapManager] Возврат в BattleScene (по умолчанию)");
        UnityEngine.SceneManagement.SceneManager.LoadScene("BattleScene");
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    // Gizmos для отладки
    void OnDrawGizmos()
    {
        if (playerCharacter != null)
        {
            // Радиус взаимодействия
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(playerCharacter.transform.position, interactionRadius);
        }

        // Маркеры локаций
        foreach (WorldMapLocationMarker marker in spawnedMarkers)
        {
            if (marker != null)
            {
                Gizmos.color = marker.IsUnlocked() ? Color.green : Color.red;
                Gizmos.DrawWireSphere(marker.transform.position, 1f);
            }
        }
    }

    /// <summary>
    /// Сохранить позицию персонажа перед переходом в локацию
    /// ВАЖНО: Сохраняем только X и Z координаты, Y будет определена по terrain при спавне
    /// </summary>
    private void SavePlayerPosition()
    {
        if (playerCharacter == null)
        {
            Debug.LogWarning("[WorldMapManager] ⚠️ playerCharacter == null, позиция не сохранена");
            return;
        }

        Vector3 position = playerCharacter.transform.position;

        // Сохраняем только X и Z (Y будет определена по terrain)
        PlayerPrefs.SetFloat("WorldMap_PlayerX", position.x);
        PlayerPrefs.SetFloat("WorldMap_PlayerZ", position.z);
        PlayerPrefs.Save();

        Debug.Log($"[WorldMapManager] 💾 Позиция персонажа сохранена (X, Z): ({position.x}, {position.z})");
    }
}
