using System;
using System.Globalization;
using System.IO;
using System.Net;
using BannerBuddy.AddIn.Models;

namespace BannerBuddy.AddIn.Core
{
    public static class TimedContentFactory
    {
        private const int DefaultVacationNoticeDays = 14;

        private static DateTime ParseIsoDate(string value)
        {
            return DateTime.ParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture);
        }

        private static string ToHtmlText(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            var encoded = WebUtility.HtmlEncode(value.Trim());
            encoded = encoded
                .Replace("\r\n", "\n")
                .Replace("\r", "\n")
                .Replace("\n", "<br/>");
            return encoded;
        }

        public static TimedContent CreateBanner(BannerConfig config)
        {
            if (config == null || !config.Enabled)
                return new TimedContent { Enabled = false };

            var bannerPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "BannerBuddy",
                "banners",
                config.File
            );

            return new TimedContent
            {
                Enabled = true,
                Start = ParseIsoDate(config.Start),
                End = ParseIsoDate(config.End),
                Html = $"<p style='margin:0cm'><img src='file:///{bannerPath.Replace("\\", "/")}' style='max-width:600px;' /></p>"
            };
        }

        public static TimedContent CreateVacation(VacationConfig config)
        {
            if (config == null || !config.Enabled)
                return new TimedContent { Enabled = false };

            if (string.IsNullOrWhiteSpace(config.Start) || string.IsNullOrWhiteSpace(config.End))
                return new TimedContent { Enabled = false };

            var vacationStart = ParseIsoDate(config.Start);
            var vacationEnd = ParseIsoDate(config.End);

            var noticeDays = config.NoticeDays;
            if (noticeDays <= 0)
                noticeDays = DefaultVacationNoticeDays;

            // Hinweis läuft VOR Urlaubsbeginn, damit es nicht mit Abwesenheitsagent kollidiert.
            var noticeStart = vacationStart.AddDays(-noticeDays);
            var noticeEnd = vacationStart.AddDays(-1);
            if (noticeEnd < noticeStart)
                noticeEnd = noticeStart;

            var de = CultureInfo.GetCultureInfo("de-DE");
            var rangeText = $"{vacationStart.ToString("dd.MM.yyyy", de)} bis {vacationEnd.ToString("dd.MM.yyyy", de)}";

            var extra = ToHtmlText(config.Text);
            var extraSuffix = string.IsNullOrWhiteSpace(extra) ? string.Empty : $"<br/>{extra}";

            var html = $"<p style='margin:0cm'><i>Hinweis: Ich werde vom {rangeText} im Urlaub sein.{extraSuffix}</i></p><p style='margin:0cm'>&nbsp;</p>";

            return new TimedContent
            {
                Enabled = true,
                Start = noticeStart,
                End = noticeEnd,
                Html = html
            };
        }
    }
}
