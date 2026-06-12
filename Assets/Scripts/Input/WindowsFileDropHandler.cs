#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;

public class WindowsFileDropHandler : MonoBehaviour
{
    private const int GWL_WNDPROC = -4;
    private const uint WM_DROPFILES = 0x0233;
    private const uint DRAG_QUERY_FILE_COUNT = 0xFFFFFFFF;

    private delegate IntPtr WndProcDelegate(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    private static WndProcDelegate _wndProcDelegate;
    private static IntPtr _oldWndProc = IntPtr.Zero;
    private static IntPtr _windowHandle = IntPtr.Zero;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Create()
    {
        var gameObject = new GameObject(nameof(WindowsFileDropHandler));
        DontDestroyOnLoad(gameObject);
        gameObject.AddComponent<WindowsFileDropHandler>();
    }

    private void Start()
    {
        _windowHandle = GetActiveWindow();
        if (_windowHandle == IntPtr.Zero)
        {
            Debug.LogWarning("WindowsFileDropHandler: Could not find active window.");
            return;
        }

        _wndProcDelegate = WndProc;
        _oldWndProc = SetWindowLongPtr(_windowHandle, GWL_WNDPROC, Marshal.GetFunctionPointerForDelegate(_wndProcDelegate));
        DragAcceptFiles(_windowHandle, true);
    }

    private void OnDestroy()
    {
        if (_windowHandle == IntPtr.Zero || _oldWndProc == IntPtr.Zero) return;

        DragAcceptFiles(_windowHandle, false);
        SetWindowLongPtr(_windowHandle, GWL_WNDPROC, _oldWndProc);
        _oldWndProc = IntPtr.Zero;
        _windowHandle = IntPtr.Zero;
    }

    private static IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
    {
        if (msg == WM_DROPFILES)
        {
            HandleDrop(wParam);
            return IntPtr.Zero;
        }

        return CallWindowProc(_oldWndProc, hWnd, msg, wParam, lParam);
    }

    private static void HandleDrop(IntPtr dropHandle)
    {
        try
        {
            uint fileCount = DragQueryFile(dropHandle, DRAG_QUERY_FILE_COUNT, null, 0);
            var paths = new List<string>((int)fileCount);

            for (uint i = 0; i < fileCount; i++)
            {
                uint length = DragQueryFile(dropHandle, i, null, 0);
                var buffer = new StringBuilder((int)length + 1);
                DragQueryFile(dropHandle, i, buffer, (uint)buffer.Capacity);
                paths.Add(buffer.ToString());
            }

            if (FileDropdownMenu.Singleton != null)
            {
                FileDropdownMenu.LoadPaths(paths);
            }
        }
        finally
        {
            DragFinish(dropHandle);
        }
    }

    private static IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong)
    {
        return IntPtr.Size == 8
            ? SetWindowLongPtr64(hWnd, nIndex, dwNewLong)
            : SetWindowLong32(hWnd, nIndex, dwNewLong);
    }

    [DllImport("user32.dll")]
    private static extern IntPtr GetActiveWindow();

    [DllImport("user32.dll", EntryPoint = "SetWindowLong")]
    private static extern IntPtr SetWindowLong32(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr")]
    private static extern IntPtr SetWindowLongPtr64(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    [DllImport("user32.dll")]
    private static extern IntPtr CallWindowProc(IntPtr lpPrevWndFunc, IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    [DllImport("shell32.dll")]
    private static extern void DragAcceptFiles(IntPtr hWnd, bool fAccept);

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern uint DragQueryFile(IntPtr hDrop, uint iFile, StringBuilder lpszFile, uint cch);

    [DllImport("shell32.dll")]
    private static extern void DragFinish(IntPtr hDrop);
}
#endif
