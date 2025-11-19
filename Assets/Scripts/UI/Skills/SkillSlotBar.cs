using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// Один слот скилла в Skill Bar (Arena Scene)
/// Отображает иконку, кулдаун и хоткей
/// MOBILE: Поддерживает клик/тап через IPointerClickHandler
/// </summary>
public class SkillSlotBar : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private Image iconImage;
    [SerializeField] private Image cooldownOverlay;
    [SerializeField] private TextMeshProUGUI cooldownText;
    [SerializeField] private TextMeshProUGUI hotkeyText;
    [SerializeField] private int slotIndex;

    private SkillData currentSkill;
    private float cooldownRemaining = 0f;
    private bool isOnCooldown = false;

    /// <summary>
    /// IPointerClickHandler - обработка клика/тапа (работает БЕЗ Button component!)
    /// Этот метод вызывается автоматически при клике/тапе на этот GameObject
    /// </summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log($"[SkillSlotBar] 🔘 OnSlotClicked для слота {slotIndex}");

        if (currentSkill == null)
        {
            Debug.Log($"[SkillSlotBar] Слот {slotIndex} пустой");
            return;
        }

        if (isOnCooldown)
        {
            Debug.Log($"[SkillSlotBar] Скилл на кулдауне! Осталось: {cooldownRemaining:F1}с");
            return;
        }

        // Находим SkillBarUI и вызываем UseSkill
        SkillBarUI skillBarUI = GetComponentInParent<SkillBarUI>();
        if (skillBarUI != null)
        {
            Debug.Log($"[SkillSlotBar] Активирую скилл '{currentSkill.skillName}' через SkillBarUI");
            skillBarUI.UseSkill(slotIndex);

            // Вибрация для мобильных
#if UNITY_ANDROID || UNITY_IOS
            Handheld.Vibrate();
#endif
        }
        else
        {
            Debug.LogError("[SkillSlotBar] SkillBarUI не найден!");
        }
    }

    /// <summary>
    /// Установить ссылки на UI компоненты (вызывается из CreateSkillBarUI)
    /// </summary>
    public void SetReferences(Image icon, Image cooldown, TextMeshProUGUI cooldownTxt, TextMeshProUGUI hotkey, int index)
    {
        iconImage = icon;
        cooldownOverlay = cooldown;
        cooldownText = cooldownTxt;
        hotkeyText = hotkey;
        slotIndex = index;

        // По умолчанию скрываем кулдаун
        if (cooldownOverlay != null)
        {
            cooldownOverlay.fillAmount = 0f;
        }
        if (cooldownText != null)
        {
            cooldownText.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Установить скилл в слот
    /// </summary>
    public void SetSkill(SkillData skill)
    {
        currentSkill = skill;

        Debug.Log($"[SkillSlotBar] SetSkill вызван для слота {slotIndex + 1}. Скилл: {(skill != null ? skill.skillName : "NULL")}");

        if (iconImage == null)
        {
            Debug.LogError($"[SkillSlotBar] ❌ Слот {slotIndex + 1}: iconImage = NULL!");
            return;
        }

        if (skill != null)
        {
            Debug.Log($"[SkillSlotBar] Слот {slotIndex + 1}: устанавливаю иконку. Icon: {(skill.icon != null ? skill.icon.name : "NULL")}");

            iconImage.sprite = skill.icon;
            iconImage.enabled = true;
            iconImage.color = Color.white;

            // Проверяем что иконка установлена
            if (iconImage.sprite != null)
            {
                Debug.Log($"[SkillSlotBar] ✅ Слот {slotIndex + 1}: иконка установлена! Enabled: {iconImage.enabled}, Color: {iconImage.color}");
            }
            else
            {
                Debug.LogWarning($"[SkillSlotBar] ⚠️ Слот {slotIndex + 1}: у скилла '{skill.skillName}' НЕТ иконки!");
            }

            Debug.Log($"[SkillSlotBar] Слот {slotIndex + 1}: установлен скилл '{skill.skillName}'");
        }
        else
        {
            iconImage.enabled = false;
            Debug.Log($"[SkillSlotBar] Слот {slotIndex + 1}: пустой");
        }
    }

    /// <summary>
    /// Запустить кулдаун скилла
    /// </summary>
    public void StartCooldown(float duration)
    {
        cooldownRemaining = duration;
        isOnCooldown = true;

        if (cooldownText != null)
        {
            cooldownText.gameObject.SetActive(true);
        }

        Debug.Log($"[SkillSlotBar] Слот {slotIndex + 1}: кулдаун {duration}с");
    }

    void Update()
    {
        if (isOnCooldown && cooldownRemaining > 0f)
        {
            cooldownRemaining -= Time.deltaTime;

            // Обновляем визуал кулдауна
            if (currentSkill != null && cooldownOverlay != null)
            {
                float progress = cooldownRemaining / currentSkill.cooldown;
                cooldownOverlay.fillAmount = progress;
            }

            // Обновляем текст
            if (cooldownText != null)
            {
                cooldownText.text = Mathf.Ceil(cooldownRemaining).ToString();
            }

            // Кулдаун закончен
            if (cooldownRemaining <= 0f)
            {
                isOnCooldown = false;
                cooldownRemaining = 0f;

                if (cooldownOverlay != null)
                {
                    cooldownOverlay.fillAmount = 0f;
                }

                if (cooldownText != null)
                {
                    cooldownText.gameObject.SetActive(false);
                }

                Debug.Log($"[SkillSlotBar] Слот {slotIndex + 1}: кулдаун завершён");
            }
        }
    }

    /// <summary>
    /// Получить текущий скилл
    /// </summary>
    public SkillData GetSkill()
    {
        return currentSkill;
    }

    /// <summary>
    /// Проверка: скилл на кулдауне?
    /// </summary>
    public bool IsOnCooldown()
    {
        return isOnCooldown;
    }

    /// <summary>
    /// Получить оставшееся время кулдауна
    /// </summary>
    public float GetCooldownRemaining()
    {
        return cooldownRemaining;
    }
}
