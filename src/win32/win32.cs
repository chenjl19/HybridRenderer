using System;
using System.Runtime.InteropServices;

namespace NT
{
    public static class Win32 {
        [DllImport("user32.dll")]
        public static extern bool SetCursorPos(int x, int y);
    }
}