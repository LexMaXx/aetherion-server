using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

/// <summary>
/// Editor скрипт для автоматического исправления переходов Death во всех Animator Controllers
///
/// Исправления:
/// 1. Добавляет переход Any State → Death с условием isDead == true
/// 2. Отключает "Has Exit Time" на всех переходах в Death
/// 3. Устанавливает Transition Duration = 0.25 для мгновенного перехода
///
/// Использование: Unity Menu → Tools → Fix Death Animation Transitions
/// </summary>
public class FixDeathAnimationTransitions : EditorWindow
{
    [MenuItem("Tools/Fix Death Animation Transitions")]
    public static void FixAllAnimators()
    {
        Debug.Log("=== НАЧАЛО ИСПРАВЛЕНИЯ ANIMATOR CONTROLLERS ===");

        // Список всех Animator Controllers в проекте
        string[] animatorGuids = AssetDatabase.FindAssets("t:AnimatorController");
        int fixedCount = 0;
        int skippedCount = 0;

        foreach (string guid in animatorGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);

            if (controller == null)
            {
                Debug.LogWarning($"❌ Не удалось загрузить: {path}");
                continue;
            }

            Debug.Log($"\n🔍 Проверка: {controller.name}");

            bool wasModified = FixAnimatorController(controller);

            if (wasModified)
            {
                EditorUtility.SetDirty(controller);
                fixedCount++;
                Debug.Log($"✅ {controller.name} исправлен!");
            }
            else
            {
                skippedCount++;
                Debug.Log($"⏭️ {controller.name} пропущен (нет состояния Death или уже исправлен)");
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"\n=== ГОТОВО! ===");
        Debug.Log($"✅ Исправлено: {fixedCount}");
        Debug.Log($"⏭️ Пропущено: {skippedCount}");
        Debug.Log($"📦 Всего проверено: {animatorGuids.Length}");

        EditorUtility.DisplayDialog("Fix Death Animations",
            $"Исправление завершено!\n\n" +
            $"✅ Исправлено: {fixedCount}\n" +
            $"⏭️ Пропущено: {skippedCount}\n" +
            $"📦 Всего: {animatorGuids.Length}",
            "OK");
    }

