const jwt = require('jsonwebtoken');

module.exports = function(req, res, next) {
  // Получаем токен из заголовка
  const token = req.header('Authorization');

  // Проверка наличия токена
  if (!token || !token.startsWith('Bearer ')) {
    return res.status(401).json({
      success: false,
      message: 'Нет токена, доступ запрещен'
    });
  }

  try {
    // Извлекаем токен из "Bearer TOKEN"
    const actualToken = token.split(' ')[1];

    // Верификация токена
    const decoded = jwt.verify(actualToken, process.env.JWT_SECRET);

    // Добавляем пользователя в запрос
    req.user = decoded.user;
    next();

  } catch (error) {
    res.status(401).json({
      success: false,
      message: 'Токен недействителен'
    });
  }
};
