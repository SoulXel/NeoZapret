using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace NeoZapret
{
    /// <summary>
    /// Форма для отображения конфликтов и рекомендаций пользователю.
    /// </summary>
    public partial class ConflictRecommendationsForm : Form
    {
        private ConflictDetector.ConflictResult conflictResult;
        private RichTextBox txtConflicts;
        private RichTextBox txtRecommendations;
        private Button btnClose;
        private Button btnOpenSettings;

        public ConflictRecommendationsForm(ConflictDetector.ConflictResult conflicts)
        {
            conflictResult = conflicts;
            InitializeComponent();
            PopulateData();
        }

        private void InitializeComponent()
        {
            this.Text = "NeoZapret - Рекомендации и предупреждения";
            this.Size = new Size(700, 550);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Color.FromArgb(30, 30, 35);

            // Заголовок
            var lblTitle = new Label
            {
                Text = "🔍 Обнаружены потенциальные проблемы",
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = Color.FromArgb(247, 99, 12),
                AutoSize = true,
                Location = new Point(20, 20)
            };
            this.Controls.Add(lblTitle);

            // Описание конфликтов
            var lblConflicts = new Label
            {
                Text = "Обнаруженные проблемы:",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Color.FromArgb(220, 220, 230),
                AutoSize = true,
                Location = new Point(20, 60)
            };
            this.Controls.Add(lblConflicts);

            // Текстовое поле для конфликтов
            txtConflicts = new RichTextBox
            {
                Location = new Point(20, 85),
                Size = new Size(650, 150),
                BackColor = Color.FromArgb(40, 40, 45),
                ForeColor = Color.FromArgb(220, 220, 230),
                BorderStyle = BorderStyle.None,
                ReadOnly = true,
                Font = new Font("Segoe UI", 9),
                ScrollBars = RichTextBoxScrollBars.Vertical
            };
            this.Controls.Add(txtConflicts);

            // Рекомендации
            var lblRecommendations = new Label
            {
                Text = "Рекомендации:",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Color.FromArgb(150, 200, 150),
                AutoSize = true,
                Location = new Point(20, 250)
            };
            this.Controls.Add(lblRecommendations);

            // Текстовое поле для рекомендаций
            txtRecommendations = new RichTextBox
            {
                Location = new Point(20, 275),
                Size = new Size(650, 150),
                BackColor = Color.FromArgb(40, 40, 45),
                ForeColor = Color.FromArgb(150, 200, 150),
                BorderStyle = BorderStyle.None,
                ReadOnly = true,
                Font = new Font("Segoe UI", 9),
                ScrollBars = RichTextBoxScrollBars.Vertical
            };
            this.Controls.Add(txtRecommendations);

            // Кнопка открыть настройки
            btnOpenSettings = UIHelper.CreateStyledButton(
                "⚙ Открыть настройки",
                new Point(20, 440),
                Color.FromArgb(0, 120, 215),
                200
            );
            btnOpenSettings.Click += BtnOpenSettings_Click;
            this.Controls.Add(btnOpenSettings);

            // Кнопка закрыть
            btnClose = UIHelper.CreateStyledButton(
                "✓ Понятно",
                new Point(470, 440),
                Color.FromArgb(60, 60, 70),
                200
            );
            btnClose.Click += (s, e) => this.Close();
            this.Controls.Add(btnClose);
        }

        private void PopulateData()
        {
            // Заполняем конфликты
            txtConflicts.Clear();
            
            var criticalConflicts = conflictResult.Conflicts.Where(c => c.Severity == ConflictDetector.Severity.Critical).ToList();
            var warnings = conflictResult.Conflicts.Where(c => c.Severity == ConflictDetector.Severity.Warning).ToList();
            var info = conflictResult.Conflicts.Where(c => c.Severity == ConflictDetector.Severity.Info).ToList();

            foreach (var conflict in criticalConflicts)
            {
                AppendConflict(conflict, Color.FromArgb(196, 43, 28)); // Красный
            }

            foreach (var conflict in warnings)
            {
                AppendConflict(conflict, Color.FromArgb(247, 99, 12)); // Оранжевый
            }

            foreach (var conflict in info)
            {
                AppendConflict(conflict, Color.FromArgb(0, 120, 215)); // Синий
            }

            if (conflictResult.Conflicts.Count == 0)
            {
                txtConflicts.SelectionColor = Color.FromArgb(150, 200, 150);
                txtConflicts.AppendText("✓ Конфликтов не обнаружено\n");
            }

            // Заполняем рекомендации
            txtRecommendations.Clear();
            foreach (var recommendation in conflictResult.Recommendations)
            {
                var color = recommendation.StartsWith("⚠") || recommendation.Contains("VPN") || recommendation.Contains("IP")
                    ? Color.FromArgb(247, 99, 12)
                    : Color.FromArgb(150, 200, 150);
                
                txtRecommendations.SelectionColor = color;
                txtRecommendations.AppendText($"{recommendation}\n");
            }

            // Добавляем общие рекомендации
            txtRecommendations.SelectionColor = Color.FromArgb(0, 120, 215);
            txtRecommendations.AppendText("\n");
            txtRecommendations.AppendText("💡 Важно помнить:\n");
            txtRecommendations.AppendText("• NeoZapret обходит только DPI блокировки\n");
            txtRecommendations.AppendText("• Для IP-блокировок нужен VPN\n");
            txtRecommendations.AppendText("• NeoZapret не шифрует трафик\n");
            txtRecommendations.AppendText("• Провайдер видит, куда вы заходите\n");
            txtRecommendations.AppendText("• Списки обновляются автоматически");
        }

        private void AppendConflict(ConflictDetector.ConflictInfo conflict, Color color)
        {
            var severity = conflict.Severity == ConflictDetector.Severity.Critical ? "🔴 КРИТИЧНО"
                : conflict.Severity == ConflictDetector.Severity.Warning ? "⚠ ПРЕДУПРЕЖДЕНИЕ"
                : "ℹ ИНФОРМАЦИЯ";

            txtConflicts.SelectionColor = color;
            txtConflicts.AppendText($"{severity}: {conflict.Name}\n");
            txtConflicts.SelectionColor = Color.FromArgb(200, 200, 210);
            txtConflicts.AppendText($"   {conflict.Description}\n");
            
            if (!string.IsNullOrEmpty(conflict.Recommendation))
            {
                txtConflicts.SelectionColor = Color.FromArgb(150, 200, 150);
                txtConflicts.AppendText($"   → {conflict.Recommendation}\n");
            }
            
            txtConflicts.AppendText("\n");
        }

        private void BtnOpenSettings_Click(object sender, EventArgs e)
        {
            using (var form = new AdvancedSettingsForm())
            {
                form.ShowDialog();
            }
        }
    }
}

