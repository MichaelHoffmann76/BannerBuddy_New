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
            var html = $@"<p>
{d.Greeting}<br>
{d.Hashtag}
</p>

<p>
{d.Prefix} <strong>{d.Name}</strong><br>
{d.Role}
</p>";

            // Optional: Skyline
            if (d.ShowSkyline && !string.IsNullOrWhiteSpace(d.SkylineImageFile))
            {
                html += $@"
<p>
<img src=""{d.SkylineImageFile}"" style=""max-width:420px;"" alt=""Skyline"" />
</p>";
            }

            // Firmenblock
            html += $@"
<p>
<strong>{d.Company}</strong>
</p>

<p>
{d.Branch}<br>
{d.Street}<br>
{d.ZipCity}
</p>";

            // Kontaktdaten
            html += $@"
<p>
Fon&nbsp;&nbsp;&nbsp;{d.Phone}<br>
Mobil&nbsp;{d.Mobile}<br>
Mail&nbsp;&nbsp;<a href=""mailto:{d.Email}"">{d.Email}</a><br>
Net&nbsp;&nbsp;&nbsp;<a href=""{d.Website}"">{d.Website}</a>
</p>

<hr style=""border:0; border-top:1px solid #ccc; margin:10px 0;""/>";

            // Rechtliches (klein, mehrzeilig)
            html += $@"
<p style=""font-size:10px; line-height:1.4;"">
Sitz der Gesellschaft: {d.Jurisdiction}<br>
Geschäftsführer: {d.ManagingDirectors}<br>
{d.RegisterInfo}<br>
Erfüllungsort und Gerichtsstand: {d.Jurisdiction}
</p>";

            // Social Media Links (nur wenn mindestens ein Link vorhanden)
            var socialLinks = new System.Collections.Generic.List<string>();
            if (!string.IsNullOrWhiteSpace(d.LinkedInUrl))
                socialLinks.Add($@"<a href=""{d.LinkedInUrl}"">LinkedIn</a>");
            if (!string.IsNullOrWhiteSpace(d.TwitterUrl))
                socialLinks.Add($@"<a href=""{d.TwitterUrl}"">Twitter</a>");
            if (!string.IsNullOrWhiteSpace(d.FacebookUrl))
                socialLinks.Add($@"<a href=""{d.FacebookUrl}"">Facebook</a>");
            if (!string.IsNullOrWhiteSpace(d.XingUrl))
                socialLinks.Add($@"<a href=""{d.XingUrl}"">Xing</a>");

            if (socialLinks.Count > 0)
            {
                html += $@"
<p style=""font-size:10px;"">
Folgen Sie uns: {string.Join(" | ", socialLinks)}
</p>";
            }

            // Optional: Umwelt-Hinweis
            if (d.ShowEnvironmentHint)
            {
                html += @"
<p style=""font-size:10px;"">
Please consider the environment before printing this email
</p>";
            }

            return html;
        }
    }
}
