# 🚀 Быстрый деплой на Render.com

## Шаг 1: Создать GitHub репозиторий

1. Зайдите на [github.com](https://github.com)
2. Нажмите "New repository"
3. Название: `aetherion-server` (или любое)
4. Visibility: **Private** (рекомендуется) или Public
5. НЕ добавляйте README, .gitignore или license (у нас уже есть)
6. Нажмите "Create repository"

## Шаг 2: Подключить локальный репозиторий к GitHub

GitHub покажет вам команды. Выполните:

```bash
cd C:\Users\Asus\Aetherion
git remote add origin https://github.com/ВАШ_USERNAME/aetherion-server.git
git branch -M main
git push -u origin main
```

**Важно**: Замените `ВАШ_USERNAME` на ваш реальный GitHub username!

## Шаг 3: Создать Web Service на Render

1. Зайдите на [render.com](https://render.com)
2. Нажмите "New +" → **Web Service**
3. Подключите GitHub:
   - Нажмите "Connect GitHub"
   - Авторизуйтесь
   - Выберите репозиторий `aetherion-server`

4. Заполните настройки:

```
Name: aetherion-server
Region: Frankfurt (EU Central)
Branch: main
Root Directory: SERVER_CODE
Runtime: Node
Build Command: npm install
Start Command: npm start
```

5. Instance Type:
   - **Free** (для тестирования, засыпает через 15 мин)
   - **Starter** ($7/мес, рекомендуется для продакшена)

## Шаг 4: Environment Variables

В разделе **Environment Variables** добавьте:

### MONGODB_URI
Ваш connection string из MongoDB Atlas. Формат:
```
mongodb+srv://username:password@cluster.mongodb.net/aetherion?retryWrites=true&w=majority
```

**Где взять**:
1. Зайдите на [cloud.mongodb.com](https://cloud.mongodb.com)
2. Clusters → Connect → Connect your application
3. Скопируйте connection string
4. Замените `<password>` на ваш реальный пароль

### JWT_SECRET
Любая случайная строка (секретный ключ). Например:
```
aetherion_super_secret_key_2024_change_me
```

### NODE_ENV
```
production
```

### PORT
```
3000
```

## Шаг 5: Deploy!

Нажмите **"Create Web Service"**

Render начнет деплой:
1. Clone репозитория
2. `npm install` (установка зависимостей)
3. `npm start` (запуск сервера)

Это займет 2-5 минут.

## Шаг 6: Проверка

После деплоя Render даст вам URL:
```
https://aetherion-server-xxxx.onrender.com
```

Проверьте:
```bash
curl https://aetherion-server-xxxx.onrender.com/health
```

Ожидаемый ответ:
```json
{
  "status": "healthy",
  "uptime": 12.345,
  "mongodb": "connected",
  "timestamp": 1234567890
}
```

## Шаг 7: Обновить Unity

Скопируйте ваш Render URL и вставьте в Unity скрипты:

**RoomManager.cs** (строка ~12):
```csharp
[SerializeField] private string serverUrl = "https://aetherion-server-xxxx.onrender.com";
```

**WebSocketClient.cs** (строка ~17):
```csharp
[SerializeField] private string serverUrl = "https://aetherion-server-xxxx.onrender.com";
```

**ApiClient.cs** (если есть):
```csharp
private string baseURL = "https://aetherion-server-xxxx.onrender.com";
```

## ✅ Готово!

Теперь ваш сервер работает на Render и доступен из Unity!

## 🔄 Обновление сервера

Когда вы меняете серверный код:

```bash
cd C:\Users\Asus\Aetherion
git add SERVER_CODE/
git commit -m "Update server"
git push
```

Render автоматически пересоберет и задеплоит новую версию!

## 🐛 Проблемы?

### "Deploy failed: npm install error"
- Проверьте `package.json` на ошибки
- Убедитесь что Root Directory = `SERVER_CODE`

### "MongoDB connection failed"
- MongoDB Atlas → Network Access → Add IP `0.0.0.0/0`
- Проверьте что MONGODB_URI правильный

### "Health check shows mongodb: 'disconnected'"
- Проверьте Environment Variables в Render
- Убедитесь что пароль в MONGODB_URI не содержит специальных символов

### "Server sleeping (Free tier)"
- Free tier засыпает через 15 минут
- При первом запросе просыпается (30-60 сек)
- Решение: перейти на Starter plan ($7/мес)

---

**Удачи с деплоем! 🚀**
