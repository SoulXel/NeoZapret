using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.Collections.Generic;

namespace NeoZapret
{
    public partial class ServiceSelectForm : Form
    {
        public List<string> SelectedServices { get; private set; }
        private List<CheckBox> serviceCheckboxes;

        public ServiceSelectForm()
        {
            InitializeComponent();
            SelectedServices = new List<string>();
        }

        private void InitializeComponent()
        {
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.Text = "Выбор сервисов для обхода";
            this.Size = new Size(800, 650);
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
                Text = "Выберите сервисы для обхода",
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                ForeColor = Color.FromArgb(220, 220, 230),
                AutoSize = true,
                Location = new Point(30, 20)
            };

            Label lblAuthor = new Label
            {
                Text = "Разработчик: Soulxel | GitHub: soulxel | Telegram: @xeldi | Discord: Lu1ky\nТестеровщик: Матвей Котов",
                Font = new Font("Segoe UI", 8, FontStyle.Italic),
                ForeColor = Color.FromArgb(150, 150, 160),
                AutoSize = true,
                Location = new Point(30, 600)
            };

            Label lblInfo = new Label
            {
                Text = "Выберите сервисы, которые нужно обойти",
                Font = new Font("Segoe UI", 10),
                ForeColor = Color.FromArgb(150, 150, 160),
                AutoSize = true,
                Location = new Point(30, 55)
            };

            serviceCheckboxes = new List<CheckBox>();
            int yPos = 90;
            int xPos1 = 30;
            int xPos2 = 420;

            // Группа 1: Основные сервисы (левая колонка)
            var servicesLeft = new[]
            {
                new { Name = "Discord", Desc = "Discord и Discord.media", Key = "discord" },
                new { Name = "YouTube & Google", Desc = "YouTube, Google сервисы", Key = "youtube" },
                new { Name = "GitHub", Desc = "GitHub, GitLab, Bitbucket", Key = "github" },
            };

            foreach (var service in servicesLeft)
            {
                var chk = CreateServiceCheckbox(service.Name, service.Desc, new Point(xPos1, yPos), service.Key);
                serviceCheckboxes.Add(chk);
                yPos += 60;
            }

            // Правая колонка
            yPos = 90;
            var servicesRight = new[]
            {
                new { Name = "Cursor Editor", Desc = "Cursor Editor и его API", Key = "cursor" },
                new { Name = "AI Сервисы", Desc = "OpenAI, Claude, Perplexity", Key = "ai" },
                new { Name = "EA Games", Desc = "Battlefield, EA серверы", Key = "games" },
            };

            foreach (var service in servicesRight)
            {
                var chk = CreateServiceCheckbox(service.Name, service.Desc, new Point(xPos2, yPos), service.Key);
                serviceCheckboxes.Add(chk);
                yPos += 60;
            }

            // Группа 2: Социальные сети и стриминг
            yPos = 300;
            var services2 = new[]
            {
                new { Name = "Социальные сети", Desc = "Reddit, Twitter/X, Instagram, Facebook, TikTok", Key = "social" },
                new { Name = "Стриминг", Desc = "Twitch, Spotify, SoundCloud, Netflix, Vimeo", Key = "streaming" },
                new { Name = "Adobe & Autodesk", Desc = "Adobe и Autodesk сервисы", Key = "adobe" },
                new { Name = "Игровые платформы", Desc = "Steam, Epic Games, Unity, Unreal", Key = "gaming" },
            };

            foreach (var service in services2)
            {
                var chk = CreateServiceCheckbox(service.Name, service.Desc, new Point(xPos1, yPos), service.Key);
                serviceCheckboxes.Add(chk);
                yPos += 60;
            }

            // Кнопка "Выбрать все"
            Button btnSelectAll = CreateButton("Выбрать все", new Point(30, 550), Color.FromArgb(80, 80, 90), 160);
            btnSelectAll.Click += (s, e) =>
            {
                foreach (var chk in serviceCheckboxes)
                    chk.Checked = true;
            };

            // Кнопка "Снять все"
            Button btnDeselectAll = CreateButton("Снять все", new Point(200, 550), Color.FromArgb(75, 75, 85), 160);
            btnDeselectAll.Click += (s, e) =>
            {
                foreach (var chk in serviceCheckboxes)
                    chk.Checked = false;
            };

