using System.Runtime.InteropServices;
using ElaraMacro.Models;
using ElaraMacro.Native;

namespace ElaraMacro.Services;

public sealed class HookManager : IDisposable
{
    private readonly object _gate = new();
    private readonly ManualResetEventSlim _threadReady = new(false);
    private Thread? _hookThread;
    private uint _hookThreadId;
    private IntPtr _keyboardHook = IntPtr.Zero;
    private IntPtr _mouseHook = IntPtr.Zero;
    private NativeMethods.HookProc? _keyboardProc;
    private NativeMethods.HookProc? _mouseProc;
    private bool _disposed;

    public event EventHandler<RecordedEvent>? KeyDown;
    public event EventHandler<RecordedEvent>? KeyUp;
    public event EventHandler<RecordedEvent>? MouseDown;
    public event EventHandler<RecordedEvent>? MouseUp;
    public event EventHandler<RecordedEvent>? MouseMove;
    public event EventHandler<RecordedEvent>? MouseWheel;

    public void Start()
    {
        lock (_gate)
        {
            if (_disposed || _hookThread is not null)
            {
                return;
            }

            _threadReady.Reset();
            _hookThread = new Thread(HookThreadMain)
            {
                IsBackground = true,
                Name = "ElaraMacro Hook Thread"
            };
            _hookThread.SetApartmentState(ApartmentState.STA);
            _hookThread.Start();
        }

        _threadReady.Wait();
    }

    public void Stop()
    {
        Thread? threadToJoin;
        uint threadId;

        lock (_gate)
        {
            threadToJoin = _hookThread;
            threadId = _hookThreadId;
        }

        if (threadToJoin is null)
        {
            return;
        }

        if (threadId != 0)
        {
            NativeMethods.PostThreadMessage(threadId, NativeMethods.WM_QUIT, UIntPtr.Zero, IntPtr.Zero);
        }

        if (Thread.CurrentThread != threadToJoin)
        {
            threadToJoin.Join(TimeSpan.FromSeconds(2));
        }

        lock (_gate)
        {
            _hookThread = null;
            _hookThreadId = 0;
        }
    }

    private void HookThreadMain()
    {
        _hookThreadId = NativeMethods.GetCurrentThreadId();

        _keyboardProc = KeyboardHookCallback;
        _mouseProc = MouseHookCallback;

        var module = NativeMethods.GetModuleHandle(null);
        _keyboardHook = NativeMethods.SetWindowsHookEx(NativeMethods.WH_KEYBOARD_LL, _keyboardProc, module, 0);
        _mouseHook = NativeMethods.SetWindowsHookEx(NativeMethods.WH_MOUSE_LL, _mouseProc, module, 0);

        if (_keyboardHook == IntPtr.Zero || _mouseHook == IntPtr.Zero)
        {
            CleanupHooks();
            _threadReady.Set();
            return;
        }

        _threadReady.Set();

        while (NativeMethods.GetMessage(out var msg, IntPtr.Zero, 0, 0) > 0)
        {
            NativeMethods.TranslateMessage(ref msg);
            NativeMethods.DispatchMessage(ref msg);
        }

        CleanupHooks();
    }

    private void CleanupHooks()
    {
        if (_keyboardHook != IntPtr.Zero)
        {
            NativeMethods.UnhookWindowsHookEx(_keyboardHook);
            _keyboardHook = IntPtr.Zero;
        }

        if (_mouseHook != IntPtr.Zero)
        {
            NativeMethods.UnhookWindowsHookEx(_mouseHook);
            _mouseHook = IntPtr.Zero;
        }
    }

    private IntPtr KeyboardHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            var data = Marshal.PtrToStructure<NativeMethods.KBDLLHOOKSTRUCT>(lParam);
            if ((data.flags & NativeMethods.LLKHF_INJECTED) == 0)
            {
                var message = wParam.ToInt32();
                if (message is NativeMethods.WM_KEYDOWN or NativeMethods.WM_SYSKEYDOWN)
                {
                    KeyDown?.Invoke(this, new RecordedEvent { Kind = EventKind.KeyDown, KeyCode = (Keys)data.vkCode });
                }
                else if (message is NativeMethods.WM_KEYUP or NativeMethods.WM_SYSKEYUP)
                {
                    KeyUp?.Invoke(this, new RecordedEvent { Kind = EventKind.KeyUp, KeyCode = (Keys)data.vkCode });
                }
            }
        }

        return NativeMethods.CallNextHookEx(_keyboardHook, nCode, wParam, lParam);
    }

    private IntPtr MouseHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            var data = Marshal.PtrToStructure<NativeMethods.MSLLHOOKSTRUCT>(lParam);
            if ((data.flags & NativeMethods.LLMHF_INJECTED) == 0)
            {
                var message = wParam.ToInt32();
                var e = new RecordedEvent
                {
                    X = data.pt.X,
                    Y = data.pt.Y,
                    MouseData = unchecked((short)((data.mouseData >> 16) & 0xffff))
                };

                switch (message)
                {
                    case NativeMethods.WM_MOUSEMOVE:
                        e.Kind = EventKind.MouseMove;
                        MouseMove?.Invoke(this, e);
                        break;
                    case NativeMethods.WM_LBUTTONDOWN:
                        e.Kind = EventKind.LeftDown;
                        MouseDown?.Invoke(this, e);
                        break;
                    case NativeMethods.WM_LBUTTONUP:
                        e.Kind = EventKind.LeftUp;
                        MouseUp?.Invoke(this, e);
                        break;
                    case NativeMethods.WM_RBUTTONDOWN:
                        e.Kind = EventKind.RightDown;
                        MouseDown?.Invoke(this, e);
                        break;
                    case NativeMethods.WM_RBUTTONUP:
                        e.Kind = EventKind.RightUp;
                        MouseUp?.Invoke(this, e);
                        break;
                    case NativeMethods.WM_MBUTTONDOWN:
                        e.Kind = EventKind.MiddleDown;
                        MouseDown?.Invoke(this, e);
                        break;
                    case NativeMethods.WM_MBUTTONUP:
                        e.Kind = EventKind.MiddleUp;
                        MouseUp?.Invoke(this, e);
                        break;
                    case NativeMethods.WM_MOUSEWHEEL:
                        e.Kind = EventKind.MouseWheel;
                        MouseWheel?.Invoke(this, e);
                        break;
                }
            }
        }

        return NativeMethods.CallNextHookEx(_mouseHook, nCode, wParam, lParam);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        Stop();
        _threadReady.Dispose();
    }
}
