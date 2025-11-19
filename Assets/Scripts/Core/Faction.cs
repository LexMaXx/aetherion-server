using UnityEngine;

/// <summary>
/// Фракция юнита (как в MMO RPG)
/// </summary>
public enum Faction
{
    /// <summary>
    /// Нейтральные юниты (не атакуют первыми, можно атаковать)
    /// </summary>
    Neutral = 0,

    /// <summary>
    /// Игрок (локальный игрок - НЕЛЬЗЯ таргетить самого себя!)
    /// </summary>
    Player = 1,

    /// <summary>
    /// Враги (можно атаковать, нельзя хилить)
    /// </summary>
    Enemy = 2,

    /// <summary>
    /// Союзники (можно хилить/баффать, нельзя атаковать)
    /// </summary>
    Ally = 3,

    /// <summary>
    /// Другие игроки (в PvP - враги, в PvE - союзники)
    /// ВАЖНО: В мультиплеере другие игроки = Enemy для таргетинга атак!
    /// </summary>
    OtherPlayer = 4
}

/// <summary>
/// Утилиты для работы с фракциями
/// </summary>
public static class FactionHelper
{
    /// <summary>
    /// Может ли атакующий атаковать цель?
    /// </summary>
    public static bool CanAttack(Faction attacker, Faction target)
    {
        // Нельзя атаковать самого себя (Player)
        if (attacker == Faction.Player && target == Faction.Player)
            return false;

        // Нельзя атаковать союзников
        if (attacker == Faction.Player && target == Faction.Ally)
            return false;

        // Можно атаковать врагов
        if (attacker == Faction.Player && target == Faction.Enemy)
            return true;

        // Можно атаковать других игроков (PvP)
        if (attacker == Faction.Player && target == Faction.OtherPlayer)
            return true;

        // Можно атаковать нейтральных
        if (attacker == Faction.Player && target == Faction.Neutral)
            return true;

        return false;
    }

    /// <summary>
    /// Может ли кастер использовать heal/buff на цель?
    /// </summary>
    public static bool CanSupport(Faction caster, Faction target)
    {
        // Можно хилить/баффать самого себя
        if (caster == Faction.Player && target == Faction.Player)
            return true;

        // Можно хилить/баффать союзников
        if (caster == Faction.Player && target == Faction.Ally)
            return true;

        // НЕЛЬЗЯ хилить врагов
        if (caster == Faction.Player && target == Faction.Enemy)
            return false;

        // НЕЛЬЗЯ хилить других игроков (в PvP режиме)
        // TODO: В будущем можно добавить режим "Party" где можно хилить других игроков
        if (caster == Faction.Player && target == Faction.OtherPlayer)
            return false;

        // НЕЛЬЗЯ хилить нейтральных
        if (caster == Faction.Player && target == Faction.Neutral)
            return false;

        return false;
    }

    /// <summary>
    /// Получить цвет для фракции (для UI/индикаторов)
    /// </summary>
    public static Color GetFactionColor(Faction faction)
    {
        switch (faction)
        {
            case Faction.Player:
                return Color.green; // Зеленый - свой персонаж

            case Faction.Ally:
                return Color.cyan; // Голубой - союзники

            case Faction.Enemy:
                return Color.red; // Красный - враги

            case Faction.OtherPlayer:
                return new Color(1f, 0.5f, 0f); // Оранжевый - другие игроки (PvP)

            case Faction.Neutral:
                return Color.yellow; // Желтый - нейтральные

            default:
                return Color.white;
        }
    }

    /// <summary>
    /// Получить название фракции на русском
    /// </summary>
    public static string GetFactionName(Faction faction)
    {
        switch (faction)
        {
            case Faction.Player:
                return "Игрок";
            case Faction.Ally:
                return "Союзник";
            case Faction.Enemy:
                return "Враг";
            case Faction.OtherPlayer:
                return "Игрок";
            case Faction.Neutral:
                return "Нейтральный";
            default:
                return "Неизвестно";
        }
    }
}
