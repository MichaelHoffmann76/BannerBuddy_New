using BannerBuddy.AddIn.Models;

namespace BannerBuddy.AddIn.Models
{
    public class BannerBuddyConfig
    {
        public BannerConfig Banner { get; set; }
        public VacationConfig Vacation { get; set; }
    }

    public class BannerConfig
    {
        public bool Enabled { get; set; }
        public string File { get; set; }
        public string Start { get; set; }
        public string End { get; set; }
    }

    public class VacationConfig
    {
        public bool Enabled { get; set; }
        public string Text { get; set; }
        public string Start { get; set; }
        public string End { get; set; }
        public int NoticeDays { get; set; } = 14;
    }
}
