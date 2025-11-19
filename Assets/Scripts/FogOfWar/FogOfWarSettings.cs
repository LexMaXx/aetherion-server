using UnityEngine;

/// <summary>
/// Глобальные настройки Fog of War для всех персонажей
/// Создать: Assets -> Create -> Fog of War Settings
/// </summary>
[CreateAssetMenu(fileName = "FogOfWarSettings", menuName = "Aetherion/Fog of War Settings", order = 1)]
public class FogOfWarSettings : ScriptableObject
{
    [Header("Fog Visual - Black Mask")]
    [Tooltip("Использовать черную маску для тумана войны")]
    public bool useBlackMask = true;

    [Header("Fog Alpha")]
    [Range(0f, 1f)]
    [Tooltip("Прозрачность тумана (0 = прозрачный, 1 = непрозрачный)")]
    public float fogAlpha = 0.63f;

    [Header("Darken Buildings")]
    [Tooltip("Затемнять здания в тумане войны")]
    public bool darkenBuildings = true;

    [Range(0f, 1f)]
    [Tooltip("Насколько затемнять здания (0 = нормально, 1 = полностью темно)")]
    public float darkenedBrightness = 0.42f;

    [Header("Wall Detection")]
    [Tooltip("Проверять линию видимости (не видеть врагов за стенами)")]
    public bool checkLineOfSight = true;

    [Tooltip("Слои для определения стен")]
    public LayerMask wallLayers = -1; // Everything

    [Tooltip("Минимальный размер препятствия для блокировки видимости")]
    public float minObstacleSize = 1f;

    [Header("Height Settings")]
    [Tooltip("Игнорировать высоту (Y) при проверке расстояния - враги на любой высоте видны")]
    public bool ignoreHeight = true;

    [Tooltip("Максимальная разница по высоте если ignoreHeight = false")]
    public float maxHeightDifference = 100f;

    [Header("Update Settings")]
    [Tooltip("Интервал обновления в секундах (0.1 = 10 раз в секунду)")]
    [Range(0.05f, 1f)]
    public float updateInterval = 0.1f;
}
