using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Инструмент для автоматического перемещения орков на поверхность террейна
/// Используется когда NavMesh остался на старой высоте после изменения террейна
/// </summary>
public class SnapOrksToTerrain
{
#if UNITY_EDITOR
    [MenuItem("Tools/Snap All Orks To Terrain")]
    static void SnapAllOrks()
    {
        GameObject[] allObjects = Object.FindObjectsOfType<GameObject>();
        int count = 0;
        int failed = 0;

        Debug.Log("🔍 Ищу всех орков в сцене...");

        foreach (GameObject obj in allObjects)
        {
            // Ищем объекты с "Ork" в названии
            if (obj.name.Contains("Ork") || obj.name.Contains("ork"))
            {
                Vector3 pos = obj.transform.position;
                RaycastHit hit;

                // Raycast вниз с большой высоты чтобы найти поверхность
                if (Physics.Raycast(pos + Vector3.up * 500f, Vector3.down, out hit, 1000f))
                {
                    float oldY = pos.y;
                    pos.y = hit.point.y;
                    obj.transform.position = pos;

                    // Отметить объект как измененный для undo
                    Undo.RecordObject(obj.transform, "Snap Ork to Terrain");

                    count++;
                    Debug.Log($"📍 {obj.name} перемещён: Y={oldY:F2} → Y={hit.point.y:F2} (изменение: {(hit.point.y - oldY):F2})");
                }
                else
                {
                    failed++;
                    Debug.LogWarning($"⚠️ Не найдена поверхность под {obj.name} (Position: {pos})");
                }
            }
        }

        if (count > 0)
        {
            Debug.Log($"✅ <color=green>Успешно перемещено {count} орков на поверхность террейна!</color>");

            if (failed > 0)
            {
                Debug.LogWarning($"⚠️ Не удалось переместить {failed} орков (нет поверхности под ними)");
            }
        }
        else
        {
            Debug.LogWarning("⚠️ Не найдено ни одного орка в сцене! Убедитесь что в названии объекта есть 'Ork'");
        }
    }

    [MenuItem("Tools/Snap Selected Orks To Terrain")]
    static void SnapSelectedOrks()
    {
        if (Selection.gameObjects.Length == 0)
        {
            Debug.LogWarning("⚠️ Ничего не выбрано! Выберите орков в Hierarchy и попробуйте снова.");
            return;
        }

        int count = 0;
        int failed = 0;

        Debug.Log($"🔍 Обрабатываю {Selection.gameObjects.Length} выбранных объектов...");

        foreach (GameObject obj in Selection.gameObjects)
        {
            Vector3 pos = obj.transform.position;
            RaycastHit hit;

            if (Physics.Raycast(pos + Vector3.up * 500f, Vector3.down, out hit, 1000f))
            {
                float oldY = pos.y;
                pos.y = hit.point.y;
                obj.transform.position = pos;

                Undo.RecordObject(obj.transform, "Snap to Terrain");

                count++;
                Debug.Log($"📍 {obj.name} перемещён: Y={oldY:F2} → Y={hit.point.y:F2}");
            }
            else
            {
                failed++;
                Debug.LogWarning($"⚠️ Не найдена поверхность под {obj.name}");
            }
        }

        Debug.Log($"✅ <color=green>Перемещено {count} объектов</color>" + (failed > 0 ? $" (не удалось: {failed})" : ""));
    }

    [MenuItem("Tools/Show Ork Positions")]
    static void ShowOrkPositions()
    {
        GameObject[] allObjects = Object.FindObjectsOfType<GameObject>();
        int count = 0;

        Debug.Log("📊 === Позиции всех орков в сцене ===");

        foreach (GameObject obj in allObjects)
        {
            if (obj.name.Contains("Ork") || obj.name.Contains("ork"))
            {
                count++;
                Vector3 pos = obj.transform.position;

                // Проверяем есть ли NavMeshAgent
                UnityEngine.AI.NavMeshAgent agent = obj.GetComponent<UnityEngine.AI.NavMeshAgent>();
                string navMeshStatus = agent != null ? (agent.isOnNavMesh ? "✅ На NavMesh" : "❌ НЕ на NavMesh") : "⚠️ Нет NavMeshAgent";

                Debug.Log($"{count}. {obj.name}: Position={pos} | {navMeshStatus}");
            }
        }

        if (count == 0)
        {
            Debug.LogWarning("⚠️ Не найдено ни одного орка в сцене!");
        }
        else
        {
            Debug.Log($"📊 === Всего найдено: {count} орков ===");
        }
    }
#endif
}
