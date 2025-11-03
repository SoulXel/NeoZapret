# Добавление иконки приложения

## 🎨 Как добавить иконку

1. **Создайте файл `icon.ico`** в папке `NeoZapret/`
2. Раскомментируйте строки в `NeoZapret.csproj`:
   ```xml
   <ItemGroup>
     <EmbeddedResource Include="icon.ico" />
   </ItemGroup>
   <PropertyGroup>
     <ApplicationIcon>icon.ico</ApplicationIcon>
   </PropertyGroup>
   ```
3. Пересоберите проект

## 📐 Требования к иконке

- Формат: `.ico`
- Размеры: 16x16, 32x32, 48x48, 256x256 пикселей
- Рекомендуется: использовать все размеры для лучшего отображения

## 🛠️ Инструменты для создания иконок

- **Online**: https://www.iconfinder.com, https://favicon.io
- **Desktop**: IcoFX, IconWorkshop, GIMP
- **Convert**: https://convertico.com/

## 💡 Идея для иконки NeoZapret

- Щит с ключом
- Спутник/радар
- Мировую сеть с ключом
- Цвета: синий, зеленый, белый

Текущая версия использует системную иконку (коробку с документами).

