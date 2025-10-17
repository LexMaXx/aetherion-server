using UnityEngine;

/// <summary>
/// Система смены скинов (mesh swapping) для трансформаций
/// Заменяет визуальную модель персонажа без создания child GameObjects
/// </summary>
public class MeshSwapper : MonoBehaviour
{
    [Header("Оригинальная модель игрока")]
    private SkinnedMeshRenderer playerRenderer;
    private Mesh originalMesh;
    private Material[] originalMaterials;
    private Transform[] originalBones;
    private Transform originalRootBone;

    [Header("Состояние трансформации")]
    private bool isTransformed = false;
    private GameObject transformationPrefab; // Сохраняем префаб для восстановления

    void Awake()
    {
        // Находим SkinnedMeshRenderer игрока
        playerRenderer = GetComponentInChildren<SkinnedMeshRenderer>();
        if (playerRenderer == null)
        {
            Debug.LogError("[MeshSwapper] ❌ SkinnedMeshRenderer не найден!");
            return;
        }

        // Сохраняем оригинальные данные
        SaveOriginalMesh();
        Debug.Log($"[MeshSwapper] ✅ Инициализирован. Оригинальный mesh сохранён");
    }

    /// <summary>
    /// Сохранить оригинальный mesh и materials игрока
    /// </summary>
    private void SaveOriginalMesh()
    {
        if (playerRenderer == null) return;

        originalMesh = playerRenderer.sharedMesh;
        originalMaterials = playerRenderer.sharedMaterials;
        originalBones = playerRenderer.bones;
        originalRootBone = playerRenderer.rootBone;

        Debug.Log($"[MeshSwapper] 💾 Сохранено: mesh={originalMesh?.name}, materials={originalMaterials?.Length}, bones={originalBones?.Length}");
    }

    /// <summary>
    /// Трансформироваться в другую модель (mesh swap)
    /// </summary>
    /// <param name="transformationPrefab">Префаб модели для трансформации</param>
    /// <returns>True если успешно</returns>
    public bool TransformTo(GameObject transformationPrefab)
    {
        if (playerRenderer == null)
        {
            Debug.LogError("[MeshSwapper] ❌ playerRenderer == null!");
            return false;
        }

        if (transformationPrefab == null)
        {
            Debug.LogError("[MeshSwapper] ❌ transformationPrefab == null!");
            return false;
        }

        // Находим SkinnedMeshRenderer в префабе трансформации
        SkinnedMeshRenderer transformRenderer = transformationPrefab.GetComponentInChildren<SkinnedMeshRenderer>();
        if (transformRenderer == null)
        {
            Debug.LogError("[MeshSwapper] ❌ SkinnedMeshRenderer не найден в префабе трансформации!");
            return false;
        }

        Debug.Log($"[MeshSwapper] 🔄 Начинаем трансформацию...");
        Debug.Log($"[MeshSwapper] 📦 Префаб трансформации: {transformationPrefab.name}");
        Debug.Log($"[MeshSwapper] 📦 Mesh трансформации: {transformRenderer.sharedMesh?.name}");
        Debug.Log($"[MeshSwapper] 📦 Materials трансформации: {transformRenderer.sharedMaterials?.Length}");
        Debug.Log($"[MeshSwapper] 📦 Bones трансформации: {transformRenderer.bones?.Length}");

        // КРИТИЧЕСКОЕ: Меняем mesh и materials на модель трансформации
        playerRenderer.sharedMesh = transformRenderer.sharedMesh;
        playerRenderer.sharedMaterials = transformRenderer.sharedMaterials;

        // Пытаемся адаптировать bones если они есть
        if (transformRenderer.bones != null && transformRenderer.bones.Length > 0)
        {
            // ВАЖНО: Bones должны совпадать по структуре!
            // Если они не совпадают, используем оригинальные bones паладина
            if (transformRenderer.bones.Length == originalBones.Length)
            {
                playerRenderer.bones = transformRenderer.bones;
                Debug.Log($"[MeshSwapper] ✅ Bones трансформации применены ({transformRenderer.bones.Length} костей)");
            }
            else
            {
                Debug.LogWarning($"[MeshSwapper] ⚠️ Bones не совпадают! Transform={transformRenderer.bones.Length}, Original={originalBones.Length}");
                Debug.LogWarning($"[MeshSwapper] ⚠️ Используем оригинальные bones паладина");
                // Оставляем originalBones - они уже установлены
            }
        }

        // Сохраняем префаб для восстановления
        this.transformationPrefab = transformationPrefab;
        isTransformed = true;

        Debug.Log($"[MeshSwapper] ✅ Трансформация завершена! Новый mesh: {playerRenderer.sharedMesh?.name}");
        return true;
    }

    /// <summary>
    /// Вернуться к оригинальной модели игрока
    /// </summary>
    public void RevertToOriginal()
    {
        if (playerRenderer == null)
        {
            Debug.LogError("[MeshSwapper] ❌ playerRenderer == null!");
            return;
        }

        if (!isTransformed)
        {
            Debug.LogWarning("[MeshSwapper] ⚠️ Игрок не трансформирован, нечего возвращать");
            return;
        }

        Debug.Log($"[MeshSwapper] 🔄 Возврат к оригинальной модели...");

        // Восстанавливаем оригинальные данные
        playerRenderer.sharedMesh = originalMesh;
        playerRenderer.sharedMaterials = originalMaterials;
        playerRenderer.bones = originalBones;
        playerRenderer.rootBone = originalRootBone;

        transformationPrefab = null;
        isTransformed = false;

        Debug.Log($"[MeshSwapper] ✅ Возврат завершён! Mesh: {playerRenderer.sharedMesh?.name}");
    }

    /// <summary>
    /// Проверить, трансформирован ли игрок
    /// </summary>
    public bool IsTransformed()
    {
        return isTransformed;
    }

    /// <summary>
    /// Получить текущий SkinnedMeshRenderer
    /// </summary>
    public SkinnedMeshRenderer GetPlayerRenderer()
    {
        return playerRenderer;
    }

    /// <summary>
    /// Получить оригинальный mesh
    /// </summary>
    public Mesh GetOriginalMesh()
    {
        return originalMesh;
    }
}
