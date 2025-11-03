using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace NeoZapret
{
    /// <summary>
    /// Кастомный RichTextBox с темным скроллбаром.
    /// </summary>
    public class DarkScrollbarRichTextBox : RichTextBox
    {
        [DllImport("uxtheme.dll", CharSet = CharSet.Unicode)]
        private static extern int SetWindowTheme(IntPtr hWnd, string pszSubAppName, string pszSubIdList);

        private const int WM_PAINT = 0x000F;
        private const int WM_NCPAINT = 0x0085;
        private const int WM_VSCROLL = 0x115;
        private const int WM_HSCROLL = 0x114;
        private const int WM_MOUSEWHEEL = 0x020A;

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            
            try
            {
                // Отключаем тему Windows для использования классического скроллбара
                // Это сделает скроллбар более темным/черным
                SetWindowTheme(this.Handle, "", "");
            }
            catch { }
        }

        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);

            // НЕ вызываем Invalidate на WM_PAINT, чтобы избежать бесконечного цикла перерисовки
            // Обновляем только при прокрутке
            if (m.Msg == WM_VSCROLL || m.Msg == WM_HSCROLL || m.Msg == WM_MOUSEWHEEL)
            {
                try
                {
                    // Легкая перерисовка только области скроллбара при прокрутке
                    if (this.IsHandleCreated && !this.IsDisposed)
                    {
                        // Используем BeginInvoke для избежания блокировки
                        this.BeginInvoke(new Action(() =>
                        {
                            if (!this.IsDisposed && this.IsHandleCreated)
                            {
                                this.Invalidate(); // Перерисовка после прокрутки
                            }
                        }));
                    }
                }
                catch { }
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            
            // Рисуем темную рамку вокруг области скроллбара для лучшей видимости
            try
            {
                if (this.ScrollBars == RichTextBoxScrollBars.Vertical || 
                    this.ScrollBars == RichTextBoxScrollBars.Both)
                {
                    int scrollbarWidth = SystemInformation.VerticalScrollBarWidth;
                    Rectangle scrollbarArea = new Rectangle(
                        this.ClientSize.Width - scrollbarWidth - 1,
                        0,
                        scrollbarWidth + 1,
                        this.ClientSize.Height);

                    // Темная рамка вокруг скроллбара
                    using (Pen pen = new Pen(Color.FromArgb(20, 20, 25), 1))
                    {
                        e.Graphics.DrawLine(pen, 
                            scrollbarArea.Left, 
                            scrollbarArea.Top, 
                            scrollbarArea.Left, 
                            scrollbarArea.Bottom);
                    }
                }
            }
            catch { }
        }
    }
}

