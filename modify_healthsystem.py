import sys
import time

# Wait a bit to ensure file is not locked
time.sleep(1)

file_path = r'c:/Users/Asus/Aetherion/Assets/Scripts/Player/HealthSystem.cs'

# Read the file
with open(file_path, 'r', encoding='utf-8') as f:
    content = f.read()

# Define the old and new strings
old_string = """    public void TakeDamage(float damage)
    {
        if (!IsAlive) return;

        // Применяем снижение урона
        float originalDamage = damage;"""

new_string = """    public void TakeDamage(float damage)
    {
        if (!IsAlive) return;

        // Проверяем неуязвимость
        EffectManager effectManager = GetComponent<EffectManager>();
        if (effectManager != null && effectManager.HasInvulnerability())
        {
            Debug.Log($"[HealthSystem] 🛡️ НЕУЯЗВИМОСТЬ! Урон {damage:F0} заблокирован");
            return;
        }

        // Применяем снижение урона
        float originalDamage = damage;"""

# Check if the string exists
if old_string in content:
    # Replace
    content = content.replace(old_string, new_string)

    # Write back
    with open(file_path, 'w', encoding='utf-8') as f:
        f.write(content)

    print("✅ Successfully added invulnerability check to TakeDamage method")
else:
    print("❌ Could not find the expected code pattern")
    print("File might have already been modified or has different formatting")
    sys.exit(1)
