using UnityEngine;
using TMPro;
using System.Collections;

/// <summary>
/// Управление всплывающими сообщениями над головами игроков (как в Lineage 2)
/// Белый текст на черном фоне, автоматически исчезает через N секунд
/// </summary>
public class ChatBubbleManager : MonoBehaviour
{
    [Header("Prefab")]
    [SerializeField] private GameObject chatBubblePrefab;

    [Header("Settings")]
    [SerializeField] private Vector3 bubbleOffset = new Vector3(0f, 2.5f, 0f);
    [SerializeField] private float fadeDuration = 0.5f;

    // Singleton
    public static ChatBubbleManager Instance { get; private set; }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Показать всплывающее сообщение над головой игрока
    /// </summary>
    public void ShowChatBubble(Transform playerTransform, string message, float displayTime)
    {
        if (chatBubblePrefab == null)
        {
            Debug.LogWarning("[ChatBubbleManager] ⚠️ chatBubblePrefab не назначен!");
            return;
        }

        if (playerTransform == null)
        {
            Debug.LogWarning("[ChatBubbleManager] ⚠️ playerTransform == null!");
            return;
        }

        // Создаем экземпляр префаба
        GameObject bubbleObj = Instantiate(chatBubblePrefab, playerTransform.position + bubbleOffset, Quaternion.identity);

        // Привязываем к игроку
        ChatBubble bubble = bubbleObj.GetComponent<ChatBubble>();
        if (bubble != null)
        {
            bubble.Initialize(playerTransform, message, displayTime, fadeDuration, bubbleOffset);
        }
        else
        {
            Debug.LogWarning("[ChatBubbleManager] ⚠️ У prefab нет компонента ChatBubble!");
            Destroy(bubbleObj);
        }
    }
}
