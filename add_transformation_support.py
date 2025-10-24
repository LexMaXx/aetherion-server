#!/usr/bin/env python3
"""
Добавляет поддержку Transformation в SkillExecutor.cs
"""

file_path = 'Assets/Scripts/Skills/SkillExecutor.cs'

# Читаем файл
with open(file_path, 'r', encoding='utf-8') as f:
    content = f.read()

# 1. Добавляем case Transformation в switch
# Ищем "case SkillConfigType.Summon:" и добавляем после его break
old_switch = """            case SkillConfigType.Summon:
                ExecuteSummon(skill);
                break;
        }
    }"""

new_switch = """            case SkillConfigType.Summon:
                ExecuteSummon(skill);
                break;
            case SkillConfigType.Transformation:
                ExecuteTransformation(skill);
                break;
        }
    }"""

if old_switch in content:
    content = content.replace(old_switch, new_switch)
    print("✅ Added Transformation case to switch")
else:
    print("⚠️ Switch statement not found - возможно уже добавлено")

# 2. Добавляем метод ExecuteTransformation перед методом Log
# Ищем "    private void Log(string message)"
log_method_marker = "    private void Log(string message)"

transformation_method = """    /// <summary>
    /// Трансформация (Bear Form для Paladin/Druid)
    /// </summary>
    private void ExecuteTransformation(SkillConfig skill)
    {
        Log($"Using transformation skill: {skill.skillName}");

        // Спавним визуальные эффекты трансформации
        SpawnEffect(skill.castEffectPrefab, transform.position, Quaternion.identity);
        SpawnEffect(skill.casterEffectPrefab, transform.position + Vector3.up * 1.5f, Quaternion.identity);

        // Проверяем что prefab трансформации существует
        if (skill.transformationModel == null)
        {
            Log($"❌ Transformation model отсутствует для {skill.skillName}!");
            return;
        }

        // Получаем компонент SimpleTransformation
        SimpleTransformation transformation = GetComponent<SimpleTransformation>();
        if (transformation == null)
        {
            transformation = gameObject.AddComponent<SimpleTransformation>();
            Log($"✅ SimpleTransformation компонент добавлен");
        }

        // Получаем аниматор паладина для передачи в TransformTo
        Animator paladinAnimator = GetComponent<Animator>();
        if (paladinAnimator == null)
        {
            paladinAnimator = GetComponentInChildren<Animator>();
        }

        // Выполняем трансформацию
        bool success = transformation.TransformTo(skill.transformationModel, paladinAnimator);

        if (!success)
        {
            Log($"❌ Трансформация не удалась!");
            return;
        }

        Log($"✅ Трансформация выполнена: {skill.skillName}");
        Log($"🐻 Модель трансформации: {skill.transformationModel.name}");

        // Применяем статус-эффекты трансформации (если есть)
        if (skill.effects != null && skill.effects.Count > 0)
        {
            EffectManager targetEffectManager = GetComponent<EffectManager>();
            if (targetEffectManager != null)
            {
                foreach (EffectConfig effect in skill.effects)
                {
                    targetEffectManager.ApplyEffect(effect, stats);
                    Log($"Effect applied: {effect.effectType}");
                }
            }
        }

        // Применяем бонусы трансформации (HP, урон)
        if (skill.hpBonusPercent > 0)
        {
            HealthSystem healthSystem = GetComponent<HealthSystem>();
            if (healthSystem != null)
            {
                float hpBonus = healthSystem.MaxHealth * (skill.hpBonusPercent / 100f);
                // TODO: Добавить временный бонус к MaxHP
                Log($"💚 HP бонус: +{skill.hpBonusPercent}% (+{hpBonus:F0} HP)");
            }
        }

        if (skill.damageBonusPercent > 0)
        {
            // TODO: Добавить временный бонус к урону через CharacterStats
            Log($"⚔️ Урон бонус: +{skill.damageBonusPercent}%");
        }

        // Запускаем таймер возврата трансформации
        if (skill.transformationDuration > 0)
        {
            StartCoroutine(RevertTransformationAfterDelay(transformation, skill.transformationDuration));
            Log($"⏱️ Длительность трансформации: {skill.transformationDuration} секунд");
        }
    }

    /// <summary>
    /// Возврат из трансформации после задержки
    /// </summary>
    private IEnumerator RevertTransformationAfterDelay(SimpleTransformation transformation, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (transformation != null && transformation.IsTransformed())
        {
            transformation.RevertToOriginal();
            Log($"⏱️ Трансформация завершилась (время истекло)");

            // Спавним эффект возврата
            SpawnEffect(Resources.Load<GameObject>("Effects/CFXR Magic Poof"), transform.position, Quaternion.identity);
        }
    }

    """

if log_method_marker in content and "ExecuteTransformation" not in content:
    content = content.replace(log_method_marker, transformation_method + log_method_marker)
    print("✅ Added ExecuteTransformation method")
elif "ExecuteTransformation" in content:
    print("⚠️ ExecuteTransformation already exists")
else:
    print("❌ Log method marker not found")

# Записываем обратно
with open(file_path, 'w', encoding='utf-8') as f:
    f.write(content)

print(f"\n✅ ГОТОВО! Файл {file_path} обновлён")
print("\nДобавлено:")
print("1. case SkillConfigType.Transformation в switch")
print("2. Метод ExecuteTransformation() для обработки трансформации")
print("3. Метод RevertTransformationAfterDelay() для автоматического возврата")
