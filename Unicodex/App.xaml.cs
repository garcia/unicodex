using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using Unicodex.Properties;

namespace Unicodex
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            if (Settings.Default.UnicodexSettings == null)
            {
                Settings.Default.UnicodexSettings = new UnicodexSettings();
            }
        }

        internal void UpdateHotkey()
        {
            IntPtr hWnd = getMainWindowHwnd();

            int modifiers = Win32.MOD_NOREPEAT;
            if (Settings.Default.UnicodexSettings.globalHotkeyCtrl) modifiers |= Win32.MOD_CONTROL;
            if (Settings.Default.UnicodexSettings.globalHotkeyAlt) modifiers |= Win32.MOD_ALT;
            if (Settings.Default.UnicodexSettings.globalHotkeyShift) modifiers |= Win32.MOD_SHIFT;
            if (Settings.Default.UnicodexSettings.globalHotkeyWin) modifiers |= Win32.MOD_WIN;

            int vk = KeyInterop.VirtualKeyFromKey(Settings.Default.UnicodexSettings.globalHotkeyNonModifier);

            Win32.UnregisterHotKey(hWnd, 0);
            if (!Win32.RegisterHotKey(hWnd, 0, modifiers, vk))
            {
                throw new Win32Exception();
            }
        }

        private IntPtr getMainWindowHwnd()
        {
            return (IntPtr)new WindowInteropHelper(MainWindow).Handle.ToInt32();
        }
    }
}
