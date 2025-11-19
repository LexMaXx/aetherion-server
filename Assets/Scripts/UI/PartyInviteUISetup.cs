using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Автоматически создаёт PartyInviteUI при старте сцены если его нет
/// Прикрепите этот скрипт к любому GameObject в BattleScene (например к Canvas)
/// </summary>
public class PartyInviteUISetup : MonoBehaviour
{
    void Start()
    {
        // Проверяем существует ли уже PartyInviteUI
        var existing = FindObjectOfType<PartyInviteUI>();
        if (existing != null)
        {
            Debug.Log("[PartyInviteUISetup] ✅ PartyInviteUI уже существует, ничего не делаем");
            return;
        }

        Debug.Log("[PartyInviteUISetup] 🔨 PartyInviteUI не найден, создаём автоматически...");

        // Находим Canvas
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("[PartyInviteUISetup] ❌ Canvas не найден!");
            return;
        }

        // Создаём PartyInvitePanel
        GameObject panel = new GameObject("PartyInvitePanel");
        panel.transform.SetParent(canvas.transform, false);

        Image panelImage = panel.AddComponent<Image>();
        panelImage.color = new Color(0f, 0f, 0f, 0.8f); // Тёмный полупрозрачный фон

        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(400f, 200f);
        panelRect.anchoredPosition = Vector2.zero;

        // Создаём текст "Приглашение от игрока"
        GameObject titleObj = new GameObject("TitleText");
        titleObj.transform.SetParent(panel.transform, false);
        TextMeshProUGUI titleText = titleObj.AddComponent<TextMeshProUGUI>();
        titleText.text = "Приглашение в группу";
        titleText.fontSize = 20;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.color = Color.white;

        RectTransform titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 0.7f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.offsetMin = new Vector2(10f, 10f);
        titleRect.offsetMax = new Vector2(-10f, -10f);

        // Создаём текст с именем приглашающего
        GameObject nameObj = new GameObject("InviterNameText");
        nameObj.transform.SetParent(panel.transform, false);
        TextMeshProUGUI nameText = nameObj.AddComponent<TextMeshProUGUI>();
        nameText.text = "Player приглашает вас в группу";
        nameText.fontSize = 16;
        nameText.alignment = TextAlignmentOptions.Center;
        nameText.color = Color.yellow;

        RectTransform nameRect = nameObj.GetComponent<RectTransform>();
        nameRect.anchorMin = new Vector2(0f, 0.5f);
        nameRect.anchorMax = new Vector2(1f, 0.7f);
        nameRect.offsetMin = new Vector2(10f, 5f);
        nameRect.offsetMax = new Vector2(-10f, -5f);

        // Создаём текст с деталями (класс, уровень)
        GameObject detailsObj = new GameObject("InviterDetailsText");
        detailsObj.transform.SetParent(panel.transform, false);
        TextMeshProUGUI detailsText = detailsObj.AddComponent<TextMeshProUGUI>();
        detailsText.text = "Воин, Ур. 1";
        detailsText.fontSize = 14;
        detailsText.alignment = TextAlignmentOptions.Center;
        detailsText.color = Color.gray;

        RectTransform detailsRect = detailsObj.GetComponent<RectTransform>();
        detailsRect.anchorMin = new Vector2(0f, 0.4f);
        detailsRect.anchorMax = new Vector2(1f, 0.5f);
        detailsRect.offsetMin = new Vector2(10f, 5f);
        detailsRect.offsetMax = new Vector2(-10f, -5f);

        // Создаём кнопку "Принять"
        GameObject acceptBtnObj = new GameObject("AcceptButton");
        acceptBtnObj.transform.SetParent(panel.transform, false);
        Button acceptBtn = acceptBtnObj.AddComponent<Button>();
        Image acceptBtnImage = acceptBtnObj.AddComponent<Image>();
        acceptBtnImage.color = new Color(0f, 0.7f, 0f, 1f); // Зелёный

