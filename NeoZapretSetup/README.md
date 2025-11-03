# NeoZapret Setup - Установщик

Данная папка содержит файлы для создания установщика NeoZapret.

## 🛠️ Варианты установки

### Вариант 1: Inno Setup (рекомендуется) ⭐

**Требования**: Inno Setup 5 или 6

**Сборка установщика**:
1. Установите Inno Setup с http://www.jrsoftware.org
2. Запустите `build_setup.bat` из корня проекта
3. Получите файл `NeoZapret-Setup.exe`

**Или вручную**:
```batch
"C:\Program Files (x86)\Inno Setup 6\ISCC.exe" NeoZapretSetup\setup_innosetup.iss
```

**Преимущества**:
- ✅ Профессиональный мастер установки
- ✅ Поддержка русского языка
- ✅ Создание ярлыков автоматически
- ✅ Функция удаления программы
- ✅ Компактный архив (LZMA)

### Вариант 2: PowerShell установщик 🔷

**Требования**: Windows PowerShell 3.0+

**Использование**:
1. Запустите `NeoZapretSetup\RunSetup.bat`
2. Следуйте инструкциям

**Или напрямую**:
```powershell
powershell -ExecutionPolicy Bypass -File "NeoZapretSetup\setup.ps1"
```

**Преимущества**:
- ✅ Не требует дополнительных программ
- ✅ Поддержка Windows 7+
- ✅ Подробные логи установки
- ✅ Автоматическая проверка прав

### Вариант 3: Batch установщик 📦

**Требования**: Windows CMD

**Использование**:
1. Запустите `create_installer.bat`
2. Скопируйте папку `NeoZapret-Installer-Temp` с файлом `setup.bat`
3. Запустите `setup.bat` от имени администратора

**Преимущества**:
- ✅ Максимальная совместимость
- ✅ Работает на всех версиях Windows
- ✅ Не требует дополнительных зависимостей

## 📋 Что устанавливается

- `NeoZapret.exe` - Главное приложение
- `bin\` - Все исполняемые файлы (winws.exe, WinDivert и др.)
- `lists\` - Списки доменов и IP адресов
- `README.txt` - Документация
- Ярлыки в меню Пуск и на рабочем столе

## 🎯 Путь установки

По умолчанию: `C:\Program Files\NeoZapret\`

Можно изменить при установке (кроме PowerShell версии).

## ⚠️ Важно

- Все установщики **требуют** прав администратора
- Base Filtering Engine должен быть включен
- Требуется .NET Framework 4.7.2 или выше для GUI

## 🔧 Настройка Inno Setup

Если нужно изменить параметры установщика, отредактируйте:
- `NeoZapretSetup\setup_innosetup.iss`

Основные параметры:
```inno
AppName=NeoZapret              # Название приложения
AppVersion=2.1.0               # Версия
DefaultDirName={commonpf}\...  # Путь установки
```

## 📝 Логи установки

PowerShell версия выводит подробные логи в консоли.  
Inno Setup создает log в `%TEMP%\setup_*.log`.

## 🐛 Решение проблем

**Проблема**: "Установка заблокирована политиками безопасности"  
**Решение**: Запустите от имени администратора через "ПКМ → Запуск от имени администратора"

**Проблема**: "PowerShell отключен"  
**Решение**: Запустите `Set-ExecutionPolicy RemoteSigned` от имени администратора

**Проблема**: Inno Setup не найден  
**Решение**: Используйте PowerShell или Batch установщик

## 👤 Автор

**SoulXel**  
GitHub: [SoulXel](https://github.com/SoulXel)  
Discord: Lu1ky  
Telegram: Lu1ky

