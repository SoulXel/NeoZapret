@echo off
setlocal EnableDelayedExpansion
chcp 65001 >nul

echo ========================================
echo   СОЗДАНИЕ EXE УСТАНОВЩИКА
echo ========================================
echo.

set "EXE_DIR=NeoZapret\bin\Release\net472"

:: Проверка сборки приложения
if not exist "%EXE_DIR%\NeoZapret.exe" (
    echo ОШИБКА: Сначала соберите приложение!
    echo Запустите: build.bat
    echo.
    pause
    exit /b 1
)

:: Проверка наличия Inno Setup
set "INNO_PATH="
if exist "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" (
    set "INNO_PATH=C:\Program Files (x86)\Inno Setup 6\ISCC.exe"
) else if exist "C:\Program Files\Inno Setup 6\ISCC.exe" (
    set "INNO_PATH=C:\Program Files\Inno Setup 6\ISCC.exe"
) else if exist "C:\Program Files (x86)\Inno Setup 5\ISCC.exe" (
    set "INNO_PATH=C:\Program Files (x86)\Inno Setup 5\ISCC.exe"
)

if defined INNO_PATH (
    echo Создание установщика через Inno Setup...
    echo Путь: %INNO_PATH%
    echo.
    call "%INNO_PATH%" "NeoZapretSetup\setup_innosetup.iss"
    
    if exist "NeoZapretSetup\NeoZapret-Setup.exe" (
        echo.
        echo ========================================
        echo   ✓ УСТАНОВЩИК СОЗДАН!
        echo ========================================
        echo.
        echo Файл: NeoZapretSetup\NeoZapret-Setup.exe
        echo.
        echo Этот файл можно распространять!
        echo.
        goto :end
    )
)

:: Если Inno Setup не найден
echo.
echo Inno Setup не найден!
echo.
echo Вы можете:
echo   1. Установить Inno Setup с http://www.jrsoftware.org
echo   2. Использовать PowerShell установщик
echo.
echo Создание альтернативного установщика...
call create_installer.bat

echo.
echo Можно распространять папку NeoZapret-Installer-Temp\
echo и файл setup.bat для ручной установки
echo.

:end
pause