            // Кнопка "Продолжить"
            Button btnStart = CreateButton("Продолжить", new Point(370, 550), Color.FromArgb(80, 80, 90), 180);
            btnStart.Click += BtnStart_Click;

            // Кнопка "Отмена"
            Button btnCancel = CreateButton("Отмена", new Point(560, 550), Color.FromArgb(70, 70, 80), 180);
            btnCancel.DialogResult = DialogResult.Cancel;

            this.Controls.Add(lblTitle);
            this.Controls.Add(lblInfo);
            this.Controls.Add(btnSelectAll);
            this.Controls.Add(btnDeselectAll);
            this.Controls.Add(btnStart);
            this.Controls.Add(btnCancel);
            this.Controls.Add(lblAuthor);
        }

        private CheckBox CreateServiceCheckbox(string name, string desc, Point location, string key)
        {
            var panel = new Panel
            {
                Location = location,
                Size = new Size(350, 50),
                BackColor = Color.FromArgb(38, 38, 44),
                BorderStyle = BorderStyle.None
            };
            panel.Paint += (s, e) =>
            {
                var rect = panel.ClientRectangle;
                if (rect.Width <= 0 || rect.Height <= 0) return;
                using (var pen = new Pen(Color.FromArgb(60, 60, 70), 1))
                {
                    e.Graphics.DrawRectangle(pen, 0, 0, rect.Width - 1, rect.Height - 1);
                }
            };

            var chk = new CheckBox
            {
                Text = name,
                Location = new Point(10, 15),
                AutoSize = true,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Color.FromArgb(220, 220, 230),
                BackColor = Color.FromArgb(38, 38, 44),
                Tag = key,
                Checked = true,
                FlatStyle = FlatStyle.Flat
            };
            // Кастомная отрисовка чекбокса для темной темы
            chk.Paint += (s, e) =>
            {
                var checkbox = s as CheckBox;
                if (checkbox == null) return;
                
                e.Graphics.Clear(checkbox.BackColor);
                
                // Рамка чекбокса (темная)
                var checkBoxRect = new Rectangle(0, (checkbox.Height - 18) / 2, 18, 18);
                using (var borderPen = new Pen(Color.FromArgb(120, 120, 130), 2))
                {
                    e.Graphics.DrawRectangle(borderPen, checkBoxRect);
                }
                
                // Если отмечен - рисуем галочку
                if (checkbox.Checked)
                {
                    using (var checkBrush = new SolidBrush(Color.FromArgb(150, 200, 150)))
                    {
                        e.Graphics.FillRectangle(checkBrush, checkBoxRect);
                        
                        // Галочка
                        using (var checkPen = new Pen(Color.FromArgb(38, 38, 44), 2))
                        {
                            var points = new[]
                            {
                                new Point(4, checkBoxRect.Height / 2),
                                new Point(checkBoxRect.Width / 2 - 1, checkBoxRect.Height - 4),
                                new Point(checkBoxRect.Width - 4, 2)
                            };
                            e.Graphics.DrawLines(checkPen, points);
                        }
                    }
                }
                
                // Текст
                var textRect = new Rectangle(24, 0, checkbox.Width - 24, checkbox.Height);
                TextRenderer.DrawText(e.Graphics, checkbox.Text, checkbox.Font, textRect, checkbox.ForeColor, checkbox.BackColor, 
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
            };

            var lbl = new Label
            {
                Text = desc,
                Location = new Point(30, 33),
                AutoSize = true,
                Font = new Font("Segoe UI", 8),
                ForeColor = Color.FromArgb(150, 150, 160),
                BackColor = Color.FromArgb(38, 38, 44)
            };

            panel.Controls.Add(chk);
            panel.Controls.Add(lbl);
            this.Controls.Add(panel);
            
            return chk;
        }

        private Button CreateButton(string text, Point location, Color backColor, int width)
        {
            var btn = new Button
            {
                Text = text,
                Location = location,
                Size = new Size(width, 45),
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

        private void BtnStart_Click(object sender, EventArgs e)
        {
            SelectedServices.Clear();
            foreach (var chk in serviceCheckboxes)
            {
                if (chk.Checked && chk.Tag != null)
                {
                    SelectedServices.Add(chk.Tag.ToString());
                }
            }

            if (SelectedServices.Count == 0)
            {
                MessageBox.Show("Выберите хотя бы один сервис для обхода!", "NeoZapret",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}
