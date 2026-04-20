using System.Runtime.InteropServices;
using System.Windows;
using WindowGrabber.Helpers;
using WindowGrabber.Interop;
using WindowGrabber.Models;

namespace WindowGrabber.Services;

/// <summary>
/// Déplace une fenêtre sur un moniteur cible de manière sûre :
/// - restore si minimisée/maximisée
/// - recentre dans la zone de travail du moniteur cible
/// - clampe la taille pour ne jamais dépasser l'écran
/// - active la fenêtre (avec ruse AttachThreadInput pour contourner la restriction SetForegroundWindow)
/// Les erreurs Win32 sont loguées, jamais propagées.
/// </summary>
public sealed class WindowMover
{
    public bool MoveWindowToMonitor(WindowInfo window, MonitorInfo target)
    {
        var hWnd = window.Handle;
        if (!NativeMethods.IsWindow(hWnd))
        {
            Logger.Warn($"Fenêtre disparue: {window.Title}");
            return false;
        }

        try
        {
            // 1. Récupérer le placement d'origine
            var wp = new WINDOWPLACEMENT { length = Marshal.SizeOf<WINDOWPLACEMENT>() };
            NativeMethods.GetWindowPlacement(hWnd, ref wp);

            bool wasMaximized = NativeMethods.IsZoomed(hWnd) || wp.showCmd == NativeConstants.SW_SHOWMAXIMIZED;
            bool wasMinimized = NativeMethods.IsIconic(hWnd) || wp.showCmd == NativeConstants.SW_SHOWMINIMIZED;

            // 2. Restaurer si min ou max (on déplace toujours en état "normal" puis re-maximise si besoin)
            if (wasMinimized || wasMaximized)
            {
                NativeMethods.ShowWindow(hWnd, NativeConstants.SW_RESTORE);
            }

            // 3. Déterminer taille / position
            if (!NativeMethods.GetWindowRect(hWnd, out var rect))
            {
                Logger.Warn($"GetWindowRect a échoué pour {window.Title}");
                return false;
            }

            int w = Math.Max(300, rect.Width);
            int h = Math.Max(200, rect.Height);

            // Clamp à la zone de travail du moniteur
            var work = target.WorkArea;
            w = (int)Math.Min(w, work.Width);
            h = (int)Math.Min(h, work.Height);

            // Centrer dans la zone de travail
            int x = (int)(work.X + (work.Width - w) / 2);
            int y = (int)(work.Y + (work.Height - h) / 2);

            // 4. Appliquer le déplacement
            bool ok = NativeMethods.SetWindowPos(hWnd, IntPtr.Zero, x, y, w, h,
                NativeConstants.SWP_NOZORDER | NativeConstants.SWP_NOACTIVATE | NativeConstants.SWP_ASYNCWINDOWPOS);

            if (!ok)
            {
                int err = Marshal.GetLastWin32Error();
                Logger.Warn($"SetWindowPos a échoué pour '{window.Title}' (err={err})");
                // fallback MoveWindow
                ok = NativeMethods.MoveWindow(hWnd, x, y, w, h, true);
                if (!ok)
                {
                    Logger.Error($"MoveWindow fallback a échoué pour '{window.Title}'");
                    return false;
                }
            }

            // 5. Re-maximiser si nécessaire (après avoir déplacé la fenêtre sur le bon écran)
            if (wasMaximized)
            {
                NativeMethods.ShowWindow(hWnd, NativeConstants.SW_MAXIMIZE);
            }

            // 6. Amener au premier plan
            ForceForeground(hWnd);

            Logger.Info($"Fenêtre '{window.Title}' déplacée vers {target.DisplayLabel} ({x},{y} {w}x{h})");
            return true;
        }
        catch (Exception ex)
        {
            Logger.Error($"Échec du déplacement de '{window.Title}'", ex);
            return false;
        }
    }

    /// <summary>
    /// Technique classique pour forcer <c>SetForegroundWindow</c> même quand Windows
    /// restreint l'activation : on attache temporairement notre thread d'entrée au thread
    /// du foreground actuel pour que notre appel passe la vérification.
    /// </summary>
    private static void ForceForeground(IntPtr hWnd)
    {
        try
        {
            var fg = NativeMethods.GetForegroundWindow();
            if (fg == hWnd) return;

            uint fgThread = NativeMethods.GetWindowThreadProcessId(fg, out _);
            uint ourThread = NativeMethods.GetCurrentThreadId();
            bool attached = false;

            if (fgThread != 0 && fgThread != ourThread)
            {
                attached = NativeMethods.AttachThreadInput(ourThread, fgThread, true);
            }

            NativeMethods.ShowWindowAsync(hWnd, NativeConstants.SW_SHOW);
            NativeMethods.BringWindowToTop(hWnd);
            NativeMethods.SetForegroundWindow(hWnd);

            if (attached)
                NativeMethods.AttachThreadInput(ourThread, fgThread, false);
        }
        catch (Exception ex)
        {
            Logger.Warn("ForceForeground a échoué", ex);
        }
    }
}
