using UnityEngine;

/// <summary>
/// Настройка камеры для превью персонажа в CharacterSelection
/// </summary>
public class CharacterPreviewCamera : MonoBehaviour
{
    [Header("Camera Settings")]
    [Tooltip("Высота камеры относительно персонажа (Y offset)")]
    [Range(-2f, 3f)]
    public float cameraHeight = 1.0f;

    [Tooltip("Расстояние камеры от персонажа (Z offset)")]
    [Range(1f, 10f)]
    public float cameraDistance = 4.0f;

    [Tooltip("Точка, на которую смотрит камера (высота)")]
    [Range(0f, 3f)]
    public float lookAtHeight = 1.2f;

    [Header("References")]
    [Tooltip("Трансформ персонажа, за которым следить (если null - находит автоматически)")]
    public Transform targetCharacter;

    [Tooltip("Автоматически применять настройки при старте")]
    public bool applyOnStart = true;

    private Camera cam;
    private Vector3 initialPosition;
    private Quaternion initialRotation;

    void Start()
    {
        cam = GetComponent<Camera>();

        if (cam == null)
        {
            Debug.LogError("[CharacterPreviewCamera] Camera компонент не найден!");
            return;
        }

        // Сохраняем начальную позицию
        initialPosition = transform.position;
        initialRotation = transform.rotation;

        if (applyOnStart)
        {
            ApplyCameraSettings();
        }
    }

    /// <summary>
    /// Применить настройки камеры
    /// </summary>
    public void ApplyCameraSettings()
    {
        if (targetCharacter == null)
        {
            // Пытаемся найти активный персонаж
            targetCharacter = FindActiveCharacter();
        }

        if (targetCharacter != null)
        {
            PositionCameraRelativeToCharacter();
        }
        else
        {
            // Если персонаж не найден, используем абсолютное позиционирование
            PositionCameraAbsolute();
        }
    }

    /// <summary>
    /// Позиционирование камеры относительно персонажа
    /// </summary>
    private void PositionCameraRelativeToCharacter()
    {
        Vector3 characterPosition = targetCharacter.position;

        // Позиция камеры: сзади и выше персонажа
        Vector3 cameraPosition = characterPosition + new Vector3(0, cameraHeight, -cameraDistance);
        transform.position = cameraPosition;

        // Направляем камеру на точку выше центра персонажа
        Vector3 lookAtPoint = characterPosition + Vector3.up * lookAtHeight;
        transform.LookAt(lookAtPoint);

        Debug.Log($"[CharacterPreviewCamera] Камера позиционирована относительно {targetCharacter.name}");
        Debug.Log($"  Position: {transform.position}, LookAt: {lookAtPoint}");
    }

    /// <summary>
    /// Абсолютное позиционирование камеры (без привязки к персонажу)
    /// </summary>
    private void PositionCameraAbsolute()
    {
        // Используем начальную позицию + настройки высоты
        Vector3 newPosition = initialPosition;
        newPosition.y = cameraHeight;
        transform.position = newPosition;

        // Направляем камеру на точку перед ней
        Vector3 lookAtPoint = transform.position + transform.forward * cameraDistance;
        lookAtPoint.y = lookAtHeight;
        transform.LookAt(lookAtPoint);

        Debug.Log($"[CharacterPreviewCamera] Камера позиционирована абсолютно: {transform.position}");
    }

    /// <summary>
    /// Найти активный персонаж в сцене
    /// </summary>
    private Transform FindActiveCharacter()
    {
        // Ищем GameObject с именами классов персонажей
        string[] characterNames = { "WarriorPlayer", "MagePlayer", "ArcherPlayer", "RoguePlayer", "PaladinPlayer" };

        foreach (string name in characterNames)
        {
            GameObject characterObj = GameObject.Find(name);
            if (characterObj != null && characterObj.activeInHierarchy)
            {
                Debug.Log($"[CharacterPreviewCamera] Найден активный персонаж: {name}");
                return characterObj.transform;
            }
        }

        Debug.LogWarning("[CharacterPreviewCamera] Активный персонаж не найден");
        return null;
    }

    /// <summary>
    /// Сбросить камеру к начальной позиции
    /// </summary>
    public void ResetCamera()
    {
        transform.position = initialPosition;
        transform.rotation = initialRotation;
    }

    // Для тестирования в Editor
    void OnValidate()
    {
        if (Application.isPlaying && applyOnStart)
        {
            ApplyCameraSettings();
        }
    }

#if UNITY_EDITOR
    // Отображение в Scene view
    void OnDrawGizmos()
    {
        if (targetCharacter != null)
        {
            Gizmos.color = Color.yellow;
            Vector3 lookAtPoint = targetCharacter.position + Vector3.up * lookAtHeight;
            Gizmos.DrawWireSphere(lookAtPoint, 0.1f);
            Gizmos.DrawLine(transform.position, lookAtPoint);
        }
    }
#endif
}
