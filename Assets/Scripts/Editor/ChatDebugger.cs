using UnityEngine;
using UnityEditor;
using System.Reflection;

/// <summary>
/// Отладочный инструмент для проверки состояния ChatManager
/// </summary>
public class ChatDebugger : EditorWindow
{
    [MenuItem("Aetherion/Debug Chat System")]
    public static void ShowWindow()
    {
        GetWindow<ChatDebugger>("Chat Debugger");
    }

    void OnGUI()
    {
        GUILayout.Label("Chat System Debugger", EditorStyles.boldLabel);
        GUILayout.Space(10);

        if (GUILayout.Button("🔍 Проверить состояние ChatManager"))
        {
            CheckChatManager();
        }

        GUILayout.Space(10);

        if (GUILayout.Button("🔧 Попытаться исправить ссылки"))
        {
            FixChatManagerReferences();
        }
    }

    private static void CheckChatManager()
    {
        ChatManager chatManager = FindObjectOfType<ChatManager>();

        if (chatManager == null)
        {
            Debug.LogError("[ChatDebugger] ❌ ChatManager не найден в сцене!");
            EditorUtility.DisplayDialog("Ошибка", "ChatManager не найден в сцене!", "OK");
            return;
        }

        Debug.Log("[ChatDebugger] ✅ ChatManager найден!");

        var type = typeof(ChatManager);
        var flags = BindingFlags.NonPublic | BindingFlags.Instance;

        // Проверяем все критичные поля
        CheckField(type, chatManager, "chatPanel", flags);
        CheckField(type, chatManager, "messageInputField", flags);
        CheckField(type, chatManager, "sendButton", flags);
        CheckField(type, chatManager, "chatScrollRect", flags);
        CheckField(type, chatManager, "chatContentContainer", flags);
        CheckField(type, chatManager, "chatMessagePrefab", flags);
        CheckField(type, chatManager, "allChatTab", flags);
        CheckField(type, chatManager, "partyChatTab", flags);
        CheckField(type, chatManager, "allChatTabHighlight", flags);
        CheckField(type, chatManager, "partyChatTabHighlight", flags);

        // Проверяем активность ChatPanel
        var chatPanelField = type.GetField("chatPanel", flags);
        if (chatPanelField != null)
        {
            GameObject chatPanel = chatPanelField.GetValue(chatManager) as GameObject;
            if (chatPanel != null)
            {
                Debug.Log($"[ChatDebugger] ChatPanel активен: {chatPanel.activeSelf}");
                Debug.Log($"[ChatDebugger] ChatPanel путь: {GetGameObjectPath(chatPanel)}");
            }
        }

        EditorUtility.DisplayDialog("Готово", "Проверка завершена. Смотрите Console для деталей.", "OK");
    }

    private static void CheckField(System.Type type, object instance, string fieldName, BindingFlags flags)
    {
        var field = type.GetField(fieldName, flags);
        if (field != null)
        {
            var value = field.GetValue(instance);
            string status = value != null ? "✅ НАЗНАЧЕНО" : "❌ NULL";
            string valueStr = value != null ? value.ToString() : "null";
            Debug.Log($"[ChatDebugger] {fieldName}: {status} ({valueStr})");
        }
        else
        {
            Debug.LogWarning($"[ChatDebugger] ⚠️ Поле {fieldName} не найдено!");
        }
    }

    private static void FixChatManagerReferences()
    {
        ChatManager chatManager = FindObjectOfType<ChatManager>();

        if (chatManager == null)
        {
            Debug.LogError("[ChatDebugger] ❌ ChatManager не найден в сцене!");
            EditorUtility.DisplayDialog("Ошибка", "ChatManager не найден в сцене!", "OK");
            return;
        }

        var type = typeof(ChatManager);
        var flags = BindingFlags.NonPublic | BindingFlags.Instance;

        // Пытаемся найти и назначить chatPanel
        var chatPanelField = type.GetField("chatPanel", flags);
        if (chatPanelField != null)
        {
            GameObject currentPanel = chatPanelField.GetValue(chatManager) as GameObject;
            if (currentPanel == null)
            {
                Debug.Log("[ChatDebugger] 🔧 chatPanel == null, пытаемся найти...");

                // ChatManager должен быть прикреплен к ChatPanel
                GameObject chatPanel = chatManager.gameObject;
                chatPanelField.SetValue(chatManager, chatPanel);

                Debug.Log($"[ChatDebugger] ✅ chatPanel назначен на: {chatPanel.name}");
                EditorUtility.SetDirty(chatManager);
            }
            else
            {
                Debug.Log($"[ChatDebugger] ℹ️ chatPanel уже назначен: {currentPanel.name}");
            }
        }

        // Убеждаемся что ChatPanel активен
        GameObject panel = chatManager.gameObject;
        if (!panel.activeSelf)
        {
            Debug.Log("[ChatDebugger] 🔧 Активируем ChatPanel...");
            panel.SetActive(true);
            EditorUtility.SetDirty(panel);
        }

        EditorUtility.DisplayDialog("Готово", "Попытка исправления завершена. Смотрите Console.", "OK");
    }

    private static string GetGameObjectPath(GameObject obj)
    {
        string path = obj.name;
        Transform parent = obj.transform.parent;
        while (parent != null)
        {
            path = parent.name + "/" + path;
            parent = parent.parent;
        }
        return path;
    }
}
