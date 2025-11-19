using UnityEngine;

/// <summary>
/// Показывает структуру персонажа и камеры во время игры
/// Нажми F9 чтобы посмотреть информацию
/// </summary>
public class DebugPlayerStructure : MonoBehaviour
{
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F9))
        {
            ShowDebugInfo();
        }
    }

    void ShowDebugInfo()
    {
        Debug.Log("=== DEBUG: Структура персонажа и камеры ===");

        // Ищем всех игроков
        GameObject[] players = new GameObject[]
        {
            GameObject.Find("WarriorPlayer"),
            GameObject.Find("MagePlayer"),
            GameObject.Find("ArcherPlayer"),
            GameObject.Find("RoguePlayer"),
            GameObject.Find("PaladinPlayer")
        };

        GameObject activePlayer = null;
        foreach (GameObject p in players)
        {
            if (p != null)
            {
                activePlayer = p;
                break;
            }
        }

        if (activePlayer != null)
        {
            Debug.Log($"\n👤 Активный персонаж: {activePlayer.name}");
            Debug.Log($"   Позиция: {activePlayer.transform.position}");

            // Показываем всех детей
            Debug.Log($"   Дочерние объекты ({activePlayer.transform.childCount}):");
            for (int i = 0; i < activePlayer.transform.childCount; i++)
            {
                Transform child = activePlayer.transform.GetChild(i);
                Debug.Log($"     - {child.name} (pos: {child.localPosition})");

                // Компоненты на ребенке
                CharacterController childCC = child.GetComponent<CharacterController>();
                if (childCC != null)
                {
                    Debug.LogWarning($"       ⚠ CharacterController на РЕБЕНКЕ! (center: {childCC.center}, height: {childCC.height})");
                }
            }

            // Компоненты на родителе
            Debug.Log($"\n   Компоненты на родителе:");
            CharacterController parentCC = activePlayer.GetComponent<CharacterController>();
            if (parentCC != null)
            {
                Debug.Log($"     ✓ CharacterController (center: {parentCC.center}, height: {parentCC.height})");
            }
            else
            {
                Debug.LogError($"     ✗ CharacterController НЕ НАЙДЕН!");
            }

            Animator animator = activePlayer.GetComponent<Animator>();
            if (animator != null)
            {
                Debug.Log($"     ✓ Animator");
            }

            PlayerController pc = activePlayer.GetComponent<PlayerController>();
            if (pc != null)
            {
                Debug.Log($"     ✓ PlayerController");
            }
        }
        else
        {
            Debug.LogError("❌ Персонаж не найден!");
        }

        // Проверяем камеру
        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            Debug.Log($"\n📹 Main Camera:");
            Debug.Log($"   Позиция: {mainCamera.transform.position}");

            TPSCameraController tps = mainCamera.GetComponent<TPSCameraController>();
            if (tps != null)
            {
                Debug.Log($"   ✓ TPSCameraController найден");
                // Пытаемся получить target через reflection
                var targetField = typeof(TPSCameraController).GetField("target", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (targetField != null)
                {
                    Transform target = (Transform)targetField.GetValue(tps);
                    if (target != null)
                    {
                        Debug.Log($"   ✓ Target: {target.name} (pos: {target.position})");
                    }
                    else
                    {
                        Debug.LogError($"   ✗ Target = NULL!");
                    }
                }
            }
            else
            {
                Debug.LogWarning($"   ⚠ TPSCameraController не найден");
            }

            CameraFollow oldCam = mainCamera.GetComponent<CameraFollow>();
            if (oldCam != null)
            {
                Debug.LogWarning($"   ⚠ Старый CameraFollow всё ещё на камере!");
            }
        }

        Debug.Log("\n=== Конец отладки ===");
    }
}
