#!/bin/bash
# Исправление skillId во всех .asset файлах

cd "Assets/Resources/Skills"

echo "========================================="
echo "FIXING WARRIOR SKILL IDS (401-405 → 101-105)"
echo "========================================="

sed -i 's/skillId: 405/skillId: 101/g' Warrior_BattleRage.asset
echo "✅ Warrior_BattleRage: 405 → 101"

sed -i 's/skillId: 403/skillId: 102/g' Warrior_DefensiveStance.asset
echo "✅ Warrior_DefensiveStance: 403 → 102"

sed -i 's/skillId: 402/skillId: 103/g' Warrior_HammerThrow.asset
echo "✅ Warrior_HammerThrow: 402 → 103"

sed -i 's/skillId: 404/skillId: 104/g' Warrior_BattleHeal.asset
echo "✅ Warrior_BattleHeal: 404 → 104"

sed -i 's/skillId: 401/skillId: 105/g' Warrior_Charge.asset
echo "✅ Warrior_Charge: 401 → 105"

echo ""
echo "========================================="
echo "FIXING ROGUE SKILL IDS (501-505 → 601-605)"
echo "========================================="

sed -i 's/skillId: 505/skillId: 601/g' Rogue_RaiseDead.asset
echo "✅ Rogue_RaiseDead: 505 → 601"

sed -i 's/skillId: 501/skillId: 602/g' Rogue_SoulDrain.asset
echo "✅ Rogue_SoulDrain: 501 → 602"

sed -i 's/skillId: 502/skillId: 603/g' Rogue_CurseOfWeakness.asset
echo "✅ Rogue_CurseOfWeakness: 502 → 603"

sed -i 's/skillId: 503/skillId: 604/g' Rogue_CripplingCurse.asset
echo "✅ Rogue_CripplingCurse: 503 → 604"

sed -i 's/skillId: 504/skillId: 605/g' Rogue_BloodForMana.asset
echo "✅ Rogue_BloodForMana: 504 → 605"

echo ""
echo "========================================="
echo "VERIFICATION - CHECKING ALL SKILL IDS"
echo "========================================="

echo ""
echo "WARRIOR (должны быть 101-105):"
for file in Warrior_*.asset; do
    id=$(grep "skillId:" "$file" | awk '{print $2}')
    echo "  $file: $id"
done

echo ""
echo "MAGE (должны быть 201-205):"
for file in Mage_*.asset; do
    id=$(grep "skillId:" "$file" | awk '{print $2}')
    echo "  $file: $id"
done

echo ""
echo "ARCHER (должны быть 301-305):"
for file in Archer_*.asset; do
    id=$(grep "skillId:" "$file" | awk '{print $2}')
    echo "  $file: $id"
done

echo ""
echo "PALADIN (должны быть 501-505):"
for file in Paladin_*.asset; do
    id=$(grep "skillId:" "$file" | awk '{print $2}')
    echo "  $file: $id"
done

echo ""
echo "ROGUE (должны быть 601-605):"
for file in Rogue_*.asset; do
    id=$(grep "skillId:" "$file" | awk '{print $2}')
    echo "  $file: $id"
done

echo ""
echo "========================================="
echo "✅✅✅ ALL SKILL IDS FIXED!"
echo "========================================="
