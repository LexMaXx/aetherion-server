# ⚡ БЫСТРЫЕ КОМАНДЫ ДЛЯ PUSH

## 🎯 РЕКОМЕНДУЕМЫЙ СПОСОБ (копируй все команды сразу)

Откройте **PowerShell** или **Git Bash** и вставьте эти команды:

```bash
cd C:\Users\Asus\Aetherion
git branch backup-all-commits
git reset --soft HEAD~3
git add Assets/Scripts/Skills/SkillManager.cs Assets/Scripts/Network/SocketIOManager.cs Assets/Scripts/Network/NetworkSyncManager.cs Assets/Scripts/Player/Projectile.cs Assets/Scripts/Skills/ActiveEffect.cs SERVER_UPDATE_INSTRUCTIONS.md SERVER_VISUAL_EFFECTS_FIX.md
git commit -m "FEAT: Добавлена анимация каста и синхронизация визуальных эффектов скиллов

1. Анимация каста для всех скиллов
2. Полная синхронизация визуальных эффектов в мультиплеере
3. Исправлены ошибки компиляции

🤖 Generated with Claude Code"
git push origin main
```

---

## ✅ ЧТО ЭТО СДЕЛАЕТ:

1. **Создаст резервную ветку** `backup-all-commits` (на случай если что-то пойдет не так)
2. **Отменит 3 последних коммита** (но изменения останутся!)
3. **Добавит только важные файлы кода** (7 файлов вместо 2720)
4. **Создаст новый коммит** с понятным описанием
5. **Запушит на GitHub** (быстро, без ошибок HTTP 500)

---

## 📊 РЕЗУЛЬТАТ:

Вместо **3 коммитов с 2720 файлами** → **1 коммит с 7 файлами**

**Файлы которые будут запушены:**
- ✅ Assets/Scripts/Skills/SkillManager.cs
- ✅ Assets/Scripts/Network/SocketIOManager.cs
- ✅ Assets/Scripts/Network/NetworkSyncManager.cs
- ✅ Assets/Scripts/Player/Projectile.cs
- ✅ Assets/Scripts/Skills/ActiveEffect.cs
- ✅ SERVER_UPDATE_INSTRUCTIONS.md
- ✅ SERVER_VISUAL_EFFECTS_FIX.md

---

## 🔄 ЕСЛИ ХОТИТЕ ВЕРНУТЬ ОБРАТНО:

```bash
git checkout backup-all-commits
git branch -D main
git checkout -b main
```

---

## 🚀 ПОСЛЕ PUSH:

1. Откройте ваш **серверный репозиторий** (Node.js)
2. Добавьте код из файла **SERVER_VISUAL_EFFECTS_FIX.md**
3. Задеплойте сервер на Render.com
4. Протестируйте в игре!
