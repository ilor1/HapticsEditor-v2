#if UNITY_STANDALONE_OSX && !UNITY_EDITOR
using System;
using System.Collections;
using System.Runtime.InteropServices;
using UnityEngine;

public class MacFileDropHandler : MonoBehaviour
{
    private const float InstallRetryDelaySeconds = 0.25f;
    private const int MaxInstallAttempts = 40;

    private bool _nativeInstalled;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Create()
    {
        var gameObject = new GameObject(nameof(MacFileDropHandler));
        DontDestroyOnLoad(gameObject);
        gameObject.AddComponent<MacFileDropHandler>();
    }

    private void Start()
    {
        StartCoroutine(InstallWhenWindowExists());
    }

    private IEnumerator InstallWhenWindowExists()
    {
        for (int attempt = 0; attempt < MaxInstallAttempts; attempt++)
        {
            if (TryInstallNative())
            {
                _nativeInstalled = true;
                yield break;
            }

            yield return new WaitForSeconds(InstallRetryDelaySeconds);
        }
    }

    private void Update()
    {
        if (!_nativeInstalled) return;
        if (!TryConsumeNativePayload(out string payload)) return;

        LoadDroppedPayload(payload);
    }

    private static void LoadDroppedPayload(string payload)
    {
        if (string.IsNullOrEmpty(payload)) return;

        string[] paths = payload.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
        if (paths.Length == 0) return;

        FileDropdownMenu.LoadPaths(paths);
    }

    private static bool TryConsumeNativePayload(out string payload)
    {
        payload = null;

        try
        {
            IntPtr payloadPtr = IloriMacFileDropConsumePayload();
            if (payloadPtr == IntPtr.Zero) return false;

            payload = Marshal.PtrToStringUTF8(payloadPtr);
            return !string.IsNullOrEmpty(payload);
        }
        catch (DllNotFoundException)
        {
            return false;
        }
        catch (EntryPointNotFoundException)
        {
            return false;
        }
    }

    private static bool TryInstallNative()
    {
        try
        {
            return IloriMacFileDropInstall();
        }
        catch (DllNotFoundException)
        {
            return false;
        }
        catch (EntryPointNotFoundException)
        {
            return false;
        }
    }

    [DllImport("@executable_path/../PlugIns/MacFileDrop.bundle/Contents/MacOS/MacFileDrop", EntryPoint = "IloriMacFileDropInstall")]
    private static extern bool IloriMacFileDropInstall();

    [DllImport("@executable_path/../PlugIns/MacFileDrop.bundle/Contents/MacOS/MacFileDrop", EntryPoint = "IloriMacFileDropConsumePayload")]
    private static extern IntPtr IloriMacFileDropConsumePayload();
}
#endif
