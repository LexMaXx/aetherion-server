using UnityEngine;
using TMPro;

/// <summary>
/// Всплывающая цифра урона
/// </summary>
public class DamageNumber : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float lifetime = 1.5f;
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float fadeSpeed = 1f;

    private TextMeshProUGUI textMesh;
    private float timer = 0f;
    private Vector3 moveDirection;
    private Color startColor;

    void Awake()
    {
        textMesh = GetComponent<TextMeshProUGUI>();
        if (textMesh != null)
        {
            startColor = textMesh.color;
        }
    }

    /// <summary>
    /// Инициализация цифры урона
    /// </summary>
    public void Initialize(float damage, bool isCritical = false, bool isHeal = false)
    {
        if (textMesh == null)
        {
            textMesh = GetComponent<TextMeshProUGUI>();
        }

        // Форматируем текст
        string damageText = Mathf.RoundToInt(damage).ToString();

        if (isCritical)
        {
            damageText = $"<b>{damageText}!</b>";
            textMesh.fontSize = 48; // Крупнее для крита
            textMesh.color = Color.yellow; // Жёлтый для крита
        }
        else if (isHeal)
        {
            damageText = $"+{damageText}";
            textMesh.fontSize = 36;
            textMesh.color = Color.green; // Зелёный для лечения
        }
        else
        {
            textMesh.fontSize = 36;
            textMesh.color = Color.white; // Белый для обычного урона
        }

        textMesh.text = damageText;
        startColor = textMesh.color;

        // Случайное направление (вверх + немного в стороны)
        float randomX = Random.Range(-0.3f, 0.3f);
        moveDirection = new Vector3(randomX, 1f, 0f).normalized;
    }

    void Update()
    {
        if (textMesh == null) return;

        timer += Time.deltaTime;

        // Движение вверх
        transform.position += moveDirection * moveSpeed * Time.deltaTime;

        // Затухание
        if (timer > lifetime * 0.5f)
        {
            float alpha = Mathf.Lerp(1f, 0f, (timer - lifetime * 0.5f) / (lifetime * 0.5f));
            Color newColor = startColor;
            newColor.a = alpha;
            textMesh.color = newColor;
        }

        // Уничтожение
        if (timer >= lifetime)
        {
            Destroy(gameObject);
        }
    }
}
