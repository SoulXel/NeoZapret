@echo off
chcp 65001 >nul
echo ========================================
echo   ЗАПУСК УСТАНОВКИ NEOZAPRET
echo ========================================
echo.

:: Проверка прав администратора
net session >nul 2>&1
if %errorlevel% neq 0 (
    echo Требуются права администратора!
    echo Запускаю от имени администратора...
    timeout /t 2 >nul
    powershell -NoProfile -ExecutionPolicy Bypass -Command "Start-Process -FilePath '%~f0' -Verb RunAs"
    exit /b
)

:: Запуск PowerShell скрипта установки
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0setup.ps1"

pause

