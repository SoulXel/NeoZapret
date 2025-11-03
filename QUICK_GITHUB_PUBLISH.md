# ⚡ Быстрая публикация на GitHub

## Что уже готово

✅ `.gitignore` - настроен для исключения ненужных файлов  
✅ `README.md` - полная документация проекта  
✅ `LICENSE` - MIT лицензия  
✅ `GITHUB_PUBLISH_GUIDE.md` - подробная инструкция  
✅ `PUBLISH_STEPS.md` - пошаговая инструкция  
✅ Шаблоны Issues для GitHub  

## 🚀 Быстрый старт (3 шага)

### 1. Создайте репозиторий на GitHub

1. Откройте https://github.com
2. Нажмите **"+"** → **"New repository"**
3. Название: `NeoZapret`
4. Выберите **Public**
5. Нажмите **"Create repository"**

### 2. Выполните команды в PowerShell

Откройте PowerShell в папке проекта и выполните:

```powershell
# Перейдите в папку проекта
cd C:\Users\xeldi\Desktop\NeoZapret

# Инициализация Git (если еще не сделано)
if (!(Test-Path .git)) { git init }

# Добавление всех файлов
git add .

# Создание коммита
git commit -m "Initial commit: NeoZapret v3.1.0"

# Добавление удаленного репозитория (ЗАМЕНИТЕ YOUR_USERNAME!)
git remote add origin https://github.com/YOUR_USERNAME/NeoZapret.git

# Переименование ветки
git branch -M main

# Загрузка на GitHub
git push -u origin main
```

### 3. Готово!

Откройте ваш репозиторий на GitHub и проверьте результат.

---

## ⚠️ Важно: Замена YOUR_USERNAME

В команде `git remote add origin` замените `YOUR_USERNAME` на ваш реальный GitHub username.

Например, если ваш username `soulxel`, команда будет:
```bash
git remote add origin https://github.com/soulxel/NeoZapret.git
```

---

## 🔐 Если требуется авторизация

При `git push` GitHub может запросить авторизацию:

1. **Username**: Ваш GitHub username
2. **Password**: Используйте **Personal Access Token** (не обычный пароль)

### Как создать Personal Access Token:

1. GitHub → Settings → Developer settings
2. Personal access tokens → Tokens (classic)
3. Generate new token (classic)
4. Выберите scope: ✅ `repo` (полный доступ к репозиториям)
5. Generate token
6. Скопируйте token (он показывается только один раз!)
7. Используйте token как пароль при `git push`

---

## 📋 Что будет загружено

✅ Исходный код (все `.cs` файлы)  
✅ Документация (`README.md`, `CHANGELOG.md`)  
✅ Конфигурация проекта (`.csproj`, `.sln`)  
✅ Скрипты сборки (`build.bat`, etc.)  
✅ Папка `bin/` с необходимыми файлами (`winws.exe`, `WinDivert.dll`, etc.)  
✅ Папка `lists/` со списками доменов  

❌ **НЕ будет загружено** (благодаря `.gitignore`):  
- Скомпилированные `.exe` файлы (кроме необходимых в `bin/`)
- Папки `obj/`, временные файлы
- Логи пользователей
- Статистика пользователей

---

## 🎯 Следующие шаги после публикации

1. **Создайте Release**:
   - GitHub → Releases → Create a new release
   - Tag: `v3.1.0`
   - При необходимости прикрепите скомпилированный `.exe`

2. **Настройте описание репозитория**:
   - Settings → добавить описание и теги

3. **Добавьте скриншоты** в README.md для лучшего представления

---

## 📚 Дополнительная документация

- **Подробная инструкция**: `GITHUB_PUBLISH_GUIDE.md`
- **Пошаговая инструкция**: `PUBLISH_STEPS.md`

---

## ❓ Возникли проблемы?

1. Убедитесь, что Git установлен: `git --version`
2. Проверьте, что вы авторизованы на GitHub
3. Проверьте правильность URL репозитория
4. Убедитесь, что репозиторий существует на GitHub

---

**Удачи с публикацией! 🎉**

