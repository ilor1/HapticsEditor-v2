using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class BindingsMenu : UIBehaviour
{
    private static BindingsMenu Singleton;

    private VisualElement _root;
    private VisualElement _popup;
    private VisualElement _container;

    private bool _isListeningForKey = false;
    private ControlName _currentControlName;

    private Dictionary<ControlName, Button> _buttonDictionary = new Dictionary<ControlName, Button>();
    private Dictionary<ControlName, KeyCode> _keyCodeDictionary = new Dictionary<ControlName, KeyCode>();

    private void Awake()
    {
        if (Singleton == null)
        {
            Singleton = this;
        }
        else if (Singleton != this)
        {
            Destroy(this);
        }
    }

    private void OnEnable()
    {
        MainUI.RootCreated += Generate;
    }

    private void OnDisable()
    {
        MainUI.RootCreated -= Generate;
    }

    private void Generate(VisualElement root)
    {
        _root = root;

        _popup = Create("bindings-popup");
        _container = Create("bindings-container");
        _popup.Add(_container);

        // Create input buttons dynamically for each control
        foreach (ControlName controlName in Enum.GetValues(typeof(ControlName)))
        {
            var button = CreateInputButton(controlName, _container);
            button.focusable = false;
            button.clicked += () => { StartListeningForKey(controlName); };

            _buttonDictionary.Add(controlName, button);
        }

        var bindingsButtons = Create("bindings-buttons");
        var saveButton = Create<Button>();
        saveButton.clicked += OnSave;
        saveButton.text = "Save";
        bindingsButtons.Add(saveButton);

        var cancelButton = Create<Button>();
        cancelButton.text = "Cancel";
        cancelButton.clicked += OnCancel;
        bindingsButtons.Add(cancelButton);

        _container.Add(bindingsButtons);
    }

    private void StartListeningForKey(ControlName controlName)
    {
        _isListeningForKey = true;
        _currentControlName = controlName;
    }

    private void Update()
    {
        if (!_isListeningForKey) return;

        Debug.Log("BindingsMenu: Listening for key...");

        // Go through each KeyCode to find which was pressed
        foreach (KeyCode keyCode in Enum.GetValues(typeof(KeyCode)))
        {
            if (Input.GetKeyDown(keyCode))
            {
                if (keyCode == KeyCode.Escape)
                {
                    // Escape to Cancel
                }
                else
                {
                    SetBindingKey(_currentControlName, keyCode);
                }

                _isListeningForKey = false;
                break;
            }
        }
    }

    private void SetBindingKey(ControlName controlName, KeyCode pressedKey)
    {
        _keyCodeDictionary[controlName] = pressedKey;
        UpdateBindingButtons();
    }

    private void UpdateBindingButtons()
    {
        foreach (var kvp in _buttonDictionary)
        {
            if (_keyCodeDictionary.TryGetValue(kvp.Key, out var value))
            {
                kvp.Value.text = value.ToString();
            }
            else
            {
                kvp.Value.text = "None";
            }
        }
    }

    private Button CreateInputButton(ControlName controlName, VisualElement parent)
    {
        var container = Create("bindings-field");
        var label = Create<Label>();
        label.text = controlName.ToString();

        var inputField = Create<Button>();
        container.Add(label);
        container.Add(inputField);

        parent.Add(container);

        return inputField;
    }

    private void OnCancel()
    {
        // Close without saving
        _root.Remove(_popup);

        InputManager.InputBlocked = false;
    }

    private void OnSave()
    {
        // Save and close
        InputManager.Singleton.SetKeyboardControls(_keyCodeDictionary);
        InputManager.Singleton.SaveBindings();
        _root.Remove(_popup);

        InputManager.InputBlocked = false;
    }

    public static void Open()
    {
        // Mark BindingsMenu open so we block keypresses
        InputManager.InputBlocked = true;

        // Get copy of actual controls
        Singleton._keyCodeDictionary = InputManager.Singleton.GetKeyboardControls();
        Singleton.UpdateBindingButtons();
        Singleton._root.Add(Singleton._popup);
    }
}