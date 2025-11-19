using UnityEngine;
using UnityEditor;

/// <summary>
/// Диагностика физики и коллайдеров
/// Tools > Aetherion > Diagnose Physics
/// </summary>
public class DiagnosePhysics : EditorWindow
{
    [MenuItem("Tools/Aetherion/Diagnose Physics")]
    public static void ShowWindow()
    {
        GetWindow<DiagnosePhysics>("Physics Diagnostic");
    }

    private Vector2 scrollPosition;

    void OnGUI()
    {
        GUILayout.Label("Диагностика физики и коллайдеров", EditorStyles.boldLabel);
        GUILayout.Space(10);

        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("Запустите игру (Play Mode) для диагностики!", MessageType.Warning);
            return;
        }

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        // Проверяем игрока
        GUILayout.Label("=== ПРОВЕРКА ИГРОКА ===", EditorStyles.boldLabel);

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            // Ищем по компоненту
            AetherionPlayerController controller = FindObjectOfType<AetherionPlayerController>();
            if (controller != null)
            {
                player = controller.gameObject;
            }
        }

        if (player == null)
        {
            EditorGUILayout.HelpBox("❌ Игрок не найден!", MessageType.Error);
        }
        else
        {
            GUILayout.Label($"✅ Игрок найден: {player.name}", EditorStyles.boldLabel);
            GUILayout.Label($"Позиция: {player.transform.position}");
            GUILayout.Label($"Активен: {player.activeInHierarchy}");
            GUILayout.Space(10);

            // CharacterController
            CharacterController cc = player.GetComponent<CharacterController>();
            if (cc != null)
            {
                GUILayout.Label("CharacterController:", EditorStyles.boldLabel);
                GUILayout.Label($"  • Enabled: {(cc.enabled ? "✅" : "❌")}");
                GUILayout.Label($"  • Height: {cc.height}");
                GUILayout.Label($"  • Radius: {cc.radius}");
                GUILayout.Label($"  • Center: {cc.center}");
                GUILayout.Label($"  • Grounded: {(cc.isGrounded ? "✅ НА ЗЕМЛЕ" : "❌ В ВОЗДУХЕ")}");
                GUILayout.Label($"  • Velocity: {cc.velocity}");
            }
            else
            {
                EditorGUILayout.HelpBox("❌ CharacterController НЕ НАЙДЕН!", MessageType.Error);
            }

            GUILayout.Space(10);

            // Rigidbody
            Rigidbody rb = player.GetComponent<Rigidbody>();
            if (rb != null)
            {
                GUILayout.Label("Rigidbody:", EditorStyles.boldLabel);
                GUILayout.Label($"  • Use Gravity: {(rb.useGravity ? "✅" : "❌")}");
                GUILayout.Label($"  • Is Kinematic: {(rb.isKinematic ? "✅" : "❌")}");
                GUILayout.Label($"  • Mass: {rb.mass}");
                GUILayout.Label($"  • Velocity: {rb.linearVelocity}");

                if (!rb.isKinematic)
                {
                    EditorGUILayout.HelpBox("⚠️ Rigidbody.isKinematic = false может конфликтовать с CharacterController!", MessageType.Warning);
                }
            }

            // Collider (не CharacterController)
            Collider[] colliders = player.GetComponents<Collider>();
            if (colliders.Length > 0)
            {
                GUILayout.Label($"Colliders ({colliders.Length}):", EditorStyles.boldLabel);
                foreach (var col in colliders)
                {
                    if (col is CharacterController) continue;

                    GUILayout.Label($"  • {col.GetType().Name}: {(col.enabled ? "✅" : "❌")}");
                    GUILayout.Label($"    - isTrigger: {col.isTrigger}");
                }
            }
        }

        GUILayout.Space(20);

        // Проверяем Terrain
        GUILayout.Label("=== ПРОВЕРКА TERRAIN ===", EditorStyles.boldLabel);

        Terrain terrain = FindObjectOfType<Terrain>();
        if (terrain == null)
        {
            EditorGUILayout.HelpBox("❌ Terrain не найден!", MessageType.Warning);
        }
        else
        {
            GUILayout.Label($"✅ Terrain найден: {terrain.gameObject.name}");
            GUILayout.Label($"Активен: {terrain.gameObject.activeInHierarchy}");

            TerrainCollider tc = terrain.GetComponent<TerrainCollider>();
            if (tc != null)
            {
                GUILayout.Label("TerrainCollider:");
                GUILayout.Label($"  • Enabled: {(tc.enabled ? "✅" : "❌")}");
            }
            else
            {
                EditorGUILayout.HelpBox("❌ TerrainCollider НЕ НАЙДЕН!", MessageType.Error);
            }
        }

        GUILayout.Space(20);

        // Проверяем Physics Settings
        GUILayout.Label("=== PHYSICS SETTINGS ===", EditorStyles.boldLabel);
        GUILayout.Label($"Gravity: {Physics.gravity}");
        GUILayout.Label($"Default Solver Iterations: {Physics.defaultSolverIterations}");

        if (Physics.gravity.y > -9.81f)
        {
            EditorGUILayout.HelpBox($"⚠️ Гравитация слабая! Должна быть около -9.81, сейчас {Physics.gravity.y}", MessageType.Warning);
        }
        else if (Physics.gravity.y < -50f)
        {
            EditorGUILayout.HelpBox($"⚠️ Гравитация ОЧЕНЬ сильная! {Physics.gravity.y}", MessageType.Error);
        }

        GUILayout.Space(20);

        // Кнопка для телепорта игрока вверх
        if (player != null)
        {
            if (GUILayout.Button("🚀 Телепортировать игрока на Y=10"))
            {
                player.transform.position = new Vector3(player.transform.position.x, 10f, player.transform.position.z);
                Debug.Log($"[DiagnosePhysics] Игрок телепортирован на {player.transform.position}");
            }

            if (GUILayout.Button("🔄 Сбросить CharacterController"))
            {
                CharacterController cc = player.GetComponent<CharacterController>();
                if (cc != null)
                {
                    cc.enabled = false;
                    cc.enabled = true;
                    Debug.Log("[DiagnosePhysics] CharacterController сброшен");
                }
            }
        }

        EditorGUILayout.EndScrollView();
    }
}
