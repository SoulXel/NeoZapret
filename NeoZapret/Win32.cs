using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace NeoZapret
{
    /// <summary>
    /// Win32 API функции для улучшенной работы с RichTextBox.
    /// </summary>
    internal static class Win32
    {
        public const int WM_VSCROLL = 0x115;
        public const int SB_LINEDOWN = 1;
        public const int SB_LINEUP = 0;
        public const int SB_PAGEDOWN = 3;
        public const int SB_PAGEUP = 2;
        public const int SB_ENDSCROLL = 8;
        public const int EM_SCROLL = 0xB5;
        public const int EM_LINESCROLL = 0xB6;
        public const int EM_SCROLLCARET = 0xB7;

        // Константы для кастомизации скроллбара
        public const int SBM_SETSCROLLINFO = 0x00E9;
        public const int GWL_STYLE = -16;
        public const int ES_DISABLENOSCROLL = 0x2000;
        
        // Константы для цветов скроллбара
        public const int COLOR_SCROLLBAR = 0;
        public const int COLOR_BTNFACE = 15;
        public const int COLOR_BTNSHADOW = 16;
        public const int COLOR_BTNHIGHLIGHT = 20;
        public const int COLOR_3DDKSHADOW = 21;

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern int SendMessage(IntPtr hWnd, int wMsg, int wParam, int lParam);

        [DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, uint Msg, int wParam, int lParam);

        [DllImport("user32.dll")]
        public static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll")]
        public static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("uxtheme.dll", CharSet = CharSet.Unicode)]
        public static extern int SetWindowTheme(IntPtr hWnd, string pszSubAppName, string pszSubIdList);

        /// <summary>
        /// Настраивает стиль скроллбара RichTextBox (темный/черный).
        /// </summary>
        public static void SetDarkScrollbar(RichTextBox rtb)
        {
            try
            {
                if (rtb == null || rtb.IsDisposed || !rtb.IsHandleCreated)
                    return;

                // Отключаем тему Windows для кастомного стиля
                SetWindowTheme(rtb.Handle, "", "");
            }
            catch
            {
                // Игнорируем ошибки
            }
        }

        /// <summary>
        /// Прокручивает RichTextBox вниз на указанное количество строк.
        /// </summary>
        public static void ScrollDown(RichTextBox rtb, int lines = 5)
        {
            try
            {
                if (rtb == null || rtb.IsDisposed || !rtb.IsHandleCreated)
                    return;

                for (int i = 0; i < lines; i++)
                {
                    SendMessage(rtb.Handle, WM_VSCROLL, SB_LINEDOWN, 0);
                }
                
                // Дополнительно используем EM_SCROLLCARET для гарантированной прокрутки
                SendMessage(rtb.Handle, EM_SCROLLCARET, 0, 0);
            }
            catch
            {
                // Игнорируем ошибки
            }
        }
    }
}

