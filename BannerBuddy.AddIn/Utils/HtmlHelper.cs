using System;
using System.Text.RegularExpressions;

namespace BannerBuddy.AddIn.Utils
{
    public static class HtmlHelper
    {
        private static int IndexOfIgnoreCase(string text, string value)
        {
            return text.IndexOf(value, StringComparison.OrdinalIgnoreCase);
        }

        private static int FindInsertionPointForBanner(string html)
        {
            // Banner nicht mitten im Satz einfügen: wenn der Umwelt-Hinweis in einem <p> steht,
            // dann direkt NACH diesem Absatz einfügen.
            var envParagraph = Regex.Match(
                html,
                "<p\\b[^>]*>.*?consider\\s+the\\s+environment.*?</p>",
                RegexOptions.IgnoreCase | RegexOptions.Singleline
            );
            if (envParagraph.Success)
                return envParagraph.Index + envParagraph.Length;

            // sonst: vor MsoAutoSig, falls vorhanden
            var autoSig = IndexOfIgnoreCase(html, "<p class=MsoAutoSig");
            if (autoSig >= 0)
                return autoSig;

            // fallback: vor </body> / </html>
            var insertAt = IndexOfIgnoreCase(html, "</body>");
            if (insertAt < 0)
                insertAt = IndexOfIgnoreCase(html, "</html>");

            return insertAt;
        }

        private static int FindInsertionPointForVacation(string html)
        {
            // Vacation soll direkt ÜBER der Grußformel stehen, aber nicht mitten in einem <span>.
            var best = Regex.Match(html, "\\bBeste\\b", RegexOptions.IgnoreCase);
            if (!best.Success)
                return -1;

            var before = html.Substring(0, best.Index);

            // Nächstes öffnendes <p ...> vor der Grußformel
            var pMatches = Regex.Matches(before, "<p\\b[^>]*>", RegexOptions.IgnoreCase | RegexOptions.Singleline);
            var pIdx = pMatches.Count > 0 ? pMatches[pMatches.Count - 1].Index : -1;

            // oder (bei "Standard") ein <div ...>
            var divMatches = Regex.Matches(before, "<div\\b[^>]*>", RegexOptions.IgnoreCase | RegexOptions.Singleline);
            var divIdx = divMatches.Count > 0 ? divMatches[divMatches.Count - 1].Index : -1;

            return Math.Max(pIdx, divIdx);
        }

        public static string FixCommonUmlautCorruption(string html)
        {
            if (string.IsNullOrEmpty(html))
                return html;

            // Repariert den häufigsten Schaden: "Beste Gr??e" -> "Beste Grüße"
            html = Regex.Replace(
                html,
                "\\bBeste\\b\\s+Gr\\?\\?e",
                "Beste Grüße",
                RegexOptions.IgnoreCase
            );

            // Konkrete, wiederkehrende Schäden aus Outlook-Signaturen (Datenverlust als '?'/"ï¿½")
            html = html.Replace("Gesch?ftsf?hrer", "Geschäftsführer");
            html = html.Replace("W?lllner", "Wüllner");
            html = html.Replace("Wülllner", "Wüllner");
            html = html.Replace("M?nkedamm", "Münkedamm");
            html = html.Replace("Mï¿½nkedamm", "Münkedamm");

            return html;
        }

        private static string InsertAt(string html, int index, string block)
        {
            if (index < 0)
                return html.TrimEnd() + "\n\n" + block;

            return html.Substring(0, index).TrimEnd()
                   + "\n\n" + block + "\n\n"
                   + html.Substring(index);
        }

        private static bool TryExtractBlock(string html, string startMarker, string endMarker, out int startIndex, out int endIndex, out string block)
        {
            startIndex = html.IndexOf(startMarker, StringComparison.Ordinal);
            if (startIndex < 0)
            {
                endIndex = -1;
                block = null;
                return false;
            }

            endIndex = html.IndexOf(endMarker, startIndex, StringComparison.Ordinal);
            if (endIndex < 0)
            {
                block = null;
                return false;
            }

            endIndex += endMarker.Length;
            block = html.Substring(startIndex, endIndex - startIndex);
            return true;
        }

        private static string RemoveAllBlocks(string html, string startMarker, string endMarker)
        {
            if (string.IsNullOrEmpty(html))
                return html;

            while (TryExtractBlock(html, startMarker, endMarker, out var s, out var e, out _))
            {
                html = html.Remove(s, e - s);
            }

            return html;
        }

        public static bool ContainsMarkers(string html)
        {
            return html.Contains(SignatureMarkers.BannerStart)
                && html.Contains(SignatureMarkers.BannerEnd)
                && html.Contains(SignatureMarkers.VacationStart)
                && html.Contains(SignatureMarkers.VacationEnd);
        }

