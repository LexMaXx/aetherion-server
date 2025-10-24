#!/usr/bin/env python3
"""
Исправление скорости Ethereal Skull снаряда
Уменьшает baseSpeed и accelerationRate для более плавного полета
"""

import re

prefab_path = r"c:\Users\Asus\Aetherion\Assets\Prefabs\Projectiles\Ethereal_Skull_1020210937_texture.prefab"

print("🔧 Исправление скорости Ethereal Skull...")

# Читаем файл
with open(prefab_path, 'r', encoding='utf-8') as f:
    content = f.read()

# Сохраняем оригинал для сравнения
original_content = content

# Заменяем параметры
# baseSpeed: 15 → 12
content = re.sub(r'baseSpeed: 15', 'baseSpeed: 12', content)
print("✅ baseSpeed: 15 → 12")

# homingSpeed: 8 → 6
content = re.sub(r'homingSpeed: 8', 'homingSpeed: 6', content)
print("✅ homingSpeed: 8 → 6")

# accelerationRate: 1.5 → 1.0 (убираем ускорение)
content = re.sub(r'accelerationRate: 1\.5', 'accelerationRate: 1.0', content)
print("✅ accelerationRate: 1.5 → 1.0")

# Проверяем что изменения были сделаны
if content == original_content:
    print("❌ Никаких изменений не было сделано!")
    exit(1)

# Записываем обратно
with open(prefab_path, 'w', encoding='utf-8') as f:
    f.write(content)

print("💀 Готово! Скорость снаряда исправлена:")
print("   - Базовая скорость: 12 м/с (было 15)")
print("   - Скорость наведения: 6 (было 8)")
print("   - Ускорение отключено: 1.0 (было 1.5)")
print("\n🎯 Теперь череп летит плавнее и медленнее!")
