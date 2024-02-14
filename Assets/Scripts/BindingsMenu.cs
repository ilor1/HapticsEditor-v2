using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;

public class BindingsMenu : UIBehaviour
{
    public static BindingsMenu Singleton;

    private VisualElement _root;
    private VisualElement _popup;
    private VisualElement _container;

    private Bindings _controls;

    private bool _isListeningForKey = false;
    private ControlName _currentControlName;

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
        // Modify the Bindings struct based on the control name
        switch (controlName)
        {
            case ControlName.Play:
                _controls.Play = pressedKey;
                break;
            case ControlName.FastForward:
                _controls.FastForward = pressedKey;
                break;
            case ControlName.TargetPreviousModifier:
                _controls.TargetPreviousModifier = pressedKey;
                break;
            // Add more cases for other controls

            default:
                Debug.LogWarning("BindingsMenu: Unhandled control name: " + controlName);
                break;
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
    }

    private void OnSave()
    {
        // Save and close
        InputManager.Singleton.Controls = _controls;
        InputManager.Singleton.SaveBindings();
        _root.Remove(_popup);
    }

    public void Open()
    {
        // Get copy of actual controls
        _controls = InputManager.Singleton.Controls;
        _root.Add(_popup);
    }
}