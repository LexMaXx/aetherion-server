using UnityEngine;
using UnityEditor;

/// <summary>
/// Custom Editor для PlayerAttack - удобная настройка атаки
/// </summary>
[CustomEditor(typeof(PlayerAttack))]
public class PlayerAttackEditor : Editor
{
    // Serialized Properties
    private SerializedProperty attackCooldown;
    private SerializedProperty attackRange;
    private SerializedProperty attackDamage;
    private SerializedProperty attackRotationOffset;
    private SerializedProperty projectilePrefab;
    private SerializedProperty isRangedAttack;
    private SerializedProperty projectileSpeed;
    private SerializedProperty weaponTipTransform;
    private SerializedProperty attackAnimationSpeed;
    private SerializedProperty attackHitTiming;

    private void OnEnable()
    {
        // Привязываем поля
        attackCooldown = serializedObject.FindProperty("attackCooldown");
        attackRange = serializedObject.FindProperty("attackRange");
        attackDamage = serializedObject.FindProperty("attackDamage");
        attackRotationOffset = serializedObject.FindProperty("attackRotationOffset");
        projectilePrefab = serializedObject.FindProperty("projectilePrefab");
        isRangedAttack = serializedObject.FindProperty("isRangedAttack");
        projectileSpeed = serializedObject.FindProperty("projectileSpeed");
        weaponTipTransform = serializedObject.FindProperty("weaponTipTransform");
        attackAnimationSpeed = serializedObject.FindProperty("attackAnimationSpeed");
        attackHitTiming = serializedObject.FindProperty("attackHitTiming");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // Заголовок
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("⚔️ НАСТРОЙКА АТАКИ", EditorStyles.boldLabel);
        EditorGUILayout.Space(5);

        // ===== ОСНОВНЫЕ ПАРАМЕТРЫ =====
        DrawHeader("Основные параметры");
        EditorGUILayout.PropertyField(attackDamage, new GUIContent("💥 Урон"));
        EditorGUILayout.PropertyField(attackCooldown, new GUIContent("⏱️ Кулдаун (сек)"));
        EditorGUILayout.PropertyField(attackRange, new GUIContent("📏 Дальность (м)"));
        EditorGUILayout.Space(10);

        // ===== АНИМАЦИЯ =====
        DrawHeader("⚡ Настройка анимации");

        // Скорость анимации с подсказкой
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PropertyField(attackAnimationSpeed, new GUIContent("🎬 Скорость анимации"));
        if (attackAnimationSpeed.floatValue != 1.0f)
        {
            GUILayout.Label($"({attackAnimationSpeed.floatValue}x)", EditorStyles.miniLabel);
        }
        EditorGUILayout.EndHorizontal();

        // Слайдер скорости с пресетами
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("0.5x", GUILayout.Width(45))) attackAnimationSpeed.floatValue = 0.5f;
        if (GUILayout.Button("1x", GUILayout.Width(45))) attackAnimationSpeed.floatValue = 1.0f;
        if (GUILayout.Button("2x", GUILayout.Width(45))) attackAnimationSpeed.floatValue = 2.0f;
        if (GUILayout.Button("3x", GUILayout.Width(45))) attackAnimationSpeed.floatValue = 3.0f;
        if (GUILayout.Button("5x", GUILayout.Width(45))) attackAnimationSpeed.floatValue = 5.0f;
        if (GUILayout.Button("10x", GUILayout.Width(45))) attackAnimationSpeed.floatValue = 10.0f;
        if (GUILayout.Button("20x", GUILayout.Width(45))) attackAnimationSpeed.floatValue = 20.0f;
        EditorGUILayout.EndHorizontal();

        // Дополнительная строка с экстремальными скоростями
        if (attackAnimationSpeed.floatValue >= 10.0f)
        {
            EditorGUILayout.HelpBox($"⚡ ОЧЕНЬ БЫСТРО! Анимация будет проигрываться в {attackAnimationSpeed.floatValue} раз быстрее", MessageType.Warning);
        }

        EditorGUILayout.Space(5);

        // Момент выстрела/удара
        EditorGUILayout.PropertyField(attackHitTiming, new GUIContent("🎯 Момент выстрела/удара"));

        // Визуальный индикатор
        DrawTimingBar(attackHitTiming.floatValue);

        // Кнопки пресетов для тайминга
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Начало (20%)", GUILayout.Width(100))) attackHitTiming.floatValue = 0.2f;
        if (GUILayout.Button("Середина (50%)", GUILayout.Width(120))) attackHitTiming.floatValue = 0.5f;
        if (GUILayout.Button("Конец (80%)", GUILayout.Width(100))) attackHitTiming.floatValue = 0.8f;
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(10);

        // ===== СНАРЯДЫ =====
        DrawHeader("🎯 Настройка снарядов");
        EditorGUILayout.PropertyField(isRangedAttack, new GUIContent("🏹 Дальняя атака?"));

