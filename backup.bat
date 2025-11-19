@echo off
echo ====================================
echo Aetherion Project Backup Script
echo ====================================
echo.

REM Установите путь к вашему внешнему диску или сетевой папке
REM Текущая настройка: C:\Backups\Aetherion (измените если нужно)
set BACKUP_DIR=C:\Backups\Aetherion

REM Создаём папку с датой
set DATE=%date:~-4%-%date:~3,2%-%date:~0,2%
set BACKUP_PATH=%BACKUP_DIR%\%DATE%

echo Создаём папку для бэкапа: %BACKUP_PATH%
mkdir "%BACKUP_PATH%" 2>nul

echo.
echo Копируем важные файлы...
echo.

REM Копируем только важное (без Library, Temp, и т.д.)
xcopy /E /I /Y "Assets\Scripts" "%BACKUP_PATH%\Assets\Scripts\"
xcopy /E /I /Y "Assets\Scenes" "%BACKUP_PATH%\Assets\Scenes\"
xcopy /E /I /Y "Assets\Resources" "%BACKUP_PATH%\Assets\Resources\"
xcopy /E /I /Y "Assets\Editor" "%BACKUP_PATH%\Assets\Editor\"
xcopy /E /I /Y "ProjectSettings" "%BACKUP_PATH%\ProjectSettings\"

REM Копируем серверный код
xcopy /E /I /Y "config" "%BACKUP_PATH%\config\"
xcopy /E /I /Y "models" "%BACKUP_PATH%\models\"
xcopy /E /I /Y "routes" "%BACKUP_PATH%\routes\"
xcopy /E /I /Y "controllers" "%BACKUP_PATH%\controllers\"
xcopy /Y "*.js" "%BACKUP_PATH%\"
xcopy /Y "*.json" "%BACKUP_PATH%\"

echo.
echo ====================================
echo Бэкап завершён!
echo Расположение: %BACKUP_PATH%
echo ====================================
pause
