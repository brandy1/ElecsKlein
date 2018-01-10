using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace SL_Tek_Studio_Pro
{
    class SL_Sytem_API
    {
        public const int WM_SETREDRAW = 0x0b;

        [DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hwnd, uint wMsg, int wParam, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern uint RegisterWindowMessage(string lpString);

        [DllImport("user32.dll")]
        public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

    }
}
