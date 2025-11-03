using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace NeoZapret
{
    public static class UIHelper
    {
        // Единый стиль для всех кнопок приложения
        public static Button CreateStandardButton(string text, Point location, Color backColor, int width, int height = 45)
        {
            var btn = new Button
            {
                Text = text,
                Location = location,
                Size = new Size(width, height),
                FlatStyle = FlatStyle.Flat,
                BackColor = backColor,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10),
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(
                Math.Min(255, backColor.R + 15),
                Math.Min(255, backColor.G + 15),
                Math.Min(255, backColor.B + 15));
            btn.FlatAppearance.MouseDownBackColor = Color.FromArgb(
                Math.Max(0, backColor.R - 15),
                Math.Max(0, backColor.G - 15),
                Math.Max(0, backColor.B - 15));
            
            btn.Paint += (s, e) =>
            {
                var rect = btn.ClientRectangle;
                if (rect.Width <= 0 || rect.Height <= 0) return;
                
                using (var path = new GraphicsPath())
                {
                    int radius = 10;
                    path.AddArc(0, 0, radius * 2, radius * 2, 180, 90);
                    path.AddArc(rect.Width - radius * 2, 0, radius * 2, radius * 2, 270, 90);
                    path.AddArc(rect.Width - radius * 2, rect.Height - radius * 2, radius * 2, radius * 2, 0, 90);
                    path.AddArc(0, rect.Height - radius * 2, radius * 2, radius * 2, 90, 90);
                    path.CloseAllFigures();
                    btn.Region = new Region(path);
                }
            };
            
            return btn;
        }

        // Валидация IP адреса
        public static bool ValidateIPAddress(string ip)
        {
            if (string.IsNullOrWhiteSpace(ip))
                return false;
            
            return System.Text.RegularExpressions.Regex.IsMatch(ip, 
                @"^((25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)$");
        }

        // Создание стандартного TextBox
        public static TextBox CreateStandardTextBox(Point location, Size size, string placeholder = "")
        {
            var txt = new TextBox
            {
                Location = location,
                Size = size,
                Font = new Font("Segoe UI", 10),
                BackColor = Color.FromArgb(38, 38, 44),
                ForeColor = Color.FromArgb(220, 220, 230),
                BorderStyle = BorderStyle.FixedSingle
            };

            if (!string.IsNullOrEmpty(placeholder))
            {
                txt.Text = placeholder;
                txt.ForeColor = Color.FromArgb(150, 150, 160);
                txt.GotFocus += (s, e) =>
                {
                    if (txt.Text == placeholder)
                    {
                        txt.Text = "";
                        txt.ForeColor = Color.FromArgb(220, 220, 230);
                    }
                };
                txt.LostFocus += (s, e) =>
                {
                    if (string.IsNullOrEmpty(txt.Text))
                    {
                        txt.Text = placeholder;
                        txt.ForeColor = Color.FromArgb(150, 150, 160);
                    }
                };
            }

            return txt;
        }

        // Стандартный темный фон для форм
        public static void PaintDarkGradientBackground(object sender, PaintEventArgs e)
        {
            var rect = ((Control)sender).ClientRectangle;
            if (rect.Width <= 0 || rect.Height <= 0) return;
            
            using (var brush = new LinearGradientBrush(
                rect,
                Color.FromArgb(32, 32, 38),
                Color.FromArgb(28, 28, 34),
                135f))
            {
                e.Graphics.FillRectangle(brush, rect);
            }
        }

        // Создание улучшенной стильной кнопки с hover эффектами (как в MainForm)
        public static Button CreateStyledButton(string text, Point location, Color baseColor, int width, int height = 50, bool isPrimary = false)
        {
            var btn = new Button
            {
                Text = text,
                Location = location,
                Size = new Size(width, height),
                FlatStyle = FlatStyle.Flat,
                BackColor = baseColor,
                ForeColor = Color.FromArgb(240, 240, 245),
                Font = new Font("Segoe UI", 10, FontStyle.Regular),
                Cursor = Cursors.Hand,
                UseVisualStyleBackColor = false
            };
            btn.FlatAppearance.BorderSize = 0;
            
            bool isHovered = false;
            bool isPressed = false;
            
            btn.MouseEnter += (s, e) => { isHovered = true; btn.Invalidate(); };
            btn.MouseLeave += (s, e) => { isHovered = false; btn.Invalidate(); };
            btn.MouseDown += (s, e) => { isPressed = true; btn.Invalidate(); };
            btn.MouseUp += (s, e) => { isPressed = false; btn.Invalidate(); };
            
            // Улучшенный темно-серый дизайн
            btn.Paint += (s, e) =>
            {
                var rect = btn.ClientRectangle;
                if (rect.Width <= 0 || rect.Height <= 0) return;
                
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                e.Graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                
                // Закругленный путь
                using (var path = new GraphicsPath())
                {
                    int radius = 8;
                    path.AddArc(0, 0, radius * 2, radius * 2, 180, 90);
                    path.AddArc(rect.Width - radius * 2, 0, radius * 2, radius * 2, 270, 90);
                    path.AddArc(rect.Width - radius * 2, rect.Height - radius * 2, radius * 2, radius * 2, 0, 90);
                    path.AddArc(0, rect.Height - radius * 2, radius * 2, radius * 2, 90, 90);
                    path.CloseAllFigures();
                    
                    // Определяем цвет кнопки в зависимости от состояния
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
                    
                    // Тонкая темная рамка
                    using (var borderPen = new Pen(Color.FromArgb(80, 80, 90), 1f))
                    {
                        e.Graphics.DrawPath(borderPen, path);
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



