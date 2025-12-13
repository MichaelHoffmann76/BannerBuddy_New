using System.Runtime.InteropServices;
using Office = Microsoft.Office.Core;

namespace BannerBuddy.AddIn
{
    // Ribbon XML callbacks are invoked by Outlook via IDispatch by method name.
    // Exposing an explicit IDispatch interface is more reliable than AutoDispatch.
    [ComVisible(true)]
    [Guid("1B5D0F91-8F50-4E7F-8B5A-1234567890AA")]
    [InterfaceType(ComInterfaceType.InterfaceIsIDispatch)]
    public interface IRibbonCallbacks
    {
        [DispId(1)]
        void OnRibbonLoad(Office.IRibbonUI ribbonUI);

        [DispId(2)]
        void OnBannerBuddyClicked(Office.IRibbonControl control);

        [DispId(3)]
        stdole.IPictureDisp GetImage(Office.IRibbonControl control);

        [DispId(4)]
        stdole.IPictureDisp LoadImage(string imageId);
    }
}
