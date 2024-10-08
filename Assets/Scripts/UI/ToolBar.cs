﻿using System;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UIElements;

public class ToolBar : UIBehaviour
{
    public static ToolBar Singleton;
    
    private bool _isInitialized;
    private VisualElement _toolBar;

    // Shared tools
    private Button _modeChangeButton;
    private VisualElement _patternToggleContainer;
    private Toggle _patternToggle;
    private VisualElement _snappingToggleContainer;
    private Toggle _snappingToggle;

    // Default tools
    private VisualElement _stepMode;
    private Toggle _stepToggle;

    // Pattern tools
    private Button _nextPatternButton;
    private VisualElement _repeatContainer;
    private IntegerField _repeatField;
    private VisualElement _spacingContainer;
    private Slider _spacingField;
    private VisualElement _scaleXContainer;
    private Slider _scaleXField;
    private VisualElement _invertXContainer;
    private Toggle _invertXToggle;
    private VisualElement _scaleYContainer;
    private Slider _scaleYField;
    private VisualElement _invertYContainer;
    private Toggle _invertYToggle;

    private void Awake()
    {
        if (Singleton == null) Singleton = this;
        else if (Singleton != this) Destroy(this);
    }

    private void OnEnable()
    {
        MainUI.RootCreated += Generate;
    }

    private void OnDisable()
    {
        MainUI.RootCreated -= Generate;
    }

    private void Update()
    {
        if (!_isInitialized) return;
        if (InputManager.InputBlocked) return;

        // Hotkeys change the visuals which triggers the events
        if (InputManager.Singleton.GetKeyDown(ControlName.ToggleSnapping))
        {
            _snappingToggle.value = !_snappingToggle.value;
        }

        if (InputManager.Singleton.GetKeyDown(ControlName.DefaultMode))
        {
            SettingsManager.ApplicationSettings.Mode = ScriptingMode.Default;
            OnScriptingModeChanged(SettingsManager.ApplicationSettings.Mode);
        }
        
        if (InputManager.Singleton.GetKeyDown(ControlName.PatternMode))
        {
            SettingsManager.ApplicationSettings.Mode = ScriptingMode.Pattern;
            OnScriptingModeChanged(SettingsManager.ApplicationSettings.Mode);
        }
        
        if (InputManager.Singleton.GetKeyDown(ControlName.FreeMode))
        {
            SettingsManager.ApplicationSettings.Mode = ScriptingMode.Free;
            OnScriptingModeChanged(SettingsManager.ApplicationSettings.Mode);
        }
        
        if (InputManager.Singleton.GetKeyDown(ControlName.CycleMode))
        {
            switch (SettingsManager.ApplicationSettings.Mode)
            {
                case ScriptingMode.Default:
                    SettingsManager.ApplicationSettings.Mode = ScriptingMode.Pattern;
                    break;
                case ScriptingMode.Pattern:
                    SettingsManager.ApplicationSettings.Mode = ScriptingMode.Free;
                    break;
                case ScriptingMode.Free:
                    SettingsManager.ApplicationSettings.Mode = ScriptingMode.Default;
                    break;
                default:
                    SettingsManager.ApplicationSettings.Mode = ScriptingMode.Pattern;
                    break;
            }

            OnScriptingModeChanged(SettingsManager.ApplicationSettings.Mode);
        }

        if (InputManager.Singleton.GetKeyDown(ControlName.ChangeModeOrPattern))
        {
            switch (SettingsManager.ApplicationSettings.Mode)
            {
                case ScriptingMode.Default:
                    _stepToggle.value = !_stepToggle.value;
                    break;
                case ScriptingMode.Pattern:
                    OnNextPattern();
                    break;
            }
        }
    }