        if (isRangedAttack.boolValue)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(projectilePrefab, new GUIContent("Префаб снаряда"));
            EditorGUILayout.PropertyField(projectileSpeed, new GUIContent("Скорость снаряда"));
            EditorGUILayout.PropertyField(weaponTipTransform, new GUIContent("Точка спавна (WeaponTip)"));
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space(10);

        // ===== ПОВОРОТ =====
        DrawHeader("🔄 Настройка поворота");
        EditorGUILayout.PropertyField(attackRotationOffset, new GUIContent("📐 Смещение поворота (°)"));

        // Подсказка
        if (attackRotationOffset.floatValue != 0)
        {
            EditorGUILayout.HelpBox($"Персонаж повернётся на {attackRotationOffset.floatValue}° при атаке", MessageType.Info);
        }

        EditorGUILayout.Space(10);

        // ===== БЫСТРЫЕ ПРЕСЕТЫ =====
        DrawHeader("⚡ Быстрые пресеты");

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("⚔️ Воин"))
        {
            SetWarriorPreset();
        }
        if (GUILayout.Button("🏹 Лучник"))
        {
            SetArcherPreset();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("🔮 Маг"))
        {
            SetMagePreset();
        }
        if (GUILayout.Button("💀 Разбойник"))
        {
            SetRoguePreset();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(10);

        // ===== ИНФОРМАЦИЯ =====
        DrawInfo();

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawHeader(string text)
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField(text, EditorStyles.boldLabel);
        EditorGUILayout.EndVertical();
    }

    private void DrawTimingBar(float timing)
    {
        EditorGUILayout.BeginHorizontal();

        Rect rect = EditorGUILayout.GetControlRect(GUILayout.Height(20));

        // Фон
        EditorGUI.DrawRect(rect, new Color(0.2f, 0.2f, 0.2f));

        // Заполнение до момента удара
        Rect fillRect = new Rect(rect.x, rect.y, rect.width * timing, rect.height);
        EditorGUI.DrawRect(fillRect, new Color(0.3f, 0.7f, 0.3f));

        // Маркер момента удара
        Rect markerRect = new Rect(rect.x + rect.width * timing - 2, rect.y, 4, rect.height);
        EditorGUI.DrawRect(markerRect, Color.yellow);

        // Текст
        GUIStyle style = new GUIStyle(EditorStyles.label);
        style.alignment = TextAnchor.MiddleCenter;
        style.normal.textColor = Color.white;
        EditorGUI.LabelField(rect, $"{timing * 100:F0}% анимации", style);

        EditorGUILayout.EndHorizontal();
    }

    private void DrawInfo()
    {
        EditorGUILayout.HelpBox(
            "💡 Подсказки:\n\n" +
            "• Скорость анимации - множитель скорости (3x = в 3 раза быстрее)\n" +
            "• Момент выстрела - когда создать снаряд (0.5 = в середине, 0.8 = ближе к концу)\n" +
            "• Дальняя атака - создаёт снаряд вместо мгновенного урона\n" +
            "• Смещение поворота - компенсация для анимаций с боковым ударом",
            MessageType.Info
        );
    }

    // ===== ПРЕСЕТЫ =====

    private void SetWarriorPreset()
    {
        attackDamage.floatValue = 30f;
        attackRange.floatValue = 3f;
        attackCooldown.floatValue = 1.0f;
        attackRotationOffset.floatValue = 45f;
        isRangedAttack.boolValue = false;
        attackAnimationSpeed.floatValue = 1.0f;
        attackHitTiming.floatValue = 0.7f;
        Debug.Log("✅ Применён пресет Воина");
    }

    private void SetArcherPreset()
    {
        attackDamage.floatValue = 35f;
        attackRange.floatValue = 50f;
        attackCooldown.floatValue = 1.2f;
        attackRotationOffset.floatValue = 0f;
        isRangedAttack.boolValue = true;
        attackAnimationSpeed.floatValue = 1.0f;
        attackHitTiming.floatValue = 0.5f; // Выстрел в середине (натяжение лука)
        projectileSpeed.floatValue = 30f;
        Debug.Log("✅ Применён пресет Лучника");
    }

    private void SetMagePreset()
    {
        attackDamage.floatValue = 40f;
        attackRange.floatValue = 20f;
        attackCooldown.floatValue = 0.8f;
        attackRotationOffset.floatValue = 0f;
        isRangedAttack.boolValue = true;
        attackAnimationSpeed.floatValue = 3.0f; // Быстрая атака!
        attackHitTiming.floatValue = 0.4f; // Рано - быстрый взмах
        projectileSpeed.floatValue = 20f;
        Debug.Log("✅ Применён пресет Мага");
    }

    private void SetRoguePreset()
    {
        attackDamage.floatValue = 50f;
        attackRange.floatValue = 20f;
        attackCooldown.floatValue = 1.0f;
        attackRotationOffset.floatValue = -30f;
        isRangedAttack.boolValue = true;
        attackAnimationSpeed.floatValue = 1.0f;
        attackHitTiming.floatValue = 0.6f;
        projectileSpeed.floatValue = 15f;
        Debug.Log("✅ Применён пресет Разбойника");
    }
}
