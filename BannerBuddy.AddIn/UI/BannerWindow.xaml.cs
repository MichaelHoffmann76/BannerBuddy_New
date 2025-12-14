using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media.Imaging;
using BannerBuddy.AddIn.Core;
using BannerBuddy.AddIn.Models;
using BannerBuddy.AddIn.Storage;

namespace BannerBuddy.AddIn.UI
{
    public partial class BannerWindow : Window
    {
        private readonly string _bannerFolder;
        private readonly ConfigService _configService;
        private bool _suppressSelection;
        private string _currentBannerFile;

        public BannerWindow()
        {
            InitializeComponent();

            _bannerFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "BannerBuddy",
                "banners"
            );

            Directory.CreateDirectory(_bannerFolder);

            _configService = new ConfigService();

            LoadBannerList();
            LoadUiFromConfig();
            LoadSignatureFields(); // Step 10.6
            UpdateSignaturePreview(); // Step 10.8: Initiale Vorschau
        }

        // Step 10.6: Signatur-Felder mit Standardwerten initialisieren
        private void LoadSignatureFields()
        {
            // Versuche, aus existierender Signatur zu laden
            try
            {
                var service = new SignatureService();
                var path = service.GetPrimarySignaturePath();
                
                if (!string.IsNullOrEmpty(path))
                {
                    // Signatur-Block lesen
                    var html = service.ReadSignatureBlock(path);
                    
                    if (!string.IsNullOrEmpty(html))
                    {
                        // Einfaches HTML-Parsing (Regex für die wichtigsten Felder)
                        var greetingMatch = System.Text.RegularExpressions.Regex.Match(html, @"<p[^>]*>(.*?)<br");
                        if (greetingMatch.Success) GreetingText.Text = System.Web.HttpUtility.HtmlDecode(greetingMatch.Groups[1].Value.Trim());
                        
                        var hashtagMatch = System.Text.RegularExpressions.Regex.Match(html, @"<br>\s*(#\S+)");
                        if (hashtagMatch.Success) HashtagText.Text = hashtagMatch.Groups[1].Value.Trim();
                        
                        var prefixMatch = System.Text.RegularExpressions.Regex.Match(html, @"</p>\s*<p[^>]*>\s*(i\.\s*[VA]\.|ppa)?");
                        if (prefixMatch.Success && !string.IsNullOrWhiteSpace(prefixMatch.Groups[1].Value))
                        {
                            var prefix = prefixMatch.Groups[1].Value.Trim();
                            foreach (System.Windows.Controls.ComboBoxItem item in PrefixCombo.Items)
                            {
                                if (item.Content.ToString() == prefix)
                                {
                                    PrefixCombo.SelectedItem = item;
                                    break;
                                }
                            }
                        }
                        
                        var nameMatch = System.Text.RegularExpressions.Regex.Match(html, @"<strong>(.*?)</strong>");
                        if (nameMatch.Success) NameText.Text = System.Web.HttpUtility.HtmlDecode(nameMatch.Groups[1].Value.Trim());
                        
                        var roleMatch = System.Text.RegularExpressions.Regex.Match(html, @"</strong><br>\s*(.*?)\s*</p>", System.Text.RegularExpressions.RegexOptions.Singleline);
                        if (roleMatch.Success) RoleText.Text = System.Web.HttpUtility.HtmlDecode(roleMatch.Groups[1].Value.Trim());
                        
                        SkylineEnabled.IsChecked = html.Contains("<img");
                        
                        var companyMatch = System.Text.RegularExpressions.Regex.Match(html, @"<p[^>]*><strong>(.*?GmbH.*?)</strong></p>");
                        if (companyMatch.Success) CompanyText.Text = System.Web.HttpUtility.HtmlDecode(companyMatch.Groups[1].Value.Trim());
                        
                        var lines = System.Text.RegularExpressions.Regex.Matches(html, @"(?:Niederlassung |)(.*?)<br>|<br>\s*(.*?)\s*</p>");
                        if (lines.Count > 0)
                        {
                            var addressLines = new System.Collections.Generic.List<string>();
                            foreach (System.Text.RegularExpressions.Match m in lines)
                            {
                                var val = System.Web.HttpUtility.HtmlDecode((m.Groups[1].Value + m.Groups[2].Value).Trim());
                                if (!string.IsNullOrWhiteSpace(val) && !val.Contains("Fon") && !val.Contains("HUNDT"))
                                    addressLines.Add(val);
                            }
                            if (addressLines.Count >= 3)
                            {
                                BranchText.Text = addressLines[0];
                                StreetText.Text = addressLines[1];
                                ZipCityText.Text = addressLines[2];
                            }
                        }
                        
                        var phoneMatch = System.Text.RegularExpressions.Regex.Match(html, @"Fon&nbsp;&nbsp;&nbsp;(.*?)<br>");
                        if (phoneMatch.Success) PhoneText.Text = System.Web.HttpUtility.HtmlDecode(phoneMatch.Groups[1].Value.Trim());
                        
                        var mobileMatch = System.Text.RegularExpressions.Regex.Match(html, @"Mobil&nbsp;(.*?)<br>");
                        if (mobileMatch.Success) MobileText.Text = System.Web.HttpUtility.HtmlDecode(mobileMatch.Groups[1].Value.Trim());
                        
                        var emailMatch = System.Text.RegularExpressions.Regex.Match(html, @"href=""mailto:(.*?)""");
                        if (emailMatch.Success) EmailText.Text = emailMatch.Groups[1].Value.Trim();
                        
                        var websiteMatch = System.Text.RegularExpressions.Regex.Match(html, @"href=""(www\.[^""]+)""");
                        if (websiteMatch.Success) WebsiteText.Text = websiteMatch.Groups[1].Value.Trim();
                        
                        var jurisdictionMatch = System.Text.RegularExpressions.Regex.Match(html, @"Sitz der Gesellschaft:\s*(.*?)<br>");
                        if (jurisdictionMatch.Success) JurisdictionText.Text = System.Web.HttpUtility.HtmlDecode(jurisdictionMatch.Groups[1].Value.Trim());
                        
                        var directorsMatch = System.Text.RegularExpressions.Regex.Match(html, @"Geschäftsführer:\s*(.*?)<br>");
                        if (directorsMatch.Success) DirectorsText.Text = System.Web.HttpUtility.HtmlDecode(directorsMatch.Groups[1].Value.Trim());
                        
                        var registerMatch = System.Text.RegularExpressions.Regex.Match(html, @"(Registergericht:.*?)<br>");
                        if (registerMatch.Success) RegisterText.Text = System.Web.HttpUtility.HtmlDecode(registerMatch.Groups[1].Value.Trim());
                        
                        var linkedinMatch = System.Text.RegularExpressions.Regex.Match(html, @"href=""(https://www\.linkedin\.com/[^""]+)""");
                        if (linkedinMatch.Success) LinkedInUrl.Text = linkedinMatch.Groups[1].Value.Trim();
                        
                        var twitterMatch = System.Text.RegularExpressions.Regex.Match(html, @"href=""(https://twitter\.com/[^""]+)""");
                        if (twitterMatch.Success) TwitterUrl.Text = twitterMatch.Groups[1].Value.Trim();
                        
                        EnvHintCheck.IsChecked = html.Contains("consider the environment");
                    }
                }
            }
            catch
            {
                // Fallback auf Standard-Werte
                GreetingText.Text = "Beste Grüße";
                HashtagText.Text = "#GernPerDu";
                NameText.Text = "Michael Hoffmann";
                RoleText.Text = "Niederlassungsleiter | Projektleiter";
                SkylineEnabled.IsChecked = true;
                CompanyText.Text = "HUNDT CONSULT GmbH";
                BranchText.Text = "Niederlassung Hamburg";
                StreetText.Text = "Mönkedamm 9";
                ZipCityText.Text = "20457 Hamburg";
                PhoneText.Text = "+49 40 33 44 153 266";
                MobileText.Text = "+49 175 340 28 57";
                EmailText.Text = "m.hoffmann@hundt-consult.de";
                WebsiteText.Text = "www.hundt-consult.de";
                JurisdictionText.Text = "Hamburg";
                DirectorsText.Text = "Alexander Wüllner, Falko Stolte";
                RegisterText.Text = "Registergericht: Hamburg, HRB 104958";
                LinkedInUrl.Text = "https://www.linkedin.com/company/hundt-consult";
                TwitterUrl.Text = "https://twitter.com/hundtconsult";
                EnvHintCheck.IsChecked = true;
            }

            // Step 10.8: Event-Handler für Live-Vorschau
            GreetingText.TextChanged += SignatureField_Changed;
            HashtagText.TextChanged += SignatureField_Changed;
            PrefixCombo.SelectionChanged += SignatureField_Changed;
            NameText.TextChanged += SignatureField_Changed;
            RoleText.TextChanged += SignatureField_Changed;
            SkylineEnabled.Checked += SignatureField_Changed;
            SkylineEnabled.Unchecked += SignatureField_Changed;
            CompanyText.TextChanged += SignatureField_Changed;
            BranchText.TextChanged += SignatureField_Changed;
            StreetText.TextChanged += SignatureField_Changed;
            ZipCityText.TextChanged += SignatureField_Changed;
            PhoneText.TextChanged += SignatureField_Changed;
            MobileText.TextChanged += SignatureField_Changed;
            EmailText.TextChanged += SignatureField_Changed;
            WebsiteText.TextChanged += SignatureField_Changed;
            JurisdictionText.TextChanged += SignatureField_Changed;
            DirectorsText.TextChanged += SignatureField_Changed;
            RegisterText.TextChanged += SignatureField_Changed;
            LinkedInUrl.TextChanged += SignatureField_Changed;
            TwitterUrl.TextChanged += SignatureField_Changed;
            FacebookUrl.TextChanged += SignatureField_Changed;
            XingUrl.TextChanged += SignatureField_Changed;
            EnvHintCheck.Checked += SignatureField_Changed;
            EnvHintCheck.Unchecked += SignatureField_Changed;
        }

