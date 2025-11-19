using System;
using UnityEngine;

/// <summary>
/// Классы персонажей
/// </summary>
public enum CharacterClass
{
    Warrior,  // Воин
    Mage,     // Маг
    Archer,   // Лучник
    Rogue,    // Разбойник
    Paladin   // Паладин
}

/// <summary>
/// Запрос на создание персонажа
/// Используем поле с именем @class для обхода зарезервированного слова C#
/// </summary>
[Serializable]
public class CreateCharacterRequest
{
    public string name;          // Имя персонажа

    // Используем SerializeField с именем "class" - JsonUtility правильно сериализует это
    [SerializeField]
    #pragma warning disable 0649 // Поле не присваивается напрямую
    private string @class;
    #pragma warning restore 0649

    // Свойство для установки класса (без сериализации, только для кода)
    public string characterClass
    {
        get { return @class; }
        set { @class = value; }
    }

    public int level;
    public int experience;
    public int availableStatPoints;
    public CharacterStatsData stats;
}

/// <summary>
/// Запрос на выбор существующего персонажа
/// </summary>
[Serializable]
public class SelectCharacterRequest
{
    public string characterId; // ID существующего персонажа
}

/// <summary>
/// Ответ при выборе/создании персонажа
/// </summary>
[Serializable]
public class CharacterResponse
{
    public bool success;
    public string message;
    public CharacterInfo character;
}

/// <summary>
/// Ответ со списком персонажей
/// </summary>
[Serializable]
public class CharactersListResponse
{
    public bool success;
    public CharacterInfo[] characters;
}

/// <summary>
/// Информация о персонаже
/// </summary>
[Serializable]
public class CharacterInfo
{
    // MongoDB возвращает "_id", а не "id"
    public string _id;

    // Для удобства использования в коде
    public string id
    {
        get { return _id; }
        set { _id = value; }
    }

    // ВАЖНО: Сервер возвращает "characterClass" (не "class"!)
    // MongoDB модель использует поле characterClass
    public string characterClass;

    public int level;
    public int experience;
    public int gold;
    public CharacterStatsDTO stats; // DEPRECATED: старая структура
    public CharacterResource health;
    public CharacterResource mana;
    public CharacterPosition position;
    public CharacterEquipment equipment;
    public string createdAt;
    public string lastPlayed;
}

/// <summary>
/// Характеристики персонажа (DTO для серверного API - УСТАРЕЛО)
/// DEPRECATED: Используйте CharacterStatsData из новой SPECIAL системы
/// </summary>
[Serializable]
public class CharacterStatsDTO
{
    public int strength;     // Сила
    public int agility;      // Ловкость
    public int intelligence; // Интеллект
    public int vitality;     // Живучесть
    public int luck;         // Удача
}

/// <summary>
/// Ресурс персонажа (HP/Mana)
/// </summary>
[Serializable]
public class CharacterResource
{
    public int current;
    public int max;
}

/// <summary>
/// Позиция персонажа в мире
/// </summary>
[Serializable]
public class CharacterPosition
{
    public float x;
    public float y;
    public float z;
    public string scene;
}

/// <summary>
/// Экипировка персонажа
/// </summary>
[Serializable]
public class CharacterEquipment
{
    public string weapon;
    public string armor;
    public string helmet;
    public string boots;
    public string accessory;
}

[Serializable]
public class CharacterProgressRequest
{
    public string characterId;
    public CharacterStatsData stats;
    public LevelingData leveling;
}

[Serializable]
public class CharacterProgressResponse
{
    public bool success;
    public string message;
    public CharacterStatsData stats;
    public LevelingData leveling;
}
