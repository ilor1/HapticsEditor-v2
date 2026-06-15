using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

public class InputManager : MonoBehaviour
{
    public static InputManager Singleton;

    private string _bindingsPath = $"{Application.streamingAssetsPath}/bindings.json";
    private Bindings _bindings;
    private Dictionary<ControlName, KeyCode> _keyboardControls = new();
    private readonly Dictionary<ControlName, KeyCode> _defaultKeyboardControls = new()
    {
        { ControlName.TogglePlay, KeyCode.Space },
        { ControlName.SkipForward, KeyCode.D },
        { ControlName.SkipBack, KeyCode.A },
        { ControlName.DecreaseSpeed, KeyCode.S },
        { ControlName.IncreaseSpeed, KeyCode.W },
        { ControlName.ZoomIn, KeyCode.PageUp },
        { ControlName.ZoomOut, KeyCode.PageDown },
        { ControlName.Reset, KeyCode.R },
        { ControlName.TargetPreviousModifier, KeyCode.LeftAlt },
        { ControlName.ChangeModeOrPattern, KeyCode.Q },
        { ControlName.CycleMode, KeyCode.F1 },
        { ControlName.ToggleSnapping, KeyCode.F2 },
        { ControlName.PatternScaleX, KeyCode.LeftShift },
        { ControlName.PatternRepeat, KeyCode.LeftControl },
        { ControlName.PatternSpacing, KeyCode.LeftAlt },
        { ControlName.DefaultMode, KeyCode.Alpha1 },
        { ControlName.PatternMode, KeyCode.Alpha2 },
        { ControlName.FreeMode, KeyCode.Alpha3 },
        { ControlName.AddNote, KeyCode.Mouse2 },
        { ControlName.ConvertForStroker, KeyCode.C },
        { ControlName.StrokerMode, KeyCode.Alpha4 },
        { ControlName.ToggleTimelineMarkerFormat, KeyCode.T },
    };

    // To create a new binding:
    // 1. Add it to Bindings.cs -> ControlName enum
    // 2. Access it using InputManager.Singleton.GetKeyDown / GetKey
    // - it gets added to bindings.json through the runtime binding system

    public static bool InputBlocked = false;

    private void Awake()
    {
        if (Singleton == null) Singleton = this;
        else if (Singleton != this) Destroy(this);
    }

    private void Start()
    {
        LoadBindings();
    }

    public void SetKeyboardControls(Dictionary<ControlName, KeyCode> keyboardControls)
    {
        _keyboardControls = new Dictionary<ControlName, KeyCode>(keyboardControls);
        AddMissingDefaultBindings();
        _keyboardControls[ControlName.TogglePlay] = KeyCode.Space;
    }

    public Dictionary<ControlName, KeyCode> GetKeyboardControls()
    {
        return new Dictionary<ControlName, KeyCode>(_keyboardControls);
    }

    public void SaveBindings()
    {
        // Save bindings
        _bindings.Keyboard.FromDictionary(_keyboardControls);
        string json = JsonUtility.ToJson(_bindings, true);
        File.WriteAllText(_bindingsPath, json);
        Debug.Log($"InputManager: Bindings saved: ({_bindingsPath})");
    }

    private void LoadBindings()
    {
        if (File.Exists(_bindingsPath))
        {
            // Load Bindings if the file exists
            string json = File.ReadAllText(_bindingsPath);

            _bindings = JsonUtility.FromJson<Bindings>(json);
            Debug.Log($"InputManager: Bindings loaded: ({_bindingsPath})");

            if (_bindings.Keyboard == null)
            {
                _bindings.Keyboard = new SerializableDictionary<ControlName, KeyCode>();
            }

            _keyboardControls = _bindings.Keyboard.ToDictionary();
            AddMissingDefaultBindings();
            _keyboardControls[ControlName.TogglePlay] = KeyCode.Space;

            var bindingsDebug = new StringBuilder();
            foreach (var kvp in _keyboardControls)
            {
                bindingsDebug.Append($"{kvp.Key}:{kvp.Value}\n");
            }
            Debug.Log(bindingsDebug.ToString());

            // Note if the file exists and any of the binds are missing, those will be unbound (null)
            // This is fine and expected I guess...
        }
        else
        {
            // Otherwise save default bindings from what's setup in the inspector.
            _bindings.Keyboard = new SerializableDictionary<ControlName, KeyCode>();
            _keyboardControls = new Dictionary<ControlName, KeyCode>(_defaultKeyboardControls);
            SaveBindings();
        }
    }

    private void AddMissingDefaultBindings()
    {
        foreach (var kvp in _defaultKeyboardControls)
        {
            if (!_keyboardControls.ContainsKey(kvp.Key))
            {
                _keyboardControls[kvp.Key] = kvp.Value;
            }
        }
    }

    public bool GetKeyDown(ControlName controlName)
    {
        KeyCode keyCode = GetKeyCode(controlName);
        if (keyCode != KeyCode.None)
        {
            return Input.GetKeyDown(keyCode);
        }
        else
        {
            return false;
        }
    }

    public bool GetKeyUp(ControlName controlName)
    {
        KeyCode keyCode = GetKeyCode(controlName);
        if (keyCode != KeyCode.None)
        {
            return Input.GetKeyUp(keyCode);
        }
        else
        {
            return false;
        }
    }

    public bool GetKey(ControlName controlName)
    {
        KeyCode keyCode = GetKeyCode(controlName);
        if (keyCode != KeyCode.None)
        {
            return Input.GetKey(keyCode);
        }
        else
        {
            return false;
        }
    }

    private KeyCode GetKeyCode(ControlName controlName)
    {
        if (_keyboardControls.TryGetValue(controlName, out KeyCode keyCode))
        {
            return keyCode;
        }

        return _defaultKeyboardControls.TryGetValue(controlName, out KeyCode defaultKeyCode)
            ? defaultKeyCode
            : KeyCode.None;
    }
}
