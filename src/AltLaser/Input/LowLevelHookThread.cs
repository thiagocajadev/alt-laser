using static AltLaser.Interop.NativeMethods;

namespace AltLaser.Input;

// Runs a low-level hook on a thread of its own. Windows silently removes a low-level hook
// whose thread is slow to answer, and the UI thread can be kept busy by drawing, cursor swaps
// and menus. This thread only waits for messages, so the hook always answers right away.
internal sealed class LowLevelHookThread : IDisposable
{
    private const int HookActionCode = 0;
    private static readonly IntPtr SwallowEvent = new(1);

    private readonly int _hookId;
    private readonly Func<IntPtr, IntPtr, bool> _onEvent;
    private readonly LowLevelHookProc _hookProc;

    private Thread? _thread;
    private uint _threadId;

    // onEvent receives the message and its data, and returns true to swallow the event.
    public LowLevelHookThread(int hookId, Func<IntPtr, IntPtr, bool> onEvent)
    {
        _hookId = hookId;
        _onEvent = onEvent;
        _hookProc = OnHookEvent;
    }

    public bool IsRunning => _thread is not null;

    public void Start()
    {
        if (IsRunning)
            return;

        using var hookInstalled = new ManualResetEventSlim();

        _thread = new Thread(() => RunHookLoop(hookInstalled))
        {
            IsBackground = true,
            Name = "Low-level hook",
        };

        _thread.Start();
        hookInstalled.Wait();
    }

    public void Stop()
    {
        if (_thread is null)
            return;

        PostThreadMessage(_threadId, WmQuit, IntPtr.Zero, IntPtr.Zero);
        _thread.Join();

        _thread = null;
    }

    private void RunHookLoop(ManualResetEventSlim hookInstalled)
    {
        _threadId = GetCurrentThreadId();

        // Creates the message queue before Stop can post WM_QUIT to it.
        PeekMessage(out _, IntPtr.Zero, 0, 0, PmNoRemove);

        var hookHandle = SetWindowsHookEx(_hookId, _hookProc, GetModuleHandle(null), 0);
        hookInstalled.Set();

        // Windows calls the hook while this thread waits here; WM_QUIT ends the loop.
        while (GetMessage(out _, IntPtr.Zero, 0, 0) > 0)
        {
        }

        UnhookWindowsHookEx(hookHandle);
    }

    private IntPtr OnHookEvent(int code, IntPtr message, IntPtr data)
    {
        var shouldSwallow = code == HookActionCode && _onEvent(message, data);

        var result = shouldSwallow ? SwallowEvent : CallNextHookEx(IntPtr.Zero, code, message, data);
        return result;
    }

    public void Dispose() => Stop();
}
