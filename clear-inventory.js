/**
 * Скрипт для очистки инвентаря персонажа
 * Используется для отладки системы инвентаря
 */

require('dotenv').config();
const mongoose = require('mongoose');
const Character = require('./models/Character');

async function clearInventory() {
  try {
    // Подключаемся к MongoDB
    await mongoose.connect(process.env.MONGODB_URI);
    console.log('✅ Подключено к MongoDB');

    // Получаем все персонажи с инвентарём
    const characters = await Character.find({ 'inventory.0': { $exists: true } });

    console.log(`📊 Найдено персонажей с инвентарём: ${characters.length}`);

    for (const character of characters) {
      console.log(`\n🔍 Персонаж: ${character.characterClass} (userId: ${character.userId})`);
      console.log(`   Инвентарь: ${character.inventory.length} предметов`);

      // Очищаем инвентарь
      character.inventory = [];
      await character.save();

      console.log(`   ✅ Инвентарь очищен`);
    }

    console.log('\n✅ Готово! Все инвентари очищены.');

  } catch (error) {
    console.error('❌ Ошибка:', error);
  } finally {
    await mongoose.disconnect();
    console.log('👋 Отключено от MongoDB');
  }
}

clearInventory();