        // Step 10.8.3: DTO aus UI sammeln (zentral, DRY)
        private SignatureInputDto CollectSignatureDtoFromUI()
        {
            // Präfix aus ComboBox auslesen
            var prefix = (PrefixCombo.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Content?.ToString() ?? "i. V.";
            if (prefix == "(kein Präfix)")
                prefix = string.Empty;

            return new SignatureInputDto
            {
                Greeting = GreetingText.Text,
                Hashtag = HashtagText.Text,
                Prefix = prefix,
                Name = NameText.Text,
                Role = RoleText.Text,
                ShowSkyline = SkylineEnabled.IsChecked == true,
                SkylineImageFile = "file:///" + Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    "BannerBuddy",
                    "skyline.png"
                ).Replace("\\", "/"),
                Company = CompanyText.Text,
                Branch = BranchText.Text,
                Street = StreetText.Text,
                ZipCity = ZipCityText.Text,
                Phone = PhoneText.Text,
                Mobile = MobileText.Text,
                Email = EmailText.Text,
                Website = WebsiteText.Text,
                Jurisdiction = JurisdictionText.Text,
                ManagingDirectors = DirectorsText.Text,
                RegisterInfo = RegisterText.Text,
                LinkedInUrl = LinkedInUrl.Text,
                TwitterUrl = TwitterUrl.Text,
                FacebookUrl = FacebookUrl.Text,
                XingUrl = XingUrl.Text,
                ShowEnvironmentHint = EnvHintCheck.IsChecked == true
            };
        }

