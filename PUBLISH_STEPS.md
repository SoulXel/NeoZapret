# 🚀 Пошаговая инструкция по публикации на GitHub

## Быстрая инструкция (для опытных пользователей Git)

```bash
# 1. Инициализация (если еще не сделано)
cd C:\Users\xeldi\Desktop\NeoZapret
git init

# 2. Добавление файлов
git add .

# 3. Первый коммит
git commit -m "Initial commit: NeoZapret v3.1.0"

# 4. Добавление удаленного репозитория (замените USERNAME)
git remote add origin https://github.com/USERNAME/NeoZapret.git

# 5. Переименование ветки в main
git branch -M main

# 6. Загрузка на GitHub
git push -u origin main
```

---

## Подробная инструкция для начинающих

### Шаг 1: Установка Git (если еще не установлен)

1. Скачайте Git с https://git-scm.com/download/win
2. Установите с настройками по умолчанию
3. Перезапустите терминал

### Шаг 2: Регистрация на GitHub

1. Перейдите на https://github.com
2. Нажмите **Sign up**
3. Заполните форму и подтвердите email

### Шаг 3: Создание репозитория на GitHub

1. Войдите в GitHub
2. Нажмите **"+"** → **"New repository"**
3. Название: `NeoZapret`
4. Описание: `DPI Bypass Tool - Обход блокировок РФ 2025`
5. Выберите **Public**
6. ❌ НЕ отмечайте "Initialize with README"
7. Нажмите **"Create repository"**

### Шаг 4: Подготовка локального проекта

Откройте **PowerShell** или **Git Bash**:

```powershell
# Перейдите в папку проекта
cd C:\Users\xeldi\Desktop\NeoZapret

# Проверьте, что Git установлен
git --version
```

### Шаг 5: Инициализация Git репозитория

```bash
# Инициализация (если еще не сделано)
git init

# Проверка статуса
git status
```

### Шаг 6: Добавление файлов

```bash
# Добавить все файлы (кроме игнорируемых в .gitignore)
git add .

# Проверить, что добавлено
git status
```

### Шаг 7: Создание первого коммита

```bash
git commit -m "Initial commit: NeoZapret v3.1.0 - Optimized for production"
```

### Шаг 8: Подключение к GitHub

```bash
# Добавить удаленный репозиторий (замените YOUR_USERNAME на ваш username)
git remote add origin https://github.com/YOUR_USERNAME/NeoZapret.git

# Проверить подключение
git remote -v
```

### Шаг 9: Переименование ветки

```bash
# Современный стандарт - использовать 'main' вместо 'master'
git branch -M main
```

### Шаг 10: Загрузка на GitHub

```bash
# Загрузить код на GitHub
git push -u origin main
```

**Внимание**: При первом push Git запросит авторизацию:
- Введите ваш GitHub username
- Введите пароль (или Personal Access Token, если включена 2FA)

---

## Если возникли проблемы

### Проблема: "Authentication failed"

**Решение**: Используйте Personal Access Token вместо пароля:
1. GitHub → Settings → Developer settings → Personal access tokens → Tokens (classic)
2. Generate new token
3. Выберите scope: `repo`
4. Скопируйте token и используйте его как пароль

### Проблема: "Permission denied"

**Решение**: Проверьте:
1. Правильность URL репозитория
2. Права доступа к репозиторию
3. Что репозиторий существует на GitHub

### Проблема: "Large files"

**Решение**: 
- Убедитесь, что `.gitignore` правильно настроен
- Не загружайте бинарные файлы (они должны быть в `.gitignore`)

---

## Проверка результата

После успешной загрузки:

1. Откройте ваш репозиторий на GitHub
2. Проверьте, что все файлы загружены
3. Убедитесь, что README.md отображается корректно

---

## Следующие шаги

### Создание Release

1. GitHub → Releases → Create a new release
2. Tag: `v3.1.0`
3. Title: `NeoZapret v3.1.0`
4. Description: Скопируйте из CHANGELOG.md
5. Publish release

### Настройка описания репозитория

1. Settings → Repository name и description
2. Add topics: `windows`, `bypass`, `dpi`, `networking`, `gui`

### Добавление изображений

Добавьте скриншоты приложения в README.md для лучшего представления проекта.

---

## Полезные команды Git

```bash
# Просмотр изменений
git status
git diff

# Просмотр истории
git log --oneline

# Обновление с GitHub
git pull origin main

# Создание новой ветки для разработки
git checkout -b feature/feature-name

# Возврат к последнему коммиту
git reset --hard HEAD

# Отмена изменений в файле
git checkout -- filename
```

---

## Дополнительные ресурсы

- [Git Documentation](https://git-scm.com/doc)
- [GitHub Guides](https://guides.github.com/)
- [GitHub Desktop](https://desktop.github.com/) - GUI для Git

---

**Готово! Ваш проект теперь на GitHub! 🎉**

