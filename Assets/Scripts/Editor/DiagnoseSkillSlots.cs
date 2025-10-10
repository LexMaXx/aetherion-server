using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Диагностика проблем с отображением скиллов
/// Запуск: Tools → Aetherion → Diagnose Skill Slots
/// </summary>
public class DiagnoseSkillSlots : Editor
{
    [MenuItem("Tools/Aetherion/Diagnose Skill Slots")]
    public static void Diagnose()
    {
        Debug.Log("=== [DiagnoseSkillSlots] Начинаю диагностику ===");

        // Находим SkillSelectionManager
        SkillSelectionManager manager = Object.FindObjectOfType<SkillSelectionManager>();

        if (manager == null)
        {
            Debug.LogError("[DiagnoseSkillSlots] ❌ SkillSelectionManager не найден в сцене!");
            return;
        }

        Debug.Log("[DiagnoseSkillSlots] ✅ SkillSelectionManager найден");

        // Получаем SerializedObject для доступа к приватным полям
        SerializedObject so = new SerializedObject(manager);

        // Проверяем librarySlots
        SerializedProperty librarySlotsProp = so.FindProperty("librarySlots");
        Debug.Log($"[DiagnoseSkillSlots] Library Slots: {librarySlotsProp.arraySize}");

        for (int i = 0; i < librarySlotsProp.arraySize; i++)
        {
            SerializedProperty slotProp = librarySlotsProp.GetArrayElementAtIndex(i);
            SkillSlotUI slot = slotProp.objectReferenceValue as SkillSlotUI;

            if (slot == null)
            {
                Debug.LogError($"[DiagnoseSkillSlots] ❌ Library Slot [{i}] = NULL!");
                continue;
            }

            Debug.Log($"[DiagnoseSkillSlots] Library Slot [{i}]: {slot.gameObject.name}");

            // Проверяем Image компонент
            DiagnoseSlot(slot, i, "Library");
        }

        // Проверяем equippedSlots
        SerializedProperty equippedSlotsProp = so.FindProperty("equippedSlots");
        Debug.Log($"[DiagnoseSkillSlots] Equipped Slots: {equippedSlotsProp.arraySize}");

        for (int i = 0; i < equippedSlotsProp.arraySize; i++)
        {
            SerializedProperty slotProp = equippedSlotsProp.GetArrayElementAtIndex(i);
            SkillSlotUI slot = slotProp.objectReferenceValue as SkillSlotUI;

            if (slot == null)
            {
                Debug.LogError($"[DiagnoseSkillSlots] ❌ Equipped Slot [{i}] = NULL!");
                continue;
            }

            Debug.Log($"[DiagnoseSkillSlots] Equipped Slot [{i}]: {slot.gameObject.name}");

            // Проверяем Image компонент
            DiagnoseSlot(slot, i, "Equipped");
        }

        Debug.Log("=== [DiagnoseSkillSlots] Диагностика завершена ===");
    }

    private static void DiagnoseSlot(SkillSlotUI slot, int index, string type)
    {
        SerializedObject slotSO = new SerializedObject(slot);

        // Проверяем iconImage
        SerializedProperty iconImageProp = slotSO.FindProperty("iconImage");

        if (iconImageProp.objectReferenceValue == null)
        {
            Debug.LogError($"[DiagnoseSkillSlots] ❌ {type} Slot [{index}]: iconImage = NULL! Нужно назначить Image компонент в Inspector.");
            return;
        }

        Image iconImage = iconImageProp.objectReferenceValue as Image;

        Debug.Log($"[DiagnoseSkillSlots] {type} Slot [{index}]:");
        Debug.Log($"  - iconImage: {iconImage.gameObject.name}");
        Debug.Log($"  - enabled: {iconImage.enabled}");
        Debug.Log($"  - sprite: {(iconImage.sprite != null ? iconImage.sprite.name : "NULL")}");
        Debug.Log($"  - color: {iconImage.color}");
        Debug.Log($"  - raycastTarget: {iconImage.raycastTarget}");
        Debug.Log($"  - preserveAspect: {iconImage.preserveAspect}");

        RectTransform iconRect = iconImage.GetComponent<RectTransform>();
        Debug.Log($"  - sizeDelta: {iconRect.sizeDelta}");
        Debug.Log($"  - anchorMin: {iconRect.anchorMin}");
        Debug.Log($"  - anchorMax: {iconRect.anchorMax}");

        // Проверяем активность объекта
        if (!iconImage.gameObject.activeInHierarchy)
        {
            Debug.LogWarning($"[DiagnoseSkillSlots] ⚠️ {type} Slot [{index}]: Icon GameObject неактивен!");
        }

        // Проверяем родителя
        if (iconImage.transform.parent != slot.transform)
        {
            Debug.LogWarning($"[DiagnoseSkillSlots] ⚠️ {type} Slot [{index}]: Icon НЕ дочерний объект слота!");
        }
    }
}
