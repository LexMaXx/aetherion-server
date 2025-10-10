using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// Расширение ServerAPI для работы со скиллами
/// Сохраняет/загружает экипированные скиллы в MongoDB
/// </summary>
public partial class ServerAPI
{
    /// <summary>
    /// Сохранить экипированные скиллы персонажа
    /// </summary>
    public void SaveCharacterSkills(string characterClass, List<int> equippedSkillIds, Action<bool> onComplete = null)
    {
        if (useLocalStorage)
        {
            // Сохраняем локально
            SaveSkillsLocal(characterClass, equippedSkillIds);
            onComplete?.Invoke(true);
            Debug.Log($"[ServerAPI] ✅ Скиллы сохранены локально: [{string.Join(", ", equippedSkillIds)}]");
        }
        else
        {
            // TODO: Отправить на сервер MongoDB
            // POST /api/characters/{class}/skills
            // Body: { "equippedSkills": [101, 102, 103] }
            Debug.LogWarning("[ServerAPI] Сохранение скиллов на сервер не реализовано!");
            onComplete?.Invoke(false);
        }
    }

    /// <summary>
    /// Загрузить экипированные скиллы персонажа
    /// </summary>
    public void LoadCharacterSkills(string characterClass, Action<List<int>, bool> onComplete)
    {
        if (useLocalStorage)
        {
            // Загружаем локально
            List<int> skills = LoadSkillsLocal(characterClass);
            bool success = skills != null && skills.Count > 0;
            onComplete?.Invoke(skills, success);
            Debug.Log($"[ServerAPI] Скиллы загружены локально: {(success ? $"[{string.Join(", ", skills)}]" : "Нет сохранённых")}");
        }
        else
        {
            // TODO: Загрузить с сервера MongoDB
            // GET /api/characters/{class}/skills
            Debug.LogWarning("[ServerAPI] Загрузка скиллов с сервера не реализовано!");
            onComplete?.Invoke(new List<int>(), false);
        }
    }

    // ===== ЛОКАЛЬНОЕ ХРАНИЛИЩЕ (PlayerPrefs) =====

    /// <summary>
    /// Сохранить скиллы локально
    /// </summary>
    private void SaveSkillsLocal(string characterClass, List<int> skillIds)
    {
        string key = $"Character_{characterClass}_Skills";

        // Сохраняем количество скиллов
        PlayerPrefs.SetInt($"{key}_Count", skillIds.Count);

        // Сохраняем каждый skill ID
        for (int i = 0; i < skillIds.Count; i++)
        {
            PlayerPrefs.SetInt($"{key}_{i}", skillIds[i]);
        }

        PlayerPrefs.Save();
    }

    /// <summary>
    /// Загрузить скиллы локально
    /// </summary>
    private List<int> LoadSkillsLocal(string characterClass)
    {
        string key = $"Character_{characterClass}_Skills";
        List<int> skills = new List<int>();

        // Проверяем есть ли сохранённые скиллы
        if (!PlayerPrefs.HasKey($"{key}_Count"))
        {
            Debug.Log($"[ServerAPI] Нет сохранённых скиллов для {characterClass}");
            return null;
        }

        int count = PlayerPrefs.GetInt($"{key}_Count", 0);

        // Загружаем каждый skill ID
        for (int i = 0; i < count; i++)
        {
            int skillId = PlayerPrefs.GetInt($"{key}_{i}", 0);
            if (skillId > 0)
            {
                skills.Add(skillId);
            }
        }

        return skills;
    }

    /// <summary>
    /// Очистить сохранённые скиллы персонажа
    /// </summary>
    public void ClearCharacterSkills(string characterClass)
    {
        string key = $"Character_{characterClass}_Skills";

        if (PlayerPrefs.HasKey($"{key}_Count"))
        {
            int count = PlayerPrefs.GetInt($"{key}_Count", 0);

            // Удаляем все ключи
            PlayerPrefs.DeleteKey($"{key}_Count");
            for (int i = 0; i < count; i++)
            {
                PlayerPrefs.DeleteKey($"{key}_{i}");
            }

            PlayerPrefs.Save();
            Debug.Log($"[ServerAPI] Скиллы персонажа {characterClass} очищены");
        }
    }
}

/// <summary>
/// Данные скиллов персонажа для сериализации (JSON для MongoDB)
/// </summary>
[System.Serializable]
public class CharacterSkillsData
{
    public string characterClass;
    public List<int> equippedSkills = new List<int>(); // ID экипированных скиллов
    public long timestamp; // Время последнего обновления
}
