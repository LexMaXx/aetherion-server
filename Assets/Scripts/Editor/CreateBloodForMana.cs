using UnityEngine;
using UnityEditor;

/// <summary>
/// Editor script для создания Blood for Mana - четвёртый скилл Rogue (Necromancer)
/// Жертвенное заклинание: жертвует 20% HP для восстановления 20% маны
/// Не требует ману для использования!
/// </summary>
public class CreateBloodForMana : MonoBehaviour
{
    [MenuItem("Aetherion/Skills/Rogue/Create Blood for Mana")]
    public static void CreateSkill()
    {
        // ════════════════════════════════════════════════════════════
        // ОСНОВНЫЕ ПАРАМЕТРЫ
        // ════════════════════════════════════════════════════════════

        SkillConfig skill = ScriptableObject.CreateInstance<SkillConfig>();

        skill.skillId = 504;
        skill.skillName = "Blood for Mana";
        skill.skillType = SkillConfigType.Buff; // Buff на себя (жертвенный)
        skill.targetType = SkillTargetType.Self;
        skill.characterClass = CharacterClass.Rogue;

        // ════════════════════════════════════════════════════════════
        // ЖЕРТВЕННАЯ МЕХАНИКА
        // ════════════════════════════════════════════════════════════

        // Урон себе: -20% HP (отрицательное значение = процент от MaxHP)
        skill.baseDamageOrHeal = -20f;       // -20 = жертвуем 20% HP

        // ВАЖНО: Не требует ману!
        skill.manaCost = 0f;                 // 0 маны (жертвенное заклинание)

        // ════════════════════════════════════════════════════════════
        // РЕСУРСЫ
        // ════════════════════════════════════════════════════════════

        skill.cooldown = 15f;                // 15 секунд кулдаун (нельзя спамить)

        // ════════════════════════════════════════════════════════════
        // КАСТОМНЫЕ ДАННЫЕ (для специальной механики)
        // ════════════════════════════════════════════════════════════

        // Используем customData для хранения процента восстановления маны
        skill.customData = new SkillCustomData();
        skill.customData.manaRestorePercent = 20f; // 20% маны восстанавливается

        // ════════════════════════════════════════════════════════════
        // ВИЗУАЛЬНЫЕ ЭФФЕКТЫ
        // ════════════════════════════════════════════════════════════

        // Cast Effect: Кровавая жертва (тёмно-красный эффект)
        skill.castEffectPrefab = Resources.Load<GameObject>("Effects/CFXR3 Fire Explosion B");

        // Caster Effect: Эффект восстановления маны на кастере
        skill.casterEffectPrefab = Resources.Load<GameObject>("Effects/CFXR3 Magic Poof");

        // ════════════════════════════════════════════════════════════
        // ОПИСАНИЕ
        // ════════════════════════════════════════════════════════════

        skill.description = "Жертвенное заклинание некроманта. Жертвует 20% своего HP для восстановления 20% маны.\n\n" +
                           "Стоимость: 20% текущего HP\n" +
                           "Восстановление: 20% маны\n" +
                           "Mana Cost: 0 (не требует ману!)\n" +
                           "Cooldown: 15 сек\n\n" +
                           "⚠️ Используй осторожно на низком HP!";

        // ════════════════════════════════════════════════════════════
        // СОХРАНЕНИЕ ASSET
        // ════════════════════════════════════════════════════════════

        string path = "Assets/Resources/Skills/Rogue_BloodForMana.asset";
        AssetDatabase.CreateAsset(skill, path);
        AssetDatabase.SaveAssets();

        Debug.Log($"✅ Blood for Mana создан: {path}");
        Debug.Log($"🩸 Жертва: 20% HP");
        Debug.Log($"💙 Восстановление: 20% маны");
        Debug.Log($"🔮 Cooldown: {skill.cooldown}с, Mana: {skill.manaCost} (БЕСПЛАТНО!)");

        EditorUtility.FocusProjectWindow();
        Selection.activeObject = skill;
    }
}
