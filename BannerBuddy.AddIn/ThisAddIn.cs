using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using Extensibility;
using BannerBuddy.AddIn.Core;
using BannerBuddy.AddIn.Models;
using BannerBuddy.AddIn.Storage;
using BannerBuddy.AddIn.UI;
using Microsoft.Office.Interop.Outlook;
using Office = Microsoft.Office.Core;

namespace BannerBuddy.AddIn
{
    [ComVisible(true)]
    [Guid("A1C7E8C4-9F4D-4E5B-9C8B-123456789001")]
    [ProgId("BannerBuddy.AddIn")]
    // IMPORTANT: Ribbon XML callbacks are invoked via IDispatch by name.
    // Use AutoDispatch so Outlook can always QI for IID_IDispatch.
    [ClassInterface(ClassInterfaceType.AutoDispatch)]
    public class ThisAddIn : Extensibility.IDTExtensibility2, Office.IRibbonExtensibility, IRibbonCallbacks
    {
        private static readonly string LogPath = Path.Combine(Path.GetTempPath(), "BannerBuddy.AddIn.log");
        private object _application;
        private Application _outlookApp;
        private TrayService _tray;

        private Office.IRibbonUI _ribbonUi;

        static ThisAddIn()
        {
            Log("ThisAddIn type loaded");
        }

        private static void Log(string message)
        {
            try
            {
                File.AppendAllText(LogPath, $"{DateTime.Now:O} {message}{Environment.NewLine}");
            }
            catch
            {
                // never throw from logging
            }
        }

        public void OnConnection(object application, ext_ConnectMode connectMode, object addInInst, ref Array custom)
        {
            try
            {
                _application = application;
                Log($"OnConnection connectMode={connectMode}");

                // Keep this interop-free for now to avoid load issues while we stabilize registration.
                OnStartup(application);
            }
            catch (System.Exception ex)
            {
                Log($"OnConnection ERROR: {ex}");
            }
        }

        public void OnDisconnection(ext_DisconnectMode disconnectMode, ref Array custom)
        {
            try
            {
                Log($"OnDisconnection disconnectMode={disconnectMode}");
                OnShutdown();
            }
            catch (System.Exception ex)
            {
                Log($"OnDisconnection ERROR: {ex}");
            }
            finally
            {
                _application = null;
            }
        }

        // STEP 8: Ribbon XML (COM Add-in: Outlook ruft GetCustomUI direkt auf diesem COM-Objekt auf)
        public string GetCustomUI(string ribbonID)
        {
            try
            {
                Log($"Ribbon GetCustomUI ribbonID={ribbonID}");
                var xml = LoadEmbeddedText("BannerBuddy.AddIn.AddIn.Ribbon.xml");

                if (string.IsNullOrWhiteSpace(xml))
                {
                    Log("Ribbon GetCustomUI returned EMPTY xml");
                    return null;
                }

                Log($"Ribbon XML length={xml.Length}");
                try
                {
                    var dumpPath = Path.Combine(Path.GetTempPath(), "BannerBuddy.Ribbon.xml");
                    File.WriteAllText(dumpPath, xml);
                    Log($"Ribbon XML dumped to {dumpPath}");
                }
                catch
                {
                    // ignore dump errors
                }

                return xml;
            }
            catch (System.Exception ex)
            {
                Log($"Ribbon GetCustomUI ERROR: {ex}");
                return null;
            }
        }

        // Callback aus Ribbon.xml
        public void OnRibbonLoad(Office.IRibbonUI ribbonUI)
        {
            _ribbonUi = ribbonUI;
            Log("Ribbon OnRibbonLoad");
        }

        // Callback aus Ribbon.xml
        public void OnBannerBuddyClicked(Office.IRibbonControl control)
        {
            try
            {
                Log("Ribbon click");
                EnsureWpfApplication();

                var dispatcher = System.Windows.Application.Current?.Dispatcher;
                if (dispatcher == null)
                    return;

                dispatcher.BeginInvoke(new System.Action(() =>
                {
                    var window = new BannerWindow();
                    window.Show();
                    window.Activate();
                }));
            }
            catch (System.Exception ex)
            {
                Log($"Ribbon OnBannerBuddyClicked ERROR: {ex}");
            }
        }

        // STEP 9: Ribbon-Button "Signatur aktualisieren"
        public void OnRefreshClicked(Office.IRibbonControl control)
        {
            try
            {
                Log("Ribbon refresh click");
                var refresher = new RefreshService();
                refresher.Refresh();
                Log("Ribbon refresh done");
            }
            catch (System.Exception ex)
            {
                Log($"Ribbon OnRefreshClicked ERROR: {ex}");
            }
        }

        // Callback aus Ribbon.xml (getImage)
        public stdole.IPictureDisp GetImage(Office.IRibbonControl control)
        {
            Log("Ribbon GetImage");
            return LoadImage("BannerBuddyIcon");
        }

