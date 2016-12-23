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
        internal static void PutWindowNear(Window window, Rect rect, PlacementSide side, PlacementInOut inOut)
        {
            Point corner = SelectPlacementSide(side, rect);
            Rect workArea = MonitorWorkAreaFromRect(rect);

            Rect windowRect = RectFromPlacementSide(side, inOut, corner, (int)window.ActualWidth, (int)window.ActualHeight);

            if (windowRect.Left < workArea.Left)
            {
                windowRect.X += workArea.Left - windowRect.Left;
            }
            else if (windowRect.Right > workArea.Right)
            {
                windowRect.X -= windowRect.Right - workArea.Right;
            }

            if (windowRect.Top < workArea.Top)
            {
                windowRect.Y += workArea.Top - windowRect.Top;
            }
            else if (windowRect.Bottom > workArea.Bottom)
            {
                windowRect.Y -= windowRect.Bottom - workArea.Bottom;
            }

            window.Left = windowRect.X;
            window.Top = windowRect.Y;

        }

        internal static Rect MonitorWorkAreaFromRect(Rect rect)
        {
            Point center = SelectPlacementSide(PlacementSide.CENTER, rect);
            IntPtr monitor = Win32.MonitorFromPoint(new Win32.POINT(center), Win32.MonitorOptions.MONITOR_DEFAULTTONEAREST);
            Win32.MONITORINFO monitorInfo = new Win32.MONITORINFO();
            monitorInfo.cbSize = Marshal.SizeOf(monitorInfo);
            Win32.GetMonitorInfo(monitor, ref monitorInfo);
            Rect workArea = monitorInfo.rcWork.asRect();
            return workArea;
        }

        internal static Point SelectPlacementSide(PlacementSide side, Rect windowRect)
        {
            Point point = new Point();

            switch (side)
            {
                case PlacementSide.TOP_LEFT:
                case PlacementSide.CENTER_LEFT:
                case PlacementSide.BOTTOM_LEFT:
                    point.X = windowRect.Left;
                    break;
                case PlacementSide.TOP_CENTER:
                case PlacementSide.CENTER:
                case PlacementSide.BOTTOM_CENTER:
                default:
                    point.X = (windowRect.Left + windowRect.Right) / 2;
                    break;
                case PlacementSide.TOP_RIGHT:
                case PlacementSide.CENTER_RIGHT:
                case PlacementSide.BOTTOM_RIGHT:
                    point.X = windowRect.Right;
                    break;
            }

            switch (side)
            {
                case PlacementSide.TOP_LEFT:
                case PlacementSide.TOP_CENTER:
                case PlacementSide.TOP_RIGHT:
                    point.Y = windowRect.Top;
                    break;
                case PlacementSide.CENTER_LEFT:
                case PlacementSide.CENTER:
                case PlacementSide.CENTER_RIGHT:
                default:
                    point.Y = (windowRect.Top + windowRect.Bottom) / 2;
                    break;
                case PlacementSide.BOTTOM_LEFT:
                case PlacementSide.BOTTOM_CENTER:
                case PlacementSide.BOTTOM_RIGHT:
                    point.Y = windowRect.Bottom;
                    break;
            }

            return point;
        }

        internal static Rect RectFromPlacementSide(PlacementSide side, PlacementInOut inOut, Point corner, int width, int height)
        {
            Rect rect = new Rect();

            switch (side)
            {
                case PlacementSide.TOP_LEFT:
                case PlacementSide.CENTER_LEFT:
                case PlacementSide.BOTTOM_LEFT:
                    rect.X = corner.X;
                    break;
                case PlacementSide.TOP_CENTER:
                case PlacementSide.CENTER:
                case PlacementSide.BOTTOM_CENTER:
                default:
                    rect.X = corner.X - width / 2;
                    break;
                case PlacementSide.TOP_RIGHT:
                case PlacementSide.CENTER_RIGHT:
                case PlacementSide.BOTTOM_RIGHT:
                    rect.X = corner.X - width;
                    break;
            }

            switch (side)
            {
                case PlacementSide.TOP_LEFT:
                case PlacementSide.TOP_CENTER:
                case PlacementSide.TOP_RIGHT:
                    rect.Y = corner.Y;
                    break;
                case PlacementSide.CENTER_LEFT:
                case PlacementSide.CENTER:
                case PlacementSide.CENTER_RIGHT:
                default:
                    rect.Y = corner.Y - height / 2;
                    break;
                case PlacementSide.BOTTOM_LEFT:
                case PlacementSide.BOTTOM_CENTER:
                case PlacementSide.BOTTOM_RIGHT:
                    rect.Y = corner.Y - height;
                    break;
            }

            rect.Width = width;
            rect.Height = height;

            /* Change some values for outside placement.  If the placement is
             * on the top or bottom, clamp the opposite side of the Unicodex
             * window to that edge.  Otherwise, if it's center-left or
             * center-right, clamp the opposite side in the left/right
             * direction.  True-center positioning is unchanged. */
            if (inOut == PlacementInOut.OUTSIDE)
            {
                switch (side)
                {
                    case PlacementSide.TOP_LEFT:
                    case PlacementSide.TOP_CENTER:
                    case PlacementSide.TOP_RIGHT:
                        rect.Y -= height;
                        break;
                    case PlacementSide.BOTTOM_LEFT:
                    case PlacementSide.BOTTOM_CENTER:
                    case PlacementSide.BOTTOM_RIGHT:
                        rect.Y += height;
                        break;
                    case PlacementSide.CENTER_LEFT:
                        rect.X -= width;
                        break;
                    case PlacementSide.CENTER_RIGHT:
                        rect.X += width;
                        break;
                }
            }

            return rect;
        }
    }
}
