# Скрипт для настройки Git и публикации на GitHub
# Выполните этот скрипт в PowerShell (от имени администратора, если нужно)

Write-Host "=== Настройка Git и публикация NeoZapret на GitHub ===" -ForegroundColor Cyan
Write-Host ""

# Проверка наличия Git
try {
    $gitVersion = git --version
    Write-Host "✓ Git найден: $gitVersion" -ForegroundColor Green
} catch {
    Write-Host "✗ Git не найден!" -ForegroundColor Red
    Write-Host ""
    Write-Host "Установите Git с https://git-scm.com/download/win" -ForegroundColor Yellow
    Write-Host "После установки перезапустите PowerShell и запустите этот скрипт снова." -ForegroundColor Yellow
    pause
    exit
}

Write-Host ""
Write-Host "Шаг 1: Настройка пользователя Git" -ForegroundColor Cyan
Write-Host ""

# Настройка имени пользователя (замените на ваше имя)
$gitName = Read-Host "Введите ваше имя для Git (или нажмите Enter для 'SoulXel')"
if ([string]::IsNullOrWhiteSpace($gitName)) {
    $gitName = "SoulXel"
}
git config user.name $gitName
Write-Host "✓ Имя пользователя установлено: $gitName" -ForegroundColor Green

# Настройка email (замените на ваш email)
$gitEmail = Read-Host "Введите ваш email для Git (или нажмите Enter для 'soulxel@example.com')"
if ([string]::IsNullOrWhiteSpace($gitEmail)) {
    $gitEmail = "soulxel@example.com"
}
git config user.email $gitEmail
Write-Host "✓ Email установлен: $gitEmail" -ForegroundColor Green

Write-Host ""
Write-Host "Шаг 2: Инициализация репозитория (если еще не сделано)" -ForegroundColor Cyan
if (!(Test-Path .git)) {
    git init
    Write-Host "✓ Git репозиторий инициализирован" -ForegroundColor Green
} else {
    Write-Host "✓ Git репозиторий уже инициализирован" -ForegroundColor Green
}

Write-Host ""
Write-Host "Шаг 3: Добавление файлов" -ForegroundColor Cyan
git add .
Write-Host "✓ Файлы добавлены" -ForegroundColor Green

Write-Host ""
Write-Host "Шаг 4: Создание первого коммита" -ForegroundColor Cyan
git commit -m "Initial commit: NeoZapret v3.1.0 - Production ready"
Write-Host "✓ Коммит создан" -ForegroundColor Green

Write-Host ""
Write-Host "Шаг 5: Подключение к GitHub" -ForegroundColor Cyan
Write-Host ""
Write-Host "ВАЖНО: Сначала создайте репозиторий на GitHub!" -ForegroundColor Yellow
Write-Host "1. Откройте https://github.com" -ForegroundColor Yellow
Write-Host "2. Нажмите '+' → 'New repository'" -ForegroundColor Yellow
Write-Host "3. Название: NeoZapret" -ForegroundColor Yellow
Write-Host "4. Выберите Public" -ForegroundColor Yellow
Write-Host "5. НЕ добавляйте README, .gitignore или LICENSE" -ForegroundColor Yellow
Write-Host "6. Нажмите 'Create repository'" -ForegroundColor Yellow
Write-Host ""
$githubUsername = Read-Host "Введите ваш GitHub username"
$repoUrl = "https://github.com/$githubUsername/NeoZapret.git"

# Проверка, не добавлен ли уже remote
$existingRemote = git remote get-url origin 2>$null
if ($existingRemote) {
    Write-Host "Удаленный репозиторий уже настроен: $existingRemote" -ForegroundColor Yellow
    $replace = Read-Host "Заменить на новый? (y/n)"
    if ($replace -eq "y" -or $replace -eq "Y") {
        git remote remove origin
        git remote add origin $repoUrl
        Write-Host "✓ Удаленный репозиторий обновлен: $repoUrl" -ForegroundColor Green
    }
} else {
    git remote add origin $repoUrl
    Write-Host "✓ Удаленный репозиторий добавлен: $repoUrl" -ForegroundColor Green
}

Write-Host ""
Write-Host "Шаг 6: Переименование ветки в main" -ForegroundColor Cyan
git branch -M main
Write-Host "✓ Ветка переименована в main" -ForegroundColor Green

Write-Host ""
Write-Host "Шаг 7: Загрузка на GitHub" -ForegroundColor Cyan
Write-Host ""
Write-Host "ПРИМЕЧАНИЕ: При запросе пароля используйте Personal Access Token!" -ForegroundColor Yellow
Write-Host "Как создать токен:" -ForegroundColor Yellow
Write-Host "1. GitHub → Settings → Developer settings" -ForegroundColor Yellow
Write-Host "2. Personal access tokens → Tokens (classic)" -ForegroundColor Yellow
Write-Host "3. Generate new token (classic)" -ForegroundColor Yellow
Write-Host "4. Выберите scope: repo (полный доступ)" -ForegroundColor Yellow
Write-Host "5. Generate и скопируйте токен" -ForegroundColor Yellow
Write-Host ""
Read-Host "Нажмите Enter когда будете готовы загрузить код на GitHub"

git push -u origin main

Write-Host ""
Write-Host "=== Готово! ===" -ForegroundColor Green
Write-Host ""
Write-Host "Проект должен быть загружен на GitHub!" -ForegroundColor Cyan
Write-Host "Откройте https://github.com/$githubUsername/NeoZapret чтобы проверить" -ForegroundColor Cyan
Write-Host ""
pause