        // Step 10.8.2: Vorschau aktualisieren
        private void UpdateSignaturePreview()
        {
            try
            {
                var dto = CollectSignatureDtoFromUI();
                var html = SignatureTemplate.Build(dto);

                // HTML minimal einbetten
                var document = $@"
<html>
<head>
<meta charset='utf-8'>
<style>
body {{
    font-family: Arial, sans-serif;
    font-size: 12px;
    color: #000;
    margin: 10px;
}}
a {{
    color: #0066cc;
    text-decoration: none;
}}
a:hover {{
    text-decoration: underline;
}}
</style>
</head>
<body>
{html}
</body>
</html>";

                SignaturePreviewBrowser.NavigateToString(document);
            }
            catch
            {
                // Vorschau-Fehler nicht anzeigen (optionale Funktion)
            }
        }

        // Step 10.8.4: Event-Handler für Live-Vorschau
        private void SignatureField_Changed(object sender, EventArgs e)
        {
            UpdateSignaturePreview();
        }

        private void LoadBannerList()
        {
            var files = Directory.Exists(_bannerFolder)
                ? Directory.GetFiles(_bannerFolder)
                    .Where(IsSupportedImage)
                    .Select(Path.GetFileName)
                    .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                    .ToArray()
                : Array.Empty<string>();

            _suppressSelection = true;
            BannerSelect.ItemsSource = files;
            _suppressSelection = false;
        }

