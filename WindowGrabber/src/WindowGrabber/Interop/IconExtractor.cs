using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

namespace WindowGrabber.Interop;

/// <summary>
/// Extrait l'icône d'une fenêtre / d'un .exe en <see cref="BitmapSource"/>.
/// Essaie plusieurs stratégies (WM_GETICON, GetClassLongPtr, ExtractIconEx) et
/// retourne null si toutes échouent. Pas d'exception fuyante.
/// </summary>
internal static class IconExtractor
{
    private const uint SMTO_ABORTIFHUNG = 0x0002;

    public static BitmapSource? TryGetWindowIcon(IntPtr hWnd)
    {
        IntPtr hIcon = IntPtr.Zero;
        bool ownsIcon = false;

        try
        {
            // 1) WM_GETICON ICON_BIG
            NativeMethods.SendMessageTimeout(hWnd, NativeConstants.WM_GETICON,
                new IntPtr(NativeConstants.ICON_BIG), IntPtr.Zero, SMTO_ABORTIFHUNG, 100, out var res);
            hIcon = res;

            // 2) WM_GETICON ICON_SMALL2
            if (hIcon == IntPtr.Zero)
            {
                NativeMethods.SendMessageTimeout(hWnd, NativeConstants.WM_GETICON,
                    new IntPtr(NativeConstants.ICON_SMALL2), IntPtr.Zero, SMTO_ABORTIFHUNG, 100, out res);
                hIcon = res;
            }

            // 3) ICON_SMALL
            if (hIcon == IntPtr.Zero)
            {
                NativeMethods.SendMessageTimeout(hWnd, NativeConstants.WM_GETICON,
                    new IntPtr(NativeConstants.ICON_SMALL), IntPtr.Zero, SMTO_ABORTIFHUNG, 100, out res);
                hIcon = res;
            }

            // 4) Class icon
            if (hIcon == IntPtr.Zero)
            {
                hIcon = NativeMethods.GetClassLongPtr(hWnd, NativeConstants.GCLP_HICON);
            }
            if (hIcon == IntPtr.Zero)
            {
                hIcon = NativeMethods.GetClassLongPtr(hWnd, NativeConstants.GCLP_HICONSM);
            }

            if (hIcon != IntPtr.Zero)
            {
                return HIconToBitmapSource(hIcon);
            }
        }
        catch
        {
            // swallow – on fallback sur TryGetFileIcon ensuite
        }
        finally
        {
            if (ownsIcon && hIcon != IntPtr.Zero)
                NativeMethods.DestroyIcon(hIcon);
        }

        return null;
    }

    public static BitmapSource? TryGetFileIcon(string? filePath)
    {
        if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            return null;

        var large = new IntPtr[1];
        var small = new IntPtr[1];
        try
        {
            uint n = NativeMethods.ExtractIconEx(filePath, 0, large, small, 1);
            IntPtr hIcon = large[0] != IntPtr.Zero ? large[0] : small[0];
            if (hIcon == IntPtr.Zero && n == 0)
                return null;

            if (hIcon == IntPtr.Zero) return null;
            var bmp = HIconToBitmapSource(hIcon);
            return bmp;
        }
        catch
        {
            return null;
        }
        finally
        {
            if (large[0] != IntPtr.Zero) NativeMethods.DestroyIcon(large[0]);
            if (small[0] != IntPtr.Zero) NativeMethods.DestroyIcon(small[0]);
        }
    }

    private static BitmapSource? HIconToBitmapSource(IntPtr hIcon)
    {
        try
        {
            var bmp = Imaging.CreateBitmapSourceFromHIcon(
                hIcon,
                Int32Rect.Empty,
                BitmapSizeOptions.FromWidthAndHeight(48, 48));
            bmp.Freeze();
            return bmp;
        }
        catch
        {
            return null;
        }
    }
}
