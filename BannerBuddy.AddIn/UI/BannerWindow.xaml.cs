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
            var config = _configService.Load() ?? new BannerBuddyConfig();

            // Banner optional
            if (config.Banner == null)
                config.Banner = new BannerConfig();

            var bannerEnabled = BannerEnabled.IsChecked == true;
            if (string.IsNullOrWhiteSpace(_currentBannerFile))
                bannerEnabled = false;

            config.Banner.Enabled = bannerEnabled;
            config.Banner.File = bannerEnabled ? _currentBannerFile : null;

            if (bannerEnabled)
            {
                if (StartDate.SelectedDate == null || EndDate.SelectedDate == null)
                {
                    MessageBox.Show("Bitte Start- und Enddatum für das Banner wählen.");
                    return;
                }

                var start = StartDate.SelectedDate.Value.Date;
                var end = EndDate.SelectedDate.Value.Date;
                if (end < start)
                {
                    MessageBox.Show("Banner: Enddatum muss nach dem Startdatum liegen.");
                    return;
                }

                config.Banner.Start = start.ToString("yyyy-MM-dd");
                config.Banner.End = end.ToString("yyyy-MM-dd");
            }
            else
            {
                config.Banner.Start = null;
                config.Banner.End = null;
            }

            // Vacation optional
            if (config.Vacation == null)
                config.Vacation = new VacationConfig();

            var vacationEnabled = VacationEnabled.IsChecked == true;
            config.Vacation.Enabled = vacationEnabled;
            config.Vacation.Text = VacationText.Text;

            var noticeDaysText = (VacationNoticeDays.Text ?? string.Empty).Trim();
            if (!int.TryParse(noticeDaysText, out var noticeDays))
                noticeDays = 14;

            if (noticeDays < 1 || noticeDays > 365)
            {
                MessageBox.Show("Urlaubs-Hinweis: Bitte eine Zahl zwischen 1 und 365 für 'Tage vorher' eingeben.");
                return;
            }

            config.Vacation.NoticeDays = noticeDays;

            if (vacationEnabled)
            {
                if (VacationStartDate.SelectedDate == null || VacationEndDate.SelectedDate == null)
                {
                    MessageBox.Show("Bitte Start- und Enddatum für den Urlaubs-Hinweis wählen.");
                    return;
                }

                var vStart = VacationStartDate.SelectedDate.Value.Date;
                var vEnd = VacationEndDate.SelectedDate.Value.Date;
                if (vEnd < vStart)
                {
                    MessageBox.Show("Urlaubs-Hinweis: Enddatum muss nach dem Startdatum liegen.");
                    return;
                }

                config.Vacation.Start = vStart.ToString("yyyy-MM-dd");
                config.Vacation.End = vEnd.ToString("yyyy-MM-dd");
            }
            else
            {
                config.Vacation.Start = null;
                config.Vacation.End = null;
            }

            _configService.Save(config);

            // STEP 9: Live-Refresh – Änderungen sofort anwenden
            var refresher = new RefreshService();
            refresher.Refresh();

            MessageBox.Show("Konfiguration gespeichert und sofort angewendet.");
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
