# 🔧 Настройка Git и публикация на GitHub

## Ситуация

Репозиторий уже инициализирован (`Initialized empty Git repository`), но нужно настроить Git и загрузить код.

## Решение

### Вариант 1: Автоматический скрипт (Рекомендуется)

1. **Откройте PowerShell в папке проекта**
2. **Запустите скрипт**:
   ```powershell
   .\setup_git_and_publish.ps1
   ```
3. **Следуйте инструкциям** в скрипте

### Вариант 2: Ручная настройка

#### Шаг 1: Настройка Git (только первый раз)

```powershell
# Установите ваше имя
git config user.name "Ваше Имя"
# или
git config user.name "SoulXel"

# Установите ваш email
git config user.email "ваш@email.com"
# или
git config user.email "soulxel@example.com"
```

#### Шаг 2: Добавление файлов

```powershell
# Добавить все файлы (кроме игнорируемых)
git add .

# Проверить что добавлено
git status
```

#### Шаг 3: Создание коммита

```powershell
git commit -m "Initial commit: NeoZapret v3.1.0 - Production ready"
```

#### Шаг 4: Создание репозитория на GitHub

1. Откройте https://github.com
2. Нажмите **"+"** → **"New repository"**
3. Название: `NeoZapret`
4. Выберите **Public**
5. ❌ НЕ добавляйте README, .gitignore, LICENSE
6. Нажмите **"Create repository"**

#### Шаг 5: Подключение к GitHub

```powershell
# Замените YOUR_USERNAME на ваш GitHub username
git remote add origin https://github.com/YOUR_USERNAME/NeoZapret.git

# Переименование ветки
git branch -M main
```

#### Шаг 6: Загрузка на GitHub

```powershell
git push -u origin main
```

**Важно**: При запросе пароля используйте **Personal Access Token**, а не обычный пароль!

---

## 🔐 Создание Personal Access Token

Если Git запрашивает пароль:

1. GitHub → Settings (правый верхний угол)
2. Developer settings (внизу слева)
3. Personal access tokens → Tokens (classic)
4. Generate new token (classic)
5. Note: `NeoZapret Publishing`
6. Expiration: выберите срок действия
7. Scopes: отметьте ✅ **repo** (полный доступ к репозиториям)
8. Generate token
9. **Скопируйте токен** (он показывается только один раз!)
10. Используйте токен как пароль при `git push`

---

## ✅ Проверка результата

После успешной загрузки:

1. Откройте `https://github.com/YOUR_USERNAME/NeoZapret`
2. Проверьте, что все файлы загружены
3. Убедитесь, что README.md отображается корректно

---

## 🆘 Решение проблем

### Проблема: "fatal: not a git repository"

**Решение**: Выполните `git init` в папке проекта

### Проблема: "Authentication failed"

**Решение**: Используйте Personal Access Token вместо пароля

### Проблема: "Permission denied"

**Решение**: 
- Проверьте правильность URL репозитория
- Убедитесь, что репозиторий существует на GitHub
- Проверьте, что у вас есть права на запись

### Проблема: "Large files detected"

**Решение**: 
- Проверьте `.gitignore` - он должен исключать большие файлы
- Если нужно загрузить большие файлы, используйте Git LFS

---

## 📝 Полезные команды

```powershell
# Проверить статус
git status

# Проверить конфигурацию
git config user.name
git config user.email

# Просмотреть историю
git log --oneline

# Проверить удаленный репозиторий
git remote -v

# Если нужно изменить удаленный репозиторий
git remote remove origin
git remote add origin https://github.com/YOUR_USERNAME/NeoZapret.git
```

---

**Готово! Теперь ваш проект будет на GitHub! 🚀**

