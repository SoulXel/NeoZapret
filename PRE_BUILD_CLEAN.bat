@echo off
echo ========================================
echo Очистка перед сборкой NeoZapret
echo ========================================
echo.

echo [1/3] Остановка процессов NeoZapret...
taskkill /F /IM NeoZapret.exe 2>nul
if %errorlevel% equ 0 (
    echo ✓ Процессы NeoZapret остановлены
) else (
    echo ○ Процессы NeoZapret не запущены
)

echo.
echo [2/3] Остановка процессов winws...
taskkill /F /IM winws.exe 2>nul
if %errorlevel% equ 0 (
    echo ✓ Процессы winws остановлены
) else (
    echo ○ Процессы winws не запущены
)

echo.
echo [3/3] Ожидание освобождения файлов...
timeout /t 2 /nobreak >nul

echo.
echo ========================================
echo ✓ Очистка завершена!
echo Теперь можно запускать сборку проекта
echo ========================================
pause


