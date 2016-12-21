using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Unicodex
{
    public partial class WindowUtils
    {
        public static void PutWindowNear(Window window, int left, int top, int bottom)
        {
            IntPtr monitor = Win32.MonitorFromPoint(new Win32.POINT(left, (top + bottom) / 2), Win32.MonitorOptions.MONITOR_DEFAULTTONEAREST);
            Win32.MONITORINFO monitorInfo = new Win32.MONITORINFO();
            monitorInfo.cbSize = Marshal.SizeOf(monitorInfo);
            Win32.GetMonitorInfo(monitor, ref monitorInfo);
            Win32.RECT workArea = monitorInfo.rcWork;

            /* Wherever the window spawns, put it just below and to the left
             * of the focal point, for aesthetic reasons. */
            int leftOffset = -5;
            int topOffset = -5;
            int bottomOffset = 5;

            int newRight = left + (int)window.ActualWidth + leftOffset;
            int newBottom = bottom + (int)window.ActualHeight + bottomOffset;
            if (newRight > workArea.right)
            {
                left -= (int)window.ActualWidth;
            }
            if (newBottom > workArea.bottom)
            {
                top -= (int)window.ActualHeight;
                top += topOffset;
            }
            else
            {
                top = bottom;
                top += bottomOffset;
            }

            window.Left = Math.Max(left + leftOffset, 0);
            window.Top = Math.Max(top, 0);
        }
    }
}
