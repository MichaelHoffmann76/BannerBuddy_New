using System;
using BannerBuddy.AddIn.Models;

namespace BannerBuddy.AddIn.Core
{
    /// <summary>
    /// Step 10.6: Koordiniert Signatur-Speicherung aus strukturierten Feldern.
    /// UI sammelt Daten → Core erzeugt HTML → Service speichert → RefreshService aktualisiert.
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

            // Live-Refresh
            new RefreshService().Refresh();
        }
    }
}
