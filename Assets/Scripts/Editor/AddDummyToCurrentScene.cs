using UnityEngine;
using UnityEditor;

public class AddDummyToCurrentScene : EditorWindow
{
    [MenuItem("Aetherion/Add DummyEnemy to Scene")]
    public static void AddDummy()
    {
        // Находим игрока
        GameObject player = GameObject.Find("TestPlayer");
        if (player == null)
        {
            player = GameObject.FindObjectOfType<PlayerController>()?.gameObject;
        }

        Vector3 spawnPos = Vector3.zero;
        if (player != null)
        {
            // Создаём врага в 10м перед игроком
            spawnPos = player.transform.position + player.transform.forward * 10f;
        }

        // Создаём 3 DummyEnemy
        for (int i = 0; i < 3; i++)
        {
            GameObject dummy = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            dummy.name = $"DummyEnemy_{i + 1}";

            // Позиция
            Vector3 offset = new Vector3(i * 3f - 3f, 0, 0);
            dummy.transform.position = spawnPos + offset;

            // Масштаб
            dummy.transform.localScale = new Vector3(1f, 1.5f, 1f);

            // Цвет (красный)
            Renderer renderer = dummy.GetComponent<Renderer>();
            if (renderer != null)
            {
                Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                mat.color = Color.red;
                renderer.material = mat;
            }

            // Добавляем компонент DummyEnemy
            DummyEnemy dummyComponent = dummy.AddComponent<DummyEnemy>();

            Debug.Log($"[AddDummyToCurrentScene] ✅ Создан {dummy.name} в позиции {dummy.transform.position}");
        }

        Debug.Log("[AddDummyToCurrentScene] ✅ Добавлено 3 DummyEnemy в сцену!");
    }
}