    private void Generate(VisualElement root)
    {
        _toolBar = root.Query("tool-bar");

        // Shared tools
        _modeChangeButton = Create<Button>();
        _modeChangeButton.text = "Default";
        _modeChangeButton.AddToClassList("default-mode");
        _modeChangeButton.clicked += OnCycleMode;
        _toolBar.Add(_modeChangeButton);
        
        _snappingToggleContainer = CreateItem("Snapping:", out _snappingToggle);
        _snappingToggle.RegisterValueChangedCallback(OnSnappingChanged);
        _toolBar.Add(_snappingToggleContainer);

        // Default tools
        _stepMode = CreateItem("Step-mode:", out _stepToggle);
        _stepToggle.RegisterValueChangedCallback(OnStepModeChanged);
        _toolBar.Add(_stepMode);

        // Pattern tools
        _nextPatternButton = Create<Button>();
        _nextPatternButton.text = "Next pattern";
        _nextPatternButton.clicked += OnNextPattern;

        _repeatContainer = CreateItem("Repeat:", out _repeatField);
        _repeatField.RegisterValueChangedCallback(OnRepeatChanged);
        _repeatField.name = "repeat-field";
        PatternManager.Singleton.RepeatAmount = PatternManager.Singleton.RepeatAmountDefault;
        _repeatField.value = PatternManager.Singleton.RepeatAmountDefault;

        _spacingContainer = CreateItem("Spacing:", out _spacingField, PatternManager.Singleton.SpacingMin, PatternManager.Singleton.SpacingMax);
        _spacingField.RegisterValueChangedCallback(OnSpacingChanged);
        PatternManager.Singleton.Spacing = PatternManager.Singleton.SpacingDefault;
        _spacingField.value = PatternManager.Singleton.SpacingDefault;

        _scaleXContainer = CreateItem("X:", out _scaleXField, PatternManager.Singleton.ScaleXMin, PatternManager.Singleton.ScaleXMax);
        _scaleXField.RegisterValueChangedCallback(OnScaleXChanged);
        PatternManager.Singleton.ScaleX = PatternManager.Singleton.ScaleXDefault;
        _scaleXField.value = PatternManager.Singleton.ScaleXDefault;

        _invertXContainer = CreateItem("Invert:", out _invertXToggle);
        _invertXToggle.RegisterValueChangedCallback(OnInvertXChanged);
        PatternManager.Singleton.InvertX = false;
        _invertXToggle.value = false;

        _scaleYContainer = CreateItem("Y:", out _scaleYField, PatternManager.Singleton.ScaleYMin, PatternManager.Singleton.ScaleYMax);
        _scaleYField.RegisterValueChangedCallback(OnScaleYChanged);
        PatternManager.Singleton.ScaleY = PatternManager.Singleton.ScaleYDefault;
        _scaleYField.value = PatternManager.Singleton.ScaleYDefault;

        _invertYContainer = CreateItem("Invert:", out _invertYToggle);
        _invertYToggle.RegisterValueChangedCallback(OnInvertYChanged);
        PatternManager.Singleton.InvertY = false;
        _invertYToggle.value = false;

        _isInitialized = true;
    }

    private void OnCycleMode()
    {
        switch (SettingsManager.ApplicationSettings.Mode)
        {
            case ScriptingMode.Default:
                SettingsManager.ApplicationSettings.Mode = ScriptingMode.Pattern;
                break;
            case ScriptingMode.Pattern:
                SettingsManager.ApplicationSettings.Mode = ScriptingMode.Free;
                break;
            case ScriptingMode.Free:
                SettingsManager.ApplicationSettings.Mode = ScriptingMode.Default;
                break;
            default:
                SettingsManager.ApplicationSettings.Mode = ScriptingMode.Pattern;
                break;
        }

        // _patternToggle.value = !_patternToggle.value;
        OnScriptingModeChanged(SettingsManager.ApplicationSettings.Mode);
    }

    private void OnScriptingModeChanged(ScriptingMode mode)
    {
        // Change toolbar based on mode
        switch (mode)
        {
            // Default mode
            case ScriptingMode.Default:
                _modeChangeButton.text = "Default";
                _modeChangeButton.RemoveFromClassList("pattern-mode");
                _modeChangeButton.RemoveFromClassList("free-mode");
                _modeChangeButton.AddToClassList("default-mode");
                if (_toolBar.Contains(_nextPatternButton)) _toolBar.Remove(_nextPatternButton);
                if (_toolBar.Contains(_repeatContainer)) _toolBar.Remove(_repeatContainer);
                if (_toolBar.Contains(_spacingContainer)) _toolBar.Remove(_spacingContainer);
                if (_toolBar.Contains(_scaleXContainer)) _toolBar.Remove(_scaleXContainer);
                if (_toolBar.Contains(_invertXContainer)) _toolBar.Remove(_invertXContainer);
                if (_toolBar.Contains(_scaleYContainer)) _toolBar.Remove(_scaleYContainer);
                if (_toolBar.Contains(_invertYContainer)) _toolBar.Remove(_invertYContainer);

                _toolBar.Add(_stepMode);
                break;
            // Pattern mode
            case ScriptingMode.Pattern:
                _modeChangeButton.text = "Pattern";
                _modeChangeButton.RemoveFromClassList("free-mode");
                _modeChangeButton.RemoveFromClassList("default-mode");
                _modeChangeButton.AddToClassList("pattern-mode");
                if (_toolBar.Contains(_stepMode)) _toolBar.Remove(_stepMode);
                
                _toolBar.Add(_nextPatternButton);
                _toolBar.Add(_repeatContainer);
                _toolBar.Add(_spacingContainer);
                _toolBar.Add(_scaleXContainer);
                _toolBar.Add(_invertXContainer);
                _toolBar.Add(_scaleYContainer);
                _toolBar.Add(_invertYContainer);
                break;
            case ScriptingMode.Free:
                _modeChangeButton.text = "Freeform";
                _modeChangeButton.RemoveFromClassList("default-mode");
                _modeChangeButton.RemoveFromClassList("pattern-mode");
                _modeChangeButton.AddToClassList("free-mode");
                if (_toolBar.Contains(_stepMode)) _toolBar.Remove(_stepMode);
                if (_toolBar.Contains(_nextPatternButton)) _toolBar.Remove(_nextPatternButton);
                if (_toolBar.Contains(_repeatContainer)) _toolBar.Remove(_repeatContainer);
                if (_toolBar.Contains(_spacingContainer)) _toolBar.Remove(_spacingContainer);
                if (_toolBar.Contains(_scaleXContainer)) _toolBar.Remove(_scaleXContainer);
                if (_toolBar.Contains(_invertXContainer)) _toolBar.Remove(_invertXContainer);
                if (_toolBar.Contains(_scaleYContainer)) _toolBar.Remove(_scaleYContainer);
                if (_toolBar.Contains(_invertYContainer)) _toolBar.Remove(_invertYContainer);
                break;
        }
    }