        private void LoadUiFromConfig()
        {
            var config = _configService.Load();
            if (config?.Banner != null)
            {
                BannerEnabled.IsChecked = config.Banner.Enabled;

                if (DateTime.TryParse(config.Banner.Start, out var start))
                    StartDate.SelectedDate = start;
                if (DateTime.TryParse(config.Banner.End, out var end))
                    EndDate.SelectedDate = end;

                var file = config.Banner.File;
                if (!string.IsNullOrWhiteSpace(file))
                {
                    var full = Path.Combine(_bannerFolder, file);
                    if (File.Exists(full))
                    {
                        _currentBannerFile = file;

                        _suppressSelection = true;
                        BannerSelect.SelectedItem = file;
                        _suppressSelection = false;

                        ShowPreview(full);
                    }
                }
            }
            else
            {
                BannerEnabled.IsChecked = true;
            }

            if (config?.Vacation != null)
            {
                VacationEnabled.IsChecked = config.Vacation.Enabled;
                VacationText.Text = config.Vacation.Text ?? string.Empty;

                var noticeDays = config.Vacation.NoticeDays;
                if (noticeDays <= 0)
                    noticeDays = 14;
                VacationNoticeDays.Text = noticeDays.ToString();

                if (DateTime.TryParse(config.Vacation.Start, out var vStart))
                    VacationStartDate.SelectedDate = vStart;
                if (DateTime.TryParse(config.Vacation.End, out var vEnd))
                    VacationEndDate.SelectedDate = vEnd;
            }
            else
            {
                VacationEnabled.IsChecked = false;
                VacationText.Text = string.Empty;
                VacationNoticeDays.Text = "14";
            }

            // Default-Datumsvorschlag, falls leer
            if (StartDate.SelectedDate == null)
                StartDate.SelectedDate = DateTime.Today;
            if (EndDate.SelectedDate == null)
                EndDate.SelectedDate = DateTime.Today.AddDays(14);

            if (VacationStartDate.SelectedDate == null)
                VacationStartDate.SelectedDate = DateTime.Today;
            if (VacationEndDate.SelectedDate == null)
                VacationEndDate.SelectedDate = DateTime.Today.AddDays(14);
        }

        private static bool IsSupportedImage(string path)
        {
            var ext = Path.GetExtension(path).ToLowerInvariant();
            return ext == ".gif" || ext == ".png" || ext == ".jpg" || ext == ".jpeg";
        }

        private void DropZone_OnDragOver(object sender, DragEventArgs e)
        {
            // Debug: Alle verfügbaren Formate ausgeben
            var formats = e.Data.GetFormats();
            System.Diagnostics.Debug.WriteLine($"Drag-Formate: {string.Join(", ", formats)}");

            // Akzeptiere alles, was irgendwie ein Bild sein könnte
            e.Effects = DragDropEffects.Copy;
            e.Handled = true;
        }

