using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using BannerBuddy.AddIn.Models;
using BannerBuddy.AddIn.Utils;

namespace BannerBuddy.AddIn.Core
{
    public class SignatureService
    {

        private static readonly Encoding Windows1252 = Encoding.GetEncoding(1252);

        private static Encoding DetectHtmlEncoding(byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0)
                return Windows1252;

            // BOM-Erkennung
            if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
                return new UTF8Encoding(true);

            if (bytes.Length >= 2)
            {
                // UTF-16 LE / BE
                if (bytes[0] == 0xFF && bytes[1] == 0xFE)
                    return Encoding.Unicode;
                if (bytes[0] == 0xFE && bytes[1] == 0xFF)
                    return Encoding.BigEndianUnicode;
            }

            // Meta-Charset (ASCII-sicher im Kopfbereich)
            var headLen = Math.Min(bytes.Length, 4096);
            var head = Encoding.ASCII.GetString(bytes, 0, headLen);
            var m = Regex.Match(head, "charset\\s*=\\s*([A-Za-z0-9_\"'-]+)", RegexOptions.IgnoreCase);
            if (m.Success)
            {
                var token = m.Groups[1].Value.Trim().Trim('\"', '\'');
                if (token.IndexOf("utf-8", StringComparison.OrdinalIgnoreCase) >= 0)
                    return new UTF8Encoding(false);
                if (token.IndexOf("windows-1252", StringComparison.OrdinalIgnoreCase) >= 0)
                    return Windows1252;
                if (token.IndexOf("iso-8859-1", StringComparison.OrdinalIgnoreCase) >= 0)
                    return Windows1252;
            }

            // Fallback: erst UTF-8 strict versuchen, sonst Windows-1252
            try
            {
                var utf8Strict = new UTF8Encoding(false, true);
                utf8Strict.GetString(bytes);
                return new UTF8Encoding(false);
            }
            catch
            {
                return Windows1252;
            }
        }

        private static string ReadHtml(string path, out Encoding encoding)
        {
            var bytes = File.ReadAllBytes(path);
            encoding = DetectHtmlEncoding(bytes);
            return encoding.GetString(bytes);
        }

        private static void WriteHtml(string path, string html, Encoding encoding)
        {
            if (encoding == null)
                encoding = Windows1252;

            File.WriteAllBytes(path, encoding.GetBytes(html));
        }

        private static string NormalizeMetaCharset(string html, string charset)
        {
            if (string.IsNullOrEmpty(html) || string.IsNullOrEmpty(charset))
                return html;

            // Ersetzt vorhandene charset=... Angaben in Meta-Tags.
            // (Outlook-Signaturen enthalten i.d.R. <meta http-equiv=Content-Type ... charset=...>)
            var rx = new Regex("charset\\s*=\\s*([^\"'\\s>]+)", RegexOptions.IgnoreCase);
            if (rx.IsMatch(html))
                return rx.Replace(html, "charset=" + charset);

            return html;
        }

        private static Encoding DetectTextEncoding(byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0)
                return Encoding.Unicode;

            if (bytes.Length >= 2)
            {
                if (bytes[0] == 0xFF && bytes[1] == 0xFE)
                    return Encoding.Unicode;
                if (bytes[0] == 0xFE && bytes[1] == 0xFF)
                    return Encoding.BigEndianUnicode;
            }

            if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
                return new UTF8Encoding(true);

