using System.Runtime.InteropServices;
using ElaraMacro.Models;
using ElaraMacro.Native;

namespace ElaraMacro.Services;

public sealed class HookManager : IDisposable
{
    private Thread? _thread;
    private HookWindow? _window;
    private readonly ManualResetEventSlim _started = new(false);
    private bool _disposed;

    public event EventHandler<RecordedEvent>? InputCaptured;

    public void Start()
    {
        if (_thread is not null) return;
        _thread = new Thread(() =>
        {
            Application.SetCompatibleTextRenderingDefault(false);
            _window = new HookWindow(this);
            _started.Set();
            Application.Run(_window);
        })
        {
            IsBackground = true,
            Name = "Hook Thread"
        };
        _thread.SetApartmentState(ApartmentState.STA);
        _thread.Start();
        _started.Wait();
    }

    public void Stop()
    {
        if (_window is null) return;
        try { _window.BeginInvoke(new MethodInvoker(() => _window.Close())); } catch { }
        _thread = null;
        _window = null;
    }

    internal void RaiseCaptured(RecordedEvent e) => InputCaptured?.Invoke(this, e);

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Stop();
    }

    private sealed class HookWindow : Form
    {
        private readonly HookManager _owner;
        private IntPtr _keyboardHook;
        private IntPtr _mouseHook;
        private readonly NativeMethods.HookProc _keyboardProc;
        private readonly NativeMethods.HookProc _mouseProc;

        public HookWindow(HookManager owner)
        {
            _owner = owner;
            ShowInTaskbar = false;
            WindowState = FormWindowState.Minimized;
            FormBorderStyle = FormBorderStyle.FixedToolWindow;
            Opacity = 0;
            _keyboardProc = KeyboardHookCallback;
            _mouseProc = MouseHookCallback;
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            Visible = false;
            InstallHooks();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            UninstallHooks();
            base.OnFormClosing(e);
        }

        private void InstallHooks()
        {
            var module = NativeMethods.GetModuleHandle(null);
            _keyboardHook = NativeMethods.SetWindowsHookEx(NativeMethods.WH_KEYBOARD_LL, _keyboardProc, module, 0);
            _mouseHook = NativeMethods.SetWindowsHookEx(NativeMethods.WH_MOUSE_LL, _mouseProc, module, 0);
            if (_keyboardHook == IntPtr.Zero || _mouseHook == IntPtr.Zero)
                throw new InvalidOperationException("Failed to install global hooks.");
        }

        private void UninstallHooks()
        {
            if (_keyboardHook != IntPtr.Zero) { NativeMethods.UnhookWindowsHookEx(_keyboardHook); _keyboardHook = IntPtr.Zero; }
            if (_mouseHook != IntPtr.Zero) { NativeMethods.UnhookWindowsHookEx(_mouseHook); _mouseHook = IntPtr.Zero; }
        }

        private IntPtr KeyboardHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                var msg = wParam.ToInt32();
                var data = Marshal.PtrToStructure<NativeMethods.KBDLLHOOKSTRUCT>(lParam);
                if ((data.flags & NativeMethods.LLKHF_INJECTED) == 0)
                {
                    if (msg is NativeMethods.WM_KEYDOWN or NativeMethods.WM_SYSKEYDOWN or NativeMethods.WM_KEYUP or NativeMethods.WM_SYSKEYUP)
                    {
                        _owner.RaiseCaptured(new RecordedEvent
                        {
                            Kind = (msg == NativeMethods.WM_KEYDOWN || msg == NativeMethods.WM_SYSKEYDOWN) ? EventKind.KeyDown : EventKind.KeyUp,
                            KeyCode = (Keys)data.vkCode
                        });
                    }
                }
            }
            return NativeMethods.CallNextHookEx(_keyboardHook, nCode, wParam, lParam);
        }

        private IntPtr MouseHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                var msg = wParam.ToInt32();
                var data = Marshal.PtrToStructure<NativeMethods.MSLLHOOKSTRUCT>(lParam);
                if ((data.flags & NativeMethods.LLMHF_INJECTED) == 0)
                {
                    EventKind? kind = msg switch
                    {
                        NativeMethods.WM_MOUSEMOVE    => EventKind.MouseMove,
                        NativeMethods.WM_LBUTTONDOWN  => EventKind.LeftDown,
                        NativeMethods.WM_LBUTTONUP    => EventKind.LeftUp,
                        NativeMethods.WM_RBUTTONDOWN  => EventKind.RightDown,
                        NativeMethods.WM_RBUTTONUP    => EventKind.RightUp,
                        NativeMethods.WM_MBUTTONDOWN  => EventKind.MiddleDown,
                        NativeMethods.WM_MBUTTONUP    => EventKind.MiddleUp,
                        NativeMethods.WM_MOUSEWHEEL   => EventKind.MouseWheel,
                        _ => null
                    };
                    if (kind is not null)
                    {
                        _owner.RaiseCaptured(new RecordedEvent
                        {
                            Kind = kind.Value,
                            X = data.pt.X,
                            Y = data.pt.Y,
                            MouseData = unchecked((short)((data.mouseData >> 16) & 0xffff))
                        });
                    }
                }
            }
            return NativeMethods.CallNextHookEx(_mouseHook, nCode, wParam, lParam);
        }
    }
}
