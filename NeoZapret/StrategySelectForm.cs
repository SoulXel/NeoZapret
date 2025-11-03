using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.IO;
using System.Reflection;

namespace NeoZapret
{
    public partial class StrategySelectForm : Form
    {
        public string SelectedStrategy { get; private set; }
        public bool UseGameFilter { get; private set; }
        private ListBox lstCustomStrategies;
        private Button btnLoadCustom;
        private string customStrategiesPath;

        public StrategySelectForm()
        {
            InitializeComponent();
            
            // Безопасная инициализация пути для кастомных стратегий
            try
            {
                var startupPath = Application.StartupPath;
                if (string.IsNullOrEmpty(startupPath))
                {
                    startupPath = AppDomain.CurrentDomain.BaseDirectory;
                }
                
                if (!string.IsNullOrEmpty(startupPath))
                {
                    customStrategiesPath = Path.Combine(startupPath, "strategies");
                }
                else
                {
                    customStrategiesPath = Path.Combine(Environment.CurrentDirectory ?? ".", "strategies");
                }
            }
            catch
            {
                customStrategiesPath = Path.Combine(Environment.CurrentDirectory ?? ".", "strategies");
            }
        }

        private void InitializeComponent()
        {
            // Включаем двойную буферизацию для устранения мерцания
            this.SetStyle(ControlStyles.AllPaintingInWmPaint | 
                         ControlStyles.UserPaint | 
                         ControlStyles.DoubleBuffer | 
                         ControlStyles.ResizeRedraw, true);
            
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.Text = "Выбор стратегии обхода";
            this.Size = new Size(770, 680); // Фиксированный размер, будет скорректирован в конце
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Color.FromArgb(32, 32, 38);
            this.Paint += (s, e) =>
            {
                var rect = this.ClientRectangle;
                if (rect.Width <= 0 || rect.Height <= 0) return;
                
                using (var brush = new LinearGradientBrush(
                    rect,
                    Color.FromArgb(32, 32, 38),
                    Color.FromArgb(28, 28, 34),
                    135f))
                {
                    e.Graphics.FillRectangle(brush, rect);
                }
            };

            Label lblTitle = new Label
            {
                Text = "Выбор стратегии обхода",
                Font = new Font("Segoe UI", 18, FontStyle.Bold),
                ForeColor = Color.FromArgb(230, 230, 240),
                AutoSize = false,
                Size = new Size(710, 30),
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point(30, 15)
            };

            // Стратегии - улучшенный темно-серый цвет
            Color panelColor = Color.FromArgb(50, 50, 58);
            
            // Единые размеры для всех карточек
            int panelWidth = 345;
            int panelHeight = 72;
            int startY = 55;
            int spacingY = 82; // Компактное расстояние между карточками
            
            Panel panelRecommended = CreateStrategyPanel(
                "Рекомендуемая", 
                "Для Discord, YouTube. Лучшая для повседневного использования", 
                new Point(20, startY), 
                panelColor, 
                "recommended",
                panelWidth);

            Panel panelFast = CreateStrategyPanel(
                "Быстрая", 
                "Баланс скорости и надежности. Для минимального пинга", 
                new Point(20, startY + spacingY), 
                panelColor, 
                "fast",
                panelWidth);

            Panel panelMax = CreateStrategyPanel(
                "Максимальная защита", 
                "Если ничего не помогает. Когда заблокировано все", 
                new Point(20, startY + spacingY * 2), 
                panelColor, 
                "max",
                panelWidth);

            Panel panelAggressive = CreateStrategyPanel(
                "Агрессивная", 
                "Для жестких блокировок в России. Все методы обхода одновременно", 
                new Point(20, startY + spacingY * 3), 
                panelColor, 
                "aggressive",
                panelWidth);

            Panel panelGames = CreateStrategyPanel(
                "Только игры", 
                "Battlefield, Steam и т.д. Включает игровой фильтр для всех портов игр", 
                new Point(20, startY + spacingY * 4), 
                panelColor, 
                "games",
                panelWidth);

            // Стратегии для провайдеров - во второй колонке
            Label lblProviders = new Label
            {
                Text = "Стратегии для провайдеров:",
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = Color.FromArgb(200, 200, 210),
                AutoSize = true,
                Location = new Point(385, 55)
            };
            this.Controls.Add(lblProviders);

            // Стратегии провайдеров в правой колонке - добавляем отступ от заголовка
            int rightStartX = 385;
            int rightStartY = 55 + 25; // Отступ от заголовка "Стратегии для провайдеров:" (25px для четкого разделения)
            Panel panelRostelecom = CreateStrategyPanel(
                "Ростелеком",
                "Оптимизирована для РТК. multisplit с повышенными повторами",
                new Point(rightStartX, rightStartY),
                panelColor,
                "rostelecom",
                panelWidth);

            Panel panelMTS = CreateStrategyPanel(
                "МТС",
                "Оптимизирована для МТС. fake+fakedsplit для эффективного обхода",
                new Point(rightStartX, rightStartY + spacingY),
                panelColor,
                "mts",
                panelWidth);

            Panel panelBeeline = CreateStrategyPanel(
                "Билайн",
                "Оптимизирована для Билайн. комбинированные методы",
                new Point(rightStartX, rightStartY + spacingY * 2),
                panelColor,
                "beeline",
                panelWidth);

            Panel panelMegafon = CreateStrategyPanel(
                "Мегафон",
                "Для жестких блокировок Мегафон. максимально агрессивная",
                new Point(rightStartX, rightStartY + spacingY * 3),
                panelColor,
                "megafon",
                panelWidth);

            Panel panelTele2 = CreateStrategyPanel(
                "Теле2",
                "Оптимизирована для Теле2. агрессивные методы",
                new Point(rightStartX, rightStartY + spacingY * 4),
                panelColor,
                "tele2",
                panelWidth);

            Panel panelTTK = CreateStrategyPanel(
                "ТТК",
                "Оптимизирована для ТТК. специальная конфигурация",
                new Point(rightStartX, rightStartY + spacingY * 5),
                panelColor,
                "ttk",
                panelWidth);

            this.Controls.Add(panelRostelecom);
            this.Controls.Add(panelMTS);
            this.Controls.Add(panelBeeline);
            this.Controls.Add(panelMegafon);
            this.Controls.Add(panelTele2);
            this.Controls.Add(panelTTK);

            // Раздел кастомных стратегий - компактное расположение (после всех стратегий)
            // Используем правую колонку как ориентир (там 6 стратегий)
            int customY = rightStartY + spacingY * 6 + 10;
            Label lblCustom = new Label
            {
                Text = "Кастомные стратегии:",
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = Color.FromArgb(200, 200, 210),
                AutoSize = true,
                Location = new Point(20, customY)
            };

            lstCustomStrategies = new ListBox
            {
                Location = new Point(20, customY + 25),
                Size = new Size(710, 85),
                Font = new Font("Segoe UI", 9),
                BackColor = Color.FromArgb(38, 38, 44),
                ForeColor = Color.FromArgb(220, 220, 230),
                BorderStyle = BorderStyle.FixedSingle
            };
            
            LoadCustomStrategies();

            // Единый цвет для всех кнопок - темно-серый в одном тоне
            Color buttonColor = Color.FromArgb(50, 50, 58); // Единый цвет для всех кнопок
            
            int buttonsY = customY + 25 + 85 + 10;
            int buttonHeight = 42; // Компактная высота кнопок
            int buttonSpacing = 10; // Расстояние между кнопками
            
            // Используем UIHelper для лучшей видимости кнопок - все в одном тоне
            btnLoadCustom = UIHelper.CreateStyledButton("Загрузить выбранную", new Point(20, buttonsY), buttonColor, 170, buttonHeight);
            btnLoadCustom.Click += BtnLoadCustom_Click;

            Button btnCancel = UIHelper.CreateStyledButton("Отмена", new Point(20 + 170 + buttonSpacing, buttonsY), buttonColor, 140, buttonHeight);
            btnCancel.Click += (s, e) => { this.DialogResult = DialogResult.Cancel; this.Close(); };

            Button btnOK = UIHelper.CreateStyledButton("Применить", new Point(20 + 170 + buttonSpacing + 140 + buttonSpacing, buttonsY), buttonColor, 140, buttonHeight);
            btnOK.Click += (s, e) => 
            {
                if (string.IsNullOrWhiteSpace(SelectedStrategy))
                {
                    MessageBox.Show("Пожалуйста, выберите стратегию!", "NeoZapret", 
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                
                // Для кастомных стратегий игровой фильтр можно оставить выключенным
                UseGameFilter = false;
                
                // Сохраняем состояние (удаляем флаг, так как UseGameFilter = false)
                try
                {
                    var binPath = Path.Combine(Application.StartupPath ?? Environment.CurrentDirectory ?? ".", "bin");
                    var flagFile = Path.Combine(binPath, "game_filter.enabled");
                    if (File.Exists(flagFile))
                    {
                        File.Delete(flagFile);
                    }
                }
                catch { }
                
                this.DialogResult = DialogResult.OK;
                this.Close();
            };
            
            this.Controls.Add(lblTitle);
            this.Controls.Add(panelRecommended);
            this.Controls.Add(panelFast);
            this.Controls.Add(panelMax);
            this.Controls.Add(panelAggressive);
            this.Controls.Add(panelGames);
            this.Controls.Add(lblCustom);
            this.Controls.Add(lstCustomStrategies);
            this.Controls.Add(btnLoadCustom);
            this.Controls.Add(btnOK);
            this.Controls.Add(btnCancel);
            
            // Автор внизу формы - увеличиваем отступы
            int authorY = buttonsY + buttonHeight + 15; // Увеличенный отступ от кнопок
            Label lblAuthor = new Label
            {
                Text = "Разработчик: Soulxel | Тестровщик: Матвей Котов",
                Font = new Font("Segoe UI", 8, FontStyle.Italic),
                ForeColor = Color.FromArgb(150, 150, 160),
                AutoSize = false,
                Size = new Size(730, 15),
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point(20, authorY)
            };
            this.Controls.Add(lblAuthor);
            
            // Корректируем размер формы чтобы вместить все элементы с достаточным отступом от низа
            this.Size = new Size(770, authorY + 40); // Увеличенный отступ от нижней границы (было 25, стало 40)
        }
        
        private void LoadCustomStrategies()
        {
            lstCustomStrategies.Items.Clear();
            
            try
            {
                if (string.IsNullOrEmpty(customStrategiesPath))
                {
                    var startupPath = Application.StartupPath;
                    if (string.IsNullOrEmpty(startupPath))
                        startupPath = AppDomain.CurrentDomain.BaseDirectory;
                    customStrategiesPath = Path.Combine(startupPath ?? Environment.CurrentDirectory ?? ".", "strategies");
                }

                if (string.IsNullOrEmpty(customStrategiesPath))
                    return;

                if (!Directory.Exists(customStrategiesPath))
                {
                    try
                    {
                        Directory.CreateDirectory(customStrategiesPath);
                    }
                    catch
                    {
                        return;
                    }
                }

                var files = Directory.GetFiles(customStrategiesPath, "*.txt");
                foreach (var file in files)
                {
                    lstCustomStrategies.Items.Add(Path.GetFileName(file));
                }
            }
            catch
            {
                // Игнорируем ошибки
            }
        }
        
        private void BtnLoadCustom_Click(object sender, EventArgs e)
        {
            if (lstCustomStrategies.SelectedItem == null)
            {
                MessageBox.Show("Выберите стратегию из списка!", "NeoZapret",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var fileName = lstCustomStrategies.SelectedItem.ToString();
            var filePath = Path.Combine(customStrategiesPath, fileName);
            
            if (!File.Exists(filePath))
            {
                MessageBox.Show($"Файл не найден: {filePath}", "NeoZapret",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            SelectedStrategy = filePath;
            // Для кастомных стратегий игровой фильтр не используется
            UseGameFilter = false;
            
            // Удаляем флаг игрового фильтра для кастомных стратегий
            try
            {
                var binPath = Path.Combine(Application.StartupPath ?? Environment.CurrentDirectory ?? ".", "bin");
                var flagFile = Path.Combine(binPath, "game_filter.enabled");
                if (File.Exists(flagFile))
                {
                    File.Delete(flagFile);
                }
            }
            catch { }
            
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private Panel CreateStrategyPanel(string title, string description, Point location, Color baseColor, string strategy, int width)
        {
            var panel = new Panel
            {
                Location = location,
                Size = new Size(width, 72), // Фиксированная высота для всех карточек
                BackColor = Color.Transparent,
                BorderStyle = BorderStyle.None,
                Tag = strategy,
                Cursor = Cursors.Hand
            };
            
            // Включаем двойную буферизацию через наследование (создаем кастомную панель)
            // Используем DoubleBuffered через рефлексию для панели
            try
            {
                var prop = panel.GetType().GetProperty("DoubleBuffered", 
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                prop?.SetValue(panel, true, null);
            }
            catch { }

            bool isHovered = false;
            bool isPressed = false;
            
            // Оптимизированные обработчики - вызываем Invalidate только при реальном изменении состояния
            panel.MouseEnter += (s, e) => 
            { 
                if (!isHovered)
                {
                    isHovered = true;
                    panel.Invalidate(false); // false = не инвалидировать дочерние элементы
                }
            };
            panel.MouseLeave += (s, e) => 
            { 
                if (isHovered || isPressed)
                {
                    isHovered = false; 
                    isPressed = false;
                    panel.Invalidate(false);
                }
            };
            panel.MouseDown += (s, e) => 
            { 
                if (!isPressed)
                {
                    isPressed = true; 
                    panel.Invalidate(false);
                }
            };
            panel.MouseUp += (s, e) => 
            { 
                if (isPressed)
                {
                    isPressed = false; 
                    panel.Invalidate(false);
                }
            };

            // Улучшенный дизайн без моргания - в стиле основного приложения
            panel.Paint += (s, e) =>
            {
                var rect = panel.ClientRectangle;
                if (rect.Width <= 0 || rect.Height <= 0) return;
                
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                
                // Определяем цвет в зависимости от состояния
                Color panelColor;
                if (isPressed)
                {
                    panelColor = Color.FromArgb(
                        Math.Max(0, baseColor.R - 8), 
                        Math.Max(0, baseColor.G - 8), 
                        Math.Max(0, baseColor.B - 8));
                }
                else if (isHovered)
                {
                    panelColor = Color.FromArgb(
                        Math.Min(255, baseColor.R + 8), 
                        Math.Min(255, baseColor.G + 8), 
                        Math.Min(255, baseColor.B + 8));
                }
                else
                {
                    panelColor = baseColor;
                }
                
                // Фон с закругленными углами
                using (var path = new GraphicsPath())
                {
                    int radius = 8;
                    path.AddArc(0, 0, radius * 2, radius * 2, 180, 90);
                    path.AddArc(rect.Width - radius * 2, 0, radius * 2, radius * 2, 270, 90);
                    path.AddArc(rect.Width - radius * 2, rect.Height - radius * 2, radius * 2, radius * 2, 0, 90);
                    path.AddArc(0, rect.Height - radius * 2, radius * 2, radius * 2, 90, 90);
                    path.CloseAllFigures();
                    
                    // Фон
                    using (var brush = new SolidBrush(panelColor))
                    {
                        e.Graphics.FillPath(brush, path);
                    }
                    
                    // Темная рамка
                    using (var pen = new Pen(Color.FromArgb(80, 80, 90), 1f))
                    {
                        e.Graphics.DrawPath(pen, path);
                    }
                }
            };

            // Лейбл заголовка - компактное расположение
            var lblTitle = new Label
            {
                Text = title,
                Location = new Point(12, 10),
                Size = new Size(width - 24, 22),
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = Color.FromArgb(240, 240, 245),
                BackColor = Color.Transparent,
                AutoSize = false,
                Cursor = Cursors.Hand
            };
            
            // Лейбл описания - компактное расположение
            var lblDesc = new Label
            {
                Text = description,
                Location = new Point(12, 35),
                Size = new Size(width - 24, 30),
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.FromArgb(200, 200, 210),
                BackColor = Color.Transparent,
                AutoSize = false,
                Cursor = Cursors.Hand
            };
            
            // Перенаправляем события мыши на панель - используем те же обработчики
            EventHandler mouseEnterHandler = (s, e) => 
            {
                if (!isHovered)
                {
                    isHovered = true;
                    panel.Invalidate(false);
                }
            };
            
            EventHandler mouseLeaveHandler = (s, e) => 
            {
                if (isHovered || isPressed)
                {
                    isHovered = false; 
                    isPressed = false;
                    panel.Invalidate(false);
                }
            };
            
            MouseEventHandler mouseDownHandler = (s, e) => 
            {
                if (!isPressed)
                {
                    isPressed = true; 
                    panel.Invalidate(false);
                }
            };
            
            MouseEventHandler mouseUpHandler = (s, e) => 
            {
                if (isPressed)
                {
                    isPressed = false; 
                    panel.Invalidate(false);
                }
            };
            
            // Добавляем обработчики к labels
            lblTitle.MouseEnter += mouseEnterHandler;
            lblTitle.MouseLeave += mouseLeaveHandler;
            lblTitle.MouseDown += mouseDownHandler;
            lblTitle.MouseUp += mouseUpHandler;
            
            lblDesc.MouseEnter += mouseEnterHandler;
            lblDesc.MouseLeave += mouseLeaveHandler;
            lblDesc.MouseDown += mouseDownHandler;
            lblDesc.MouseUp += mouseUpHandler;

            // Обработка клика на панели
            EventHandler clickHandler = (s, e) =>
            {
                SelectedStrategy = strategy;
                
                // Автоматически включаем игровой фильтр только для стратегии "games"
                UseGameFilter = (strategy == "games");
                
                // Сохраняем состояние игрового фильтра
                try
                {
                    var binPath = Path.Combine(Application.StartupPath ?? Environment.CurrentDirectory ?? ".", "bin");
                    if (!Directory.Exists(binPath))
                    {
                        Directory.CreateDirectory(binPath);
                    }
                    var flagFile = Path.Combine(binPath, "game_filter.enabled");
                    if (UseGameFilter)
                    {
                        File.WriteAllText(flagFile, "");
                    }
                    else if (File.Exists(flagFile))
                    {
                        File.Delete(flagFile);
                    }
                }
                catch { }
                
                this.DialogResult = DialogResult.OK;
                this.Close();
            };

            panel.Click += clickHandler;
            lblTitle.Click += clickHandler;
            lblDesc.Click += clickHandler;

            panel.Controls.Add(lblTitle);
            panel.Controls.Add(lblDesc);
            
            return panel;
        }

        private Button CreateButton(string text, Point location, Color baseColor, int width)
        {
            var btn = new Button
            {
                Text = text,
                Location = location,
                Size = new Size(width, 50),
                FlatStyle = FlatStyle.Flat,
                BackColor = baseColor,
                ForeColor = Color.FromArgb(240, 240, 245),
                Font = new Font("Segoe UI", 10, FontStyle.Regular),
                Cursor = Cursors.Hand,
                UseVisualStyleBackColor = false
            };
            btn.FlatAppearance.BorderSize = 0;
            
            // Включаем двойную буферизацию через рефлексию
            try
            {
                var prop = btn.GetType().GetProperty("DoubleBuffered", 
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                prop?.SetValue(btn, true, null);
            }
            catch { }
            
            bool isHovered = false;
            bool isPressed = false;
            
            // Оптимизированные обработчики - вызываем Invalidate только при реальном изменении состояния
            btn.MouseEnter += (s, e) => 
            { 
                if (!isHovered)
                {
                    isHovered = true; 
                    btn.Invalidate(false);
                }
            };
            btn.MouseLeave += (s, e) => 
            { 
                if (isHovered || isPressed)
                {
                    isHovered = false; 
                    isPressed = false;
                    btn.Invalidate(false);
                }
            };
            btn.MouseDown += (s, e) => 
            { 
                if (!isPressed)
                {
                    isPressed = true; 
                    btn.Invalidate(false);
                }
            };
            btn.MouseUp += (s, e) => 
            { 
                if (isPressed)
                {
                    isPressed = false; 
                    btn.Invalidate(false);
                }
            };

            // Единый стиль как в основном приложении
            btn.Paint += (s, e) =>
            {
                var rect = btn.ClientRectangle;
                if (rect.Width <= 0 || rect.Height <= 0) return;
                
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                
                using (var path = new GraphicsPath())
                {
                    int radius = 8;
                    path.AddArc(0, 0, radius * 2, radius * 2, 180, 90);
                    path.AddArc(rect.Width - radius * 2, 0, radius * 2, radius * 2, 270, 90);
                    path.AddArc(rect.Width - radius * 2, rect.Height - radius * 2, radius * 2, radius * 2, 0, 90);
                    path.AddArc(0, rect.Height - radius * 2, radius * 2, radius * 2, 90, 90);
                    path.CloseAllFigures();
                    
                    // Определяем цвет в зависимости от состояния
                    Color btnColor;
                    if (isPressed)
                    {
                        btnColor = Color.FromArgb(
                            Math.Max(0, baseColor.R - 8), 
                            Math.Max(0, baseColor.G - 8), 
                            Math.Max(0, baseColor.B - 8));
                    }
                    else if (isHovered)
                    {
                        btnColor = Color.FromArgb(
                            Math.Min(255, baseColor.R + 8), 
                            Math.Min(255, baseColor.G + 8), 
                            Math.Min(255, baseColor.B + 8));
                    }
                    else
                    {
                        btnColor = baseColor;
                    }
                    
                    // Фон кнопки
                    using (var brush = new SolidBrush(btnColor))
                    {
                        e.Graphics.FillPath(brush, path);
                    }
                    
                    // Темная рамка
                    using (var pen = new Pen(Color.FromArgb(80, 80, 90), 1f))
                    {
                        e.Graphics.DrawPath(pen, path);
                    }
                    
                    btn.Region = new Region(path);
                    
                    // Текст
                    using (var textBrush = new SolidBrush(Color.FromArgb(240, 240, 245)))
                    {
                        var textFormat = new StringFormat
                        {
                            Alignment = StringAlignment.Center,
                            LineAlignment = StringAlignment.Center
                        };
                        e.Graphics.DrawString(text, btn.Font, textBrush, rect, textFormat);
                    }
                }
            };

            return btn;
        }
    }
}
