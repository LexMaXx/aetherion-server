using UnityEngine;

/// <summary>
/// Компонент для скелета-миньона, делает его "атакующим" для мультиплеера
/// Позволяет отслеживать владельца (некроманта) для правильного начисления урона
/// </summary>
public class SkeletonEntity : MonoBehaviour
{
    [Header("Владелец")]
    [SerializeField] private string ownerSocketId = "";  // Socket ID некроманта
    [SerializeField] private string ownerName = "";      // Имя некроманта

    /// <summary>
    /// Установить владельца скелета
    /// </summary>
    public void SetOwner(string socketId, string name)
    {
        ownerSocketId = socketId;
        ownerName = name;
        Debug.Log($"[SkeletonEntity] 💀 Владелец установлен: {ownerName} (ID: {ownerSocketId})");
    }

    /// <summary>
    /// Получить Socket ID владельца
    /// </summary>
    public string GetOwnerSocketId()
    {
        return ownerSocketId;
    }

    /// <summary>
    /// Получить имя владельца
    /// </summary>
    public string GetOwnerName()
    {
        return ownerName;
    }

    /// <summary>
    /// Проверить есть ли владелец
    /// </summary>
    public bool HasOwner()
    {
        return !string.IsNullOrEmpty(ownerSocketId);
    }
}