        // Callback aus Ribbon.xml (loadImage)
        public stdole.IPictureDisp LoadImage(string imageId)
        {
            try
            {
                Log($"Ribbon LoadImage imageId={imageId}");
                if (!string.Equals(imageId, "BannerBuddyIcon", StringComparison.OrdinalIgnoreCase))
                    return null;

                var asm = Assembly.GetExecutingAssembly();

                // Erwarteter Resource-Name, wenn du AddIn/Resources/BannerBuddy.png ablegst.
                var resName = "BannerBuddy.AddIn.AddIn.Resources.BannerBuddy.png";
                var pic = TryLoadPngAsPictureDisp(asm, resName);
                if (pic != null)
                    return pic;

                // Robust: falls der Resource-Name anders ist, nimm die erste passende PNG Ressource.
                foreach (var name in asm.GetManifestResourceNames())
                {
                    if (name.EndsWith("BannerBuddy.png", StringComparison.OrdinalIgnoreCase))
                    {
                        pic = TryLoadPngAsPictureDisp(asm, name);
                        if (pic != null)
                            return pic;
                    }
                }

                // Fallback: zumindest ein Icon anzeigen, selbst wenn PNG noch nicht vorhanden ist.
                return PictureDispConverter.FromImage(System.Drawing.SystemIcons.Application.ToBitmap());
            }
            catch (System.Exception ex)
            {
                Log($"Ribbon LoadImage ERROR: {ex}");
                return null;
            }
        }

        private static stdole.IPictureDisp TryLoadPngAsPictureDisp(Assembly asm, string resourceName)
        {
            try
            {
                using (var stream = asm.GetManifestResourceStream(resourceName))
                {
                    if (stream == null)
                        return null;

                    using (var bmp = new System.Drawing.Bitmap(stream))
                        return PictureDispConverter.FromImage(bmp);
                }
            }
            catch
            {
                return null;
            }
        }

        private static void EnsureWpfApplication()
        {
            if (System.Windows.Application.Current != null)
                return;

            new System.Windows.Application { ShutdownMode = System.Windows.ShutdownMode.OnExplicitShutdown };
        }

        private static string LoadEmbeddedText(string resourceName)
        {
            var asm = Assembly.GetExecutingAssembly();
            using (var stream = asm.GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                    return null;

                using (var reader = new StreamReader(stream))
                    return reader.ReadToEnd();
            }
        }

        // Minimaler, stabiler Converter ohne Microsoft.VisualBasic.Compatibility.
        private class PictureDispConverter : System.Windows.Forms.AxHost
        {
            private PictureDispConverter() : base(string.Empty) { }

            public static stdole.IPictureDisp FromImage(System.Drawing.Image image)
            {
                return (stdole.IPictureDisp)GetIPictureDispFromPicture(image);
            }
        }

        public void OnAddInsUpdate(ref Array custom)
        {
            Log("OnAddInsUpdate");
        }

        public void OnStartupComplete(ref Array custom)
        {
            Log("OnStartupComplete");
        }

        public void OnBeginShutdown(ref Array custom)
        {
            try
            {
                Log("OnBeginShutdown");
                OnShutdown();
            }
            catch (System.Exception ex)
            {
                Log($"OnBeginShutdown ERROR: {ex}");
            }
        }

        public void OnStartup(object application)
        {
            _application = application;
            Log("OnStartup");

            try
            {
                _outlookApp = application as Application;

                // STEP 7: Tray-Icon einmal pro Outlook-Session starten
                _tray = new TrayService();
                _tray.OnReloadRequested = ApplyConfigToSignatures;
                _tray.Start();

                ApplyConfigToSignatures();
            }
            catch (System.Exception ex)
            {
                Log($"OnStartup ERROR: {ex}");
            }
        }

        private void ApplyConfigToSignatures()
        {
            try
            {
                var signatureService = new SignatureService();
                var configService = new ConfigService();
                var config = configService.Load();

                if (config == null)
                {
                    Log("config.json nicht gefunden oder unlesbar – keine Änderungen angewendet");
                    return;
                }

                Log("config.json geladen – wende TimedContent an");

                var banner = TimedContentFactory.CreateBanner(config.Banner);
                var vacation = TimedContentFactory.CreateVacation(config.Vacation);

                var signatures = signatureService.GetHtmlSignatures();

                foreach (var sig in signatures)
                {
                    signatureService.EnsureMarkers(sig);
                    signatureService.ApplyTimedContent(sig, banner, vacation);
                }
            }
            catch (System.Exception ex)
            {
                Log($"Signature enumeration ERROR: {ex}");
            }
        }

        public void OnShutdown()
        {
            // sauber aufräumen
            try
            {
                _tray?.Stop();
            }
            catch (System.Exception ex)
            {
                Log($"Tray shutdown ERROR: {ex}");
            }
            finally
            {
                _tray = null;
            }

            _outlookApp = null;
        }
    }
}
