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
        public Characters Characters { get; private set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            if (Settings.Default.Preferences == null)
            {
                Settings.Default.Preferences = new Preferences();
            }

            if (Settings.Default.Favorites == null)
            {
                Settings.Default.Favorites = new Favorites();
            }

            Characters = new Characters();
        }

        internal void UpdateHotkey()
        {
            IntPtr hWnd = getMainWindowHwnd();

            int modifiers = Win32.MOD_NOREPEAT;
            if (Settings.Default.Preferences.globalHotkeyCtrl) modifiers |= Win32.MOD_CONTROL;
            if (Settings.Default.Preferences.globalHotkeyAlt) modifiers |= Win32.MOD_ALT;
            if (Settings.Default.Preferences.globalHotkeyShift) modifiers |= Win32.MOD_SHIFT;
            if (Settings.Default.Preferences.globalHotkeyWin) modifiers |= Win32.MOD_WIN;

            int vk = KeyInterop.VirtualKeyFromKey(Settings.Default.Preferences.globalHotkeyNonModifier);

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
