namespace BannerBuddy.AddIn.Models
{
    /// <summary>
    /// Rohe Banner-Eingaben aus der UI (unvalidiert, unformatiert).
    /// </summary>
    public class BannerInputDto
    {
        public bool? Enabled { get; set; }
        public string File { get; set; }
        public System.DateTime? StartDate { get; set; }
        public System.DateTime? EndDate { get; set; }
    }
}
