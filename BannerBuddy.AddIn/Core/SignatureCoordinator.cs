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
            var logPath = System.IO.Path.Combine(
                System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments),
                "BannerBuddy",
                "signature-save.log"
            );
            
            try
            {
                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(logPath));
                System.IO.File.AppendAllText(logPath, $"\n\n=== SaveSignature STARTED: {System.DateTime.Now:yyyy-MM-dd HH:mm:ss} ===\n");
                
                var service = new SignatureService();
                System.IO.File.AppendAllText(logPath, "SignatureService created\n");
                
                var path = service.GetPrimarySignaturePath();
                System.IO.File.AppendAllText(logPath, $"Primary signature path: {path}\n");

                if (path == null)
                    throw new InvalidOperationException("Keine Signatur gefunden.");

                // Marker-Block sicherstellen (falls noch nicht vorhanden)
                var ensured = service.EnsureSignatureBlock(path);
                System.IO.File.AppendAllText(logPath, $"EnsureSignatureBlock returned: {ensured}\n");

                // HTML aus Template erzeugen
                var signatureHtml = SignatureTemplate.Build(dto);
                System.IO.File.AppendAllText(logPath, $"Template.Build returned {signatureHtml.Length} chars\n");
                System.IO.File.AppendAllText(logPath, $"Name in DTO: {dto.Name}\n");

                // Banner + Vacation laden
                var configService = new ConfigService();
                var config = configService.Load();
                
                TimedContent banner = null;
                TimedContent vacation = null;
                
                if (config != null)
                {
                    banner = TimedContentFactory.CreateBanner(config.Banner);
                    vacation = TimedContentFactory.CreateVacation(config.Vacation);
                    System.IO.File.AppendAllText(logPath, $"Banner active: {banner?.IsActive()}, Vacation active: {vacation?.IsActive()}\n");
                }
                
                // ALLES in EINEM Schritt schreiben: Signatur + Banner + Vacation
                System.IO.File.AppendAllText(logPath, "Calling UpdateAllBlocks...\n");
                service.UpdateAllBlocks(path, signatureHtml, banner, vacation);
                System.IO.File.AppendAllText(logPath, "UpdateAllBlocks completed successfully\n");
                
                // Verification
                var savedBlock = service.ReadSignatureBlock(path);
                System.IO.File.AppendAllText(logPath, $"Read back {savedBlock?.Length ?? 0} chars from signature block\n");
                if (savedBlock != null && savedBlock.Length > 50)
                {
                    System.IO.File.AppendAllText(logPath, $"First 200 chars: {savedBlock.Substring(0, System.Math.Min(200, savedBlock.Length))}\n");
                }
                
                System.IO.File.AppendAllText(logPath, "=== SaveSignature COMPLETED ===\n");
            }
            catch (System.Exception ex)
            {
                System.IO.File.AppendAllText(logPath, $"ERROR: {ex.Message}\n{ex.StackTrace}\n");
                throw;
            }
        }
    }
}