            // Fallback
            return new UTF8Encoding(false);
        }

        private static string ReadPlainText(string path)
        {
            var bytes = File.ReadAllBytes(path);
            var enc = DetectTextEncoding(bytes);
            return enc.GetString(bytes);
        }

        private static string ApplyUmlautFixesFromPlainText(string html, string plainText)
        {
            if (string.IsNullOrEmpty(html) || string.IsNullOrEmpty(plainText))
                return html;

            // Wörter mit deutschen Umlauten/ß aus der .txt extrahieren und deren '?'-Variante im HTML ersetzen.
            var umlautWords = Regex.Matches(plainText, "\\p{L}{3,}")
                .Cast<Match>()
                .Select(m => m.Value)
                .Where(w => w.IndexOfAny(new[] { 'ä', 'ö', 'ü', 'Ä', 'Ö', 'Ü', 'ß' }) >= 0)
                .Distinct(StringComparer.Ordinal)
                .ToArray();

            foreach (var word in umlautWords)
            {
                var q = word
                    .Replace('ä', '?').Replace('ö', '?').Replace('ü', '?')
                    .Replace('Ä', '?').Replace('Ö', '?').Replace('Ü', '?')
                    .Replace('ß', '?');

                if (q.Length < 3 || q == word)
                    continue;

                // Simple Replace ist hier ok: wir ersetzen nur '?' Varianten, die in Signaturtexten auftreten.
                html = html.Replace(q, word);
            }

            return html;
        }

        private static string RemoveDuplicateBannerImagesOutsideMarkerBlock(string html, string bannerHtml)
        {
            if (string.IsNullOrWhiteSpace(html) || string.IsNullOrWhiteSpace(bannerHtml))
                return html;

            var blockStart = html.IndexOf(SignatureMarkers.BannerStart, StringComparison.Ordinal);
            if (blockStart < 0)
                return html;

            var blockEndMarker = html.IndexOf(SignatureMarkers.BannerEnd, blockStart, StringComparison.Ordinal);
            if (blockEndMarker < 0)
                return html;

            var blockEnd = blockEndMarker + SignatureMarkers.BannerEnd.Length;

            // Extrahiere src=... aus dem Banner-HTML und entferne identische <img>-Tags außerhalb des Marker-Blocks.
            var srcMatches = Regex.Matches(bannerHtml, "src\\s*=\\s*['\"](?<src>[^'\"]+)['\"]", RegexOptions.IgnoreCase);
            var sources = srcMatches.Cast<Match>()
                .Select(m => m.Groups["src"].Value)
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            foreach (var src in sources)
            {
                var searchFrom = 0;
                while (true)
                {
                    var hit = html.IndexOf(src, searchFrom, StringComparison.OrdinalIgnoreCase);
                    if (hit < 0)
                        break;

                    // Treffer innerhalb Marker-Block -> überspringen
                    if (hit >= blockStart && hit <= blockEnd)
                    {
                        searchFrom = hit + src.Length;
                        continue;
                    }

                    // Rückwärts zum <img suchen
                    var imgStart = html.LastIndexOf("<img", hit, StringComparison.OrdinalIgnoreCase);
                    if (imgStart < 0)
                    {
                        searchFrom = hit + src.Length;
                        continue;
                    }

                    // Vorwärts bis zum nächsten '>' (Tag-Ende)
                    var imgEnd = html.IndexOf('>', hit);
                    if (imgEnd < 0)
                    {
                        searchFrom = hit + src.Length;
                        continue;
                    }

                    // Nur entfernen, wenn der src tatsächlich innerhalb dieses <img ...> Bereichs liegt
                    if (hit < imgStart || hit > imgEnd)
                    {
                        searchFrom = hit + src.Length;
                        continue;
                    }

                    var removeLen = (imgEnd - imgStart) + 1;
                    html = html.Remove(imgStart, removeLen);
                    searchFrom = imgStart;
                }
            }

            return html;
        }

        private static string RemoveBannerBuddyBannerImagesOutsideMarkerBlock(string html)
        {
            if (string.IsNullOrWhiteSpace(html))
                return html;

            var blockStart = html.IndexOf(SignatureMarkers.BannerStart, StringComparison.Ordinal);
            if (blockStart < 0)
                return html;

            var blockEndMarker = html.IndexOf(SignatureMarkers.BannerEnd, blockStart, StringComparison.Ordinal);
            if (blockEndMarker < 0)
                return html;

            var blockEnd = blockEndMarker + SignatureMarkers.BannerEnd.Length;

            var before = html.Substring(0, blockStart);
            var block = html.Substring(blockStart, blockEnd - blockStart);
            var after = html.Substring(blockEnd);

            // Entferne (außerhalb des Marker-Blocks) alle <p><img ...> bzw. <img ...> die auf unseren BannerBuddy-Ordner zeigen.
            // Das verhindert Duplikate, wenn der Banner geändert/deaktiviert wird und früher einmal "außerhalb" eingefügt wurde.
            var pImgPattern = new Regex(
                "<p\\b[^>]*>\\s*<img\\b[^>]*\\bsrc\\s*=\\s*(['\"])[^'\"]*/BannerBuddy/banners/[^'\"]+\\1[^>]*>\\s*</p>",
                RegexOptions.IgnoreCase | RegexOptions.Singleline
            );

            var imgPattern = new Regex(
                "<img\\b[^>]*\\bsrc\\s*=\\s*(['\"])[^'\"]*/BannerBuddy/banners/[^'\"]+\\1[^>]*>",
                RegexOptions.IgnoreCase | RegexOptions.Singleline
            );

            var vmlImageDataPattern = new Regex(
                "<v:imagedata\\b[^>]*\\bsrc\\s*=\\s*(['\"])[^'\"]*/BannerBuddy/banners/[^'\"]+\\1[^>]*/?>",
                RegexOptions.IgnoreCase | RegexOptions.Singleline
            );

            before = pImgPattern.Replace(before, string.Empty);
            before = vmlImageDataPattern.Replace(before, string.Empty);
            before = imgPattern.Replace(before, string.Empty);

            after = pImgPattern.Replace(after, string.Empty);
            after = vmlImageDataPattern.Replace(after, string.Empty);
            after = imgPattern.Replace(after, string.Empty);

            return before + block + after;
        }

        public string GetSignatureFolder()
        {
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Microsoft",
                "Signatures"
            );
        }

        public string[] GetHtmlSignatures()
        {
            var folder = GetSignatureFolder();

            if (!Directory.Exists(folder))
                return Array.Empty<string>();

            return Directory
                .GetFiles(folder, "*.htm", SearchOption.TopDirectoryOnly);
        }

        public string ReadSignature(string filePath)
        {
            return ReadHtml(filePath, out _);
        }

        public bool EnsureMarkers(string signaturePath)
        {
            var html = ReadHtml(signaturePath, out var enc);

            var txtPath = Path.ChangeExtension(signaturePath, ".txt");
            if (File.Exists(txtPath))
            {
                var plain = ReadPlainText(txtPath);
                html = ApplyUmlautFixesFromPlainText(html, plain);
            }

            var normalized = HtmlHelper.EnsureMarkerBlocks(html);
            if (normalized == html)
                return false; // nichts zu tun

            normalized = HtmlHelper.FixCommonUmlautCorruption(normalized);
            normalized = NormalizeMetaCharset(normalized, "utf-8");
            WriteHtml(signaturePath, normalized, new UTF8Encoding(false));
            return true;
        }

        public void ApplyTimedContent(
            string signaturePath,
            TimedContent banner,
            TimedContent vacation)
        {
            var html = ReadHtml(signaturePath, out var enc);

            var txtPath = Path.ChangeExtension(signaturePath, ".txt");
            if (File.Exists(txtPath))
            {
                var plain = ReadPlainText(txtPath);
                html = ApplyUmlautFixesFromPlainText(html, plain);
            }

            html = HtmlHelper.FixCommonUmlautCorruption(html);

            // Robust gegen Altzustände: sorge für genau ein Marker-Paar je Block.
            html = HtmlHelper.EnsureMarkerBlocks(html);

            html = HtmlHelper.ReplaceBetween(
                html,
                SignatureMarkers.BannerStart,
                SignatureMarkers.BannerEnd,
                banner.IsActive() ? banner.Html : string.Empty
            );

            html = HtmlHelper.ReplaceBetween(
                html,
                SignatureMarkers.VacationStart,
                SignatureMarkers.VacationEnd,
                vacation.IsActive() ? vacation.Html : string.Empty
            );

            // Entferne alte Banner-Bilder aus früheren Versionen (außerhalb Marker), egal ob Banner aktiv ist.
            html = RemoveBannerBuddyBannerImagesOutsideMarkerBlock(html);

            html = NormalizeMetaCharset(html, "utf-8");
            WriteHtml(signaturePath, html, new UTF8Encoding(false));
        }

        // Step 10.4: Signatur-Block einmalig anlegen (falls nicht vorhanden)
        public bool EnsureSignatureBlock(string signaturePath)
        {
            var html = ReadHtml(signaturePath, out var enc);

            // Schon vorhanden? Dann nichts tun.
            if (html.Contains(SignatureMarkers.SignatureStart))
                return false;

            // Minimaler, neutraler Initialinhalt
            var insert =
                "\n\n" +
                SignatureMarkers.SignatureStart + "\n" +
                "<p><!-- Signaturinhalt hier bearbeiten --></p>\n" +
                SignatureMarkers.SignatureEnd + "\n";

            WriteHtml(signaturePath, html + insert, enc);
            return true;
        }

        // Step 10.5: Signatur-Block aktualisieren (nur zwischen Markern)
        public void UpdateSignatureBlock(string signaturePath, string newContent)
        {
            var html = ReadHtml(signaturePath, out var enc);

            var start = html.IndexOf(SignatureMarkers.SignatureStart);
            var end = html.IndexOf(SignatureMarkers.SignatureEnd);

            if (start < 0 || end < start)
                throw new InvalidOperationException("Signaturbereich nicht gefunden.");

            start += SignatureMarkers.SignatureStart.Length;

            var updated =
                html.Substring(0, start) +
                "\n" + newContent + "\n" +
                html.Substring(end);

            WriteHtml(signaturePath, updated, enc);
        }

        // Step 10.3.2: Signatur-Block lesen (nur zwischen Markern)
        public string ReadSignatureBlock(string signaturePath)
        {
            var html = ReadHtml(signaturePath, out _);

            var start = html.IndexOf(SignatureMarkers.SignatureStart);
            var end = html.IndexOf(SignatureMarkers.SignatureEnd);

            if (start < 0 || end < 0 || end <= start)
                return null;

            start += SignatureMarkers.SignatureStart.Length;

            return html.Substring(start, end - start).Trim();
        }

        // Step 10.3.3: Primäre Signatur ermitteln (bewusst simpel)
        public string GetPrimarySignaturePath()
        {
            var signatures = GetHtmlSignatures();
            return signatures.FirstOrDefault();
        }
    }
}
