using System;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using BannerBuddy.AddIn.Core;
using BannerBuddy.AddIn.Models;
using BannerBuddy.AddIn.Storage;
using WpfApplication = System.Windows.Application;
using WpfShutdownMode = System.Windows.ShutdownMode;

namespace BannerBuddy.AddIn.UI
{
    public class TrayService
    {
        private NotifyIcon _trayIcon;
        private bool _started;
        private Thread _trayThread;
        private SynchronizationContext _traySync;

        public Action OnReloadRequested { get; set; }

        public void Start()
        {
            if (_started)
                return;

            _started = true;

            EnsureWpfApplication();

            _trayThread = new Thread(TrayThreadMain)
            {
                IsBackground = true,
                Name = "BannerBuddy.Tray"
            };
            _trayThread.SetApartmentState(ApartmentState.STA);
            _trayThread.Start();
        }

        public void Stop()
        {
            Exit();
        }

        private void TrayThreadMain()
        {
            try
            {
                System.Windows.Forms.Application.EnableVisualStyles();
                System.Windows.Forms.Application.SetCompatibleTextRenderingDefault(false);

                _traySync = SynchronizationContext.Current ?? new WindowsFormsSynchronizationContext();
                SynchronizationContext.SetSynchronizationContext(_traySync);

                _trayIcon = new NotifyIcon
                {
                    Icon = System.Drawing.SystemIcons.Application,
                    Visible = true
                };

                var menu = new ContextMenuStrip();
                menu.Items.Add("BannerBuddy öffnen", null, (_, __) => OpenWindow());
                menu.Items.Add("Konfiguration neu laden", null, (_, __) => ReloadConfig());
                menu.Items.Add(new ToolStripSeparator());
                menu.Items.Add("Beenden", null, (_, __) => Exit());

                _trayIcon.ContextMenuStrip = menu;

                UpdateStatusText();

                System.Windows.Forms.Application.Run();
            }
            catch
            {
                // Tray sollte Outlook nicht stören.
            }
        }

        private static void EnsureWpfApplication()
        {
            if (WpfApplication.Current != null)
                return;

            var app = new WpfApplication();
            app.ShutdownMode = WpfShutdownMode.OnExplicitShutdown;
        }

        private void OpenWindow()
        {
            try
            {
                var dispatcher = WpfApplication.Current?.Dispatcher;
                if (dispatcher == null)
                    return;

                dispatcher.Invoke(() =>
                {
                    var window = new BannerWindow();
                    window.Show();
                    window.Activate();
                });
            }
            catch
            {
                // Tray sollte Outlook nicht stören.
            }
        }

        private void ReloadConfig()
        {
            try
            {
                // STEP 9: Live-Refresh – Signatur sofort neu anwenden
                var refresher = new RefreshService();
                refresher.Refresh();

                OnReloadRequested?.Invoke();
                UpdateStatusText();

                _trayIcon?.ShowBalloonTip(
                    2000,
                    "BannerBuddy",
                    "Konfiguration neu geladen und angewendet",
                    ToolTipIcon.Info);
            }
            catch
            {
                // Tray sollte Outlook nicht stören.
            }
        }

        private void UpdateStatusText()
        {
            if (_traySync != null && SynchronizationContext.Current != _traySync)
            {
                _traySync.Post(_ => UpdateStatusText(), null);
                return;
            }

            if (_trayIcon == null)
                return;

            var config = new ConfigService().Load();
            if (config == null)
            {
                _trayIcon.Icon = System.Drawing.SystemIcons.Application;
                SetTooltip("BannerBuddy – keine Konfiguration");
                return;
            }

            if (IsBannerActiveToday(config.Banner, out var bannerUntil))
            {
                _trayIcon.Icon = System.Drawing.SystemIcons.Information;
                SetTooltip($"BannerBuddy – Banner aktiv bis {bannerUntil}");
                return;
            }

            if (IsVacationNoticeActiveToday(config.Vacation, out var noticeUntil))
            {
                _trayIcon.Icon = System.Drawing.SystemIcons.Warning;
                SetTooltip($"BannerBuddy – Urlaubs-Hinweis aktiv bis {noticeUntil}");
                return;
            }

            _trayIcon.Icon = System.Drawing.SystemIcons.Application;
            SetTooltip("BannerBuddy – nichts aktiv");
        }

        private void SetTooltip(string text)
        {
            if (_trayIcon == null)
                return;

            // Windows NotifyIcon.Text hat ein hartes Limit (typisch 63 Zeichen).
            if (string.IsNullOrWhiteSpace(text))
                text = "BannerBuddy";

            text = text.Trim();
            if (text.Length > 63)
                text = text.Substring(0, 63);

            _trayIcon.Text = text;
        }

        private static bool IsBannerActiveToday(BannerConfig banner, out string until)
        {
            until = null;

            if (banner?.Enabled != true)
                return false;

            if (!TryParseIsoDate(banner.Start, out var start) || !TryParseIsoDate(banner.End, out var end))
                return false;

            var today = DateTime.Today;
            if (today < start.Date || today > end.Date)
                return false;

            until = end.ToString("dd.MM.yyyy", CultureInfo.GetCultureInfo("de-DE"));
            return true;
        }

        private static bool IsVacationNoticeActiveToday(VacationConfig vacation, out string until)
        {
            until = null;

            if (vacation?.Enabled != true)
                return false;

            if (!TryParseIsoDate(vacation.Start, out var vacationStart) || !TryParseIsoDate(vacation.End, out var vacationEnd))
                return false;

            var noticeDays = vacation.NoticeDays;
            if (noticeDays <= 0)
                noticeDays = 14;

            var noticeStart = vacationStart.AddDays(-noticeDays).Date;
            var noticeEnd = vacationStart.AddDays(-1).Date;
            if (noticeEnd < noticeStart)
                noticeEnd = noticeStart;

            var today = DateTime.Today;
            if (today < noticeStart || today > noticeEnd)
                return false;

            until = noticeEnd.ToString("dd.MM.yyyy", CultureInfo.GetCultureInfo("de-DE"));
            return true;
        }

        private static bool TryParseIsoDate(string value, out DateTime date)
        {
            return DateTime.TryParseExact(
                value,
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out date);
        }

        private void Exit()
        {
            try
            {
                if (_traySync != null && SynchronizationContext.Current != _traySync)
                {
                    _traySync.Post(_ => Exit(), null);
                    return;
                }

                if (_trayIcon != null)
                {
                    _trayIcon.Visible = false;
                    _trayIcon.Dispose();
                    _trayIcon = null;
                }

                try
                {
                    System.Windows.Forms.Application.ExitThread();
                }
                catch
                {
                    // ignore
                }
            }
            catch
            {
                // ignore
            }
        }
    }
}
