namespace BannerBuddy.AddIn.Models
{
    /// <summary>
    /// Rohe Urlaubs-Eingaben aus der UI (unvalidiert, unformatiert).
    /// </summary>
    public class VacationInputDto
    {
        public bool? Enabled { get; set; }
        public string Text { get; set; }
        public System.DateTime? StartDate { get; set; }
        public System.DateTime? EndDate { get; set; }
        public string NoticeDaysText { get; set; } // bewusst string - Parsing erfolgt im Service
    }
}
