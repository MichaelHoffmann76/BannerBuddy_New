using BannerBuddy.AddIn.Models;
using BannerBuddy.AddIn.Storage;

namespace BannerBuddy.AddIn.Core
{
    /// <summary>
    /// Step 10.2: Koordiniert Save-Prozess (Validierung, Formatierung, Speichern, Refresh).
    /// UI liefert nur rohe Daten; hier findet die gesamte Geschäftslogik statt.
    /// </summary>
    public class ConfigurationCoordinator
    {
        private readonly ConfigService _configService;
        private readonly RefreshService _refreshService;

        public ConfigurationCoordinator()
        {
            _configService = new ConfigService();
            _refreshService = new RefreshService();
        }

        public SaveResult SaveConfiguration(BannerInputDto bannerInput, VacationInputDto vacationInput)
        {
            var config = _configService.Load() ?? new BannerBuddyConfig();

            // Banner verarbeiten
            var bannerResult = ProcessBanner(config, bannerInput);
            if (!bannerResult.Success)
                return bannerResult;

            // Vacation verarbeiten
            var vacationResult = ProcessVacation(config, vacationInput);
            if (!vacationResult.Success)
                return vacationResult;

            // Speichern
            _configService.Save(config);

            // Refresh (Step 9 - Live-Update)
            _refreshService.Refresh();

            return SaveResult.Ok();
        }

        private SaveResult ProcessBanner(BannerBuddyConfig config, BannerInputDto input)
        {
            if (config.Banner == null)
                config.Banner = new BannerConfig();

            // Geschäftslogik: Enabled nur wenn Datei vorhanden
            var bannerEnabled = input.Enabled == true && !string.IsNullOrWhiteSpace(input.File);

            config.Banner.Enabled = bannerEnabled;
            config.Banner.File = bannerEnabled ? input.File : null;

            if (bannerEnabled)
            {
                // Validierung: Datumspflicht
                if (!input.StartDate.HasValue || !input.EndDate.HasValue)
                    return SaveResult.Error("Bitte Start- und Enddatum für das Banner wählen.");

                var start = input.StartDate.Value.Date;
                var end = input.EndDate.Value.Date;

                // Validierung: Enddatum nach Startdatum
                if (end < start)
                    return SaveResult.Error("Banner: Enddatum muss nach dem Startdatum liegen.");

                // Formatierung: ISO 8601
                config.Banner.Start = start.ToString("yyyy-MM-dd");
                config.Banner.End = end.ToString("yyyy-MM-dd");
            }
            else
            {
                config.Banner.Start = null;
                config.Banner.End = null;
            }

            return SaveResult.Ok();
        }

        private SaveResult ProcessVacation(BannerBuddyConfig config, VacationInputDto input)
        {
            if (config.Vacation == null)
                config.Vacation = new VacationConfig();

            var vacationEnabled = input.Enabled == true;

            config.Vacation.Enabled = vacationEnabled;
            config.Vacation.Text = input.Text;

            // Parsing + Default: NoticeDays
            if (!int.TryParse((input.NoticeDaysText ?? string.Empty).Trim(), out var noticeDays))
                noticeDays = 14;

            // Validierung: Bereich
            if (noticeDays < 1 || noticeDays > 365)
                return SaveResult.Error("Urlaubs-Hinweis: Bitte eine Zahl zwischen 1 und 365 für 'Tage vorher' eingeben.");

            config.Vacation.NoticeDays = noticeDays;

            if (vacationEnabled)
            {
                // Validierung: Datumspflicht
                if (!input.StartDate.HasValue || !input.EndDate.HasValue)
                    return SaveResult.Error("Bitte Start- und Enddatum für den Urlaubs-Hinweis wählen.");

                var vStart = input.StartDate.Value.Date;
                var vEnd = input.EndDate.Value.Date;

                // Validierung: Enddatum nach Startdatum
                if (vEnd < vStart)
                    return SaveResult.Error("Urlaubs-Hinweis: Enddatum muss nach dem Startdatum liegen.");

                // Formatierung: ISO 8601
                config.Vacation.Start = vStart.ToString("yyyy-MM-dd");
                config.Vacation.End = vEnd.ToString("yyyy-MM-dd");
            }
            else
            {
                config.Vacation.Start = null;
                config.Vacation.End = null;
            }

            return SaveResult.Ok();
        }
    }
}
