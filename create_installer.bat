@echo off
setlocal EnableDelayedExpansion
chcp 65001 >nul

echo ========================================
echo   СОЗДАНИЕ УСТАНОВЩИКА NEOZAPRET
echo ========================================
echo.

set "INSTALLER_DIR=NeoZapret-Installer-Temp"
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

echo Подготовка файлов для установщика...
echo.

:: Очистка и создание временной директории
if exist "%INSTALLER_DIR%" (
    rd /s /q "%INSTALLER_DIR%"
)
mkdir "%INSTALLER_DIR%"
mkdir "%INSTALLER_DIR%\bin"
mkdir "%INSTALLER_DIR%\lists"

:: Копирование файлов
echo Копирование приложения...
copy "%EXE_DIR%\NeoZapret.exe" "%INSTALLER_DIR%\" >nul

echo Копирование файлов bin\...
xcopy "bin\*.*" "%INSTALLER_DIR%\bin\" /E /I /Y /Q >nul 2>&1

echo Копирование списков...
xcopy "lists\*.*" "%INSTALLER_DIR%\lists\" /E /I /Y /Q >nul 2>&1

echo Копирование документации...
copy "README.md" "%INSTALLER_DIR%\README.txt" >nul 2>&1
copy "NeoZapret\README.md" "%INSTALLER_DIR%\README-GUI.txt" >nul 2>&1

:: Создание setup.iss для Inno Setup (если установлен)
if exist "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" (
    echo.
    echo Создание установщика через Inno Setup...
    call "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" "setup_innosetup.iss"
    goto :done
)

:: Создание batch-установщика
echo.
echo Создание batch-установщика...
call :create_batch_installer

:done
echo.
echo ========================================
echo   УСТАНОВЩИК ГОТОВ!
echo ========================================
echo.
echo Файлы находятся в: %INSTALLER_DIR%\
echo.
pause
exit /b 0

:create_batch_installer
set "setup_content=@echo off
setlocal EnableDelayedExpansion
chcp 65001 ^>nul

echo ========================================
echo   УСТАНОВКА NEOZAPRET
echo ========================================
echo.

:: Проверка прав администратора
net session ^>nul 2^>^&1
if %%errorlevel%% neq 0 (
    echo ОШИБКА: Требуются права администратора!
    echo.
    echo Нажмите правой кнопкой на этот файл и выберите
    echo \"Запуск от имени администратора\"
    echo.
    pause
    exit /b 1
)

set \"INSTALL_DIR=%%ProgramFiles%%\\NeoZapret\"
set \"STARTMENU_DIR=%%APPDATA%%\\Microsoft\\Windows\\Start Menu\\Programs\"

echo Установка в: %%INSTALL_DIR%%
echo.

:: Создание директорий
if not exist \"%%INSTALL_DIR%%\" mkdir \"%%INSTALL_DIR%%\"
if not exist \"%%INSTALL_DIR%%\\bin\" mkdir \"%%INSTALL_DIR%%\\bin\"
if not exist \"%%INSTALL_DIR%%\\lists\" mkdir \"%%INSTALL_DIR%%\\lists\"

:: Копирование файлов
echo Копирование файлов...
copy \"NeoZapret.exe\" \"%%INSTALL_DIR%%\\\" ^>nul
xcopy \"bin\\*.*\" \"%%INSTALL_DIR%%\\bin\\\" /E /I /Y /Q ^>nul 2^>^&1
xcopy \"lists\\*.*\" \"%%INSTALL_DIR%%\\lists\\\" /E /I /Y /Q ^>nul 2^>^&1
copy \"README.txt\" \"%%INSTALL_DIR%%\\\" ^>nul 2^>^&1

:: Создание ярлыка
echo Создание ярлыка...
set \"SHORTCUT_PATH=%%STARTMENU_DIR%%\\NeoZapret.lnk\"
powershell -Command \"$WshShell = New-Object -comObject WScript.Shell; $Shortcut = $WshShell.CreateShortcut('%%SHORTCUT_PATH%%'); $Shortcut.TargetPath = '%%INSTALL_DIR%%\\NeoZapret.exe'; $Shortcut.WorkingDirectory = '%%INSTALL_DIR%%'; $Shortcut.Description = 'Обход блокировок РФ 2025'; $Shortcut.Save()\" ^>nul 2^>^&1

echo.
echo ========================================
echo   УСТАНОВКА ЗАВЕРШЕНА!
echo ========================================
echo.
echo NeoZapret установлен в: %%INSTALL_DIR%%
echo.
echo Запустить приложение сейчас? (Y/N)
set /p choice=
if /i \"%%choice%%\"==\"Y\" (
    start \"\" \"%%INSTALL_DIR%%\\NeoZapret.exe\"
)

echo.
echo Готово! Можно закрыть это окно.
echo.
timeout /t 5 ^>nul
exit /b 0"

:: Создание batch-файла установщика
echo !setup_content! > "%INSTALLER_DIR%\setup.bat"

:: Создание архивного установщика
echo Создание SFX архива (если доступен 7-Zip)...
if exist "C:\Program Files\7-Zip\7z.exe" (
    cd "%INSTALLER_DIR%"
    "C:\Program Files\7-Zip\7z.exe" a -sfx -t7z -mx=9 "..\NeoZapret-Setup.exe" *.* >nul 2>&1
    cd ..
    
    if exist "NeoZapret-Setup.exe" (
        echo.
        echo ✓ SFX установщик создан: NeoZapret-Setup.exe
        echo.
    )
)

goto :eof

