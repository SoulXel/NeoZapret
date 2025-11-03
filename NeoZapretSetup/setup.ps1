# PowerShell скрипт установки NeoZapret
# Требует прав администратора

param(
    [string]$InstallDir = "$env:ProgramFiles\NeoZapret"
)

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  УСТАНОВКА NEOZAPRET" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Проверка прав администратора
$isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
if (-not $isAdmin) {
    Write-Host "ОШИБКА: Требуются права администратора!" -ForegroundColor Red
    Write-Host ""
    Write-Host "Нажмите правой кнопкой на этот файл и выберите"
    Write-Host "'Запуск от имени администратора'"
    Write-Host ""
    Read-Host "Нажмите Enter для выхода"
    exit 1
}

Write-Host "Установка в: $InstallDir" -ForegroundColor Yellow
Write-Host ""

# Получаем путь к директории скрипта
$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
$parentPath = Split-Path -Parent $scriptPath

# Создание директорий
if (-not (Test-Path $InstallDir)) {
    New-Item -ItemType Directory -Path $InstallDir | Out-Null
}
New-Item -ItemType Directory -Path "$InstallDir\bin" -Force | Out-Null
New-Item -ItemType Directory -Path "$InstallDir\lists" -Force | Out-Null

# Копирование файлов
Write-Host "Копирование файлов..." -ForegroundColor Green

try {
    # Приложение
    if (Test-Path "$parentPath\NeoZapret\bin\Release\net472\NeoZapret.exe") {
        Copy-Item "$parentPath\NeoZapret\bin\Release\net472\NeoZapret.exe" "$InstallDir\" -Force
        Write-Host "  ✓ NeoZapret.exe" -ForegroundColor Green
    } else {
        Write-Host "  ✗ NeoZapret.exe не найден!" -ForegroundColor Red
        Write-Host "    Сначала запустите build.bat" -ForegroundColor Yellow
        Read-Host "Нажмите Enter для выхода"
        exit 1
    }

    # bin файлы
    if (Test-Path "$parentPath\bin") {
        Copy-Item "$parentPath\bin\*" "$InstallDir\bin\" -Recurse -Force
        Write-Host "  ✓ bin\*" -ForegroundColor Green
    }

    # lists файлы
    if (Test-Path "$parentPath\lists") {
        Copy-Item "$parentPath\lists\*" "$InstallDir\lists\" -Recurse -Force
        Write-Host "  ✓ lists\*" -ForegroundColor Green
    }

    # Документация
    if (Test-Path "$parentPath\README.md") {
        Copy-Item "$parentPath\README.md" "$InstallDir\README.txt" -Force
    }

} catch {
    Write-Host "Ошибка копирования файлов: $_" -ForegroundColor Red
    Read-Host "Нажмите Enter для выхода"
    exit 1
}

# Создание ярлыка в меню Пуск
Write-Host ""
Write-Host "Создание ярлыка..." -ForegroundColor Green
try {
    $WshShell = New-Object -comObject WScript.Shell
    $Shortcut = $WshShell.CreateShortcut("$env:APPDATA\Microsoft\Windows\Start Menu\Programs\NeoZapret.lnk")
    $Shortcut.TargetPath = "$InstallDir\NeoZapret.exe"
    $Shortcut.WorkingDirectory = $InstallDir
    $Shortcut.Description = "Обход блокировок РФ 2025"
    $Shortcut.Save()
    Write-Host "  ✓ Ярлык создан" -ForegroundColor Green
} catch {
    Write-Host "  ⚠ Не удалось создать ярлык" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  УСТАНОВКА ЗАВЕРШЕНА!" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "NeoZapret установлен в: $InstallDir" -ForegroundColor Green
Write-Host ""

# Запуск приложения
$choice = Read-Host "Запустить приложение сейчас? (Y/N)"
if ($choice -eq "Y" -or $choice -eq "y") {
    Start-Process "$InstallDir\NeoZapret.exe"
}

Write-Host ""
Write-Host "Готово! Можно закрыть это окно." -ForegroundColor Green
Start-Sleep -Seconds 3

