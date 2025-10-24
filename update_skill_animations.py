#!/usr/bin/env python3
"""
Скрипт для изменения animationTrigger всех скиллов на MageAttack
"""

import os
import re

# Путь к папке со скиллами
SKILLS_PATH = r"c:\Users\Asus\Aetherion\Assets\Resources\Skills"

# Список скиллов которые нужно изменить (ВСЕ скиллы с projectilePrefab)
SKILLS_TO_UPDATE = [
    "Mage_Fireball.asset",
    "Mage_IceNova.asset",  # Уже изменён
    "Mage_LightningStorm.asset",  # Уже изменён
    "Mage_Meteor.asset",
    "Paladin_HammerofJustice.asset",  # Уже изменён
]

def update_skill_animation(filepath):
    """Изменить animationTrigger на MageAttack"""
    with open(filepath, 'r', encoding='utf-8') as f:
        content = f.read()

    # Проверяем есть ли у скилла projectilePrefab (не пустой)
    if 'projectilePrefab: {fileID: 0}' in content:
        print(f"⏭️  Пропуск {os.path.basename(filepath)} - нет снаряда")
        return False

    # Проверяем текущий animationTrigger
    current_trigger = re.search(r'animationTrigger:\s*(\w*)', content)
    if current_trigger:
        trigger_value = current_trigger.group(1)
        if trigger_value == "MageAttack":
            print(f"✅ {os.path.basename(filepath)} - уже использует MageAttack")
            return False

        # Заменяем animationTrigger
        content = re.sub(
            r'animationTrigger:\s*\w*',
            'animationTrigger: MageAttack',
            content
        )

        # Если нет castTime, добавляем значение по умолчанию 0.5
        if 'castTime: 0\n' in content or 'castTime: 0.0\n' in content:
            content = re.sub(
                r'castTime: 0(?:\.0)?\n',
                'castTime: 0.5\n',
                content
            )

        with open(filepath, 'w', encoding='utf-8') as f:
            f.write(content)

        print(f"✏️  {os.path.basename(filepath)} - изменён на MageAttack (было: {trigger_value})")
        return True

    return False

def main():
    print("🔧 Обновление animationTrigger для скиллов...")
    print(f"📂 Путь: {SKILLS_PATH}\n")

    updated_count = 0

    # Обрабатываем все .asset файлы
    for filename in os.listdir(SKILLS_PATH):
        if not filename.endswith('.asset'):
            continue

        filepath = os.path.join(SKILLS_PATH, filename)
        if update_skill_animation(filepath):
            updated_count += 1

    print(f"\n✅ Обновлено скиллов: {updated_count}")

if __name__ == "__main__":
    main()
