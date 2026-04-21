using System.Runtime.InteropServices;
using System.Windows.Threading;
using WindowGrabber.Helpers;

namespace WindowGrabber.Services;

/// <summary>
/// Écoute les événements système de création/destruction de fenêtres (via
/// <c>SetWinEventHook</c>) et déclenche un callback debouncé. N'est actif que lorsque
/// <see cref="Start"/> a été appelé — à n'utiliser que quand l'app est au premier plan
/// pour ne pas consommer de ressources en arrière-plan.
/// </summary>
public sealed class WindowEventsService : IDisposable
{
    private const uint EVENT_OBJECT_CREATE = 0x8000;
    private const uint EVENT_OBJECT_DESTROY = 0x8001;
    private const uint WINEVENT_OUTOFCONTEXT = 0x0000;
    private const uint WINEVENT_SKIPOWNPROCESS = 0x0002;
    private const int OBJID_WINDOW = 0;

    private delegate void WinEventDelegate(IntPtr hWinEventHook, uint eventType, IntPtr hwnd,
        int idObject, int idChild, uint dwEventThread, uint dwmsEventTime);

    [DllImport("user32.dll")]
    private static extern IntPtr SetWinEventHook(uint eventMin, uint eventMax, IntPtr hmodWinEventProc,
        WinEventDelegate lpfnWinEventProc, uint idProcess, uint idThread, uint dwFlags);

    [DllImport("user32.dll")]
    private static extern bool UnhookWinEvent(IntPtr hWinEventHook);

    private IntPtr _hook = IntPtr.Zero;
    private readonly WinEventDelegate _delegate;
    private readonly DispatcherTimer _debounce;
    private Action? _onChanged;

    public WindowEventsService()
    {
        _delegate = OnWinEvent;
        _debounce = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(400) };
        _debounce.Tick += (_, _) =>
        {
            _debounce.Stop();
            try { _onChanged?.Invoke(); }
            catch (Exception ex) { Logger.Warn("WindowEventsService debounce handler", ex); }
        };
    }

    public bool IsRunning => _hook != IntPtr.Zero;

    public void Start(Action onChanged)
    {
        if (_hook != IntPtr.Zero) return;
        _onChanged = onChanged;
        try
        {
            _hook = SetWinEventHook(EVENT_OBJECT_CREATE, EVENT_OBJECT_DESTROY, IntPtr.Zero,
                _delegate, 0, 0, WINEVENT_OUTOFCONTEXT | WINEVENT_SKIPOWNPROCESS);
            if (_hook == IntPtr.Zero) Logger.Warn("SetWinEventHook a retourné 0");
        }
        catch (Exception ex) { Logger.Warn("SetWinEventHook exception", ex); }
    }

    public void Stop()
    {
        if (_hook != IntPtr.Zero)
        {
            try { UnhookWinEvent(_hook); } catch { }
            _hook = IntPtr.Zero;
        }
        _debounce.Stop();
        _onChanged = null;
    }

    private void OnWinEvent(IntPtr hWinEventHook, uint eventType, IntPtr hwnd,
        int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
    {
        // On ne s'intéresse qu'aux fenêtres top-level (pas aux contrôles enfants)
        if (idObject != OBJID_WINDOW || idChild != 0) return;
        if (hwnd == IntPtr.Zero) return;

        // Debounce : on relance le timer, le handler ne s'exécute que 400 ms après
        // la dernière notification (évite une tempête d'events lors d'ouvertures rapides).
        _debounce.Stop();
        _debounce.Start();
    }

    public void Dispose() => Stop();
}
