﻿using System;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UIElements;

public class ToolBar : UIBehaviour
{
    public static ToolBar Singleton;

    private bool _isInitialized = false;

    private VisualElement _toolBar;

    // Shared tools
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

        if (InputManager.Singleton.GetKeyDown(ControlName.TogglePatternMode))
        {
            _patternToggle.value = !_patternToggle.value;
        }

        if (InputManager.Singleton.GetKeyDown(ControlName.ChangeModeOrPattern))
        {
            if (_patternToggle.value)
            {
                OnNextPattern();
            }
            else
            {
                _stepToggle.value = !_stepToggle.value;
            }
        }
    }

    private void Generate(VisualElement root)
    {
        _toolBar = root.Query(className: "tool-bar");

        // Shared tools
        _patternToggleContainer = CreateItem("Pattern-mode:", out _patternToggle);
        _patternToggle.RegisterValueChangedCallback(OnPatternModeChanged);
        _toolBar.Add(_patternToggleContainer);

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

    private void OnPatternModeChanged(ChangeEvent<bool> evt)
    {
        PatternManager.Singleton.PatternMode = evt.newValue;

        // Change toolbar based on mode
        switch (evt.newValue)
        {
            // Default mode
            case false:
                _toolBar.Remove(_nextPatternButton);
                _toolBar.Remove(_repeatContainer);
                _toolBar.Remove(_spacingContainer);
                _toolBar.Remove(_scaleXContainer);
                _toolBar.Remove(_invertXContainer);
                _toolBar.Remove(_scaleYContainer);
                _toolBar.Remove(_invertYContainer);

                _toolBar.Add(_stepMode);
                break;
            // Pattern mode
            case true:
                _toolBar.Remove(_stepMode);

                _toolBar.Add(_nextPatternButton);
                _toolBar.Add(_repeatContainer);
                _toolBar.Add(_spacingContainer);
                _toolBar.Add(_scaleXContainer);
                _toolBar.Add(_invertXContainer);
                _toolBar.Add(_scaleYContainer);
                _toolBar.Add(_invertYContainer);
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
        var container = Create("tool-bar-item");

        var label = Create<Label>();
        label.text = text;
        container.Add(label);

        toggle = Create<Toggle>();
        container.Add(toggle);

        return container;
    }

    private VisualElement CreateItem(string text, out IntegerField integerField)
    {
        var container = Create("tool-bar-item");

        var label = Create<Label>();
        label.text = text;
        container.Add(label);

        integerField = Create<IntegerField>();
        container.Add(integerField);

        return container;
    }

    private VisualElement CreateItem(string text, out Slider sliderField, float min, float max)
    {
        var container = Create("tool-bar-item");

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
        var container = Create("tool-bar-item");

        var label = Create<Label>();
        label.text = text;
        container.Add(label);

        floatField = Create<FloatField>();
        container.Add(floatField);

        return container;
    }
}