    private void OnSnappingChanged(ChangeEvent<bool> evt)
    {
        FunscriptMouseInput.Singleton.Snapping = evt.newValue;
    }

    private void OnStepModeChanged(ChangeEvent<bool> evt)
    {
        FunscriptMouseInput.Singleton.StepMode = evt.newValue;
    }

    private void OnNextPattern()
    {
        PatternManager.Singleton.NextPattern();
    }

    private void OnRepeatChanged(ChangeEvent<int> evt)
    {
        SetRepeat(evt.newValue);
    }

    public void SetRepeat(int value)
    {
        // validate value
        value = math.clamp(value, PatternManager.Singleton.RepeatAmountMin, PatternManager.Singleton.RepeatAmountMax);
        _repeatField.SetValueWithoutNotify(value);

        // set value
        PatternManager.Singleton.RepeatAmount = value;
    }

    private void OnSpacingChanged(ChangeEvent<float> evt)
    {
        SetSpacing(evt.newValue);
    }

    public void SetSpacing(float value)
    {
        // validate value
        int intValue = (int)math.clamp(value, PatternManager.Singleton.SpacingMin, PatternManager.Singleton.SpacingMax);
        _spacingField.SetValueWithoutNotify(intValue);

        // set value
        PatternManager.Singleton.Spacing = intValue;
    }


    private void OnScaleXChanged(ChangeEvent<float> evt)
    {
        SetScaleX(evt.newValue);
    }

    public void SetScaleX(float value)
    {
        // validate value
        value = math.clamp(value, PatternManager.Singleton.ScaleXMin, PatternManager.Singleton.ScaleXMax);
        value = (float)Math.Round(value, 2);
        _scaleXField.SetValueWithoutNotify(value);

        // set value
        PatternManager.Singleton.ScaleX = value;
    }

    private void OnInvertXChanged(ChangeEvent<bool> evt)
    {
        PatternManager.Singleton.InvertX = evt.newValue;
    }

    private void OnScaleYChanged(ChangeEvent<float> evt)
    {
        SetScaleY(evt.newValue);
    }

    public void SetScaleY(float value)
    {
        // validate value
        value = math.clamp(value, PatternManager.Singleton.ScaleYMin, PatternManager.Singleton.ScaleYMax);
        value = (float)System.Math.Round(value, 2);
        _scaleYField.SetValueWithoutNotify(value);

        // set value
        PatternManager.Singleton.ScaleY = value;
    }

    private void OnInvertYChanged(ChangeEvent<bool> evt)
    {
        PatternManager.Singleton.InvertY = evt.newValue;
    }


    private VisualElement CreateItem(string text, out Toggle toggle)
    {
        var container = Create("item", "row");

        var label = Create<Label>();
        label.text = text;
        container.Add(label);

        toggle = Create<Toggle>();
        container.Add(toggle);

        return container;
    }

    private VisualElement CreateItem(string text, out IntegerField integerField)
    {
        var container = Create("item", "row");

        var label = Create<Label>();
        label.text = text;
        container.Add(label);

        integerField = Create<IntegerField>();
        container.Add(integerField);

        return container;
    }

    private VisualElement CreateItem(string text, out Slider sliderField, float min, float max)
    {
        var container = Create("item", "row");

        var label = Create<Label>();
        label.text = text;
        container.Add(label);

        sliderField = Create<Slider>();
        sliderField.lowValue = min;
        sliderField.highValue = max;
        container.Add(sliderField);

        return container;
    }

    private VisualElement CreateItem(string text, out FloatField floatField)
    {
        var container = Create("item", "row");

        var label = Create<Label>();
        label.text = text;
        container.Add(label);

        floatField = Create<FloatField>();
        container.Add(floatField);

        return container;
    }
}