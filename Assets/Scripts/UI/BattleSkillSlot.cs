using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

/// <summary>
/// Отдельный слот для скилла в BattleScene
/// Поддерживает иконку, кулдаун, нажатие
/// </summary>
public class BattleSkillSlot : MonoBehaviour, IPointerClickHandler
{
    [Header("UI Elements")]
    public Image skillIconImage;
    public Image cooldownOverlay;
    public TextMeshProUGUI cooldownText;
    public Image frameImage;
    public Button button;

    [Header("Skill Data")]
    private int slotIndex = -1;
    private SkillExecutor skillExecutor;
    private SkillConfig skillConfig;

    [Header("Colors")]
    public Color normalColor = Color.white;
    public Color onCooldownColor = new Color(0.5f, 0.5f, 0.5f, 1f);
    public Color noManaColor = new Color(1f, 0.3f, 0.3f, 1f);
    public Color noAPColor = new Color(1f, 0.6f, 0.1f, 1f); // Оранжевый для недостатка AP

    private bool isInitialized = false;

    /// <summary>
    /// Инициализация слота
    /// </summary>
    public void Initialize(int index, SkillExecutor executor)
    {
        slotIndex = index;
        skillExecutor = executor;

        if (skillExecutor != null && slotIndex >= 0 && slotIndex < skillExecutor.equippedSkills.Count)
        {
            skillConfig = skillExecutor.equippedSkills[slotIndex];
        }

        // Устанавливаем иконку скилла
        if (skillConfig != null && skillIconImage != null)
        {
            if (skillConfig.icon != null)
            {
                skillIconImage.sprite = skillConfig.icon;
                skillIconImage.color = Color.white;
            }
            else
            {
                // Placeholder icon
                skillIconImage.sprite = null;
                skillIconImage.color = new Color(0.3f, 0.3f, 0.3f, 1f);
            }
        }
        else if (skillIconImage != null)
        {
            // Пустой слот
            skillIconImage.sprite = null;
            skillIconImage.color = new Color(0.2f, 0.2f, 0.2f, 0.5f);
        }

        // Скрываем cooldown overlay по умолчанию
        if (cooldownOverlay != null)
            cooldownOverlay.fillAmount = 0f;

        if (cooldownText != null)
            cooldownText.text = "";

        isInitialized = true;
    }

    /// <summary>
    /// Обновление UI слота (кулдаун, мана)
    /// </summary>
    public void UpdateUI()
    {
        if (!isInitialized || skillExecutor == null || skillConfig == null)
            return;

        // Получаем оставшийся кулдаун
        float remainingCooldown = skillExecutor.GetRemainingCooldown(slotIndex);

        if (remainingCooldown > 0f)
        {
            // Скилл на кулдауне
            if (cooldownOverlay != null)
            {
                cooldownOverlay.fillAmount = remainingCooldown / skillConfig.cooldown;
            }

            if (cooldownText != null)
            {
                cooldownText.text = Mathf.Ceil(remainingCooldown).ToString();
            }

            if (frameImage != null)
            {
                frameImage.color = onCooldownColor;
            }
        }
        else
        {
            // Кулдаун закончился
            if (cooldownOverlay != null)
            {
                cooldownOverlay.fillAmount = 0f;
            }

            if (cooldownText != null)
            {
                cooldownText.text = "";
            }

            // Проверяем Action Points (приоритет над маной)
            ActionPointsSystem apSystem = skillExecutor.GetComponent<ActionPointsSystem>();
            if (apSystem != null)
            {
                // Если не указана стоимость AP - используем стоимость атаки (4 AP)
                int apCost = skillConfig.actionPointCost > 0 ? skillConfig.actionPointCost : apSystem.GetAttackCost();

                if (apSystem.GetCurrentPoints() < apCost)
                {
                    // Недостаточно AP - оранжевый
                    if (frameImage != null)
                    {
                        frameImage.color = noAPColor;
                    }
                    return; // Не проверяем ману если нет AP
                }
            }

            // Проверяем ману
            ManaSystem manaSystem = skillExecutor.GetComponent<ManaSystem>();
            if (manaSystem != null && skillConfig.manaCost > 0)
            {
                if (manaSystem.HasEnoughMana(skillConfig.manaCost))
                {
                    // Достаточно маны
                    if (frameImage != null)
                    {
                        frameImage.color = normalColor;
                    }
                }
                else
                {
                    // Недостаточно маны - красный
                    if (frameImage != null)
                    {
                        frameImage.color = noManaColor;
                    }
                }
            }
            else
            {
                // Нет стоимости маны или ManaSystem
                if (frameImage != null)
                {
                    frameImage.color = normalColor;
                }
            }
        }
    }

    /// <summary>
    /// Обработка клика по слоту
    /// </summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        UseSkill();
    }

    /// <summary>
    /// Использовать скилл из этого слота
    /// </summary>
    public void UseSkill()
    {
        if (!isInitialized || skillExecutor == null || skillConfig == null)
        {
            Debug.Log($"[BattleSkillSlot] ⚠️ Cannot use skill - not initialized");
            return;
        }

        Debug.Log($"[BattleSkillSlot] 🎯 Using skill: {skillConfig.skillName} (slot {slotIndex})");

        // Получаем текущую цель
        TargetSystem targetSystem = skillExecutor.GetComponent<TargetSystem>();
        Transform target = null;
        Vector3? groundTarget = null;

        if (targetSystem != null)
        {
            TargetableEntity currentTarget = targetSystem.GetCurrentTarget();
            if (currentTarget != null)
            {
                target = currentTarget.transform;
            }
        }

        // Для ground target скиллов (например, Meteor) нужна отдельная логика
        if (skillConfig.targetType == SkillTargetType.Ground)
        {
            // TODO: Показать индикатор выбора позиции на земле
            // Пока используем позицию впереди персонажа
            groundTarget = skillExecutor.transform.position + skillExecutor.transform.forward * 5f;
            Debug.Log($"[BattleSkillSlot] 📍 Ground target: {groundTarget}");
        }

        // Используем скилл
        bool success = skillExecutor.UseSkill(slotIndex, target, groundTarget);

        if (success)
        {
            Debug.Log($"[BattleSkillSlot] ✅ Skill used: {skillConfig.skillName}");
        }
        else
        {
            Debug.Log($"[BattleSkillSlot] ❌ Failed to use skill: {skillConfig.skillName}");
        }
    }

    /// <summary>
    /// Получить skill config для этого слота
    /// </summary>
    public SkillConfig GetSkillConfig()
    {
        return skillConfig;
    }

    /// <summary>
    /// Установить новый скилл в этот слот
    /// </summary>
    public void SetSkill(SkillConfig newSkill)
    {
        skillConfig = newSkill;

        if (skillConfig != null && skillIconImage != null)
        {
            if (skillConfig.icon != null)
            {
                skillIconImage.sprite = skillConfig.icon;
                skillIconImage.color = Color.white;
            }
        }
    }
}
