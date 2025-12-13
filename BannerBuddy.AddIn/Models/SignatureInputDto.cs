namespace BannerBuddy.AddIn.Models
{
    /// <summary>
    /// Step 10.6: Strukturierte Signatur-Eingabe (UI → Core).
    /// Keine HTML-Formatierung in der UI - nur Daten sammeln.
    /// </summary>
    public class SignatureInputDto
    {
        // Persönlich
        public string Greeting { get; set; }
        public string Hashtag { get; set; }
        public string Prefix { get; set; }  // i. V., i. A., ppa
        public string Name { get; set; }
        public string Role { get; set; }

        // Skyline
        public bool ShowSkyline { get; set; }
        public string SkylineImageFile { get; set; }

        // Firma
        public string Company { get; set; }
        public string Branch { get; set; }
        public string Street { get; set; }
        public string ZipCity { get; set; }

        // Kontakt
        public string Phone { get; set; }
        public string Mobile { get; set; }
        public string Email { get; set; }
        public string Website { get; set; }

        // Rechtliches
        public string ManagingDirectors { get; set; }
        public string RegisterInfo { get; set; }
        public string Jurisdiction { get; set; }

        // Social Media (URLs)
        public string LinkedInUrl { get; set; }
        public string TwitterUrl { get; set; }
        public string FacebookUrl { get; set; }
        public string XingUrl { get; set; }

        // Footer
        public bool ShowEnvironmentHint { get; set; }
    }
}
