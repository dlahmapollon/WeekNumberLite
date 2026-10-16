using System;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Win32;

namespace WeekNumberLite
{
    internal sealed class TrayApplication : ApplicationContext
    {
        private readonly NotifyIcon _notifyIcon;
        private readonly Timer _midnightTimer = new Timer();

        internal TrayApplication()
        {
            _notifyIcon = new NotifyIcon
            {
                ContextMenu = new ContextMenu(new[] { new MenuItem("Beenden", (sender, e) => ExitThread()) }),
                Visible = true
            };
            _midnightTimer.Tick += OnRefreshNeeded;
            SystemEvents.TimeChanged += OnRefreshNeeded;
            SystemEvents.DisplaySettingsChanged += OnRefreshNeeded;
            SystemEvents.PowerModeChanged += OnPowerModeChanged;
            SystemEvents.UserPreferenceChanged += OnUserPreferenceChanged;
            Refresh();
        }

        private void OnRefreshNeeded(object sender, EventArgs e) => Refresh();

        private void OnPowerModeChanged(object sender, PowerModeChangedEventArgs e)
        {
            if (e.Mode == PowerModes.Resume) Refresh();
        }

        private void OnUserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
        {
            // Switching between light and dark theme is reported as a general preference change.
            if (e.Category == UserPreferenceCategory.General ||
                e.Category == UserPreferenceCategory.Color ||
                e.Category == UserPreferenceCategory.Accessibility) Refresh();
        }

        private void Refresh()
        {
            DateTime now = DateTime.Now;
            int week = Week.Of(now);
            _notifyIcon.Text = $"KW {week}\r\n{now:dd.MM.yyyy}";
            Icon previous = _notifyIcon.Icon;
            _notifyIcon.Icon = WeekIcon.Create(week);
            previous?.Dispose();

            _midnightTimer.Stop();
            // The extra second keeps a slightly early tick from landing before midnight.
            _midnightTimer.Interval = (int)(now.Date.AddDays(1) - now).TotalMilliseconds + 1000;
            _midnightTimer.Start();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                SystemEvents.TimeChanged -= OnRefreshNeeded;
                SystemEvents.DisplaySettingsChanged -= OnRefreshNeeded;
                SystemEvents.PowerModeChanged -= OnPowerModeChanged;
                SystemEvents.UserPreferenceChanged -= OnUserPreferenceChanged;
                _midnightTimer.Dispose();
                _notifyIcon.Visible = false;
                _notifyIcon.Icon?.Dispose();
                _notifyIcon.ContextMenu.Dispose();
                _notifyIcon.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
