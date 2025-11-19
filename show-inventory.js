/**
 * Скрипт для просмотра инвентаря персонажей
 * Полезен для отладки
 */

require('dotenv').config();
const mongoose = require('mongoose');
const Character = require('./models/Character');

async function showInventory() {
  try {
    // Подключаемся к MongoDB
    await mongoose.connect(process.env.MONGODB_URI);
    console.log('✅ Подключено к MongoDB');

    // Получаем все персонажи
    const characters = await Character.find({});

    console.log(`\n📊 Всего персонажей: ${characters.length}\n`);

    for (const character of characters) {
      console.log(`═══════════════════════════════════════════════════════`);
      console.log(`🎮 Персонаж: ${character.characterClass}`);
      console.log(`   User ID: ${character.userId}`);
      console.log(`   Level: ${character.level}`);
      console.log(`   Gold: ${character.gold}`);
      console.log(`   Предметов в инвентаре: ${character.inventory?.length || 0}`);

      if (character.inventory && character.inventory.length > 0) {
        console.log(`\n   📦 Инвентарь:`);
        character.inventory.forEach((item, index) => {
          console.log(`      [${index}] ${item.itemName || 'Unknown'} x${item.quantity || 1}`);
          console.log(`          ID: ${item.itemId || 'EMPTY'}`);
          console.log(`          Slot: ${item.slotIndex ?? 'UNDEFINED'}`);
          console.log(`          Timestamp: ${item.timestamp ? new Date(item.timestamp).toISOString() : 'N/A'}`);
        });
      }
      console.log('');
    }

    console.log('═══════════════════════════════════════════════════════');

  } catch (error) {
    console.error('❌ Ошибка:', error);
  } finally {
    await mongoose.disconnect();
    console.log('👋 Отключено от MongoDB');
  }
}

showInventory();
