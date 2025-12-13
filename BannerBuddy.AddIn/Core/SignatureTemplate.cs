using BannerBuddy.AddIn.Models;

namespace BannerBuddy.AddIn.Core
{
    /// <summary>
    /// Step 10.6: Zentrales HTML-Template für Firmensignatur.
    /// Dies ist der EINZIGE Ort, an dem Signatur-HTML existiert.
    /// Struktur basiert auf HUNDT CONSULT Signatur.
    /// </summary>
    public static class SignatureTemplate
    {
        public static string Build(SignatureInputDto d)
        {
            // Persönlicher Bereich
            var html = $"<p style=\"margin:0 0 8px 0;\">\n{d.Greeting}<br>\n{d.Hashtag}\n</p>\n<p style=\"margin:0 0 8px 0;\">\n{d.Prefix} <strong>{d.Name}</strong><br>\n{d.Role}\n</p>";

            // Optional: Skyline
            if (d.ShowSkyline && !string.IsNullOrWhiteSpace(d.SkylineImageFile))
            {
                html += $"\n<p style=\"margin:6px 0 8px 0;\">\n<img src=\"{d.SkylineImageFile}\" style=\"max-width:420px;\" alt=\"Skyline\" />\n</p>";
            }

            // Firmenblock
            html += $"\n<p style=\"margin:0 0 6px 0;\"><strong>{d.Company}</strong></p>\n<p style=\"margin:0 0 8px 0;\">\n{d.Branch}<br>\n{d.Street}<br>\n{d.ZipCity}\n</p>";

            // Kontaktdaten
            html += $"\n<p style=\"margin:0 0 8px 0;\">\nFon&nbsp;&nbsp;&nbsp;{d.Phone}<br>\nMobil&nbsp;{d.Mobile}<br>\nMail&nbsp;&nbsp;<a href=\"mailto:{d.Email}\">{d.Email}</a><br>\nNet&nbsp;&nbsp;&nbsp;<a href=\"{d.Website}\">{d.Website}</a>\n</p>\n\n<hr style=\"border:0; border-top:1px solid #ccc; margin:6px 0;\"/>";

            // Rechtliches (klein, mehrzeilig)
            html += $"\n<p style=\"font-size:10px; line-height:1.4; margin:0 0 8px 0;\">\nSitz der Gesellschaft: {d.Jurisdiction}<br>\nGeschäftsführer: {d.ManagingDirectors}<br>\n{d.RegisterInfo}<br>\nErfüllungsort und Gerichtsstand: {d.Jurisdiction}\n</p>";

            // Social Media Links (nur wenn mindestens ein Link vorhanden)
            var socialLinks = new System.Collections.Generic.List<string>();
            if (!string.IsNullOrWhiteSpace(d.LinkedInUrl))
                socialLinks.Add($"<a href=\"{d.LinkedInUrl}\">LinkedIn</a>");
            if (!string.IsNullOrWhiteSpace(d.TwitterUrl))
                socialLinks.Add($"<a href=\"{d.TwitterUrl}\">Twitter</a>");
            if (!string.IsNullOrWhiteSpace(d.FacebookUrl))
                socialLinks.Add($"<a href=\"{d.FacebookUrl}\">Facebook</a>");
            if (!string.IsNullOrWhiteSpace(d.XingUrl))
                socialLinks.Add($"<a href=\"{d.XingUrl}\">Xing</a>");

            if (socialLinks.Count > 0)
            {
                html += $"\n<p style=\"font-size:10px;\">\nFolgen Sie uns: {string.Join(" | ", socialLinks)}\n</p>";
            }

            // Optional: Umwelt-Hinweis
            if (d.ShowEnvironmentHint)
            {
                html += "\n<p style=\"font-size:10px;\">\nPlease consider the environment before printing this email\n</p>";
            }

            return html;
        }
    }
}