        public static string InsertMarkers(string html)
        {
            var bannerBlock =
                $"{SignatureMarkers.BannerStart}\n" +
                $"{SignatureMarkers.BannerEnd}\n";

            var vacationBlock =
                $"{SignatureMarkers.VacationStart}\n" +
                $"{SignatureMarkers.VacationEnd}\n";

            var vacIdx = FindInsertionPointForVacation(html);
            var banIdx = FindInsertionPointForBanner(html);

            // Wenn beide Indizes gültig sind, Insert-Reihenfolge beachten
            if (vacIdx >= 0 && banIdx >= 0 && vacIdx <= banIdx)
            {
                html = InsertAt(html, vacIdx, vacationBlock);
                banIdx = FindInsertionPointForBanner(html);
                html = InsertAt(html, banIdx, bannerBlock);
                return html;
            }

            if (banIdx >= 0)
                html = InsertAt(html, banIdx, bannerBlock);
            else
                html = html.TrimEnd() + "\n\n" + bannerBlock;

            vacIdx = FindInsertionPointForVacation(html);
            html = InsertAt(html, vacIdx, vacationBlock);
            return html;
        }

        public static string EnsureMarkerBlocks(string html)
        {
            if (string.IsNullOrEmpty(html))
                return html;

            // Robust: entferne ALLE vorhandenen Marker-Blöcke (auch bei Duplikaten oder Teilzuständen)
            // und füge genau ein leeres Block-Paar an die gewünschten Stellen ein.
            html = RemoveAllBlocks(html, SignatureMarkers.BannerStart, SignatureMarkers.BannerEnd);
            html = RemoveAllBlocks(html, SignatureMarkers.VacationStart, SignatureMarkers.VacationEnd);

            var bannerBlock =
                $"{SignatureMarkers.BannerStart}\n" +
                $"{SignatureMarkers.BannerEnd}\n";

            var vacationBlock =
                $"{SignatureMarkers.VacationStart}\n" +
                $"{SignatureMarkers.VacationEnd}\n";

            var vacIdx = FindInsertionPointForVacation(html);
            var banIdx = FindInsertionPointForBanner(html);

            // Wenn beide Indizes gültig sind, Insert-Reihenfolge beachten
            if (vacIdx >= 0 && banIdx >= 0 && vacIdx <= banIdx)
            {
                html = InsertAt(html, vacIdx, vacationBlock);
                banIdx = FindInsertionPointForBanner(html);
                html = InsertAt(html, banIdx, bannerBlock);
                return html;
            }

            html = InsertAt(html, banIdx, bannerBlock);
            vacIdx = FindInsertionPointForVacation(html);
            html = InsertAt(html, vacIdx, vacationBlock);
            return html;
        }

        public static string EnsureMarkersInsideHtml(string html)
        {
            if (!ContainsMarkers(html))
                return html;

            // Extrahiere Banner- und Vacation-Block separat
            var hasBanner = TryExtractBlock(html, SignatureMarkers.BannerStart, SignatureMarkers.BannerEnd, out var bStart, out var bEnd, out var bannerBlock);
            var hasVacation = TryExtractBlock(html, SignatureMarkers.VacationStart, SignatureMarkers.VacationEnd, out var vStart, out var vEnd, out var vacationBlock);

            if (!hasBanner || !hasVacation)
                return html;

            // Entferne in absteigender Reihenfolge, damit Indizes stabil bleiben
            var without = html;
            if (vStart > bStart)
            {
                without = without.Remove(vStart, vEnd - vStart);
                without = without.Remove(bStart, bEnd - bStart);
            }
            else
            {
                without = without.Remove(bStart, bEnd - bStart);
                without = without.Remove(vStart, vEnd - vStart);
            }

            // Zielpositionen berechnen
            var vacIdx = FindInsertionPointForVacation(without);
            var banIdx = FindInsertionPointForBanner(without);

            // In Reihenfolge einfügen
            if (vacIdx >= 0 && banIdx >= 0 && vacIdx <= banIdx)
            {
                without = InsertAt(without, vacIdx, vacationBlock.Trim());
                banIdx = FindInsertionPointForBanner(without);
                without = InsertAt(without, banIdx, bannerBlock.Trim());
                return without;
            }

            without = InsertAt(without, banIdx, bannerBlock.Trim());
            vacIdx = FindInsertionPointForVacation(without);
            without = InsertAt(without, vacIdx, vacationBlock.Trim());
            return without;
        }

        public static string ReplaceBetween(
            string html,
            string startMarker,
            string endMarker,
            string newContent)
        {
            var startIndex = html.IndexOf(startMarker, StringComparison.Ordinal);
            if (startIndex == -1)
                return html;

            startIndex += startMarker.Length;

            // Wichtig: End-Marker muss NACH dem Start-Marker liegen (sonst kann es zu Duplikaten/kaputten Blöcken kommen)
            var endIndex = html.IndexOf(endMarker, startIndex, StringComparison.Ordinal);

            if (endIndex == -1)
                return html;

            return html.Substring(0, startIndex)
                   + "\n" + newContent + "\n"
                   + html.Substring(endIndex);
        }
    }
}
