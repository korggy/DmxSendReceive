using System.ComponentModel;
using System.Runtime.InteropServices;

namespace DmxSender;

public sealed class WindowsMousePositionSource : IMousePositionSource
{
    private const int SmXVirtualScreen = 76;
    private const int SmYVirtualScreen = 77;
    private const int SmCxVirtualScreen = 78;
    private const int SmCyVirtualScreen = 79;

    public MousePosition GetPosition()
    {
        if (!GetCursorPos(out Point point))
        {
            throw new Win32Exception(Marshal.GetLastWin32Error(), "Could not read the current mouse position.");
        }

        return new MousePosition(point.X, point.Y);
    }

    public ScreenBounds GetScreenBounds()
    {
        return new ScreenBounds(
            GetSystemMetrics(SmXVirtualScreen),
            GetSystemMetrics(SmYVirtualScreen),
            GetSystemMetrics(SmCxVirtualScreen),
            GetSystemMetrics(SmCyVirtualScreen));
    }

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetCursorPos(out Point point);

    [DllImport("user32.dll")]
    private static extern int GetSystemMetrics(int index);

    [StructLayout(LayoutKind.Sequential)]
    private readonly struct Point
    {
        public readonly int X;
        public readonly int Y;
    }
}
