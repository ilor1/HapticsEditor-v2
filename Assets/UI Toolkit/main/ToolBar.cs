using UnityEngine;
using UnityEngine.UIElements;

public class ToolBar : UIBehaviour
{
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
        var toolBar = Create("tool-bar");
        root.Add(toolBar);

        var snappingToggle = Create<Toggle>("tool-toggle");
        snappingToggle.text = "Snapping";
        toolBar.Add(snappingToggle);
    }
}