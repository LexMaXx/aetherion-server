using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Компонент слота члена группы.
/// Размещается на префабе в PartyUI.
/// </summary>
public class PartyMemberSlot : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI usernameText;
    [SerializeField] private TextMeshProUGUI classText;
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private Image healthBar;
    [SerializeField] private Image manaBar;
    [SerializeField] private TextMeshProUGUI healthText; // "100/100"
    [SerializeField] private TextMeshProUGUI manaText; // "100/100"
    [SerializeField] private Button leaveButton; // Кнопка "Покинуть"

    private string socketId;
    private System.Action<string> onLeaveCallback;

    /// <summary>
    /// Инициализация UI значениями участника группы.
    /// </summary>
    public void Initialize(PartyMember member, bool isLocalPlayer, System.Action<string> onLeaveCallback)
    {
        socketId = member.socketId;
        this.onLeaveCallback = onLeaveCallback;

        if (usernameText != null)
        {
            usernameText.text = member.username;
            usernameText.color = isLocalPlayer ? Color.cyan : Color.white;
        }

        if (classText != null)
        {
            classText.text = member.characterClass;
        }

        if (levelText != null)
        {
            levelText.text = $"Ур. {member.level}";
        }

        UpdateStats(member.health, member.mana, member.maxHealth, member.maxMana);

        if (leaveButton != null)
        {
            leaveButton.gameObject.SetActive(isLocalPlayer);
            leaveButton.onClick.RemoveAllListeners();
            leaveButton.onClick.AddListener(() => onLeaveCallback?.Invoke(socketId));
        }
    }

    /// <summary>
    /// Обновить шкалы HP/MP.
    /// </summary>
    public void UpdateStats(float health, float mana, float maxHealth, float maxMana)
    {
        if (healthBar != null)
        {
            healthBar.fillAmount = maxHealth > 0f ? Mathf.Clamp01(health / maxHealth) : 0f;
        }

        if (healthText != null)
        {
            healthText.text = $"{Mathf.RoundToInt(health)}/{Mathf.RoundToInt(maxHealth)}";
        }

        if (manaBar != null)
        {
            manaBar.fillAmount = maxMana > 0f ? Mathf.Clamp01(mana / maxMana) : 0f;
        }

        if (manaText != null)
        {
            manaText.text = $"{Mathf.RoundToInt(mana)}/{Mathf.RoundToInt(maxMana)}";
        }
    }
}
