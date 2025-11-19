using UnityEngine;
using UnityEditor;

/// <summary>
/// Инструмент для сохранения текущих трансформов оружия во время Play mode
/// </summary>
[InitializeOnLoad]
public class SaveWeaponTransformsInPlayMode
{
    static SaveWeaponTransformsInPlayMode()
    {
        // Добавляем пункт меню только когда игра запущена
        EditorApplication.update += UpdateMenu;
    }

    private static void UpdateMenu()
    {
        // Можно использовать в любое время
    }

    [MenuItem("Tools/Save Current Weapon Transforms", true)]
    private static bool ValidateSaveTransforms()
    {
        // Можно использовать только в Play mode
        return Application.isPlaying;
    }

    [MenuItem("Tools/Save Current Weapon Transforms")]
    public static void SaveCurrentWeaponTransforms()
    {
        if (!Application.isPlaying)
        {
            EditorUtility.DisplayDialog("Ошибка",
                "Этот инструмент работает только в Play mode!\n\n" +
                "1. Запустите игру (Arena или CharacterSelection)\n" +
                "2. Настройте оружие в Inspector\n" +
                "3. Запустите этот инструмент снова",
                "OK");
            return;
        }

        Debug.Log("\n=== СОХРАНЕНИЕ ТРАНСФОРМОВ ОРУЖИЯ ===\n");

        // Ищем все ClassWeaponManager в сцене
        ClassWeaponManager[] allManagers = Object.FindObjectsOfType<ClassWeaponManager>();

        if (allManagers.Length == 0)
        {
            EditorUtility.DisplayDialog("Ошибка",
                "Не найдено ни одного ClassWeaponManager в сцене!",
                "OK");
            return;
        }

        Debug.Log($"Найдено менеджеров оружия: {allManagers.Length}");

        // Загружаем WeaponDatabase
        WeaponDatabase db = Resources.Load<WeaponDatabase>("WeaponDatabase");
        if (db == null)
        {
            EditorUtility.DisplayDialog("Ошибка",
                "WeaponDatabase не найдена!",
                "OK");
            return;
        }

        int savedCount = 0;

        foreach (ClassWeaponManager manager in allManagers)
        {
            CharacterClass characterClass = manager.GetCharacterClass();

            Debug.Log($"\n--- Обработка: {manager.gameObject.name} ({characterClass}) ---");

            // Получаем приватные поля через рефлексию
            var type = typeof(ClassWeaponManager);
            var attachedRightField = type.GetField("attachedRightWeapon", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var attachedLeftField = type.GetField("attachedLeftWeapon", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var attachedBackField = type.GetField("attachedBackWeapon", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            GameObject rightWeapon = attachedRightField?.GetValue(manager) as GameObject;
            GameObject leftWeapon = attachedLeftField?.GetValue(manager) as GameObject;
            GameObject backWeapon = attachedBackField?.GetValue(manager) as GameObject;

            // Сохраняем правую руку
            if (rightWeapon != null)
            {
                WeaponDatabase.WeaponEntry entry = GetEntryForWeapon(db, characterClass, "right");
                if (entry != null)
                {
                    entry.defaultPosition = rightWeapon.transform.localPosition;
                    entry.defaultRotation = rightWeapon.transform.localEulerAngles;
                    entry.defaultScale = rightWeapon.transform.localScale;
                    Debug.Log($"✓ Сохранено {entry.weaponName}: Pos={entry.defaultPosition}, Rot={entry.defaultRotation}, Scale={entry.defaultScale}");
                    savedCount++;
                }
            }

            // Сохраняем левую руку
            if (leftWeapon != null)
            {
                WeaponDatabase.WeaponEntry entry = GetEntryForWeapon(db, characterClass, "left");
                if (entry != null)
                {
                    entry.defaultPosition = leftWeapon.transform.localPosition;
                    entry.defaultRotation = leftWeapon.transform.localEulerAngles;
                    entry.defaultScale = leftWeapon.transform.localScale;
                    Debug.Log($"✓ Сохранено {entry.weaponName}: Pos={entry.defaultPosition}, Rot={entry.defaultRotation}, Scale={entry.defaultScale}");
                    savedCount++;
                }
            }

            // Сохраняем спину
            if (backWeapon != null)
            {
                WeaponDatabase.WeaponEntry entry = GetEntryForWeapon(db, characterClass, "back");
                if (entry != null)
                {
                    entry.defaultPosition = backWeapon.transform.localPosition;
                    entry.defaultRotation = backWeapon.transform.localEulerAngles;
                    entry.defaultScale = backWeapon.transform.localScale;
                    Debug.Log($"✓ Сохранено {entry.weaponName}: Pos={entry.defaultPosition}, Rot={entry.defaultRotation}, Scale={entry.defaultScale}");
                    savedCount++;
                }
            }
        }

        // Сохраняем изменения
        EditorUtility.SetDirty(db);

        Debug.Log($"\n✓✓✓ Сохранено трансформов: {savedCount}");
        EditorUtility.DisplayDialog("Успех!",
            $"Сохранено трансформов оружия: {savedCount}\n\n" +
            "WeaponDatabase обновлена!\n" +
            "Изменения будут применены после выхода из Play mode.",
            "OK");
    }

    private static WeaponDatabase.WeaponEntry GetEntryForWeapon(WeaponDatabase db, CharacterClass characterClass, string slot)
    {
        switch (characterClass)
        {
            case CharacterClass.Warrior:
                if (slot == "right") return db.warriorSword;
                if (slot == "left") return db.warriorShield;
                break;
            case CharacterClass.Mage:
                if (slot == "right") return db.mageStaff;
                break;
            case CharacterClass.Archer:
                if (slot == "left") return db.archerBow;
                if (slot == "back") return db.archerQuiver;
                break;
            case CharacterClass.Rogue:
                if (slot == "right") return db.rogueDagger;
                break;
            case CharacterClass.Paladin:
                if (slot == "right") return db.paladinSword;
                break;
        }
        return null;
    }
}
