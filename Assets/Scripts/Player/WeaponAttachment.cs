using UnityEngine;

/// <summary>
/// Присоединяет оружие к руке персонажа
/// </summary>
public class WeaponAttachment : MonoBehaviour
{
    [Header("Weapon Settings")]
    [Tooltip("Префаб оружия для присоединения")]
    public GameObject weaponPrefab;

    [Tooltip("Название кости правой руки (обычно 'RightHand' или 'mixamorig:RightHand')")]
    public string rightHandBoneName = "mixamorig:RightHand";

    [Tooltip("Локальная позиция оружия относительно руки")]
    public Vector3 weaponLocalPosition = Vector3.zero;

    [Tooltip("Локальный поворот оружия")]
    public Vector3 weaponLocalRotation = Vector3.zero;

    [Tooltip("Локальный масштаб оружия")]
    public Vector3 weaponLocalScale = Vector3.one;

    private GameObject attachedWeapon;
    private Transform rightHandBone;

    void Start()
    {
        AttachWeapon();
    }

    /// <summary>
    /// Присоединить оружие к правой руке
    /// </summary>
    public void AttachWeapon()
    {
        // Удаляем старое оружие если есть
        if (attachedWeapon != null)
        {
            Destroy(attachedWeapon);
        }

        // Находим кость правой руки
        rightHandBone = FindBoneRecursive(transform, rightHandBoneName);

        if (rightHandBone == null)
        {
            Debug.LogError($"[WeaponAttachment] Не найдена кость '{rightHandBoneName}' у {gameObject.name}");
            return;
        }

        Debug.Log($"[WeaponAttachment] Найдена кость: {rightHandBone.name}");

        // Создаем оружие если префаб назначен
        if (weaponPrefab != null)
        {
            attachedWeapon = Instantiate(weaponPrefab, rightHandBone);
            attachedWeapon.name = weaponPrefab.name;

            // Устанавливаем локальные трансформы
            attachedWeapon.transform.localPosition = weaponLocalPosition;
            attachedWeapon.transform.localRotation = Quaternion.Euler(weaponLocalRotation);
            attachedWeapon.transform.localScale = weaponLocalScale;

            Debug.Log($"[WeaponAttachment] Оружие '{weaponPrefab.name}' присоединено к {rightHandBone.name}");
        }
        else
        {
            // Создаем простое примитивное оружие (меч/посох)
            CreatePrimitiveWeapon();
        }
    }

    /// <summary>
    /// Создать примитивное оружие из кубов (для тестирования)
    /// </summary>
    private void CreatePrimitiveWeapon()
    {
        // Создаем простой меч из куба
        attachedWeapon = GameObject.CreatePrimitive(PrimitiveType.Cube);
        attachedWeapon.name = "SimpleSword";
        attachedWeapon.transform.SetParent(rightHandBone);

        // Настройка размера и позиции
        attachedWeapon.transform.localPosition = new Vector3(0, 0.05f, 0);
        attachedWeapon.transform.localRotation = Quaternion.Euler(0, 90, 0);
        attachedWeapon.transform.localScale = new Vector3(0.05f, 0.5f, 0.05f);

        // Удаляем коллайдер (для отображения)
        Destroy(attachedWeapon.GetComponent<Collider>());

        // Устанавливаем цвет
        Renderer renderer = attachedWeapon.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = Color.gray;
        }

        Debug.Log($"[WeaponAttachment] Создано примитивное оружие для {gameObject.name}");
    }

    /// <summary>
    /// Рекурсивный поиск кости по имени
    /// </summary>
    private Transform FindBoneRecursive(Transform parent, string boneName)
    {
        if (parent.name == boneName)
            return parent;

        foreach (Transform child in parent)
        {
            Transform found = FindBoneRecursive(child, boneName);
            if (found != null)
                return found;
        }

        return null;
    }

    /// <summary>
    /// Отсоединить оружие
    /// </summary>
    public void DetachWeapon()
    {
        if (attachedWeapon != null)
        {
            Destroy(attachedWeapon);
            attachedWeapon = null;
        }
    }

    void OnDestroy()
    {
        DetachWeapon();
    }
}
