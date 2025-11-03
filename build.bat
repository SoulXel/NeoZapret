@echo off
chcp 65001 >nul
echo ========================================
echo   СБОРКА NEOZAPRET GUI
echo ========================================
echo.

:: Проверка наличия MSBuild
where msbuild >nul 2>&1
if %errorlevel% neq 0 (
    echo ========================================
    echo   MSBuild не найден!
    echo ========================================
    echo.
    echo ⚠️ Требуется Visual Studio для сборки NeoZapret
    echo.
    echo Инструкция по установке:
    echo.
    echo 1. Скачайте Visual Studio Community (бесплатно)
    echo    https://visualstudio.microsoft.com/downloads/
    echo.
    echo 2. Установите ".NET desktop development"
    echo.
    echo 3. Запустите build.bat снова
    echo.
    echo ========================================
    echo.
    pause
    exit /b 1
)

echo Найден MSBuild, начинаю сборку...
echo.

:: Сборка Release версии
msbuild NeoZapret.sln /p:Configuration=Release /p:Platform="Any CPU" /t:Rebuild

if %errorlevel% equ 0 (
    echo.
    echo ========================================
    echo   СБОРКА ЗАВЕРШЕНА УСПЕШНО!
    echo ========================================
    echo.
    echo Файлы находятся в:
    echo NeoZapret\bin\Release\net472\NeoZapret.exe
    echo.
    echo Для распространения скопируйте:
    echo - NeoZapret.exe
    echo - bin\ (из исходного проекта)
    echo - lists\ (из исходного проекта)
    echo.
) else (
    echo.
    echo ========================================
    echo   ОШИБКА СБОРКИ!
    echo ========================================
    echo.
    echo Проверьте ошибки выше
    echo.
)

pause

