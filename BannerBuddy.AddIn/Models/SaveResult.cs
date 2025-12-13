namespace BannerBuddy.AddIn.Models
{
    /// <summary>
    /// Ergebnis einer Save-Operation mit Erfolg/Fehler.
    /// </summary>
    public class SaveResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }

        public static SaveResult Ok() => new SaveResult { Success = true };
        public static SaveResult Error(string message) => new SaveResult { Success = false, ErrorMessage = message };
    }
}
