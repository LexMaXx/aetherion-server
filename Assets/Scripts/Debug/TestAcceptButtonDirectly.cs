using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ТЕСТ: Проверка что AcceptButton может вызывать метод напрямую
/// Добавьте этот компонент на AcceptButton
/// </summary>
[RequireComponent(typeof(Button))]
public class TestAcceptButtonDirectly : MonoBehaviour
{
    private Button button;

    void Start()
    {
        button = GetComponent<Button>();

        // Добавляем тестовый listener
        button.onClick.AddListener(OnTestClick);

        Debug.Log("[TestAcceptButtonDirectly] ✅ Тестовый listener добавлен к AcceptButton");
    }

    void OnTestClick()
    {
        Debug.Log("[TestAcceptButtonDirectly] 🔘 КНОПКА НАЖАТА! Клик работает!");

        // Теперь пробуем найти локацию и войти вручную
        WorldMapPlayerController player = FindObjectOfType<WorldMapPlayerController>();

        if (player != null)
        {
            Debug.Log("[TestAcceptButtonDirectly] ✅ WorldMapPlayerController найден");

            // Используем рефлексию чтобы получить currentNearMarker
            var markerField = typeof(WorldMapPlayerController).GetField("currentNearMarker",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (markerField != null)
            {
                WorldMapLocationMarker marker = markerField.GetValue(player) as WorldMapLocationMarker;

                if (marker != null)
                {
                    Debug.Log($"[TestAcceptButtonDirectly] ✅ Найден маркер: {marker.GetLocationData().locationName}");

                    if (marker.IsUnlocked())
                    {
                        Debug.Log("[TestAcceptButtonDirectly] ✅ Маркер разблокирован, входим...");
                        marker.TryEnterLocation();
                    }
                    else
                    {
                        Debug.LogWarning("[TestAcceptButtonDirectly] ⚠️ Маркер заблокирован!");
                    }
                }
                else
                {
                    Debug.LogWarning("[TestAcceptButtonDirectly] ⚠️ currentNearMarker == null (слишком далеко от локации?)");
                }
            }
            else
            {
                Debug.LogError("[TestAcceptButtonDirectly] ❌ Поле currentNearMarker не найдено!");
            }
        }
        else
        {
            Debug.LogError("[TestAcceptButtonDirectly] ❌ WorldMapPlayerController не найден!");
        }
    }
}
