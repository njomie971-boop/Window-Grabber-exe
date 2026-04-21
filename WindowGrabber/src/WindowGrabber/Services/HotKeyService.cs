using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using WindowGrabber.Helpers;

namespace WindowGrabber.Services;

/// <summary>
/// Enregistre un raccourci clavier global Windows via <c>RegisterHotKey</c>.
/// Invoque <see cref="Pressed"/> quand la combinaison est détectée. Thread UI.
/// </summary>
public sealed class HotKeyService : IDisposable
{
    private const int WM_HOTKEY = 0x0312;
    private const uint MOD_ALT = 0x0001;
    private const uint MOD_CONTROL = 0x0002;
    private const uint MOD_SHIFT = 0x0004;
    private const uint MOD_WIN = 0x0008;
    private const uint MOD_NOREPEAT = 0x4000;

    [DllImport("user32.dll")] private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);
    [DllImport("user32.dll")] private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    private HwndSource? _source;
    private int _id = 0x42F1;
    private bool _registered;

    public event Action? Pressed;

    public bool Register(Window window, string combo)
    {
        Unregister();
        try
        {
            var helper = new WindowInteropHelper(window);
            var hwnd = helper.EnsureHandle();
            _source = HwndSource.FromHwnd(hwnd);
            _source?.AddHook(WndProc);

            if (!TryParse(combo, out uint mods, out uint vk))
            {
                Logger.Warn($"HotKey invalide : '{combo}'");
                return false;
            }

            _registered = RegisterHotKey(hwnd, _id, mods | MOD_NOREPEAT, vk);
            if (!_registered) Logger.Warn($"RegisterHotKey a échoué pour '{combo}'");
            else Logger.Info($"HotKey enregistré : {combo}");
            return _registered;
        }
        catch (Exception ex) { Logger.Warn("HotKeyService.Register exception", ex); return false; }
    }

    public void Unregister()
    {
        if (_registered && _source != null)
        {
            try { UnregisterHotKey(_source.Handle, _id); } catch { }
            _registered = false;
        }
        if (_source != null)
        {
            try { _source.RemoveHook(WndProc); } catch { }
        }
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_HOTKEY && wParam.ToInt32() == _id)
        {
            try { Pressed?.Invoke(); } catch (Exception ex) { Logger.Warn("HotKey handler exception", ex); }
            handled = true;
        }
        return IntPtr.Zero;
    }

    public void Dispose() => Unregister();

    /// <summary>
    /// Parse simple "Ctrl+Alt+W" / "Shift+Win+G" en (modifiers, virtual-key).
    /// </summary>
    private static bool TryParse(string combo, out uint mods, out uint vk)
    {
        mods = 0; vk = 0;
        if (string.IsNullOrWhiteSpace(combo)) return false;
        var parts = combo.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var p in parts)
        {
            switch (p.ToLowerInvariant())
            {
                case "ctrl": case "control": mods |= MOD_CONTROL; break;
                case "alt": mods |= MOD_ALT; break;
                case "shift": mods |= MOD_SHIFT; break;
                case "win": case "windows": mods |= MOD_WIN; break;
                default:
                    if (p.Length == 1 && char.IsLetterOrDigit(p[0]))
                        vk = char.ToUpperInvariant(p[0]);
                    else if (Enum.TryParse<Key>(p, true, out var key))
                        vk = (uint)KeyInterop.VirtualKeyFromKey(key);
                    break;
            }
        }
        return vk != 0 && mods != 0;
    }
}
