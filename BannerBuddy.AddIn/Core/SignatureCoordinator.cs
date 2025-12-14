using System;
using BannerBuddy.AddIn.Models;
using BannerBuddy.AddIn.Storage;

namespace BannerBuddy.AddIn.Core
{
    /// <summary>
    /// Step 10.6: Koordiniert Signatur-Speicherung aus strukturierten Feldern.
    /// UI sammelt Daten → Core erzeugt HTML → Service speichert → Banner+Vacation in EINEM Durchgang anwenden.
    /// </summary>
    public class SignatureCoordinator
    {
        public void SaveSignature(SignatureInputDto dto)
        {
            var service = new SignatureService();
            var path = service.GetPrimarySignaturePath();

            if (path == null)
                throw new InvalidOperationException("Keine Signatur gefunden.");

            // Marker-Block sicherstellen (falls noch nicht vorhanden)
            service.EnsureSignatureBlock(path);

            // HTML aus Template erzeugen
            var signatureHtml = SignatureTemplate.Build(dto);

            // Banner + Vacation laden
            var configService = new ConfigService();
            var config = configService.Load();
            
            TimedContent banner = null;
            TimedContent vacation = null;
            
            if (config != null)
            {
                banner = TimedContentFactory.CreateBanner(config.Banner);
                vacation = TimedContentFactory.CreateVacation(config.Vacation);
            }
            
            // ALLES in EINEM Schritt schreiben: Signatur + Banner + Vacation
            service.UpdateAllBlocks(path, signatureHtml, banner, vacation);
        }
    }
}
