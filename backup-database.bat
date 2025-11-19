@echo off
echo ====================================
echo MongoDB Database Backup Script
echo ====================================
echo.

REM Установите ваш MongoDB URI (БЕЗ пароля в скрипте!)
REM Лучше создайте переменную окружения MONGODB_URI

if "%MONGODB_URI%"=="" (
    echo ОШИБКА: Установите переменную окружения MONGODB_URI
    echo Пример: set MONGODB_URI=mongodb+srv://user:pass@cluster.mongodb.net/aetherion
    pause
    exit /b 1
)

REM Папка для бэкапов
set BACKUP_DIR=D:\Backups\MongoDB
set DATE=%date:~-4%-%date:~3,2%-%date:~0,2%_%time:~0,2%-%time:~3,2%
set BACKUP_PATH=%BACKUP_DIR%\%DATE%

mkdir "%BACKUP_PATH%" 2>nul

echo Бэкапим базу данных в: %BACKUP_PATH%
echo.

REM Нужно установить mongodump: npm install -g mongodb-database-tools
mongodump --uri="%MONGODB_URI%" --out="%BACKUP_PATH%"

if %ERRORLEVEL% EQU 0 (
    echo.
    echo ====================================
    echo Бэкап базы данных успешно создан!
    echo Расположение: %BACKUP_PATH%
    echo ====================================
) else (
    echo.
    echo ОШИБКА при создании бэкапа!
    echo Убедитесь что установлен mongodump
)

pause
