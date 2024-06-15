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
        _keyboardControls = keyboardControls;
    }

    public Dictionary<ControlName, KeyCode> GetKeyboardControls()
    {
        return _keyboardControls;
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

            _keyboardControls = _bindings.Keyboard.ToDictionary();
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
            SaveBindings();
        }
    }

    public bool GetKeyDown(ControlName controlName)
    {
        if (_keyboardControls.ContainsKey(controlName) && _keyboardControls[controlName] != KeyCode.None)
        {
            return Input.GetKeyDown(_keyboardControls[controlName]);
        }
        else
        {
            return false;
        }
    }

    public bool GetKey(ControlName controlName)
    {
        if (_keyboardControls.ContainsKey(controlName) && _keyboardControls[controlName] != KeyCode.None)
        {
            return Input.GetKey(_keyboardControls[controlName]);
        }
        else
        {
            return false;
        }
    }
}