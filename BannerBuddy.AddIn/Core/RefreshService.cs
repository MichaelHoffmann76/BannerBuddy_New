using BannerBuddy.AddIn.Storage;

namespace BannerBuddy.AddIn.Core
{
    /// <summary>
    /// Zentrale Stelle für Live-Refresh: Liest config.json und wendet Banner + Vacation sofort auf alle Signaturen an.
    /// </summary>
    public class RefreshService
    {
        public void Refresh()
        {
            var signatureService = new SignatureService();
            var configService = new ConfigService();
            var config = configService.Load();

            if (config == null)
                return;

            var banner = TimedContentFactory.CreateBanner(config.Banner);
            var vacation = TimedContentFactory.CreateVacation(config.Vacation);

            var signatures = signatureService.GetHtmlSignatures();

            foreach (var sig in signatures)
            {
                signatureService.EnsureMarkers(sig);
                signatureService.ApplyTimedContent(sig, banner, vacation);
            }
        }
    }
}
