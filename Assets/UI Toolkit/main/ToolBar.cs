using UnityEngine;
using UnityEngine.UIElements;

public class ToolBar : UIBehaviour
{
    private bool _isInitialized = false;

    private Toggle _snappingToggle;
    private Toggle _patternToggle;

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

        // Hotkeys change the visuals which triggers the events
        if (Input.GetKeyDown(InputManager.Singleton.Controls.ToggleSnapping))
        {
            _snappingToggle.value = !_snappingToggle.value;
        }

        if (Input.GetKeyDown(InputManager.Singleton.Controls.TogglePatternMode))
        {
            _patternToggle.value = !_patternToggle.value;
        }
    }

    private void Generate(VisualElement root)
    {
        VisualElement toolBar = root.Query(className: "tool-bar");

        _snappingToggle = Create<Toggle>("tool-toggle");
        _snappingToggle.text = "Snapping";
        _snappingToggle.RegisterValueChangedCallback(OnSnappingChanged);
        toolBar.Add(_snappingToggle);

        _patternToggle = Create<Toggle>("tool-toggle");
        _patternToggle.text = "Pattern";
        _patternToggle.RegisterValueChangedCallback(OnPatternModeChanged);
        toolBar.Add(_patternToggle);

        _isInitialized = true;
    }

    private void OnSnappingChanged(ChangeEvent<bool> evt)
    {
        FunscriptMouseInput.Singleton.Snapping = evt.newValue;
    }

    private void OnPatternModeChanged(ChangeEvent<bool> evt)
    {
        PatternManager.Singleton.PatternMode = evt.newValue;
    }
}