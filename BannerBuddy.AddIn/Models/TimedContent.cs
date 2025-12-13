using System;

namespace BannerBuddy.AddIn.Models
{
    public class TimedContent
    {
        public bool Enabled { get; set; }
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
        public string Html { get; set; }

        public bool IsActive()
        {
            var today = DateTime.Today;
            return Enabled && today >= Start.Date && today <= End.Date;
        }
    }
}
