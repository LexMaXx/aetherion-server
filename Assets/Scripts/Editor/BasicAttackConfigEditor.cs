using UnityEngine;
using UnityEditor;

/// <summary>
/// Custom Inspector для BasicAttackConfig
/// Красивый и удобный редактор настроек базовых атак
/// </summary>
[CustomEditor(typeof(BasicAttackConfig))]
public class BasicAttackConfigEditor : Editor
{
    private BasicAttackConfig config;
    private bool showDamageSection = true;
    private bool showProjectileSection = true;
    private bool showEffectsSection = true;
    private bool showAudioSection = true;
    private bool showAnimationSection = true;
    private bool showAdvancedSection = false;

    private void OnEnable()
    {
        config = (BasicAttackConfig)target;
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // ═══════════════════════════════════════════
        // HEADER
        // ═══════════════════════════════════════════
        EditorGUILayout.Space(10);
        GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 16,
            alignment = TextAnchor.MiddleCenter
        };

        EditorGUILayout.LabelField($"⚔️ BASIC ATTACK CONFIG", headerStyle);
        EditorGUILayout.LabelField($"{config.characterClass}", EditorStyles.centeredGreyMiniLabel);
        EditorGUILayout.Space(5);

        // ═══════════════════════════════════════════
        // БАЗОВАЯ ИНФОРМАЦИЯ
        // ═══════════════════════════════════════════
        DrawSectionHeader("БАЗОВАЯ ИНФОРМАЦИЯ", Color.cyan);
        EditorGUI.indentLevel++;