        private void DropZone_OnDrop(object sender, DragEventArgs e)
        {
            try
            {
                var formats = e.Data.GetFormats();
                System.Diagnostics.Debug.WriteLine($"Drop-Formate: {string.Join(", ", formats)}");

                // 1. Dateien vom Dateisystem
                if (e.Data.GetDataPresent(DataFormats.FileDrop))
                {
                    var files = (string[])e.Data.GetData(DataFormats.FileDrop);
                    var file = files.FirstOrDefault();

                    if (file == null)
                        return;

                    var ext = Path.GetExtension(file).ToLowerInvariant();
                    if (ext != ".gif" && ext != ".png" && ext != ".jpg" && ext != ".jpeg")
                    {
                        MessageBox.Show("Nur GIF, PNG oder JPG erlaubt.");
                        return;
                    }

                    var targetPath = Path.Combine(_bannerFolder, Path.GetFileName(file));
                    File.Copy(file, targetPath, overwrite: true);

                    _currentBannerFile = Path.GetFileName(targetPath);

                    LoadBannerList();
                    _suppressSelection = true;
                    BannerSelect.SelectedItem = _currentBannerFile;
                    _suppressSelection = false;

                    ShowPreview(targetPath);
                    return;
                }

                // 2. Versuche alle verfügbaren Formate für Bilder
                BitmapSource imageSource = null;

                // Probiere verschiedene bekannte Bildformate
                foreach (var format in formats)
                {
                    try
                    {
                        var data = e.Data.GetData(format);
                        System.Diagnostics.Debug.WriteLine($"Format '{format}': {data?.GetType().Name ?? "null"}");

                        // System.Drawing.Bitmap
                        if (data is System.Drawing.Bitmap bitmap)
                        {
                            imageSource = ConvertBitmapToBitmapSource(bitmap);
                            break;
                        }

                        // MemoryStream (PNG, GIF, etc.)
                        if (data is MemoryStream memStream && memStream.Length > 0)
                        {
                            memStream.Position = 0;
                            try
                            {
                                var decoder = BitmapDecoder.Create(memStream, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
                                if (decoder.Frames.Count > 0)
                                {
                                    imageSource = decoder.Frames[0];
                                    break;
                                }
                            }
                            catch { }
                        }

                        // Byte-Array
                        if (data is byte[] bytes && bytes.Length > 0)
                        {
                            using (var stream = new MemoryStream(bytes))
                            {
                                try
                                {
                                    var decoder = BitmapDecoder.Create(stream, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
                                    if (decoder.Frames.Count > 0)
                                    {
                                        imageSource = decoder.Frames[0];
                                        break;
                                    }
                                }
                                catch { }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Format '{format}' fehlgeschlagen: {ex.Message}");
                    }
                }

                if (imageSource != null)
                {
                    SaveDroppedImage(imageSource);
                }
                else
                {
                    MessageBox.Show($"Konnte kein Bild aus den Drag-Daten extrahieren.\\n\\nVerfügbare Formate:\\n{string.Join("\\n", formats)}");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Laden des Bildes: {ex.Message}");
            }
        }

        private BitmapSource ConvertBitmapToBitmapSource(System.Drawing.Bitmap bitmap)
        {
            var hBitmap = bitmap.GetHbitmap();
            try
            {
                return System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                    hBitmap,
                    IntPtr.Zero,
                    Int32Rect.Empty,
                    BitmapSizeOptions.FromEmptyOptions());
            }
            finally
            {
                DeleteObject(hBitmap);
            }
        }

        [System.Runtime.InteropServices.DllImport("gdi32.dll")]
        private static extern bool DeleteObject(IntPtr hObject);

        private void SaveDroppedImage(BitmapSource imageSource)
        {
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var filename = $"dropped_{timestamp}.png";
            var targetPath = Path.Combine(_bannerFolder, filename);

            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(imageSource));

            using (var stream = new FileStream(targetPath, FileMode.Create))
            {
                encoder.Save(stream);
            }

            _currentBannerFile = filename;

            LoadBannerList();
            _suppressSelection = true;
            BannerSelect.SelectedItem = _currentBannerFile;
            _suppressSelection = false;

            ShowPreview(targetPath);
        }

        private void BannerSelect_OnSelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (_suppressSelection)
                return;

            var file = BannerSelect.SelectedItem as string;
            if (string.IsNullOrWhiteSpace(file))
                return;

            var full = Path.Combine(_bannerFolder, file);
            if (!File.Exists(full))
                return;

            _currentBannerFile = file;
            ShowPreview(full);
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Step 10.2: Banner + Urlaub speichern
                var bannerData = new BannerInputDto
                {
                    Enabled = BannerEnabled.IsChecked,
                    File = _currentBannerFile,
                    StartDate = StartDate.SelectedDate,
                    EndDate = EndDate.SelectedDate
                };

                var vacationData = new VacationInputDto
                {
                    Enabled = VacationEnabled.IsChecked,
                    Text = VacationText.Text,
                    NoticeDaysText = VacationNoticeDays.Text,
                    StartDate = VacationStartDate.SelectedDate,
                    EndDate = VacationEndDate.SelectedDate
                };

                var configCoordinator = new ConfigurationCoordinator();
                var result = configCoordinator.SaveConfiguration(bannerData, vacationData);

                if (!result.Success)
                {
                    MessageBox.Show(result.ErrorMessage);
                    return;
                }

                // Step 10.6: Signatur speichern (nutzt CollectSignatureDtoFromUI)
                var signatureDto = CollectSignatureDtoFromUI();

                try
                {
                    new SignatureCoordinator().SaveSignature(signatureDto);
                }
                catch (Exception sigEx)
                {
                    MessageBox.Show($"FEHLER beim Signatur-Speichern:\n{sigEx.Message}\n\nStackTrace:\n{sigEx.StackTrace}", 
                        "Signatur-Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Verifikation: Lese den geschriebenen Signatur-Block zurück und aktualisiere die Vorschau.
                try
                {
                    var svc = new SignatureService();
                    var path = svc.GetPrimarySignaturePath();
                    if (!string.IsNullOrEmpty(path))
                    {
                        var saved = svc.ReadSignatureBlock(path);
                        if (!string.IsNullOrEmpty(saved))
                        {
                            var doc = $"<html><head><meta charset=\"utf-8\"><style>body{{font-family:Arial,Helvetica,sans-serif;}}</style></head><body>{saved}</body></html>";
                            SignaturePreviewBrowser.NavigateToString(doc);
                            
                            // Debug: Zeige ersten Teil des gespeicherten Contents
                            var preview = saved.Length > 100 ? saved.Substring(0, 100) + "..." : saved;
                            System.Diagnostics.Debug.WriteLine($"Signatur gespeichert und zurückgelesen: {preview}");
                        }
                        else
                        {
                            MessageBox.Show("Warnung: Signatur-Block konnte nicht zurückgelesen werden.", 
                                "Warnung", MessageBoxButton.OK, MessageBoxImage.Warning);
                        }
                    }
                    else
                    {
                        MessageBox.Show("Warnung: Keine primäre Signaturdatei gefunden.", 
                            "Warnung", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
                catch (Exception readEx)
                {
                    // Non-fatal, aber loggen
                    System.Diagnostics.Debug.WriteLine($"Fehler beim Zurücklesen der Signatur: {readEx.Message}");
                }

                MessageBox.Show("Konfiguration gespeichert und sofort angewendet.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Fehler beim Speichern:\n" + ex.Message);
            }
        }

        private void ShowPreview(string filePath)
        {
            var image = new BitmapImage();
            image.BeginInit();
            image.UriSource = new Uri(filePath);
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.EndInit();

            BannerPreview.Source = image;
            BannerPreview.Visibility = Visibility.Visible;

            BannerInfo.Text = $"Ausgewählt: {Path.GetFileName(filePath)}";
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
