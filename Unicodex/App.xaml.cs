using Microsoft.Win32;
using System;
using System.ComponentModel;
using System.Reflection;
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
        /* Store references to settings outside of Settings.Default since
         * settings accesses are expensive: */
        public Preferences Preferences { get; private set; }
        public Favorites Favorites { get; private set; }
        public UserTags UserTags { get; private set; }

        public Characters Characters { get; private set; }
        public TagGroups TagGroups { get; private set; }

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

            if (Settings.Default.UserTags == null)
            {
                Settings.Default.UserTags = new UserTags();
            }

            Preferences = Settings.Default.Preferences;
            Favorites = Settings.Default.Favorites;
            UserTags = Settings.Default.UserTags;

            Characters = new Characters();
            TagGroups = new TagGroups(Characters);

            MainWindow = new MainWindow();
        }

        internal void UpdateSettings()
        {
            UpdateRunOnStartup();
            UpdateHotkey();
        }

        internal void UpdateRunOnStartup()
        {
            using (RegistryKey runKey = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true))
            {
                string keyName = "Unicodex";
                bool currentRunOnStartupValue = runKey.GetValue(keyName) != null;
                bool newRunOnStartupValue = Preferences.runOnStartup;

                if (currentRunOnStartupValue != newRunOnStartupValue)
                {
                    if (newRunOnStartupValue)
                    {
                        runKey.SetValue(keyName, Assembly.GetEntryAssembly().Location);
                    }
                    else
                    {
                        runKey.DeleteValue(keyName);
                    }
                }
            }
        }

        internal void UpdateHotkey()
        {
            IntPtr hWnd = getMainWindowHwnd();

            int modifiers = Win32.MOD_NOREPEAT;
            if (Preferences.globalHotkeyCtrl) modifiers |= Win32.MOD_CONTROL;
            if (Preferences.globalHotkeyAlt) modifiers |= Win32.MOD_ALT;
            if (Preferences.globalHotkeyShift) modifiers |= Win32.MOD_SHIFT;
            if (Preferences.globalHotkeyWin) modifiers |= Win32.MOD_WIN;

            int vk = KeyInterop.VirtualKeyFromKey(Preferences.globalHotkeyNonModifier);

            Win32.UnregisterHotKey(hWnd, 0);
            if (!Win32.RegisterHotKey(hWnd, 0, modifiers, vk))
            {
                throw new Win32Exception();
            }
        }

        private IntPtr getMainWindowHwnd()
        {
            var windowInteropHelper = new WindowInteropHelper(MainWindow);
            windowInteropHelper.EnsureHandle();
            return (IntPtr)windowInteropHelper.Handle.ToInt32();
        }
    }
}
