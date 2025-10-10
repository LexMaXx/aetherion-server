# 🔥 ЗАПУСТИТЬ МУЛЬТИПЛЕЕР СЕЙЧАС!

## ⚡ 3 команды → Готово!

### 1️⃣ Создать GitHub репозиторий

Зайдите на https://github.com/new и создайте:
- Name: **aetherion**
- Private (рекомендуется)
- БЕЗ README, .gitignore, license

### 2️⃣ Запушить код (выполнить в Git Bash)

```bash
cd /c/Users/Asus/Aetherion
git remote add origin https://github.com/ВАШ_USERNAME/aetherion.git
git push -u origin main
```

**ВАЖНО**: Замените `ВАШ_USERNAME` на ваш GitHub username!

### 3️⃣ Обновить Render

1. Зайти: https://dashboard.render.com
2. Найти сервис: **aetherion-server-gv5u**
3. Settings → Build & Deploy
4. Connect Repository → выбрать ваш GitHub репозиторий **aetherion**
5. Root Directory: `SERVER_CODE` (ВАЖНО!)
6. Build Command: `npm install`
7. Start Command: `npm start`
8. Save Changes
9. Manual Deploy → Deploy latest commit

Render задеплоит обновленный код за 2-5 минут.

---

## 🎮 Настроить Unity (1 минута)

1. Открыть Unity → GameScene
2. Menu: **Tools → Aetherion → Setup Multiplayer Managers**
3. Server URL уже правильный: `https://aetherion-server-gv5u.onrender.com`
4. Нажать **"Setup NetworkManagers"**
5. Сохранить сцену (Ctrl+S)

✅ ГОТОВО!

---

## 🧪 Тест

1. Play в Unity
2. Войти в аккаунт
3. Нажать "В бой"

**Ожидаемый результат** (в Console):
```
[GameScene] Поиск доступных комнат...
[RoomManager] ✅ Комната создана
[WebSocket] ✅ Подключено!
[ArenaManager] 🌐 MULTIPLAYER MODE
[NetworkSync] ✅ Подписан на сетевые события
```

---

## ❓ Проблемы?

### GitHub: "Permission denied (publickey)"
Используйте HTTPS вместо SSH:
```bash
git remote set-url origin https://github.com/USERNAME/aetherion.git
```

### Render: "Deploy failed"
Проверьте:
- Root Directory = `SERVER_CODE` ✓
- Build Command = `npm install` ✓
- Start Command = `npm start` ✓

### Unity: "NetworkManagers not found"
- Tools → Aetherion → Setup Multiplayer Managers
- Нажмите "Setup NetworkManagers"
- Сохраните сцену

---

## 📞 Важные ссылки

- Ваш сервер: https://aetherion-server-gv5u.onrender.com
- Health check: https://aetherion-server-gv5u.onrender.com/health
- Render Dashboard: https://dashboard.render.com
- GitHub: https://github.com

---

## ✅ Checklist

- [ ] GitHub репозиторий создан
- [ ] `git remote add origin` выполнен
- [ ] `git push -u origin main` выполнен
- [ ] Render подключен к GitHub
- [ ] Root Directory = `SERVER_CODE`
- [ ] Manual Deploy нажат
- [ ] Unity: NetworkManagers созданы
- [ ] Unity: Сцена сохранена
- [ ] Тест: игра запущена и работает

---

**На всё про всё: 5-10 минут** ⏱️

**Вперёд! 🚀**
