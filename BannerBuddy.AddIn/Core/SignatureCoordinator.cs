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
            var html = SignatureTemplate.Build(dto);

            // Nur Marker-Block überschreiben
            service.UpdateSignatureBlock(path, html);

            // Banner + Vacation direkt anwenden (OHNE die Datei erneut einzulesen über Refresh)
            // Das verhindert, dass der gerade geschriebene Signature-Block verloren geht.
            var configService = new ConfigService();
            var config = configService.Load();
            if (config != null)
            {
                var banner = TimedContentFactory.CreateBanner(config.Banner);
                var vacation = TimedContentFactory.CreateVacation(config.Vacation);
                
                // ApplyTimedContent liest die Datei neu ein (mit der gerade geschriebenen Signatur),
                // aktualisiert Banner/Vacation und schreibt alles zurück.
                service.ApplyTimedContent(path, banner, vacation);
            }
        }
    }
}
