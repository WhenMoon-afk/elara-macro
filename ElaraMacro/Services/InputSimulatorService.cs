using System.Runtime.InteropServices;
using ElaraMacro.Models;
using ElaraMacro.Native;

namespace ElaraMacro.Services;

public sealed class InputSimulatorService
{
    public void ReplayEvent(RecordedEvent e)
    {
        switch (e.Kind)
        {
            case EventKind.KeyDown:    SendKeyboard(e.KeyCode, false); break;
            case EventKind.KeyUp:      SendKeyboard(e.KeyCode, true);  break;
            case EventKind.MouseMove:  MoveMouseAbsolute(e.X, e.Y);    break;
            case EventKind.LeftDown:   SendMouse(NativeMethods.MOUSEEVENTF_LEFTDOWN);   break;
            case EventKind.LeftUp:     SendMouse(NativeMethods.MOUSEEVENTF_LEFTUP);     break;
            case EventKind.RightDown:  SendMouse(NativeMethods.MOUSEEVENTF_RIGHTDOWN);  break;
            case EventKind.RightUp:    SendMouse(NativeMethods.MOUSEEVENTF_RIGHTUP);    break;
            case EventKind.MiddleDown: SendMouse(NativeMethods.MOUSEEVENTF_MIDDLEDOWN); break;
            case EventKind.MiddleUp:   SendMouse(NativeMethods.MOUSEEVENTF_MIDDLEUP);   break;
            case EventKind.MouseWheel: SendMouse(NativeMethods.MOUSEEVENTF_WHEEL, (uint)e.MouseData); break;
        }
    }

    private static void SendKeyboard(Keys key, bool keyUp)
    {
        var input = new NativeMethods.INPUT
        {
            type = NativeMethods.INPUT_KEYBOARD,
            U = new NativeMethods.InputUnion
            {
                ki = new NativeMethods.KEYBDINPUT
                {
                    wVk = (ushort)key,
                    dwFlags = keyUp ? NativeMethods.KEYEVENTF_KEYUP : 0
                }
            }
        };
        NativeMethods.SendInput(1, new[] { input }, Marshal.SizeOf<NativeMethods.INPUT>());
    }

    private static void MoveMouseAbsolute(int x, int y)
    {
        var sw = Math.Max(1, NativeMethods.GetSystemMetrics(NativeMethods.SM_CXSCREEN) - 1);
        var sh = Math.Max(1, NativeMethods.GetSystemMetrics(NativeMethods.SM_CYSCREEN) - 1);
        var input = new NativeMethods.INPUT
        {
            type = NativeMethods.INPUT_MOUSE,
            U = new NativeMethods.InputUnion
            {
                mi = new NativeMethods.MOUSEINPUT
                {
                    dx = (int)Math.Round(x * 65535.0 / sw),
                    dy = (int)Math.Round(y * 65535.0 / sh),
                    dwFlags = NativeMethods.MOUSEEVENTF_MOVE | NativeMethods.MOUSEEVENTF_ABSOLUTE
                }
            }
        };
        NativeMethods.SendInput(1, new[] { input }, Marshal.SizeOf<NativeMethods.INPUT>());
    }

    private static void SendMouse(uint flags, uint mouseData = 0)
    {
        var input = new NativeMethods.INPUT
        {
            type = NativeMethods.INPUT_MOUSE,
            U = new NativeMethods.InputUnion
            {
                mi = new NativeMethods.MOUSEINPUT { dwFlags = flags, mouseData = mouseData }
            }
        };
        NativeMethods.SendInput(1, new[] { input }, Marshal.SizeOf<NativeMethods.INPUT>());
    }
}
