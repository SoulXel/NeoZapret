# 📝 Команды для ручного выполнения

## Выполните эти команды по порядку в PowerShell:

### Шаг 1: Настройка Git (только первый раз)

```powershell
git config user.name "SoulXel"
git config user.email "soulxel@example.com"
```

(Заполните ваш реальный email)

### Шаг 2: Добавление файлов

```powershell
git add .
```

### Шаг 3: Проверка статуса (опционально)

```powershell
git status
```

### Шаг 4: Создание коммита

```powershell
git commit -m "Initial commit: NeoZapret v3.1.0 - Production ready"
```

### Шаг 5: Создание репозитория на GitHub

**ВАЖНО**: Сначала создайте репозиторий на GitHub!

1. Откройте https://github.com
2. Нажмите **"+"** → **"New repository"**
3. Название: `NeoZapret`
4. Выберите **Public**
5. ❌ НЕ добавляйте README, .gitignore, LICENSE
6. Нажмите **"Create repository"**

### Шаг 6: Подключение к GitHub

**ЗАМЕНИТЕ YOUR_USERNAME на ваш GitHub username!**

```powershell
git remote add origin https://github.com/YOUR_USERNAME/NeoZapret.git
```

Например, если ваш username `soulxel`:
```powershell
git remote add origin https://github.com/soulxel/NeoZapret.git
```

### Шаг 7: Переименование ветки

```powershell
git branch -M main
```

### Шаг 8: Загрузка на GitHub

```powershell
git push -u origin main
```

**При запросе пароля используйте Personal Access Token!** (см. ниже)

---

## 🔐 Personal Access Token

Если Git запрашивает пароль:

1. GitHub → Settings → Developer settings
2. Personal access tokens → Tokens (classic)
3. Generate new token (classic)
4. Note: `NeoZapret Publishing`
5. Scopes: ✅ **repo** (отметьте галочкой)
6. Generate token
7. **Скопируйте токен** (показывается только один раз!)
8. Используйте токен как пароль при `git push`

---

## ⚠️ Если возникла ошибка "remote origin already exists"

Выполните:

```powershell
git remote remove origin
git remote add origin https://github.com/YOUR_USERNAME/NeoZapret.git
```

---

## ✅ Проверка

После успешной загрузки откройте:
```
https://github.com/YOUR_USERNAME/NeoZapret
```

Проект должен быть там! 🎉

