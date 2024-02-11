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
        VisualElement toolBar = root.Query(className: "tool-bar");

        var snappingToggle = Create<Toggle>("tool-toggle");
        snappingToggle.text = "Snapping";
        toolBar.Add(snappingToggle);
    }
}