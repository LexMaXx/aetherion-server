using System.Text.RegularExpressions;
using UnityEngine;

/// <summary>
/// Валидатор ввода данных для регистрации и логина
/// Проверяет корректность username, email, password
/// </summary>
public static class InputValidator
{
    // Минимальные и максимальные длины
    public const int MIN_USERNAME_LENGTH = 3;
    public const int MAX_USERNAME_LENGTH = 20;
    public const int MIN_PASSWORD_LENGTH = 6;
    public const int MAX_PASSWORD_LENGTH = 50;

    // Regex паттерны
    private static readonly Regex emailRegex = new Regex(
        @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$",
        RegexOptions.Compiled
    );

    private static readonly Regex usernameRegex = new Regex(
        @"^[a-zA-Z0-9_]+$",
        RegexOptions.Compiled
    );

    /// <summary>
    /// Валидация username
    /// </summary>
    public static ValidationResult ValidateUsername(string username)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            return new ValidationResult(false, "Имя пользователя не может быть пустым");
        }

        if (username.Length < MIN_USERNAME_LENGTH)
        {
            return new ValidationResult(false, $"Имя пользователя должно быть минимум {MIN_USERNAME_LENGTH} символа");
        }

        if (username.Length > MAX_USERNAME_LENGTH)
        {
            return new ValidationResult(false, $"Имя пользователя не должно превышать {MAX_USERNAME_LENGTH} символов");
        }

        if (!usernameRegex.IsMatch(username))
        {
            return new ValidationResult(false, "Имя пользователя может содержать только буквы, цифры и нижнее подчеркивание");
        }

        return new ValidationResult(true, "Username валиден");
    }

    /// <summary>
    /// Валидация email
    /// </summary>
    public static ValidationResult ValidateEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return new ValidationResult(false, "Email не может быть пустым");
        }

        if (!emailRegex.IsMatch(email))
        {
            return new ValidationResult(false, "Некорректный формат email");
        }

        return new ValidationResult(true, "Email валиден");
    }

    /// <summary>
    /// Валидация password
    /// </summary>
    public static ValidationResult ValidatePassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            return new ValidationResult(false, "Пароль не может быть пустым");
        }

        if (password.Length < MIN_PASSWORD_LENGTH)
        {
            return new ValidationResult(false, $"Пароль должен быть минимум {MIN_PASSWORD_LENGTH} символов");
        }

        if (password.Length > MAX_PASSWORD_LENGTH)
        {
            return new ValidationResult(false, $"Пароль не должен превышать {MAX_PASSWORD_LENGTH} символов");
        }

        // Проверка на наличие хотя бы одной цифры
        if (!Regex.IsMatch(password, @"\d"))
        {
            return new ValidationResult(false, "Пароль должен содержать хотя бы одну цифру");
        }

        // Проверка на наличие хотя бы одной буквы
        if (!Regex.IsMatch(password, @"[a-zA-Z]"))
        {
            return new ValidationResult(false, "Пароль должен содержать хотя бы одну букву");
        }

        return new ValidationResult(true, "Пароль валиден");
    }

    /// <summary>
    /// Проверка совпадения паролей
    /// </summary>
    public static ValidationResult ValidatePasswordMatch(string password, string confirmPassword)
    {
        if (password != confirmPassword)
        {
            return new ValidationResult(false, "Пароли не совпадают");
        }

        return new ValidationResult(true, "Пароли совпадают");
    }

    /// <summary>
    /// Полная валидация для регистрации
    /// </summary>
    public static ValidationResult ValidateRegistration(string username, string email, string password)
    {
        var usernameResult = ValidateUsername(username);
        if (!usernameResult.isValid)
            return usernameResult;

        var emailResult = ValidateEmail(email);
        if (!emailResult.isValid)
            return emailResult;

        var passwordResult = ValidatePassword(password);
        if (!passwordResult.isValid)
            return passwordResult;

        return new ValidationResult(true, "Все данные валидны");
    }

    /// <summary>
    /// Полная валидация для логина
    /// </summary>
    public static ValidationResult ValidateLogin(string username, string password)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            return new ValidationResult(false, "Имя пользователя не может быть пустым");
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            return new ValidationResult(false, "Пароль не может быть пустым");
        }

        return new ValidationResult(true, "Данные для входа валидны");
    }
}

/// <summary>
/// Результат валидации
/// </summary>
public struct ValidationResult
{
    public bool isValid;
    public string message;

    public ValidationResult(bool isValid, string message)
    {
        this.isValid = isValid;
        this.message = message;
    }
}
