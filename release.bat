@echo off
setlocal EnableDelayedExpansion
chcp 65001 >nul

echo ========================================
echo   ПОДГОТОВКА РЕЛИЗА NEOZAPRET
echo ========================================
echo.

set "RELEASE_DIR=NeoZapret-Release"
set "EXE_DIR=NeoZapret\bin\Release\net472"

:: Проверка наличия собранного файла
if not exist "%EXE_DIR%\NeoZapret.exe" (
    echo ОШИБКА: NeoZapret.exe не найден!
    echo.
    echo Сначала запустите build.bat для сборки
    echo.
    pause
    exit /b 1
)

echo Создаю директорию релиза...
if exist "%RELEASE_DIR%" (
    rd /s /q "%RELEASE_DIR%"
)
mkdir "%RELEASE_DIR%"
mkdir "%RELEASE_DIR%\bin"
mkdir "%RELEASE_DIR%\lists"

echo Копирую файлы...

:: Exe
copy "%EXE_DIR%\NeoZapret.exe" "%RELEASE_DIR%\" >nul

:: Bin
xcopy "bin\*.*" "%RELEASE_DIR%\bin\" /E /I /Y /Q >nul 2>&1

:: Lists
xcopy "lists\*.*" "%RELEASE_DIR%\lists\" /E /I /Y /Q >nul 2>&1

:: README
copy "NeoZapret\README.md" "%RELEASE_DIR%\README.txt" >nul

echo.
echo ========================================
echo   РЕЛИЗ ГОТОВ!
echo ========================================
echo.
echo Директория: %RELEASE_DIR%\
echo.
echo Содержимое:
echo - NeoZapret.exe
echo - bin\ (все необходимые файлы)
echo - lists\ (все списки доменов)
echo - README.txt
echo.
echo Можно архивировать папку %RELEASE_DIR% и распространять!
echo.
pause

