using System;
using System.Runtime.InteropServices;

namespace TTClient
{
    //Hook mouse move
    public class MouseInput : IDisposable
    {
        public event EventHandler<EventArgs> MouseMoved;

        private WindowsHookHelper.HookDelegate mouseDelegate;
        private IntPtr mouseHandle;
        private const Int32 WH_MOUSE_LL = 14;

        private bool disposed;

        public MouseInput()
        {
            mouseDelegate = MouseHookDelegate;
            mouseHandle = WindowsHookHelper.SetWindowsHookEx(WH_MOUSE_LL, mouseDelegate, IntPtr.Zero, 0);
        }

        private IntPtr MouseHookDelegate(Int32 Code, IntPtr wParam, IntPtr lParam)
        {
            if (Code < 0)
                return WindowsHookHelper.CallNextHookEx(mouseHandle, Code, wParam, lParam);

            if (MouseMoved != null)
                MouseMoved(this, new EventArgs());

            return WindowsHookHelper.CallNextHookEx(mouseHandle, Code, wParam, lParam);
        }

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (mouseHandle != IntPtr.Zero)
                    WindowsHookHelper.UnhookWindowsHookEx(mouseHandle);

                disposed = true;
            }
        }

        ~MouseInput()
        {
            Dispose(false);
        }

        private enum MouseMessages
        {
            WM_LBUTTONDOWN = 0x0201,
            WM_LBUTTONUP = 0x0202,
            WM_MOUSEMOVE = 0x0200,
            WM_MOUSEWHEEL = 0x020A,
            WM_RBUTTONDOWN = 0x0204,
            WM_RBUTTONUP = 0x0205
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int x;
            public int y;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MSLLHOOKSTRUCT
        {
            public POINT pt;
            public uint mouseData;
            public uint flags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

    }
}
