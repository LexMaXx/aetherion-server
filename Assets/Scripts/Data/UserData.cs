using System;
using UnityEngine;

/// <summary>
/// Класс данных пользователя для сериализации в JSON
/// Используется для отправки на сервер и получения ответа
/// </summary>
[Serializable]
public class UserData
{
    public string username;
    public string email;
    public string password;
}

/// <summary>
/// Ответ сервера при регистрации/логине
/// </summary>
[Serializable]
public class AuthResponse
{
    public bool success;
    public string message;
    public string token; // JWT токен
    public UserInfo user;
}

/// <summary>
/// Информация о пользователе
/// </summary>
[Serializable]
public class UserInfo
{
    public string id;
    public string username;
    public string email;
    public int level;
    public int experience;
    public int gold; // Добавлено поле золота
    public string createdAt;
}

/// <summary>
/// Запрос на логин
/// </summary>
[Serializable]
public class LoginRequest
{
    public string username;
    public string password;
}

/// <summary>
/// Запрос на регистрацию
/// </summary>
[Serializable]
public class RegisterRequest
{
    public string username;
    public string email;
    public string password;
}
