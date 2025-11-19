using UnityEngine;

/// <summary>
/// Типы предметов в игре
/// </summary>
public enum ItemType
{
    Weapon,      // Оружие
    Armor,       // Броня
    Helmet,      // Шлем
    Accessory,   // Аксессуар (кольца, амулеты)
    Consumable,  // Расходуемые (зелья)
    Material,    // Материалы для крафта
    Quest        // Квестовые предметы
}

/// <summary>
/// Слот экипировки
/// </summary>
public enum EquipmentSlot
{
    Weapon,      // Оружие
    Armor,       // Броня
    Helmet,      // Шлем
    Accessory    // Аксессуар
}

/// <summary>
/// Данные предмета (ScriptableObject)
/// </summary>
[CreateAssetMenu(fileName = "New Item", menuName = "Aetherion/Inventory/Item")]
public class ItemData : ScriptableObject
{
    [Header("Unique ID")]
    [Tooltip("Уникальный ID предмета (НЕ МЕНЯТЬ ВРУЧНУЮ!)")]
    [SerializeField] private string itemId = "";

    [Header("Basic Info")]
    [Tooltip("Название предмета")]
    public string itemName = "New Item";

    /// <summary>
    /// Получить уникальный ID предмета
    /// </summary>
    public string ItemId
    {
        get
        {
            // Автоматически генерируем GUID если пустой
            if (string.IsNullOrEmpty(itemId))
            {
                itemId = System.Guid.NewGuid().ToString();
                #if UNITY_EDITOR
                UnityEditor.EditorUtility.SetDirty(this);
                #endif
            }
            return itemId;
        }
    }

    [Tooltip("Описание предмета")]
    [TextArea(3, 5)]
    public string description = "Item description";

    [Tooltip("Иконка предмета")]
    public Sprite icon;

    [Tooltip("Тип предмета")]
    public ItemType itemType = ItemType.Consumable;

    [Header("Stack Settings")]
    [Tooltip("Можно ли складывать предметы в стак?")]
    public bool isStackable = true;

    [Tooltip("Максимальное количество в стаке")]
    public int maxStackSize = 99;

    [Header("Equipment Settings")]
    [Tooltip("Можно ли экипировать?")]
    public bool isEquippable = false;

    [Tooltip("Слот экипировки (если isEquippable = true)")]
    public EquipmentSlot equipmentSlot = EquipmentSlot.Weapon;

    [Header("Stats")]
    [Tooltip("Добавочная атака")]
    public int attackBonus = 0;

    [Tooltip("Добавочная защита")]
    public int defenseBonus = 0;

    [Tooltip("Добавочное HP")]
    public int healthBonus = 0;

    [Tooltip("Добавочная мана")]
    public int manaBonus = 0;

    [Header("Value")]
    [Tooltip("Цена продажи")]
    public int sellPrice = 10;

    [Tooltip("Цена покупки")]
    public int buyPrice = 50;

    [Header("Consumable Settings")]
    [Tooltip("Восстанавливает HP (если consumable)")]
    public int healAmount = 0;

    [Tooltip("Восстанавливает ману (если consumable)")]
    public int manaRestoreAmount = 0;

    /// <summary>
    /// Получить полное описание предмета со статами
    /// </summary>
    public string GetFullDescription()
    {
        string fullDesc = description;

        if (isEquippable)
        {
            fullDesc += "\n\n<b>Stats:</b>";
            if (attackBonus > 0) fullDesc += $"\n+{attackBonus} Attack";
            if (defenseBonus > 0) fullDesc += $"\n+{defenseBonus} Defense";
            if (healthBonus > 0) fullDesc += $"\n+{healthBonus} HP";
            if (manaBonus > 0) fullDesc += $"\n+{manaBonus} Mana";
        }

        if (itemType == ItemType.Consumable)
        {
            if (healAmount > 0) fullDesc += $"\n\nRestores {healAmount} HP";
            if (manaRestoreAmount > 0) fullDesc += $"\nRestores {manaRestoreAmount} Mana";
        }

        return fullDesc;
    }
}