        EditorGUILayout.PropertyField(serializedObject.FindProperty("characterClass"), new GUIContent("Класс персонажа"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("attackType"), new GUIContent("Тип атаки"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("description"), new GUIContent("Описание"));

        EditorGUI.indentLevel--;
        EditorGUILayout.Space(10);

        // ═══════════════════════════════════════════
        // УРОН
        // ═══════════════════════════════════════════
        showDamageSection = EditorGUILayout.BeginFoldoutHeaderGroup(showDamageSection, "💥 УРОН И СКЕЙЛИНГ");
        if (showDamageSection)
        {
            EditorGUI.indentLevel++;

            EditorGUILayout.PropertyField(serializedObject.FindProperty("baseDamage"), new GUIContent("Базовый урон"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("strengthScaling"), new GUIContent("Скейлинг от Strength"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("intelligenceScaling"), new GUIContent("Скейлинг от Intelligence"));

            // Пример расчета урона
            EditorGUILayout.Space(5);
            EditorGUILayout.HelpBox(
                $"Пример расчета:\n" +
                $"STR=10, INT=10\n" +
                $"Урон = {config.baseDamage} + (10×{config.strengthScaling}) + (10×{config.intelligenceScaling}) = " +
                $"{config.baseDamage + 10 * config.strengthScaling + 10 * config.intelligenceScaling}",
                MessageType.Info
            );

            EditorGUI.indentLevel--;
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
        EditorGUILayout.Space(5);

        // ═══════════════════════════════════════════
        // СКОРОСТЬ АТАКИ
        // ═══════════════════════════════════════════
        DrawSectionHeader("⚡ СКОРОСТЬ АТАКИ", new Color(1f, 0.9f, 0.4f));
        EditorGUI.indentLevel++;

        EditorGUILayout.PropertyField(serializedObject.FindProperty("attackCooldown"), new GUIContent("Кулдаун (сек)"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("attackRange"), new GUIContent("Дальность (м)"));

        // Информация о DPS
        float dps = config.baseDamage / config.attackCooldown;
        EditorGUILayout.LabelField("DPS (базовый):", $"{dps:F1}", EditorStyles.helpBox);

        EditorGUI.indentLevel--;
        EditorGUILayout.Space(10);

        // ═══════════════════════════════════════════
        // СНАРЯД (только для дальних атак)
        // ═══════════════════════════════════════════
        if (config.attackType == AttackType.Ranged)
        {
            showProjectileSection = EditorGUILayout.BeginFoldoutHeaderGroup(showProjectileSection, "🎯 НАСТРОЙКИ СНАРЯДА");
            if (showProjectileSection)
            {
                EditorGUI.indentLevel++;

                EditorGUILayout.PropertyField(serializedObject.FindProperty("projectilePrefab"), new GUIContent("Префаб снаряда"));

                // Предупреждение если префаб не назначен
                if (config.projectilePrefab == null)
                {
                    EditorGUILayout.HelpBox("⚠️ Для дальней атаки требуется prefab снаряда!", MessageType.Warning);
                }
                else
                {
                    // Показываем preview префаба
                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Preview:", GUILayout.Width(60));

                    Texture2D preview = AssetPreview.GetAssetPreview(config.projectilePrefab);
                    if (preview != null)
                    {
                        GUILayout.Label(preview, GUILayout.Width(64), GUILayout.Height(64));
                    }
                    else
                    {
                        GUILayout.Label("Нет preview", GUILayout.Width(64), GUILayout.Height(64));
                    }
                    GUILayout.EndHorizontal();

                    EditorGUILayout.Space(5);
                }

                EditorGUILayout.PropertyField(serializedObject.FindProperty("projectileSpeed"), new GUIContent("Скорость (м/с)"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("projectileLifetime"), new GUIContent("Время жизни (сек)"));

                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField("Автонаведение:", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("projectileHoming"), new GUIContent("Включить"));

                if (config.projectileHoming)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("homingSpeed"), new GUIContent("Скорость поворота"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("homingRadius"), new GUIContent("Радиус поиска цели"));
                    EditorGUI.indentLevel--;
                }

                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
            EditorGUILayout.Space(5);
        }

        // ═══════════════════════════════════════════
        // ВИЗУАЛЬНЫЕ ЭФФЕКТЫ
        // ═══════════════════════════════════════════
        showEffectsSection = EditorGUILayout.BeginFoldoutHeaderGroup(showEffectsSection, "✨ ВИЗУАЛЬНЫЕ ЭФФЕКТЫ");
        if (showEffectsSection)
        {
            EditorGUI.indentLevel++;

            EditorGUILayout.PropertyField(serializedObject.FindProperty("hitEffectPrefab"), new GUIContent("Эффект попадания"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("weaponEffectPrefab"), new GUIContent("Эффект на оружии"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("muzzleFlashPrefab"), new GUIContent("Вспышка выстрела"));

            EditorGUI.indentLevel--;
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
        EditorGUILayout.Space(5);

        // ═══════════════════════════════════════════
        // ЗВУКИ
        // ═══════════════════════════════════════════
        showAudioSection = EditorGUILayout.BeginFoldoutHeaderGroup(showAudioSection, "🔊 ЗВУКИ");
        if (showAudioSection)
        {
            EditorGUI.indentLevel++;

            EditorGUILayout.PropertyField(serializedObject.FindProperty("attackSound"), new GUIContent("Звук атаки"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("hitSound"), new GUIContent("Звук попадания"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("soundVolume"), new GUIContent("Громкость"));

            EditorGUI.indentLevel--;
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
        EditorGUILayout.Space(5);

        // ═══════════════════════════════════════════
        // АНИМАЦИЯ
        // ═══════════════════════════════════════════
        showAnimationSection = EditorGUILayout.BeginFoldoutHeaderGroup(showAnimationSection, "🎬 АНИМАЦИЯ");
        if (showAnimationSection)
        {
            EditorGUI.indentLevel++;

            EditorGUILayout.PropertyField(serializedObject.FindProperty("animationTrigger"), new GUIContent("Триггер анимации"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("animationSpeed"), new GUIContent("Скорость анимации"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("attackHitTiming"), new GUIContent("Момент удара (0-1)"));

            EditorGUI.indentLevel--;
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
        EditorGUILayout.Space(5);

        // ═══════════════════════════════════════════
        // РАСХОД РЕСУРСОВ
        // ═══════════════════════════════════════════
        DrawSectionHeader("💎 РАСХОД РЕСУРСОВ", new Color(0.5f, 1f, 0.5f));
        EditorGUI.indentLevel++;

        EditorGUILayout.PropertyField(serializedObject.FindProperty("manaCostPerAttack"), new GUIContent("Стоимость маны"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("actionPointsCost"), new GUIContent("Стоимость AP"));

        EditorGUI.indentLevel--;
        EditorGUILayout.Space(10);

        // ═══════════════════════════════════════════
        // ДОПОЛНИТЕЛЬНЫЕ ЭФФЕКТЫ
        // ═══════════════════════════════════════════
        DrawSectionHeader("🔥 ЭФФЕКТЫ ПРИ ПОПАДАНИИ", new Color(1f, 0.5f, 0.3f));
        EditorGUI.indentLevel++;

        EditorGUILayout.PropertyField(serializedObject.FindProperty("onHitEffects"), new GUIContent("Список эффектов"), true);

        if (config.onHitEffects.Count > 0)
        {
            EditorGUILayout.HelpBox($"Применяется {config.onHitEffects.Count} эффект(ов) при каждом попадании", MessageType.Info);
        }

        EditorGUI.indentLevel--;
        EditorGUILayout.Space(10);

        // ═══════════════════════════════════════════
        // РАСШИРЕННЫЕ НАСТРОЙКИ
        // ═══════════════════════════════════════════
        showAdvancedSection = EditorGUILayout.BeginFoldoutHeaderGroup(showAdvancedSection, "⚙️ РАСШИРЕННЫЕ НАСТРОЙКИ");
        if (showAdvancedSection)
        {
            EditorGUI.indentLevel++;

            EditorGUILayout.PropertyField(serializedObject.FindProperty("baseCritChance"), new GUIContent("Базовый шанс крита (%)"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("critMultiplier"), new GUIContent("Множитель крита"));

            EditorGUILayout.Space(5);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("piercingAttack"), new GUIContent("Пробивание насквозь"));

            if (config.piercingAttack)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(serializedObject.FindProperty("maxPierceTargets"), new GUIContent("Макс. целей"));
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Мультиплеер:", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("syncProjectiles"), new GUIContent("Синхронизация снарядов"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("syncHitEffects"), new GUIContent("Синхронизация эффектов"));

            EditorGUI.indentLevel--;
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
        EditorGUILayout.Space(10);

        // ═══════════════════════════════════════════
        // ВАЛИДАЦИЯ
        // ═══════════════════════════════════════════
        EditorGUILayout.Space(5);
        if (GUILayout.Button("🔍 ПРОВЕРИТЬ КОНФИГУРАЦИЮ", GUILayout.Height(30)))
        {
            ValidateConfig();
        }

        EditorGUILayout.Space(5);
        if (GUILayout.Button("📋 ПОКАЗАТЬ DEBUG INFO", GUILayout.Height(25)))
        {
            Debug.Log(config.GetDebugInfo());
            EditorUtility.DisplayDialog("Debug Info", "Информация выведена в Console", "OK");
        }

        EditorGUILayout.Space(10);

        serializedObject.ApplyModifiedProperties();
    }

    /// <summary>
    /// Нарисовать заголовок секции
    /// </summary>
    private void DrawSectionHeader(string title, Color color)
    {
        EditorGUILayout.Space(5);

        GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            normal = { textColor = color },
            fontSize = 12
        };

        EditorGUILayout.LabelField(title, headerStyle);

        // Линия под заголовком
        Rect rect = GUILayoutUtility.GetRect(1, 2);
        EditorGUI.DrawRect(rect, color * 0.5f);

        EditorGUILayout.Space(5);
    }

    /// <summary>
    /// Валидация конфигурации
    /// </summary>
    private void ValidateConfig()
    {
        string errorMessage;
        bool isValid = config.Validate(out errorMessage);

        if (isValid)
        {
            EditorUtility.DisplayDialog(
                "✅ Валидация пройдена",
                $"Конфигурация {config.characterClass} корректна!\n\nВсе необходимые параметры заполнены.",
                "OK"
            );
        }
        else
        {
            EditorUtility.DisplayDialog(
                "❌ Ошибка валидации",
                $"Найдены проблемы:\n\n{errorMessage}",
                "OK"
            );
        }
    }
}