        RectTransform acceptRect = acceptBtnObj.GetComponent<RectTransform>();
        acceptRect.anchorMin = new Vector2(0.1f, 0.1f);
        acceptRect.anchorMax = new Vector2(0.45f, 0.3f);
        acceptRect.offsetMin = Vector2.zero;
        acceptRect.offsetMax = Vector2.zero;

        GameObject acceptTextObj = new GameObject("Text");
        acceptTextObj.transform.SetParent(acceptBtnObj.transform, false);
        TextMeshProUGUI acceptText = acceptTextObj.AddComponent<TextMeshProUGUI>();
        acceptText.text = "Принять";
        acceptText.fontSize = 16;
        acceptText.alignment = TextAlignmentOptions.Center;
        acceptText.color = Color.white;

        RectTransform acceptTextRect = acceptTextObj.GetComponent<RectTransform>();
        acceptTextRect.anchorMin = Vector2.zero;
        acceptTextRect.anchorMax = Vector2.one;
        acceptTextRect.offsetMin = Vector2.zero;
        acceptTextRect.offsetMax = Vector2.zero;

        // Создаём кнопку "Отклонить"
        GameObject declineBtnObj = new GameObject("DeclineButton");
        declineBtnObj.transform.SetParent(panel.transform, false);
        Button declineBtn = declineBtnObj.AddComponent<Button>();
        Image declineBtnImage = declineBtnObj.AddComponent<Image>();
        declineBtnImage.color = new Color(0.7f, 0f, 0f, 1f); // Красный

        RectTransform declineRect = declineBtnObj.GetComponent<RectTransform>();
        declineRect.anchorMin = new Vector2(0.55f, 0.1f);
        declineRect.anchorMax = new Vector2(0.9f, 0.3f);
        declineRect.offsetMin = Vector2.zero;
        declineRect.offsetMax = Vector2.zero;

        GameObject declineTextObj = new GameObject("Text");
        declineTextObj.transform.SetParent(declineBtnObj.transform, false);
        TextMeshProUGUI declineText = declineTextObj.AddComponent<TextMeshProUGUI>();
        declineText.text = "Отклонить";
        declineText.fontSize = 16;
        declineText.alignment = TextAlignmentOptions.Center;
        declineText.color = Color.white;

        RectTransform declineTextRect = declineTextObj.GetComponent<RectTransform>();
        declineTextRect.anchorMin = Vector2.zero;
        declineTextRect.anchorMax = Vector2.one;
        declineTextRect.offsetMin = Vector2.zero;
        declineTextRect.offsetMax = Vector2.zero;

        // Создаём PartyInviteUI GameObject
        GameObject partyInviteUIObj = new GameObject("PartyInviteUI");
        partyInviteUIObj.transform.SetParent(canvas.transform, false);
        PartyInviteUI partyInviteUI = partyInviteUIObj.AddComponent<PartyInviteUI>();

        // Назначаем ссылки через рефлексию (Inspector поля private)
        var invitePanelField = typeof(PartyInviteUI).GetField("invitePanel", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var inviterNameTextField = typeof(PartyInviteUI).GetField("inviterNameText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var inviterDetailsTextField = typeof(PartyInviteUI).GetField("inviterDetailsText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var acceptButtonField = typeof(PartyInviteUI).GetField("acceptButton", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var declineButtonField = typeof(PartyInviteUI).GetField("declineButton", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        invitePanelField?.SetValue(partyInviteUI, panel);
        inviterNameTextField?.SetValue(partyInviteUI, nameText);
        inviterDetailsTextField?.SetValue(partyInviteUI, detailsText);
        acceptButtonField?.SetValue(partyInviteUI, acceptBtn);
        declineButtonField?.SetValue(partyInviteUI, declineBtn);

        Debug.Log("[PartyInviteUISetup] ✅ PartyInviteUI создан автоматически!");

        // По умолчанию скрываем панель
        panel.SetActive(false);
    }
}
