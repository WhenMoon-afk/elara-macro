using System.Runtime.InteropServices;
using ElaraMacro.Models;
using ElaraMacro.Native;

namespace ElaraMacro.Services;

public sealed class InputSimulatorService
{
    public void Replay(RecordedEvent e)
    {
        NativeMethods.INPUT input = e.Kind switch
        {
            EventKind.KeyDown => CreateKeyboardInput(e.KeyCode, false),
            EventKind.KeyUp => CreateKeyboardInput(e.KeyCode, true),
            EventKind.MouseMove => CreateMouseMoveInput(e.X, e.Y),
            EventKind.LeftDown => CreateMouseInput(NativeMethods.MOUSEEVENTF_LEFTDOWN),
            EventKind.LeftUp => CreateMouseInput(NativeMethods.MOUSEEVENTF_LEFTUP),
            EventKind.RightDown => CreateMouseInput(NativeMethods.MOUSEEVENTF_RIGHTDOWN),
            EventKind.RightUp => CreateMouseInput(NativeMethods.MOUSEEVENTF_RIGHTUP),
            EventKind.MiddleDown => CreateMouseInput(NativeMethods.MOUSEEVENTF_MIDDLEDOWN),
            EventKind.MiddleUp => CreateMouseInput(NativeMethods.MOUSEEVENTF_MIDDLEUP),
            EventKind.MouseWheel => CreateMouseInput(NativeMethods.MOUSEEVENTF_WHEEL, unchecked((uint)e.MouseData)),
            _ => throw new ArgumentOutOfRangeException(nameof(e.Kind), e.Kind, "Unsupported event kind")
        };

        NativeMethods.SendInput(1, [input], Marshal.SizeOf<NativeMethods.INPUT>());
    }

    private static NativeMethods.INPUT CreateKeyboardInput(Keys key, bool isKeyUp) => new()
    {
        type = NativeMethods.INPUT_KEYBOARD,
        U = new NativeMethods.InputUnion
        {
            ki = new NativeMethods.KEYBDINPUT
            {
                wVk = (ushort)key,
                dwFlags = isKeyUp ? NativeMethods.KEYEVENTF_KEYUP : 0
            }
        }
    };

    private static NativeMethods.INPUT CreateMouseMoveInput(int x, int y)
    {
        var bounds = Screen.PrimaryScreen?.Bounds ?? new Rectangle(0, 0, 1, 1);
        var width = Math.Max(1, bounds.Width - 1);
        var height = Math.Max(1, bounds.Height - 1);
        var normalizedX = (int)Math.Round((x - bounds.Left) * 65535d / width);
        var normalizedY = (int)Math.Round((y - bounds.Top) * 65535d / height);

        return new NativeMethods.INPUT
        {
            type = NativeMethods.INPUT_MOUSE,
            U = new NativeMethods.InputUnion
            {
                mi = new NativeMethods.MOUSEINPUT
                {
                    dx = Math.Clamp(normalizedX, 0, 65535),
                    dy = Math.Clamp(normalizedY, 0, 65535),
                    dwFlags = NativeMethods.MOUSEEVENTF_ABSOLUTE | NativeMethods.MOUSEEVENTF_MOVE
                }
            }
        };
    }

    private static NativeMethods.INPUT CreateMouseInput(uint flags, uint mouseData = 0) => new()
    {
        type = NativeMethods.INPUT_MOUSE,
        U = new NativeMethods.InputUnion
        {
            mi = new NativeMethods.MOUSEINPUT
            {
                dwFlags = flags,
                mouseData = mouseData
            }
        }
    };
}
