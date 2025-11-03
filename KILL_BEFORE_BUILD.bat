@echo off
echo Остановка всех процессов NeoZapret...
taskkill /F /IM NeoZapret.exe 2>nul
taskkill /F /IM winws.exe 2>nul
timeout /t 2 /nobreak >nul
echo Процессы остановлены. Можно запускать сборку.
pause