    private static bool FixAnimatorController(AnimatorController controller)
    {
        bool modified = false;

        // Проверяем что есть параметр isDead
        bool hasIsDeadParameter = false;
        foreach (AnimatorControllerParameter param in controller.parameters)
        {
            if (param.name == "isDead" && param.type == AnimatorControllerParameterType.Bool)
            {
                hasIsDeadParameter = true;
                break;
            }
        }

        if (!hasIsDeadParameter)
        {
            Debug.LogWarning($"  ⚠️ Параметр 'isDead' не найден! Добавляю...");
            controller.AddParameter("isDead", AnimatorControllerParameterType.Bool);
            modified = true;
        }

        // Проходимся по всем слоям (обычно Base Layer)
        foreach (AnimatorControllerLayer layer in controller.layers)
        {
            AnimatorStateMachine stateMachine = layer.stateMachine;

            // Ищем состояние Death
            AnimatorState deathState = null;
            foreach (ChildAnimatorState childState in stateMachine.states)
            {
                if (childState.state.name.Contains("Death") || childState.state.name.Contains("Dead"))
                {
                    deathState = childState.state;
                    Debug.Log($"  🎯 Найдено состояние смерти: {deathState.name}");
                    break;
                }
            }

            if (deathState == null)
            {
                Debug.LogWarning($"  ⚠️ Состояние Death не найдено в слое {layer.name}");
                continue;
            }

            // ===== ИСПРАВЛЕНИЕ 1: Убираем Has Exit Time на всех переходах В Death =====
            foreach (ChildAnimatorState childState in stateMachine.states)
            {
                foreach (AnimatorStateTransition transition in childState.state.transitions)
                {
                    if (transition.destinationState == deathState)
                    {
                        if (transition.hasExitTime)
                        {
                            Debug.Log($"  🔧 Отключаю Has Exit Time: {childState.state.name} → {deathState.name}");
                            transition.hasExitTime = false;
                            transition.exitTime = 0f;
                            modified = true;
                        }

                        // Устанавливаем быстрый переход
                        if (transition.duration > 0.25f)
                        {
                            Debug.Log($"  🔧 Ускоряю переход: {childState.state.name} → {deathState.name} (duration: {transition.duration} → 0.25)");
                            transition.duration = 0.25f;
                            modified = true;
                        }
                    }
                }
            }

            // ===== ИСПРАВЛЕНИЕ 2: Добавляем переход Any State → Death =====
            bool hasAnyStateToDeath = false;
            foreach (AnimatorStateTransition transition in stateMachine.anyStateTransitions)
            {
                if (transition.destinationState == deathState)
                {
                    hasAnyStateToDeath = true;

                    // Проверяем что настроено правильно
                    if (transition.hasExitTime)
                    {
                        Debug.Log($"  🔧 Отключаю Has Exit Time на Any State → Death");
                        transition.hasExitTime = false;
                        transition.exitTime = 0f;
                        modified = true;
                    }

                    if (transition.duration > 0.25f)
                    {
                        Debug.Log($"  🔧 Ускоряю переход Any State → Death");
                        transition.duration = 0.25f;
                        modified = true;
                    }

                    // Проверяем что есть условие isDead == true
                    bool hasCondition = false;
                    foreach (AnimatorCondition condition in transition.conditions)
                    {
                        if (condition.parameter == "isDead" && condition.mode == AnimatorConditionMode.If)
                        {
                            hasCondition = true;
                            break;
                        }
                    }

                    if (!hasCondition)
                    {
                        Debug.Log($"  🔧 Добавляю условие isDead == true на Any State → Death");
                        transition.AddCondition(AnimatorConditionMode.If, 0, "isDead");
                        modified = true;
                    }

                    break;
                }
            }

            if (!hasAnyStateToDeath)
            {
                Debug.Log($"  🔧 Создаю переход Any State → Death");

                AnimatorStateTransition newTransition = stateMachine.AddAnyStateTransition(deathState);
                newTransition.hasExitTime = false;
                newTransition.exitTime = 0f;
                newTransition.duration = 0.25f;
                newTransition.AddCondition(AnimatorConditionMode.If, 0, "isDead");

                modified = true;
            }

            // ===== ИСПРАВЛЕНИЕ 3: Добавляем переход Death → Idle (для респавна) =====
            AnimatorState idleState = null;
            foreach (ChildAnimatorState childState in stateMachine.states)
            {
                if (childState.state.name.Contains("Idle") || childState.state.name.Contains("Battle"))
                {
                    idleState = childState.state;
                    break;
                }
            }

            if (idleState != null)
            {
                bool hasDeathToIdle = false;
                foreach (AnimatorStateTransition transition in deathState.transitions)
                {
                    if (transition.destinationState == idleState)
                    {
                        hasDeathToIdle = true;

                        // Проверяем настройки
                        if (!transition.hasExitTime)
                        {
                            Debug.Log($"  🔧 Включаю Has Exit Time на Death → Idle (для респавна)");
                            transition.hasExitTime = true;
                            transition.exitTime = 0.95f; // Почти в конце анимации
                            modified = true;
                        }

                        // Проверяем условие isDead == false
                        bool hasCondition = false;
                        foreach (AnimatorCondition condition in transition.conditions)
                        {
                            if (condition.parameter == "isDead" && condition.mode == AnimatorConditionMode.IfNot)
                            {
                                hasCondition = true;
                                break;
                            }
                        }

                        if (!hasCondition)
                        {
                            Debug.Log($"  🔧 Добавляю условие isDead == false на Death → Idle");
                            transition.AddCondition(AnimatorConditionMode.IfNot, 0, "isDead");
                            modified = true;
                        }

                        break;
                    }
                }

                if (!hasDeathToIdle)
                {
                    Debug.Log($"  🔧 Создаю переход Death → {idleState.name}");

                    AnimatorStateTransition newTransition = deathState.AddTransition(idleState);
                    newTransition.hasExitTime = true;
                    newTransition.exitTime = 0.95f;
                    newTransition.duration = 0.25f;
                    newTransition.AddCondition(AnimatorConditionMode.IfNot, 0, "isDead");

                    modified = true;
                }
            }
        }

        return modified;
    }
}
