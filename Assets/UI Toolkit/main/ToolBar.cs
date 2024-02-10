using UnityEngine;
using UnityEngine.UIElements;

public class ToolBar : UIBehaviour
{
    [SerializeField]
    private MainUI _mainUI;

    private void OnEnable()
    {
        _mainUI.RootCreated += Generate;
    }

    private void OnDisable()
    {
        _mainUI.RootCreated -= Generate;
    }

    private void Generate(VisualElement root)
    {
        var toolBar = Create("tool-bar");
        root.Add(toolBar);

        var snappingToggle = Create<Toggle>("tool-toggle");
        snappingToggle.text = "Snapping";
        toolBar.Add(snappingToggle);
    }
}