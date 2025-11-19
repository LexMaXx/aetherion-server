using UnityEngine;

/// <summary>
/// Данные о локации на мировой карте
/// ScriptableObject для хранения информации о городах/локациях
/// </summary>
[CreateAssetMenu(fileName = "New Location", menuName = "Aetherion/World Map/Location Data")]
public class LocationData : ScriptableObject
{
    [Header("Location Info")]
    [Tooltip("Название локации (отображается на карте)")]
    public string locationName = "Новая Локация";

    [Tooltip("Описание локации")]
    [TextArea(3, 5)]
    public string description = "Описание локации...";

    [Tooltip("Название сцены для загрузки")]
    public string sceneName = "BattleScene";

    [Header("World Map Settings")]
    [Tooltip("Позиция на мировой карте (2D координаты)")]
    public Vector2 mapPosition = Vector2.zero;

    [Tooltip("Иконка локации на карте")]
    public Sprite locationIcon;

    [Tooltip("Цвет иконки на карте")]
    public Color iconColor = Color.white;

    [Header("Availability")]
    [Tooltip("Доступна с начала игры?")]
    public bool unlockedByDefault = false;

    [Tooltip("ID требуемого квеста для разблокировки (опционально)")]
    public string requiredQuestId = "";

    [Tooltip("Уровень игрока для доступа")]
    public int requiredLevel = 1;

    [Header("Difficulty")]
    [Tooltip("Уровень сложности локации")]
    public int difficultyLevel = 1;

    [Tooltip("Рекомендуемый уровень игрока")]
    public int recommendedLevel = 1;

    [Header("Additional Info")]
    [Tooltip("Тип локации")]
    public LocationType locationType = LocationType.City;

    [Tooltip("Быстрое перемещение доступно?")]
    public bool fastTravelEnabled = true;

    // Runtime данные (не сериализуются)
    [System.NonSerialized]
    public bool isUnlocked = false;

    [System.NonSerialized]
    public bool isVisited = false;
}

/// <summary>
/// Типы локаций
/// </summary>
public enum LocationType
{
    City,           // Город
    Village,        // Деревня
    Dungeon,        // Подземелье
    Wilderness,     // Дикая местность
    Ruins,          // Руины
    Camp,           // Лагерь
    Special         // Особая локация
}
